using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotNetty.Common;
using EventStore.Core.Exceptions;
using EventStore.Core.TransactionLog;
using EventStore.Core.Util;
using EventStore.Core.Index.Hashes;
using EventStore.Core.TransactionLog.Checkpoint;
using EventStore.Core.TransactionLog.Chunks;
using Microsoft.Extensions.Logging;

namespace EventStore.Core.Index
{
    public class TableIndex : ITableIndex
    {
        public const string IndexMapFilename = "indexmap";
        private const int MaxMemoryTables = 1;

        private static readonly ILogger Log = TraceLogger.GetLogger<TableIndex>();
        internal static readonly IndexEntry InvalidIndexEntry = new IndexEntry(0, -1, -1);

        public long CommitCheckpoint { get { return Interlocked.Read(ref _commitCheckpoint); } }
        public long PrepareCheckpoint { get { return Interlocked.Read(ref _prepareCheckpoint); } }

        private readonly int _maxSizeForMemory;
        private readonly int _maxTablesPerLevel;
        private readonly bool _additionalReclaim;
        private readonly bool _inMem;
        private readonly bool _skipIndexVerify;
        private readonly int _indexCacheDepth;
        private readonly int _initializationThreads;
        private readonly byte _ptableVersion;
        private readonly string _directory;
        private readonly Func<IMemTable> _memTableFactory;
        private readonly Func<TFReaderLease> _tfReaderFactory;
        private readonly IIndexFilenameProvider _fileNameProvider;

        private readonly object _awaitingTablesLock = new object();

        private IndexMap _indexMap;
        private List<TableItem> _awaitingMemTables;
        private long _commitCheckpoint = -1;
        private long _prepareCheckpoint = -1;

        private volatile bool _backgroundRunning;
        private readonly ManualResetEventSlim _backgroundRunningEvent = new ManualResetEventSlim(true);

        private IHasher _lowHasher;
        private IHasher _highHasher;

        private bool _initialized;
        public const string ForceIndexVerifyFilename = ".forceverify";
        private readonly int _maxAutoMergeIndexLevel;

