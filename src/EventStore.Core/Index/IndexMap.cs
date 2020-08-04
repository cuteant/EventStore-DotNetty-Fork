using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CuteAnt.IO;
using EventStore.Common.Utils;
using EventStore.Core.Data;
using EventStore.Core.Exceptions;
using EventStore.Core.Util;
using Microsoft.Extensions.Logging;

namespace EventStore.Core.Index
{
    public class IndexMap
    {
        private static readonly ILogger Log = TraceLogger.GetLogger<IndexMap>();

        public const int IndexMapVersion = 2;

        public readonly int Version;

        public readonly long PrepareCheckpoint;
        public readonly long CommitCheckpoint;

        private readonly List<List<PTable>> _map;
        private readonly int _maxTablesPerLevel;
        private readonly int _maxTableLevelsForAutomaticMerge;

        private IndexMap(int version, List<List<PTable>> tables, long prepareCheckpoint, long commitCheckpoint, int maxTablesPerLevel, int maxTableLevelsForAutomaticMerge)
        {
            if ((uint)version > Consts.TooBigOrNegative) { ThrowHelper.ThrowArgumentOutOfRangeException_Nonnegative(ExceptionArgument.version); }
            if (ThrowHelper.IsInvalidCheckpoint(prepareCheckpoint)) ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.prepareCheckpoint);
            if (ThrowHelper.IsInvalidCheckpoint(commitCheckpoint)) ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.commitCheckpoint);
            if (maxTablesPerLevel <= 1) ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.maxTablesPerLevel);

            Version = version;

            PrepareCheckpoint = prepareCheckpoint;
            CommitCheckpoint = commitCheckpoint;

            _map = CopyFrom(tables);
            _maxTablesPerLevel = maxTablesPerLevel;
            _maxTableLevelsForAutomaticMerge = maxTableLevelsForAutomaticMerge;

            VerifyStructure();
        }

        private static List<List<PTable>> CopyFrom(List<List<PTable>> tables)
        {
            var tmp = new List<List<PTable>>();
            for (int i = 0; i < tables.Count; i++)
            {
                tmp.Add(new List<PTable>(tables[i]));
            }

            return tmp;
        }

        private void VerifyStructure()
        {
            if (_map.SelectMany(level => level).Any(item => item is null))
            {
                ThrowHelper.ThrowCorruptIndexException_InternalIndexmapStructureCorruption();
            }
        }
        
        private static void AddTableToTables(List<List<PTable>> tables, int level, PTable table)
        {
            while ((uint)level >= (uint)tables.Count)
            {
                tables.Add(new List<PTable>());
            }

            var innerTables = tables[level] ?? (tables[level] = new List<PTable>());
            
            innerTables.Add(table);
        }

        private static void InsertTableToTables(List<List<PTable>> tables, int level, int position, PTable table)
        {
            while ((uint)level >= (uint)tables.Count)
                tables.Add(new List<PTable>());

            var innerTables = tables[level] ?? (tables[level] = new List<PTable>());
            
            while ((uint)position >= (uint)innerTables.Count) 
                innerTables.Add(null);
            
            innerTables[position] = table;
        }

        public IEnumerable<PTable> InOrder()
        {
            var map = _map;
            // level 0 (newest tables) -> N (oldest tables)
            for (int i = 0; i < map.Count; ++i)
            {
                // last in the level's list (newest on level) -> to first (oldest on level)
                for (int j = map[i].Count - 1; j >= 0; --j)
                {
                    yield return map[i][j];
                }
            }
        }

        public IEnumerable<PTable> InReverseOrder()
        {
            var map = _map;
            // N (oldest tables) -> level 0 (newest tables)
            for (int i = map.Count - 1; i >= 0; --i)
            {
                // from first (oldest on level) in the level's list -> last in the level's list (newest on level)
                for (int j = 0, n = map[i].Count; j < n; ++j)
                {
                    yield return map[i][j];
                }
            }
        }

        public IEnumerable<string> GetAllFilenames()
        {
            return from level in _map
                   from table in level
                   select table.Filename;
        }

        public static IndexMap CreateEmpty(int maxTablesPerLevel, int maxTableLevelsForAutomaticMerge)
        {
            return new IndexMap(IndexMapVersion, new List<List<PTable>>(), -1, -1, maxTablesPerLevel, maxTableLevelsForAutomaticMerge);
        }

        public static IndexMap FromFile(
            string filename,
            int maxTablesPerLevel,
            bool loadPTables,
            int cacheDepth,
            bool skipIndexVerify,
            int threads,
            int maxAutoMergeLevel)
        {
            if (!File.Exists(filename))
            {
                return CreateEmpty(maxTablesPerLevel, maxAutoMergeLevel);
            }

            using (var f = File.OpenRead(filename))
            {
                // calculate real MD5 hash except first 32 bytes which are string representation of stored hash
                f.Position = 32;
                var realHash = MD5Hash.GetHashFor(f);
                f.Position = 0;

                using (var reader = new StreamReader(f))
                {
                    ReadAndCheckHash(reader, realHash);

                    // at this point we can assume the format is ok, so actually no need to check errors.
                    var version = ReadVersion(reader);

                    var checkpoints = ReadCheckpoints(reader);
                    var prepareCheckpoint = checkpoints.PreparePosition;
                    var commitCheckpoint = checkpoints.CommitPosition;
                    if (version > 1)
                    {
                        var tmpMaxAutoMergeLevel = ReadMaxAutoMergeLevel(reader);
                        if (tmpMaxAutoMergeLevel < maxAutoMergeLevel)
                        {
                            ThrowHelper.ThrowCorruptIndexException_IndexMapHasLowerMaximumAutoMergeLevelThanIsCurrentlyConfigured(tmpMaxAutoMergeLevel, maxAutoMergeLevel);
                        }

                        maxAutoMergeLevel = Math.Min(maxAutoMergeLevel, tmpMaxAutoMergeLevel);
                    }
                    //we are doing a logical upgrade of the version to have the new data, so we will change the version to match so that new files are saved with the right version
                    version = IndexMapVersion;
                    var tables = loadPTables ? LoadPTables(reader, filename, checkpoints, cacheDepth, skipIndexVerify, threads) : new List<List<PTable>>();

                    if (!loadPTables && reader.ReadLine() is object)
                    {
                        ThrowHelper.ThrowCorruptIndexException_NegativePrepareCommitCheckpointInNonEmptyIndexMap(checkpoints);
                    }

                    return new IndexMap(version, tables, prepareCheckpoint, commitCheckpoint, maxTablesPerLevel, maxAutoMergeLevel);
                }
            }
        }

        private static void ReadAndCheckHash(TextReader reader, byte[] realHash)
        {
            // read stored MD5 hash and convert it from string to byte array
            string text;
            if ((text = reader.ReadLine()) is null)
            {
                ThrowHelper.ThrowCorruptIndexException_IndexMapFileIsEmpty();
            }
            if ((uint)text.Length != 32u || !text.All(x => Helper.IsDigit(x) || Helper.IsUpper(x)))
            {
                ThrowHelper.ThrowCorruptIndexException_CorruptedIndexMapMD5Hash(text);
            }

            // check expected and real hashes are the same
            var expectedHash = new byte[16];
            for (int i = 0; i < 16; ++i)
            {
                expectedHash[i] = Convert.ToByte(text.Substring(i * 2, 2), 16);
            }
            if (expectedHash.Length != realHash.Length)
            {
                ThrowHelper.ThrowCorruptIndexException_HashValidationErrorDifferentHasheSizes(expectedHash, realHash);
            }
            for (int i = 0; i < realHash.Length; ++i)
            {
                if (expectedHash[i] != realHash[i])
                {
                    ThrowHelper.ThrowCorruptIndexException_HashValidationErrorDifferentHashes(expectedHash, realHash);
                }
            }
        }

        private static int ReadVersion(TextReader reader)
        {
            string text;
            if ((text = reader.ReadLine()) is null)
            {
                ThrowHelper.ThrowCorruptIndexException_CorruptedVersion();
            }
            return int.Parse(text);
        }

        private static TFPos ReadCheckpoints(TextReader reader)
        {
            // read and check prepare/commit checkpoint
            string text;
            if ((text = reader.ReadLine()) is null)
            {
                ThrowHelper.ThrowCorruptIndexException_CorruptedCommitCheckpoint();
            }

            try
            {
                var checkpoints = text.Split('/');
                if (!long.TryParse(checkpoints[0], out long prepareCheckpoint) || ThrowHelper.IsInvalidCheckpoint(prepareCheckpoint))
                {
                    ThrowHelper.ThrowCorruptIndexException_InvalidPrepareCheckpoint(checkpoints[0]);
                }
                if (!long.TryParse(checkpoints[1], out long commitCheckpoint) || ThrowHelper.IsInvalidCheckpoint(commitCheckpoint))
                {
                    ThrowHelper.ThrowCorruptIndexException_InvalidCommitCheckpoint(checkpoints[1]);
                }
                return new TFPos(commitCheckpoint, prepareCheckpoint);
            }
            catch (Exception exc)
            {
                ThrowHelper.ThrowCorruptIndexException_CorruptedPrepareCommitCheckpointsPair(exc); return default;
            }
        }

        private static int ReadMaxAutoMergeLevel(TextReader reader)
        {
            if (!(reader.ReadLine() is string text && int.TryParse(text, out var maxAutoMergeLevel)))
                throw new CorruptIndexException("Corrupted auto merge level.");
            return maxAutoMergeLevel;
        }

        private static IEnumerable<string> GetAllLines(StreamReader reader)
        {
            // all next lines are PTables sorted by levels
            string text;
            while ((text = reader.ReadLine()) is object)
            {
                yield return text;
            }
        }

        private static List<List<PTable>> LoadPTables(StreamReader reader, string indexmapFilename, TFPos checkpoints, int cacheDepth, bool skipIndexVerify,
            int threads)
        {
            var tables = new List<List<PTable>>();
            try
            {
                try
                {
                    Parallel.ForEach(GetAllLines(reader).Reverse(), // Reverse so we load the highest levels (biggest files) first - ensures we use concurrency in the most efficient way. 
                        new ParallelOptions { MaxDegreeOfParallelism = threads }, LocalAction);
                    void LocalAction(string indexMapEntry)
                    {
                        if ((ulong)checkpoints.PreparePosition > Consts.TooBigOrNegativeUL || (ulong)checkpoints.CommitPosition > Consts.TooBigOrNegativeUL)
                            ThrowHelper.ThrowCorruptIndexException_NegativePrepareCommitCheckpointInNonEmptyIndexMap(checkpoints);

                        PTable ptable = null;
                        var pieces = indexMapEntry.Split(',');
                        try
                        {
                            var level = int.Parse(pieces[0]);
                            var position = int.Parse(pieces[1]);
                            var file = pieces[2];
                            var path = Path.GetDirectoryName(indexmapFilename);
                            var ptablePath = Path.Combine(path, file);

                            ptable = PTable.FromFile(ptablePath, cacheDepth, skipIndexVerify);

                            lock (tables)
                            {
                                InsertTableToTables(tables, level, position, ptable);
                            }
                        }
                        catch (Exception)
                        {
                            // if PTable file path was correct, but data is corrupted, we still need to dispose opened streams
                            if (ptable is object)
                                ptable.Dispose();

                            throw;
                        }
                    }
                    
                    // Verify map is correct
                    for (int i = 0; i < tables.Count; ++i)
                    {
                        for (int j = 0; j < tables[i].Count; ++j)
                        {
                            if (tables[i][j] is null)
                            {
                                ThrowHelper.ThrowCorruptIndexException_IndexmapIsMissingContiguousLevelPosition(i, j);
                            }
                        }
                    }
                    
                }
                catch (AggregateException aggEx)
                {
                    // We only care that *something* has gone wrong, throw the first exception
                    throw aggEx.InnerException;
                }
            }
            catch (Exception exc)
            {
                // also dispose all previously loaded correct PTables
                for (int i = 0; i < tables.Count; ++i)
                {
                    for (int j = 0; j < tables[i].Count; ++j)
                    {
                        if (tables[i][j] is object)
                            tables[i][j].Dispose();
                    }
                }

                ThrowHelper.ThrowCorruptIndexException_ErrorWhileLoadingIndexMap(exc);
            }

            return tables;
        }

        public void SaveToFile(string filename)
        {
            var tmpIndexMap = $"{filename}.{Guid.NewGuid()}.indexmap.tmp";

            using (var memStream = MemoryStreamManager.GetStream())
            {
                using (var memWriter = new StreamWriterX(memStream))
                {
                    memWriter.WriteLine(new string('0', 32)); // pre-allocate space for MD5 hash
                    memWriter.WriteLine(Version);
                    memWriter.WriteLine("{0}/{1}", PrepareCheckpoint, CommitCheckpoint);
                    memWriter.WriteLine(_maxTableLevelsForAutomaticMerge);
                    for (int i = 0; i < _map.Count; i++)
                    {
                        for (int j = 0; j < _map[i].Count; j++)
                        {
                            memWriter.WriteLine("{0},{1},{2}", i, j, new FileInfo(_map[i][j].Filename).Name);
                        }
                    }
                    memWriter.Flush();

                    memStream.Position = 32;
                    var hash = MD5Hash.GetHashFor(memStream);

                    memStream.Position = 0;
                    foreach (var t in hash)
                    {
                        memWriter.Write(t.ToString("X2"));
                    }
                    memWriter.Flush();

                    memStream.Position = 0;
                    using (var f = File.OpenWrite(tmpIndexMap))
                    {
                        // ## 苦竹 修改 ##
                        //f.Write(memStream.GetBuffer(), 0, (int)memStream.Length);
                        memStream.CopyTo(f);
                        f.FlushToDisk();
                    }
                }
            }

            int trial = 0;
            int maxTrials = 5;
            while (trial < maxTrials)
            {
                void errorHandler(Exception ex)
                {
                    Log.FailedTrialToReplaceIndexmap(filename, tmpIndexMap, ex);
                    trial += 1;
                };
                try
                {
                    if (File.Exists(filename))
                    {
                        File.SetAttributes(filename, FileAttributes.Normal);
                        File.Delete(filename);
                    }
                    File.Move(tmpIndexMap, filename);
                    break;
                }
                catch (IOException exc)
                {
                    errorHandler(exc);
                    if(trial>=maxTrials){
                        ProcessUtil.PrintWhoIsLocking(tmpIndexMap, Log);
                        ProcessUtil.PrintWhoIsLocking(filename, Log);
                    }
                }
                catch (UnauthorizedAccessException exc)
                {
                    errorHandler(exc);
                }
            }
        }

        public Tuple<int, PTable> GetTableForManualMerge()
        {
            //we have more than one entry at the max level
            //or we have at least one entry at the max level and tables at a level above it
            // or we have any tables > max level
            var tablesExistAtMaxLevelOrAbove = (uint)_map.Count > (uint)_maxTableLevelsForAutomaticMerge
                    && _map[_maxTableLevelsForAutomaticMerge] is object;
            bool moreThanOneEntryAtMaxLevel = tablesExistAtMaxLevelOrAbove
                                              && (uint)_map[_maxTableLevelsForAutomaticMerge].Count > 1u;
            bool atLeastOneEntryAtMaxLevelAndOneAboveIt = tablesExistAtMaxLevelOrAbove && (uint)_map[_maxTableLevelsForAutomaticMerge].Count == 1u
                                                          && (uint)_map.Count > (uint)(_maxTableLevelsForAutomaticMerge + 1)
                                                          && _map.Skip(_maxTableLevelsForAutomaticMerge).Any(x => (uint)x.Count > 0u);
            bool moreThanOneEntryAboveMaxLevel = tablesExistAtMaxLevelOrAbove &&
                                                 _map.Skip(_maxTableLevelsForAutomaticMerge).Any(x => (uint)x.Count > 1u) ||
                                                 (uint)_map.Skip(_maxTableLevelsForAutomaticMerge).Count(x => (uint)x.Count > 0u) > 1u;
            if (moreThanOneEntryAtMaxLevel || atLeastOneEntryAtMaxLevelAndOneAboveIt || moreThanOneEntryAboveMaxLevel)
            {
                //we don't actually care which table we return here as manual merge will actually just iterate over anything above the max merge level
                return Tuple.Create(_map.Count, _map[_map.Count - 1].FirstOrDefault());
            }

            return Tuple.Create(_map.Count - 1, default(PTable));
        }

        public MergeResult AddPTable(PTable tableToAdd,
            long prepareCheckpoint,
            long commitCheckpoint,
            Func<string, ulong, ulong> upgradeHash,
            Func<IndexEntry, bool> existsAt,
            Func<IndexEntry, Tuple<string, bool>> recordExistsAt,
            IIndexFilenameProvider filenameProvider,
            byte version,
            int level,
            int indexCacheDepth = 16,
            bool skipIndexVerify = false)
        {
            bool isManual;
            //if (0u >= (uint)_maxTableLevelsForAutomaticMerge)
            //{
            //    //when we are not auto merging at all, a manual merge will only be triggered if
            //    //there are entries in the index map. the table it passes is always the first table
            //    //at the maximum automerge level, so we only need to hit the first table and see if it
            //    //matches, otherwise it must be an an add of a memtable.
            //    //although we are not automatically merging, the automerge process is also responsible
            //    //for writing out the memtable that just got persisted so we want to call auto merge still
            //    isManual = _map.Count != 0 && _map[0].FirstOrDefault() == tableToAdd;
            //}
            //else
            //{
            isManual = level > _maxTableLevelsForAutomaticMerge;
            //}

            if (isManual)
            {
                //For manual merge, we are never adding any extra entries, just merging existing files, so the index p/c checkpoint won't change
                return AddPTableForManualMerge(PrepareCheckpoint, CommitCheckpoint, upgradeHash, existsAt, recordExistsAt,
                    filenameProvider, version, indexCacheDepth, skipIndexVerify);
            }
            return AddPTableForAutomaticMerge(tableToAdd, prepareCheckpoint, commitCheckpoint, upgradeHash,
                existsAt, recordExistsAt, filenameProvider, version, indexCacheDepth, skipIndexVerify);
        }

        public MergeResult AddPTableForAutomaticMerge(PTable tableToAdd,
            long prepareCheckpoint,
            long commitCheckpoint,
            Func<string, ulong, ulong> upgradeHash,
            Func<IndexEntry, bool> existsAt,
            Func<IndexEntry, Tuple<string, bool>> recordExistsAt,
            IIndexFilenameProvider filenameProvider,
            byte version,
            int indexCacheDepth = 16,
            bool skipIndexVerify = false)
        {
            if ((ulong)prepareCheckpoint > Consts.TooBigOrNegativeUL) { ThrowHelper.ThrowArgumentOutOfRangeException_Nonnegative(ExceptionArgument.prepareCheckpoint); }
            if ((ulong)commitCheckpoint > Consts.TooBigOrNegativeUL) { ThrowHelper.ThrowArgumentOutOfRangeException_Nonnegative(ExceptionArgument.commitCheckpoint); }

            var tables = CopyFrom(_map);
            AddTableToTables(tables, 0, tableToAdd);

            var toDelete = new List<PTable>();
            var maxTableLevelsToMerge = Math.Min(tables.Count, _maxTableLevelsForAutomaticMerge);
            for (int level = 0; level < maxTableLevelsToMerge; level++)
            {
                if ((uint)tables[level].Count >= (uint)_maxTablesPerLevel)
                {
                    var filename = filenameProvider.GetFilenameNewTable();
                    PTable mergedTable = PTable.MergeTo(tables[level], filename, upgradeHash, existsAt, recordExistsAt,
                        version, indexCacheDepth, skipIndexVerify);

                    AddTableToTables(tables, level + 1, mergedTable);
                    toDelete.AddRange(tables[level]);
                    tables[level].Clear();
                }
            }
            var indexMap = new IndexMap(Version, tables, prepareCheckpoint, commitCheckpoint, _maxTablesPerLevel, _maxTableLevelsForAutomaticMerge);
            return new MergeResult(indexMap, toDelete);
        }

        public MergeResult AddPTableForManualMerge(long prepareCheckpoint,
            long commitCheckpoint,
            Func<string, ulong, ulong> upgradeHash,
            Func<IndexEntry, bool> existsAt,
            Func<IndexEntry, Tuple<string, bool>> recordExistsAt,
            IIndexFilenameProvider filenameProvider,
            byte version,
            int indexCacheDepth = 16,
            bool skipIndexVerify = false)
        {
            if ((ulong)prepareCheckpoint > Consts.TooBigOrNegativeUL) { ThrowHelper.ThrowArgumentOutOfRangeException_Nonnegative(ExceptionArgument.prepareCheckpoint); }
            if ((ulong)commitCheckpoint > Consts.TooBigOrNegativeUL) { ThrowHelper.ThrowArgumentOutOfRangeException_Nonnegative(ExceptionArgument.commitCheckpoint); }

            var tables = CopyFrom(_map);

            if ((uint)tables.Count < (uint)_maxTableLevelsForAutomaticMerge)
            {
                return new MergeResult(this, new List<PTable>());
            }

            var toDelete = new List<PTable>();
            var tablesToMerge = tables.Skip(_maxTableLevelsForAutomaticMerge).SelectMany(a => a).ToList();
            if ((uint)tablesToMerge.Count == 1u)
                return new MergeResult(this, new List<PTable>());

            var filename = filenameProvider.GetFilenameNewTable();
            PTable mergedTable = PTable.MergeTo(tablesToMerge, filename, upgradeHash, existsAt, recordExistsAt,
                version, indexCacheDepth, skipIndexVerify);

            for (int i = tables.Count - 1; i > _maxTableLevelsForAutomaticMerge; i--)
            {
                tables.RemoveAt(i);
            }

            tables[_maxTableLevelsForAutomaticMerge].Clear();
            AddTableToTables(tables, _maxTableLevelsForAutomaticMerge + 1, mergedTable);
            toDelete.AddRange(tablesToMerge);

            var indexMap = new IndexMap(Version, tables, prepareCheckpoint, commitCheckpoint, _maxTablesPerLevel, _maxTableLevelsForAutomaticMerge);
            return new MergeResult(indexMap, toDelete);
        }

        public ScavengeResult Scavenge(Guid toScavenge, CancellationToken ct,
            Func<string, ulong, ulong> upgradeHash,
            Func<IndexEntry, bool> existsAt,
            Func<IndexEntry, Tuple<string, bool>> recordExistsAt,
            IIndexFilenameProvider filenameProvider,
            byte version,
            int indexCacheDepth = 16,
            bool skipIndexVerify = false)
        {
            var scavengedMap = CopyFrom(_map);
            for (int level = 0; level < scavengedMap.Count; level++)
            {
                for (int i = 0; i < scavengedMap[level].Count; i++)
                {
                    if (scavengedMap[level][i].Id == toScavenge)
                    {
                        var filename = filenameProvider.GetFilenameNewTable();
                        var oldTable = scavengedMap[level][i];

                        PTable scavenged = PTable.Scavenged(oldTable, filename, upgradeHash, existsAt, recordExistsAt, version, out long spaceSaved, indexCacheDepth, skipIndexVerify, ct);

                        if (scavenged is null)
                        {
                            return ScavengeResult.Failed(oldTable, level, i);
                        }

                        scavengedMap[level][i] = scavenged;

                        var indexMap = new IndexMap(Version, scavengedMap, PrepareCheckpoint, CommitCheckpoint, _maxTablesPerLevel, _maxTableLevelsForAutomaticMerge);
                        
                        return ScavengeResult.Success(indexMap, oldTable, scavenged, spaceSaved, level, i);
                    }
                }
            }

            ThrowHelper.ThrowArgumentException(ExceptionResource.Unable_to_find_table_in_map, ExceptionArgument.toScavenge); return null;
        }

        public void Dispose(TimeSpan timeout)
        {
            foreach (var ptable in InOrder())
            {
                ptable.Dispose();
            }
            foreach (var ptable in InOrder())
            {
                ptable.WaitForDisposal(timeout);
            }
        }
    }
}