        public TableIndex(string directory,
            IHasher lowHasher,
            IHasher highHasher,
            Func<IMemTable> memTableFactory,
            Func<TFReaderLease> tfReaderFactory,
            byte ptableVersion,
            int maxAutoMergeIndexLevel,
            int maxSizeForMemory = 1000000,
            int maxTablesPerLevel = 4,
            bool additionalReclaim = false,
            bool inMem = false,
            bool skipIndexVerify = false,
            int indexCacheDepth = 16,
            int initializationThreads = 1)
        {
            if (string.IsNullOrEmpty(directory)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.directory); }
            if (memTableFactory is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.memTableFactory); }
            if (lowHasher is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.lowHasher); }
            if (highHasher is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.highHasher); }
            if (tfReaderFactory is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.tfReaderFactory); }
            if ((uint)(initializationThreads - 1) >= Consts.TooBigOrNegative) { ThrowHelper.ThrowArgumentOutOfRangeException_Positive(ExceptionArgument.initializationThreads); }
            if (maxTablesPerLevel <= 1) { ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.maxTablesPerLevel); }
            if (indexCacheDepth > 28 || indexCacheDepth < 8) { ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.indexCacheDepth); }

            _upgradeHash = UpgradeHash;

            _directory = directory;
            _memTableFactory = memTableFactory;
            _tfReaderFactory = tfReaderFactory;
            _fileNameProvider = new GuidFilenameProvider(directory);
            _maxSizeForMemory = maxSizeForMemory;
            _maxTablesPerLevel = maxTablesPerLevel;
            _additionalReclaim = additionalReclaim;
            _inMem = inMem;
            _skipIndexVerify = ShouldForceIndexVerify() ? false : skipIndexVerify;
            _indexCacheDepth = indexCacheDepth;
            _initializationThreads = initializationThreads;
            _ptableVersion = ptableVersion;
            _awaitingMemTables = new List<TableItem> { new TableItem(_memTableFactory(), -1, -1, 0) };

            _lowHasher = lowHasher;
            _highHasher = highHasher;

            _maxAutoMergeIndexLevel = maxAutoMergeIndexLevel;
        }

        public void Initialize(long chaserCheckpoint)
        {
            if ((ulong)chaserCheckpoint > Consts.TooBigOrNegativeUL) { ThrowHelper.ThrowArgumentOutOfRangeException_Nonnegative(ExceptionArgument.chaserCheckpoint); }

            //NOT THREAD SAFE (assumes one thread)
            if (_initialized) { ThrowHelper.ThrowIOException_TableIndexIsAlreadyInitialized(); }
            _initialized = true;

            if (_inMem)
            {
                _indexMap = IndexMap.CreateEmpty(_maxTablesPerLevel, int.MaxValue);
                _prepareCheckpoint = _indexMap.PrepareCheckpoint;
                _commitCheckpoint = _indexMap.CommitCheckpoint;
                return;
            }

#if DEBUG
            if (ShouldForceIndexVerify() && Log.IsDebugLevelEnabled())
            {
                Log.Forcing_verification_of_index_files();
            }
#endif

            CreateIfDoesNotExist(_directory);
            var indexmapFile = Path.Combine(_directory, IndexMapFilename);

            // if TableIndex's CommitCheckpoint is >= amount of written TFChunk data,
            // we'll have to remove some of PTables as they point to non-existent data
            // this can happen (very unlikely, though) on master crash
            try
            {
                _indexMap = IndexMap.FromFile(indexmapFile, _maxTablesPerLevel, true, _indexCacheDepth, _skipIndexVerify, _initializationThreads, _maxAutoMergeIndexLevel);
                if (_indexMap.CommitCheckpoint >= chaserCheckpoint)
                {
                    _indexMap.Dispose(TimeSpan.FromMilliseconds(5000));
                    ThrowHelper.ThrowCorruptIndexException_CommitCheckpointIsGreaterThanChaserCheckpoint(_indexMap.CommitCheckpoint, chaserCheckpoint);
                }

                //verification should be completed by now
                DeleteForceIndexVerifyFile();
            }
            catch (CorruptIndexException exc)
            {
                Log.ReadindexIsCorrupted(exc);
                LogIndexMapContent(indexmapFile);
                DumpAndCopyIndex();
                File.SetAttributes(indexmapFile, FileAttributes.Normal);
                File.Delete(indexmapFile);
                DeleteForceIndexVerifyFile();
                _indexMap = IndexMap.FromFile(indexmapFile, _maxTablesPerLevel, true, _indexCacheDepth, _skipIndexVerify, _initializationThreads, _maxAutoMergeIndexLevel);
            }
            _prepareCheckpoint = _indexMap.PrepareCheckpoint;
            _commitCheckpoint = _indexMap.CommitCheckpoint;

            // clean up all other remaining files
            var indexFiles = _indexMap.InOrder().Select(x => Path.GetFileName(x.Filename))
                                                .Union(new[] { IndexMapFilename });
            var toDeleteFiles = Directory.EnumerateFiles(_directory).Select(Path.GetFileName)
                                         .Except(indexFiles, StringComparer.OrdinalIgnoreCase);
            foreach (var filePath in toDeleteFiles)
            {
                var file = Path.Combine(_directory, filePath);
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }
        }

        private static void LogIndexMapContent(string indexmapFile)
        {
            try
            {
                Log.IndexMapAndContent(indexmapFile);
            }
            catch (Exception exc)
            {
                Log.UnexpectedErrorWhileDumpingIndexmap(indexmapFile, exc);
            }
        }

        private void DumpAndCopyIndex()
        {
            string dumpPath = null;
            try
            {
                dumpPath = Path.Combine(Path.GetDirectoryName(_directory),
                                        $"index-backup-{DateTime.UtcNow:yyyy-MM-dd_HH-mm-ss.fff}");
                Log.MakingBackupOfIndexFolderForInspectionTo(dumpPath);
                FileUtils.DirectoryCopy(_directory, dumpPath, copySubDirs: true);
            }
            catch (Exception exc)
            {
                Log.UnexpectedErrorWhileCopyingIndexToBackupDir(dumpPath, exc);
            }
        }

        private static void CreateIfDoesNotExist(string directory)
        {
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        public void Add(long commitPos, string streamId, long version, long position)
        {
            if ((ulong)commitPos > Consts.TooBigOrNegativeUL) { ThrowHelper.ThrowArgumentOutOfRangeException_Nonnegative(ExceptionArgument.commitPos); }
            if ((ulong)version > Consts.TooBigOrNegativeUL) { ThrowHelper.ThrowArgumentOutOfRangeException_Nonnegative(ExceptionArgument.version); }
            if ((ulong)position > Consts.TooBigOrNegativeUL) { ThrowHelper.ThrowArgumentOutOfRangeException_Nonnegative(ExceptionArgument.position); }

            AddEntries(commitPos, new[] { CreateIndexKey(streamId, version, position) });
        }

        public void AddEntries(long commitPos, IList<IndexKey> entries)
        {
            //should only be called on a single thread.
            var table = (IMemTable)_awaitingMemTables[0].Table; // always a memtable

            var collection = entries.Select(x => CreateIndexEntry(x)).ToList();
            table.AddEntries(collection);

            if ((uint)table.Count >= (uint)_maxSizeForMemory)
            {
                long prepareCheckpoint = collection[0].Position;
                for (int i = 1, n = collection.Count; i < n; ++i)
                {
                    prepareCheckpoint = Math.Max(prepareCheckpoint, collection[i].Position);
                }

                TryProcessAwaitingTables(commitPos, prepareCheckpoint);
            }
        }

        public Task MergeIndexes()
        {
            TryManualMerge();
            return Task.CompletedTask;
        }

        public bool IsBackgroundTaskRunning
        {
            get { return _backgroundRunning; }
        }

        //Automerge only
        private void TryProcessAwaitingTables(long commitPos, long prepareCheckpoint)
        {
            lock (_awaitingTablesLock)
            {
                var newTables = new List<TableItem> { new TableItem(_memTableFactory(), -1, -1, 0) };
                newTables.AddRange(_awaitingMemTables.Select(
                    (x, i) => 0u >= (uint)i ? new TableItem(x.Table, prepareCheckpoint, commitPos, x.Level) : x));

#if DEBUG
                if (Log.IsTraceLevelEnabled()) Log.SwitchingMemTableCurrentlyAwaitingTables(newTables.Count);
#endif

                _awaitingMemTables = newTables;
                if (_inMem) return;
                TryProcessAwaitingTables();

                if (_additionalReclaim)
                    ThreadPoolScheduler.Schedule(s => ReclaimMemoryIfNeeded(s), _awaitingMemTables);
            }
        }

        public void TryManualMerge()
        {
            lock (_awaitingTablesLock)
            {

                var (maxLevel, highest) = _indexMap.GetTableForManualMerge();
                if (highest is null) return; //no work to do

                //These values are actually ignored later as manual merge will never change the checkpoint as no
                //new entries are added, but they can be helpful to see when the manual merge was called
                //because of the way the "queue" currently works (LIFO) it should always be the same
                var prepare = _indexMap.PrepareCheckpoint;
                var commit = _indexMap.CommitCheckpoint;
                var newTables = new List<TableItem>(_awaitingMemTables)
                {
                    new TableItem(highest, prepare, commit, maxLevel)
                };
                _awaitingMemTables = newTables;
                TryProcessAwaitingTables();
            }
        }

        private void TryProcessAwaitingTables()
        {
            lock (_awaitingTablesLock)
            {
                if (!_backgroundRunning)
                {
                    _backgroundRunningEvent.Reset();
                    _backgroundRunning = true;
                    ThreadPoolScheduler.Schedule(x => ReadOffQueue(), (object)null);
                }
            }
        }

        private void ReadOffQueue()
        {
            try
            {
                while (true)
                {
                    TableItem tableItem;
                    //ISearchTable table;
                    lock (_awaitingTablesLock)
                    {
#if DEBUG
                        if (Log.IsTraceLevelEnabled()) Log.AwaitingTablesQueueSizeIs(_awaitingMemTables.Count);
#endif
                        if ((uint)_awaitingMemTables.Count == 1u)
                        {
                            return;
                        }

                        tableItem = _awaitingMemTables[_awaitingMemTables.Count - 1];
                    }

                    PTable ptable;
                    var memtable = tableItem.Table as IMemTable;
                    if (memtable is object)
                    {
                        memtable.MarkForConversion();
                        ptable = PTable.FromMemtable(memtable, _fileNameProvider.GetFilenameNewTable(),
                            _indexCacheDepth, _skipIndexVerify);
                    }
                    else
                        ptable = (PTable)tableItem.Table;

                    var indexmapFile = Path.Combine(_directory, IndexMapFilename);
                    MergeResult mergeResult;
                    using (var reader = _tfReaderFactory())
                    {
                        mergeResult = _indexMap.AddPTable(ptable, tableItem.PrepareCheckpoint,
                            tableItem.CommitCheckpoint,
                            _upgradeHash,
                            entry => reader.ExistsAt(entry.Position),
                            entry => ReadEntry(reader, entry.Position),
                            _fileNameProvider,
                            _ptableVersion,
                            tableItem.Level,
                            _indexCacheDepth,
                            _skipIndexVerify);
                    }

                    _indexMap = mergeResult.MergedMap;
                    _indexMap.SaveToFile(indexmapFile);

                    lock (_awaitingTablesLock)
                    {
                        var memTables = _awaitingMemTables.ToList();

                        var corrTable = memTables.First(x => x.Table.Id == ptable.Id);
                        memTables.Remove(corrTable);

                        // parallel thread could already switch table,
                        // so if we have another PTable instance with same ID,
                        // we need to kill that instance as we added ours already
                        if (!ReferenceEquals(corrTable.Table, ptable) && corrTable.Table is PTable pTable)
                            pTable.MarkForDestruction();

#if DEBUG
                        if (Log.IsTraceLevelEnabled()) Log.ThereAreNowAwaitingTables(memTables.Count);
#endif
                        _awaitingMemTables = memTables;
                    }

                    mergeResult.ToDelete.ForEach(x => x.MarkForDestruction());
                }
            }
            catch (FileBeingDeletedException exc)
            {
                Log.CouldNotAcquireChunkInTableIndexReadOffQueue(exc);
            }
            catch (Exception exc)
            {
                Log.ErrorInTableIndexReadOffQueue(exc);
                throw;
            }
            finally
            {
                lock (_awaitingTablesLock)
                {
                    _backgroundRunning = false;
                    _backgroundRunningEvent.Set();
                }
            }
        }

        internal void WaitForBackgroundTasks()
        {
            if (!_backgroundRunningEvent.Wait(7000))
            {
                ThrowHelper.ThrowTimeoutException(ExceptionResource.Waiting_for_background_tasks_took_too_long);
            }
        }

        public void Scavenge(IIndexScavengerLog log, CancellationToken ct)
        {
            GetExclusiveBackgroundTask(ct);
            var sw = Stopwatch.StartNew();

            try
            {
                if (Log.IsInformationLevelEnabled()) Log.Starting_scavenge_of_TableIndex();
                ScavengeInternal(log, ct);
            }
            finally
            {
                // Since scavenging indexes is the only place the ExistsAt optimization makes sense (and takes up a lot of memory), we can clear it after an index scavenge has completed. 
                TFChunkReaderExistsAtOptimizer.Instance.DeOptimizeAll();

                lock (_awaitingTablesLock)
                {
                    _backgroundRunning = false;
                    _backgroundRunningEvent.Set();

                    TryProcessAwaitingTables();
                }

                if (Log.IsInformationLevelEnabled()) Log.Completed_scavenge_of_TableIndex(sw.Elapsed);
            }
        }

        private void ScavengeInternal(IIndexScavengerLog log, CancellationToken ct)
        {
            var toScavenge = _indexMap.InOrder().ToList();

            foreach (var pTable in toScavenge)
            {
                var startNew = Stopwatch.StartNew();

                try
                {
                    ct.ThrowIfCancellationRequested();

                    using (var reader = _tfReaderFactory())
                    {
                        var indexmapFile = Path.Combine(_directory, IndexMapFilename);

                        var scavengeResult = _indexMap.Scavenge(pTable.Id, ct,
                            (streamId, currentHash) => UpgradeHash(streamId, currentHash),
                            entry => reader.ExistsAt(entry.Position),
                            entry => ReadEntry(reader, entry.Position), _fileNameProvider, _ptableVersion,
                            _indexCacheDepth, _skipIndexVerify);

                        if (scavengeResult.IsSuccess)
                        {
                            _indexMap = scavengeResult.ScavengedMap;
                            _indexMap.SaveToFile(indexmapFile);

                            scavengeResult.OldTable.MarkForDestruction();

                            var entriesDeleted = scavengeResult.OldTable.Count - scavengeResult.NewTable.Count;
                            log.IndexTableScavenged(scavengeResult.Level, scavengeResult.Index, startNew.Elapsed, entriesDeleted, scavengeResult.NewTable.Count, scavengeResult.SpaceSaved);
                        }
                        else
                        {
                            log.IndexTableNotScavenged(scavengeResult.Level, scavengeResult.Index, startNew.Elapsed, pTable.Count, "");
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    log.IndexTableNotScavenged(-1, -1, startNew.Elapsed, pTable.Count, "Scavenge cancelled");
                    throw;
                }
                catch (Exception ex)
                {
                    log.IndexTableNotScavenged(-1, -1, startNew.Elapsed, pTable.Count, ex.Message);
                    throw;
                }
            }
        }

        private void GetExclusiveBackgroundTask(CancellationToken ct)
        {
            var infoEnabled = Log.IsInformationLevelEnabled();
            while (true)
            {
                lock (_awaitingTablesLock)
                {
                    if (!_backgroundRunning)
                    {
                        _backgroundRunningEvent.Reset();
                        _backgroundRunning = true;
                        return;
                    }
                }

                if (infoEnabled) Log.WaitingForTableindexBackgroundTaskToCompleteBeforeStartingScavenge();
                _backgroundRunningEvent.Wait(ct);
            }
        }

        private static Tuple<string, bool> ReadEntry(in TFReaderLease reader, long position)
        {
            RecordReadResult result = reader.TryReadAt(position);
            if (!result.Success)
            {
                return new Tuple<string, bool>(String.Empty, false);
            }

            if (result.LogRecord.RecordType != TransactionLog.LogRecords.LogRecordType.Prepare)
            {
                ThrowHelper.ThrowException_IncorrectTypeOfLogRecord(result.LogRecord.RecordType);
            }

            return new Tuple<string, bool>(((TransactionLog.LogRecords.PrepareLogRecord)result.LogRecord).EventStreamId, true);
        }

        private void ReclaimMemoryIfNeeded(List<TableItem> awaitingMemTables)
        {
            var toPutOnDisk = awaitingMemTables.OfType<IMemTable>().Count() - MaxMemoryTables;
#if DEBUG
            var traceEnabled = Log.IsTraceLevelEnabled();
#endif
            for (var i = awaitingMemTables.Count - 1; i >= 1 && toPutOnDisk > 0; i--)
            {
                var memtable = awaitingMemTables[i].Table as IMemTable;
                if (memtable is null || !memtable.MarkForConversion()) { continue; }

#if DEBUG
                if (traceEnabled) Log.PuttingAwaitingFileAsPTableInsteadOfMemTable(memtable.Id);
#endif

                var ptable = PTable.FromMemtable(memtable, _fileNameProvider.GetFilenameNewTable(), _indexCacheDepth, _skipIndexVerify);
                var swapped = false;
                lock (_awaitingTablesLock)
                {
                    for (var j = _awaitingMemTables.Count - 1; j >= 1; j--)
                    {
                        var tableItem = _awaitingMemTables[j];
                        if (!(tableItem.Table is IMemTable) || tableItem.Table.Id != ptable.Id) { continue; }
                        swapped = true;
                        _awaitingMemTables[j] = new TableItem(ptable, tableItem.PrepareCheckpoint, tableItem.CommitCheckpoint, tableItem.Level);
                        break;
                    }
                }
                if (!swapped) { ptable.MarkForDestruction(); }
                toPutOnDisk--;
            }
        }

        public bool TryGetOneValue(string streamId, long version, out long position)
        {
            ulong stream = CreateHash(streamId);
            int counter = 0;
            while (counter < 5)
            {
                counter++;
                try
                {
                    return TryGetOneValueInternal(stream, version, out position);
                }
                catch (FileBeingDeletedException)
                {
                    if (Log.IsTraceLevelEnabled()) Log.FileBeingDeleted();
                }
                catch (MaybeCorruptIndexException e)
                {
                    ForceIndexVerifyOnNextStartup();
                    throw e;
                }
            }
            ThrowHelper.ThrowInvalidOperationException_FilesAreLocked(); position = 0; return false;
        }

        private bool TryGetOneValueInternal(ulong stream, long version, out long position)
        {
            if ((ulong)version > Consts.TooBigOrNegativeUL) { ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.version); }

            var awaiting = Volatile.Read(ref _awaitingMemTables);
            foreach (var tableItem in awaiting)
            {
                if (tableItem.Table.TryGetOneValue(stream, version, out position)) { return true; }
            }

            var map = _indexMap;
            foreach (var table in map.InOrder())
            {
                if (table.TryGetOneValue(stream, version, out position)) { return true; }
            }

            position = 0;
            return false;
        }

        public bool TryGetLatestEntry(string streamId, out IndexEntry entry)
        {
            ulong stream = CreateHash(streamId);
            var counter = 0;
            while (counter < 5)
            {
                counter++;
                try
                {
                    return TryGetLatestEntryInternal(stream, out entry);
                }
                catch (FileBeingDeletedException)
                {
                    if (Log.IsTraceLevelEnabled()) Log.FileBeingDeleted();
                }
                catch (MaybeCorruptIndexException e)
                {
                    ForceIndexVerifyOnNextStartup();
                    throw e;
                }
            }
            ThrowHelper.ThrowInvalidOperationException_FilesAreLocked(); entry = default; return false;
        }

        private bool TryGetLatestEntryInternal(ulong stream, out IndexEntry entry)
        {
            var awaiting = Volatile.Read(ref _awaitingMemTables);
            foreach (var t in awaiting)
            {
                if (t.Table.TryGetLatestEntry(stream, out entry)) { return true; }
            }

            var map = _indexMap;
            foreach (var table in map.InOrder())
            {
                if (table.TryGetLatestEntry(stream, out entry)) { return true; }
            }

            entry = InvalidIndexEntry;
            return false;
        }

        public bool TryGetOldestEntry(string streamId, out IndexEntry entry)
        {
            ulong stream = CreateHash(streamId);
            var counter = 0;
            while (counter < 5)
            {
                counter++;
                try
                {
                    return TryGetOldestEntryInternal(stream, out entry);
                }
                catch (FileBeingDeletedException)
                {
                    if (Log.IsTraceLevelEnabled()) Log.FileBeingDeleted();
                }
                catch (MaybeCorruptIndexException e)
                {
                    ForceIndexVerifyOnNextStartup();
                    throw e;
                }
            }
            ThrowHelper.ThrowInvalidOperationException_FilesAreLocked(); entry = default; return false;
        }

        private bool TryGetOldestEntryInternal(ulong stream, out IndexEntry entry)
        {
            var map = _indexMap;
            foreach (var table in map.InReverseOrder())
            {
                if (table.TryGetOldestEntry(stream, out entry)) { return true; }
            }

            var awaiting = Volatile.Read(ref _awaitingMemTables);
            for (var index = awaiting.Count - 1; index >= 0; index--)
            {
                if (awaiting[index].Table.TryGetOldestEntry(stream, out entry)) { return true; }
            }

            entry = InvalidIndexEntry;
            return false;
        }

        public IEnumerable<IndexEntry> GetRange(string streamId, long startVersion, long endVersion, int? limit = null)
        {
            ulong hash = CreateHash(streamId);
            var counter = 0;
            while (counter < 5)
            {
                counter++;
                try
                {
                    return GetRangeInternal(hash, startVersion, endVersion, limit);
                }
                catch (FileBeingDeletedException)
                {
                    if (Log.IsTraceLevelEnabled()) Log.FileBeingDeleted();
                }
                catch (MaybeCorruptIndexException e)
                {
                    ForceIndexVerifyOnNextStartup();
                    throw e;
                }
            }
            ThrowHelper.ThrowInvalidOperationException_FilesAreLocked(); return null;
        }

        private IEnumerable<IndexEntry> GetRangeInternal(ulong hash, long startVersion, long endVersion, int? limit = null)
        {
            if ((ulong)startVersion > Consts.TooBigOrNegativeUL) { ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.startVersion); }
            if ((ulong)endVersion > Consts.TooBigOrNegativeUL) { ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.endVersion); }

            var candidates = ThreadLocalList<IEnumerator<IndexEntry>>.NewInstance();
            try
            {
                var awaiting = Volatile.Read(ref _awaitingMemTables);
                for (int index = 0; index < awaiting.Count; index++)
                {
                    var range = awaiting[index].Table.GetRange(hash, startVersion, endVersion, limit).GetEnumerator();
                    if (range.MoveNext()) { candidates.Add(range); }
                }

                var map = _indexMap;
                foreach (var table in map.InOrder())
                {
                    var range = table.GetRange(hash, startVersion, endVersion, limit).GetEnumerator();
                    if (range.MoveNext()) { candidates.Add(range); }
                }

                var last = new IndexEntry(0, 0, 0);
                var first = true;

                var sortedCandidates = new List<IndexEntry>();
                while ((uint)candidates.Count > 0u)
                {
                    var maxIdx = GetMaxOf(candidates);
                    var winner = candidates[maxIdx];

                    var best = winner.Current;
                    if (first || ((last.Stream != best.Stream) && (last.Version != best.Version)) || last.Position != best.Position)
                    {
                        last = best;
                        sortedCandidates.Add(best);
                        first = false;
                    }

                    if (!winner.MoveNext()) { candidates.RemoveAt(maxIdx); }
                }

                return sortedCandidates;
            }
            finally
            {
                candidates.Return();
            }
        }

        private static int GetMaxOf(List<IEnumerator<IndexEntry>> enumerators)
        {
            var max = new IndexEntry(ulong.MinValue, 0, long.MinValue);
            int idx = 0;
            for (int i = 0; i < enumerators.Count; i++)
            {
                var cur = enumerators[i].Current;
                if (cur.CompareTo(max) > 0)
                {
                    max = cur;
                    idx = i;
                }
            }
            return idx;
        }

        public void Close(bool removeFiles = true)
        {
            if (!_backgroundRunningEvent.Wait(7000))
            {
                ThrowHelper.ThrowTimeoutException(ExceptionResource.Could_not_finish_background_thread_in_reasonable_time);
            }
            if (_inMem) { return; }
            if (_indexMap is null) { return; }
            if (removeFiles)
            {
                _indexMap.InOrder().ToList().ForEach(x => x.MarkForDestruction());
                var fileName = Path.Combine(_directory, IndexMapFilename);
                if (File.Exists(fileName))
                {
                    File.SetAttributes(fileName, FileAttributes.Normal);
                    File.Delete(fileName);
                }
            }
            else
            {
                _indexMap.InOrder().ToList().ForEach(x => x.Dispose());
            }
            _indexMap.InOrder().ToList().ForEach(x => x.WaitForDisposal(TimeSpan.FromMilliseconds(5000)));
        }

        private IndexEntry CreateIndexEntry(in IndexKey key)
        {
            var newkey = CreateIndexKey(key.StreamId, key.Version, key.Position);
            return new IndexEntry(newkey.Hash, newkey.Version, newkey.Position);
        }

        private readonly Func<string, ulong, ulong> _upgradeHash;
        private ulong UpgradeHash(string streamId, ulong lowHash)
        {
            return lowHash << 32 | _highHasher.Hash(streamId);
        }

        private ulong CreateHash(string streamId)
        {
            return (ulong)_lowHasher.Hash(streamId) << 32 | _highHasher.Hash(streamId);
        }

        private IndexKey CreateIndexKey(string streamId, long version, long position)
        {
            return new IndexKey(streamId, version, position, CreateHash(streamId));
        }

        private class TableItem
        {
            public readonly ISearchTable Table;
            public readonly long PrepareCheckpoint;
            public readonly long CommitCheckpoint;
            public readonly int Level;

            public TableItem(ISearchTable table, long prepareCheckpoint, long commitCheckpoint, int level)
            {
                Table = table;
                PrepareCheckpoint = prepareCheckpoint;
                CommitCheckpoint = commitCheckpoint;
                Level = level;
            }
        }

        private void ForceIndexVerifyOnNextStartup()
        {
#if DEBUG
            if (Log.IsDebugLevelEnabled()) Log.Forcing_index_verification_on_next_startup();
#endif
            string path = Path.Combine(_directory, ForceIndexVerifyFilename);
            try
            {
                using (FileStream fs = new FileStream(path, FileMode.OpenOrCreate))
                {
                };
            }
            catch
            {
                Log.CouldNotCreateForceIndexVerificationFileAt(path);
            }

            return;
        }

        private bool ShouldForceIndexVerify()
        {
            string path = Path.Combine(_directory, ForceIndexVerifyFilename);
            return File.Exists(path);
        }

        private void DeleteForceIndexVerifyFile()
        {
            string path = Path.Combine(_directory, ForceIndexVerifyFilename);
            try
            {
                if (File.Exists(path))
                {
                    File.SetAttributes(path, FileAttributes.Normal);
                    File.Delete(path);
                }
            }
            catch
            {
                Log.CouldNotDeleteForceIndexVerificationFileAt(path);
            }
        }
    }
}
