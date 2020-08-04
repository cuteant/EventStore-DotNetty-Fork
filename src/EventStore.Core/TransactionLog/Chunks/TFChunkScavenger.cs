using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotNetty.Common;
using EventStore.Common.Utils;
using EventStore.Core.Data;
using EventStore.Core.DataStructures;
using EventStore.Core.Exceptions;
using EventStore.Core.Index;
using EventStore.Core.Services;
using EventStore.Core.Services.Storage.ReaderIndex;
using EventStore.Core.TransactionLog.Chunks.TFChunk;
using EventStore.Core.TransactionLog.LogRecords;
using Microsoft.Extensions.Logging;

namespace EventStore.Core.TransactionLog.Chunks
{
    public class TFChunkScavenger
    {
        private static readonly ILogger Log = TraceLogger.GetLogger<TFChunkScavenger>();

        private readonly TFChunkDb _db;
        private readonly ITFChunkScavengerLog _scavengerLog;
        private readonly ITableIndex _tableIndex;
        private readonly IReadIndex _readIndex;
        private readonly long _maxChunkDataSize;
        private readonly bool _unsafeIgnoreHardDeletes;
        private readonly int _threads;
        private const int MaxRetryCount = 5;
        internal const int MaxThreadCount = 4;
        private const int FlushPageInterval = 32; // max 65536 pages to write resulting in 2048 flushes per chunk

        public TFChunkScavenger(TFChunkDb db, ITFChunkScavengerLog scavengerLog, ITableIndex tableIndex, IReadIndex readIndex, long? maxChunkDataSize = null,
            bool unsafeIgnoreHardDeletes = false, int threads = 1)
        {
            if (db is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.db); }
            if (scavengerLog is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.scavengerLog); }
            if (tableIndex is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.tableIndex); }
            if (readIndex is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.readIndex); }
            if ((uint)(threads - 1) >= Consts.TooBigOrNegative) { ThrowHelper.ThrowArgumentOutOfRangeException_Positive(ExceptionArgument.threads); }

            if (threads > MaxThreadCount)
            {
                if (Log.IsWarningLevelEnabled()) Log.Scavenging_threads_not_allowed(threads, MaxThreadCount);
                threads = MaxThreadCount;
            }

            _db = db;
            _scavengerLog = scavengerLog;
            _tableIndex = tableIndex;
            _readIndex = readIndex;
            _maxChunkDataSize = maxChunkDataSize ?? db.Config.ChunkSize;
            _unsafeIgnoreHardDeletes = unsafeIgnoreHardDeletes;
            _threads = threads;
        }

        public string ScavengeId => _scavengerLog.ScavengeId;

        private IEnumerable<TFChunk.TFChunk> GetAllChunks(int startFromChunk)
        {
            long scavengePos = _db.Config.ChunkSize * (long)startFromChunk;
            while (scavengePos < _db.Config.ChaserCheckpoint.Read())
            {
                var chunk = _db.Manager.GetChunkFor(scavengePos);
                if (!chunk.IsReadOnly)
                {
                    yield break;
                }

                yield return chunk;

                scavengePos = chunk.ChunkHeader.ChunkEndPosition;
            }
        }

        public Task Scavenge(bool alwaysKeepScavenged, bool mergeChunks, int startFromChunk = 0, CancellationToken ct = default(CancellationToken))
        {
            if ((uint)startFromChunk > Consts.TooBigOrNegative) { ThrowHelper.ThrowArgumentOutOfRangeException_Nonnegative(ExceptionArgument.startFromChunk); }

            // Note we aren't passing the CancellationToken to the task on purpose so awaiters
            // don't have to handle Exceptions and can wait for the actual completion of the task.
            return Task.Factory.StartNew(() =>
            {
                var sw = Stopwatch.StartNew();

                ScavengeResult result = ScavengeResult.Success;
                string error = null;
                try
                {
                    _scavengerLog.ScavengeStarted();

                    ScavengeInternal(alwaysKeepScavenged, mergeChunks, startFromChunk, ct);

                    _tableIndex.Scavenge(_scavengerLog, ct);
                }
                catch (OperationCanceledException)
                {
                    if (Log.IsInformationLevelEnabled()) Log.ScavengeCancelled();
                    result = ScavengeResult.Stopped;
                }
                catch (Exception exc)
                {
                    result = ScavengeResult.Failed;
                    Log.ScavengingErrorWhileScavengingDb(exc);
                    error = string.Format("Error while scavenging DB: {0}.", exc.Message);
                }
                finally
                {
                    try
                    {
                        _scavengerLog.ScavengeCompleted(result, error, sw.Elapsed);
                    }
                    catch (Exception ex)
                    {
                        Log.ErrorWhilstRecordingScavengeCompleted(result, sw.Elapsed, error, ex);
                    }
                }
            }, TaskCreationOptions.LongRunning);
        }

        private void ScavengeInternal(bool alwaysKeepScavenged, bool mergeChunks, int startFromChunk, CancellationToken ct)
        {
            var totalSw = Stopwatch.StartNew();
            var sw = Stopwatch.StartNew();
#if DEBUG
            var traceEnabled = Log.IsTraceLevelEnabled();

            if (traceEnabled) Log.StartedScavengingOfDB(_db.Manager.ChunksCount, alwaysKeepScavenged, mergeChunks);
#endif

            // Initial scavenge pass
            var chunksToScavenge = GetAllChunks(startFromChunk);

            using (var scavengeCacheObjectPool = CreateThreadLocalScavengeCachePool(_threads))
            {
                Parallel.ForEach(chunksToScavenge,
                    new ParallelOptions { MaxDegreeOfParallelism = _threads, CancellationToken = ct },
                    (chunk, pls) =>
                    {
                        var cache = scavengeCacheObjectPool.Get();
                        try
                        {
                            ScavengeChunk(alwaysKeepScavenged, chunk, cache, ct);
                        }
                        finally
                        {
                            cache.Reset(); // reset thread local cache before next iteration.
                            scavengeCacheObjectPool.Return(cache);
                        }
                    });
            }

#if DEBUG
            if (traceEnabled) Log.ScavengingInitialPassCompleted(sw.Elapsed);
#endif

            // Merge scavenge pass
            if (mergeChunks)
            {
                bool mergedSomething;
                int passNum = 0;
                do
                {
                    mergedSomething = false;
                    passNum += 1;
                    sw.Restart();

                    var chunksToMerge = ThreadLocalList<TFChunk.TFChunk>.NewInstance();
                    try
                    {
                        long totalDataSize = 0;
                        foreach (var chunk in GetAllChunks(0))
                        {
                            ct.ThrowIfCancellationRequested();

                            if (totalDataSize + chunk.PhysicalDataSize > _maxChunkDataSize)
                            {
                                if (0u >= (uint)chunksToMerge.Count) { ThrowHelper.ThrowException(ExceptionResource.SCAVENGING_no_chunks_to_merge); }

                                if ((uint)chunksToMerge.Count > 1u && MergeChunks(chunksToMerge, ct))
                                {
                                    mergedSomething = true;
                                }

                                chunksToMerge.Clear();
                                totalDataSize = 0;
                            }

                            chunksToMerge.Add(chunk);
                            totalDataSize += chunk.PhysicalDataSize;
                        }

                        if ((uint)chunksToMerge.Count > 1u)
                        {
                            if (MergeChunks(chunksToMerge, ct))
                            {
                                mergedSomething = true;
                            }
                        }
                    }
                    finally
                    {
                        chunksToMerge.Return();
                    }
#if DEBUG
                    if (traceEnabled) Log.ScavengingMergePassCompleted(passNum, sw.Elapsed, mergedSomething);
#endif
                } while (mergedSomething);
            }

#if DEBUG
            if (traceEnabled) Log.ScavengingTotaltimetakenTotalSpaceSaved(totalSw.Elapsed, _scavengerLog.SpaceSaved);
#endif
        }

        private void ScavengeChunk(bool alwaysKeepScavenged, TFChunk.TFChunk oldChunk, ThreadLocalScavengeCache threadLocalCache, CancellationToken ct)
        {
            if (oldChunk is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.oldChunk); }

            var sw = Stopwatch.StartNew();

            int chunkStartNumber = oldChunk.ChunkHeader.ChunkStartNumber;
            long chunkStartPos = oldChunk.ChunkHeader.ChunkStartPosition;
            int chunkEndNumber = oldChunk.ChunkHeader.ChunkEndNumber;
            long chunkEndPos = oldChunk.ChunkHeader.ChunkEndPosition;

            var tmpChunkPath = Path.Combine(_db.Config.Path, Guid.NewGuid() + ".scavenge.tmp");
#if DEBUG
            var oldChunkName = oldChunk.ToString();
            if (Log.IsTraceLevelEnabled()) { Log.StartedToScavengeChunks(oldChunkName, chunkStartNumber, chunkEndNumber, chunkStartPos, chunkEndPos, tmpChunkPath); }
#endif

            TFChunk.TFChunk newChunk;
            try
            {
                newChunk = TFChunk.TFChunk.CreateNew(tmpChunkPath,
                    _db.Config.ChunkSize,
                    chunkStartNumber,
                    chunkEndNumber,
                    isScavenged: true,
                    inMem: _db.Config.InMemDb,
                    unbuffered: _db.Config.Unbuffered,
                    writethrough: _db.Config.WriteThrough,
                    initialReaderCount: _db.Config.InitialReaderCount,
                    reduceFileCachePressure: _db.Config.ReduceFileCachePressure);
            }
            catch (IOException exc)
            {
                Log.IOExceptionDuringCreatingNewChunkForScavengingPurposes(exc);
                throw;
            }

            try
            {
                TraverseChunkBasic(oldChunk, ct,
                    result =>
                    {
                        threadLocalCache.Records.Add(result);

                        if (result.LogRecord.RecordType == LogRecordType.Commit)
                        {
                            var commit = (CommitLogRecord)result.LogRecord;
                            if (commit.TransactionPosition >= chunkStartPos)
                                threadLocalCache.Commits.Add(commit.TransactionPosition, new CommitInfo(commit));
                        }
                    });

                long newSize = 0;
                int filteredCount = 0;

                for (int i = 0; i < threadLocalCache.Records.Count; i++)
                {
                    ct.ThrowIfCancellationRequested();

                    var recordReadResult = threadLocalCache.Records[i];
                    if (ShouldKeep(recordReadResult, threadLocalCache.Commits, chunkStartPos, chunkEndPos))
                    {
                        newSize += recordReadResult.RecordLength + 2 * sizeof(int);
                        filteredCount++;
                    }
                    else
                    {
                        // We don't need this record any more.
                        threadLocalCache.Records[i] = default(CandidateRecord);
                    }
                }

#if DEBUG
                if (Log.IsTraceLevelEnabled()) Log.ScavengingChunkTraversedIncluding(oldChunkName, threadLocalCache.Records.Count, filteredCount);
#endif

                newSize += filteredCount * PosMap.FullSize + ChunkHeader.Size + ChunkFooter.Size;
                if (newChunk.ChunkHeader.Version >= (byte)TFChunk.TFChunk.ChunkVersions.Aligned)
                    newSize = TFChunk.TFChunk.GetAlignedSize((int)newSize);

                bool oldVersion = oldChunk.ChunkHeader.Version != TFChunk.TFChunk.CurrentChunkVersion;
                long oldSize = oldChunk.FileSize;

                if (oldSize <= newSize && !alwaysKeepScavenged && !_unsafeIgnoreHardDeletes && !oldVersion)
                {
#if DEBUG
                    if (Log.IsTraceLevelEnabled()) Log.ScavengingOfChunks(oldChunkName, sw.Elapsed, oldSize, newSize);
#endif

                    newChunk.MarkForDeletion();
                    _scavengerLog.ChunksNotScavenged(chunkStartNumber, chunkEndNumber, sw.Elapsed, "");
                }
                else
                {
                    var positionMapping = ThreadLocalList<PosMap>.NewInstance(filteredCount);

                    var lastFlushedPage = -1;
                    try
                    {
                        for (int i = 0; i < threadLocalCache.Records.Count; i++)
                        {
                            ct.ThrowIfCancellationRequested();

                            // Since we replaced the ones we don't want with `default`, the success flag will only be true on the ones we want to keep.
                            var recordReadResult = threadLocalCache.Records[i];

                            // Check log record, if not present then assume we can skip. 
                            if (recordReadResult.LogRecord is object)
                                positionMapping.Add(WriteRecord(newChunk, recordReadResult.LogRecord));

                            var currentPage = newChunk.RawWriterPosition / 4096;
                            if (currentPage - lastFlushedPage > FlushPageInterval)
                            {
                                newChunk.Flush();
                                lastFlushedPage = currentPage;
                            }
                        }

                        newChunk.CompleteScavenge(positionMapping);
                    }
                    finally
                    {
                        positionMapping.Return();
                    }

#if DEBUG
                    if (Log.IsTraceLevelEnabled())
                    {
                        if (_unsafeIgnoreHardDeletes)
                        {
                            Log.Forcing_scavenge_chunk_to_be_kept_even_if_bigger();
                        }

                        if (oldVersion)
                        {
                            Log.ForcingScavengedChunkToBeKeptAsOldChunkIsAPreviousVersion();
                        }
                    }
#endif

                    var chunk = _db.Manager.SwitchChunk(newChunk, verifyHash: false, removeChunksWithGreaterNumbers: false);
                    if (chunk is object)
                    {
#if DEBUG
                        if (Log.IsTraceLevelEnabled())
                        {
                            Log.ScavengingOfChunks(oldChunkName, sw.Elapsed, tmpChunkPath, chunkStartNumber, chunkEndNumber, chunk.FileName, oldSize, newSize);
                        }
#endif
                        var spaceSaved = oldSize - newSize;
                        _scavengerLog.ChunksScavenged(chunkStartNumber, chunkEndNumber, sw.Elapsed, spaceSaved);
                    }
                    else
                    {
#if DEBUG
                        if (Log.IsTraceLevelEnabled())
                        {
                            Log.ScavengingOfChunks(oldChunkName, sw.Elapsed, chunkStartNumber, chunkEndNumber, tmpChunkPath, oldSize, newSize);
                        }
#endif
                        _scavengerLog.ChunksNotScavenged(chunkStartNumber, chunkEndNumber, sw.Elapsed, "Chunk switch prevented.");
                    }
                }
            }
            catch (FileBeingDeletedException exc)
            {
                if (Log.IsInformationLevelEnabled())
                {
#if !DEBUG
                    var oldChunkName = oldChunk.ToString();
#endif
                    Log.GotFilebeingdeletedexceptionExceptionDuringScavenging(oldChunkName, tmpChunkPath, exc);
                }
                newChunk.Dispose();
                DeleteTempChunk(tmpChunkPath, MaxRetryCount);
                _scavengerLog.ChunksNotScavenged(chunkStartNumber, chunkEndNumber, sw.Elapsed, exc.Message);
            }
            catch (OperationCanceledException)
            {
                if (Log.IsInformationLevelEnabled())
                {
#if !DEBUG
                    var oldChunkName = oldChunk.ToString();
#endif
                    Log.ScavengingCancelledAtOldChunk(oldChunkName);
                }
                newChunk.MarkForDeletion();
                _scavengerLog.ChunksNotScavenged(chunkStartNumber, chunkEndNumber, sw.Elapsed, "Scavenge cancelled");
            }
            catch (Exception ex)
            {
                if (Log.IsInformationLevelEnabled()) Log.GotExceptionWhileScavengingChunk(chunkStartNumber, chunkEndNumber, ex);
                newChunk.Dispose();
                DeleteTempChunk(tmpChunkPath, MaxRetryCount);
                _scavengerLog.ChunksNotScavenged(chunkStartNumber, chunkEndNumber, sw.Elapsed, ex.Message);
            }
        }

        private bool MergeChunks(IList<TFChunk.TFChunk> oldChunks, CancellationToken ct)
        {
            if (oldChunks.IsEmpty()) ThrowHelper.ThrowArgumentException(ExceptionResource.Provided_list_of_chunks_to_merge_is_empty);

            var oldChunksList = string.Join("\n", oldChunks);

            if ((uint)oldChunks.Count < 2u)
            {
#if DEBUG
                if (Log.IsTraceLevelEnabled()) Log.TriedToMergeLessThan2chunksAborting(oldChunksList);
#endif
                return false;
            }

            var sw = Stopwatch.StartNew();

            int chunkStartNumber = oldChunks.First().ChunkHeader.ChunkStartNumber;
            int chunkEndNumber = oldChunks.Last().ChunkHeader.ChunkEndNumber;

            var tmpChunkPath = Path.Combine(_db.Config.Path, Guid.NewGuid() + ".merge.scavenge.tmp");
#if DEBUG
            if (Log.IsTraceLevelEnabled()) Log.StartedToMergeChunks(oldChunksList, tmpChunkPath);
#endif

            TFChunk.TFChunk newChunk;
            try
            {
                newChunk = TFChunk.TFChunk.CreateNew(tmpChunkPath,
                    _db.Config.ChunkSize,
                    chunkStartNumber,
                    chunkEndNumber,
                    isScavenged: true,
                    inMem: _db.Config.InMemDb,
                    unbuffered: _db.Config.Unbuffered,
                    writethrough: _db.Config.WriteThrough,
                    initialReaderCount: _db.Config.InitialReaderCount,
                    reduceFileCachePressure: _db.Config.ReduceFileCachePressure);
            }
            catch (IOException exc)
            {
                Log.IoExceptionDuringCreatingNewChunkForScavengingMergePurposes(exc);
                return false;
            }

            try
            {
                var oldVersion = oldChunks.Any(x => x.ChunkHeader.Version != TFChunk.TFChunk.CurrentChunkVersion);

                var positionMapping = ThreadLocalList<PosMap>.NewInstance();
                try
                {
                    for (int idx = 0; idx < oldChunks.Count; idx++)
                    {
                        var lastFlushedPage = -1;
                        TraverseChunkBasic(oldChunks[idx], ct,
                            result => 
                            {
                                positionMapping.Add(WriteRecord(newChunk, result.LogRecord));

                                var currentPage = newChunk.RawWriterPosition / 4096;
                                if (currentPage - lastFlushedPage > FlushPageInterval)
                                {
                                    newChunk.Flush();
                                    lastFlushedPage = currentPage;
                                }
                            });
                    }

                    newChunk.CompleteScavenge(positionMapping);
                }
                finally
                {
                    positionMapping.Return();
                }

#if DEBUG
                if (Log.IsTraceLevelEnabled())
                {
                    if (_unsafeIgnoreHardDeletes) { Log.Forcing_merged_chunk_to_be_kept_even_if_bigger(); }
                    if (oldVersion) { Log.Forcing_merged_chunk_to_be_kept_as_old_chunk_is_a_previous_version(); }
                }
#endif

                var chunk = _db.Manager.SwitchChunk(newChunk, verifyHash: false, removeChunksWithGreaterNumbers: false);
                if (chunk is object)
                {
#if DEBUG
                    if (Log.IsTraceLevelEnabled()) { Log.MergingOfChunks(oldChunksList, sw.Elapsed, tmpChunkPath, chunkStartNumber, chunkEndNumber, chunk.FileName); }
#endif
                    var spaceSaved = oldChunks.Sum(_ => _.FileSize) - newChunk.FileSize;
                    _scavengerLog.ChunksMerged(chunkStartNumber, chunkEndNumber, sw.Elapsed, spaceSaved);
                    return true;
                }
                else
                {
#if DEBUG
                    if (Log.IsTraceLevelEnabled()) { Log.MergingOfChunks(oldChunksList, sw.Elapsed, chunkStartNumber, chunkEndNumber, tmpChunkPath); }
#endif
                    _scavengerLog.ChunksNotMerged(chunkStartNumber, chunkEndNumber, sw.Elapsed, "Chunk switch prevented.");
                    return false;
                }
            }
            catch (FileBeingDeletedException exc)
            {
                if (Log.IsInformationLevelEnabled()) Log.GotFilebeingdeletedexceptionExceptionDuringScavengeMerging(oldChunksList, tmpChunkPath, exc);
                newChunk.Dispose();
                DeleteTempChunk(tmpChunkPath, MaxRetryCount);
                _scavengerLog.ChunksNotMerged(chunkStartNumber, chunkEndNumber, sw.Elapsed, exc.Message);
                return false;
            }
            catch (OperationCanceledException)
            {
                if (Log.IsInformationLevelEnabled()) Log.ScavengingCancelledAt(oldChunksList);
                newChunk.MarkForDeletion();
                _scavengerLog.ChunksNotMerged(chunkStartNumber, chunkEndNumber, sw.Elapsed, "Scavenge cancelled");
                return false;
            }
            catch (Exception ex)
            {
                if (Log.IsInformationLevelEnabled()) Log.GotExceptionWhileMergingChunk(oldChunks, ex);
                newChunk.Dispose();
                DeleteTempChunk(tmpChunkPath, MaxRetryCount);
                _scavengerLog.ChunksNotMerged(chunkStartNumber, chunkEndNumber, sw.Elapsed, ex.Message);
                return false;
            }
        }

        private void DeleteTempChunk(string tmpChunkPath, int retries)
        {
            try
            {
                File.SetAttributes(tmpChunkPath, FileAttributes.Normal);
                File.Delete(tmpChunkPath);
            }
            catch (Exception ex)
            {
                if (retries > 0)
                {
                    Log.FailedToDeleteTheTempChunkRetrying(retries, MaxRetryCount, ex);
                    Thread.Sleep(5000);
                    DeleteTempChunk(tmpChunkPath, retries - 1);
                }
                else
                {
                    Log.FailedToDeleteTheTempChunkRetryLimitOfReached(MaxRetryCount, ex);
                    if (ex is System.IO.IOException)
                        ProcessUtil.PrintWhoIsLocking(tmpChunkPath, Log);
                    throw;
                }
            }
        }

        private bool ShouldKeep(in CandidateRecord result, Dictionary<long, CommitInfo> commits, long chunkStartPos, long chunkEndPos)
        {
            switch (result.LogRecord.RecordType)
            {
                case LogRecordType.Prepare:
                    var prepare = (PrepareLogRecord)result.LogRecord;
                    if (ShouldKeepPrepare(prepare, commits, chunkStartPos, chunkEndPos))
                        return true;
                    break;
                case LogRecordType.Commit:
                    var commit = (CommitLogRecord)result.LogRecord;
                    if (ShouldKeepCommit(commit, commits))
                        return true;
                    break;
                case LogRecordType.System:
                    return true;
            }

            return false;
        }

        private bool ShouldKeepCommit(CommitLogRecord commit, Dictionary<long, CommitInfo> commits)
        {
            CommitInfo commitInfo;
            if (!commits.TryGetValue(commit.TransactionPosition, out commitInfo))
            {
                // This should never happen given that we populate `commits` from the commit records.
                return true;
            }

            return commitInfo.KeepCommit != false;
        }

        private bool ShouldKeepPrepare(PrepareLogRecord prepare, Dictionary<long, CommitInfo> commits, long chunkStart, long chunkEnd)
        {
            CommitInfo commitInfo;
            bool hasSeenCommit = commits.TryGetValue(prepare.TransactionPosition, out commitInfo);
            bool isCommitted = hasSeenCommit || prepare.Flags.HasAnyOf(PrepareFlags.IsCommitted);

            if (prepare.Flags.HasAnyOf(PrepareFlags.StreamDelete))
            {
                if (_unsafeIgnoreHardDeletes)
                {
                    if (Log.IsInformationLevelEnabled()) Log.RemovingHardDeletedStreamTombstoneForStreamAtPosition(prepare);
                    commitInfo.TryNotToKeep();
                }
                else
                {
                    commitInfo.ForciblyKeep();
                }

                return !_unsafeIgnoreHardDeletes;
            }

            if (!isCommitted && prepare.Flags.HasAnyOf(PrepareFlags.TransactionBegin))
            {
                // So here we have prepare which commit is in the following chunks or prepare is not committed at all.
                // Now, whatever heuristic on prepare scavenge we use, we should never delete the very first prepare
                // in transaction, as in some circumstances we need it.
                // For instance, this prepare could be part of ongoing transaction and though we sometimes can determine
                // that prepare wouldn't ever be needed (e.g., stream was deleted, $maxAge or $maxCount rule it out)
                // we still need the first prepare to find out StreamId for possible commit in StorageWriterService.WriteCommit method.
                // There could be other reasons where it is needed, so we just safely filter it out to not bother further.
                return true;
            }

            var lastEventNumber = _readIndex.GetStreamLastEventNumber(prepare.EventStreamId);
            if (lastEventNumber == EventNumber.DeletedStream)
            {
                // When all prepares and commit of transaction belong to single chunk and the stream is deleted,
                // we can safely delete both prepares and commit.
                // Even if this prepare is not committed, but its stream is deleted, then as long as it is
                // not TransactionBegin prepare we can remove it, because any transaction should fail either way on commit stage.
                commitInfo.TryNotToKeep();
                return false;
            }

            if (!isCommitted)
            {
                // If we could somehow figure out (from read index) the event number of this prepare
                // (if it is actually committed, but commit is in another chunk) then we can apply same scavenging logic.
                // Unfortunately, if it is not committed prepare we can say nothing for now, so should conservatively keep it.
                return true;
            }

            if (prepare.Flags.HasNoneOf(PrepareFlags.Data))
            {
                // We encountered system prepare with no data. As of now it can appear only in explicit
                // transactions so we can safely remove it. The performance shouldn't hurt, because
                // TransactionBegin prepare is never needed either way and TransactionEnd should be in most
                // circumstances close to commit, so shouldn't hurt performance too much.
                // The advantage of getting rid of system prepares is ability to completely eliminate transaction
                // prepares and commit, if transaction events are completely ruled out by $maxAge/$maxCount.
                // Otherwise we'd have to either keep prepare not requiring to keep commit, which could leave
                // this prepare as never discoverable garbage, or we could insist on keeping commit forever
                // even if all events in transaction are scavenged.
                commitInfo.TryNotToKeep();
                return false;
            }

            if (IsSoftDeletedTempStreamWithinSameChunk(prepare.EventStreamId, chunkStart, chunkEnd))
            {
                commitInfo.TryNotToKeep();
                return false;
            }

            var eventNumber = prepare.Flags.HasAnyOf(PrepareFlags.IsCommitted)
                ? prepare.ExpectedVersion + 1 // IsCommitted prepares always have explicit expected version
                : commitInfo.EventNumber + prepare.TransactionOffset;

            if (!KeepOnlyFirstEventOfDuplicate(_tableIndex, prepare, eventNumber))
            {
                commitInfo.TryNotToKeep();
                return false;
            }

            // We should always physically keep the very last prepare in the stream.
            // Otherwise we get into trouble when trying to resolve LastStreamEventNumber, for instance.
            // That is because our TableIndex doesn't keep EventStreamId, only hash of it, so on doing some operations
            // that needs TableIndex, we have to make sure we have prepare records in TFChunks when we need them.
            if (eventNumber >= lastEventNumber)
            {
                // Definitely keep commit, otherwise current prepare wouldn't be discoverable.
                commitInfo.ForciblyKeep();
                return true;
            }

            var meta = _readIndex.GetStreamMetadata(prepare.EventStreamId);
            bool canRemove = (meta.MaxCount.HasValue && eventNumber < lastEventNumber - meta.MaxCount.Value + 1)
                             || (meta.TruncateBefore.HasValue && eventNumber < meta.TruncateBefore.Value)
                             || (meta.MaxAge.HasValue && prepare.TimeStamp < DateTime.UtcNow - meta.MaxAge.Value);

            if (canRemove)
                commitInfo.TryNotToKeep();
            else
                commitInfo.ForciblyKeep();
            return !canRemove;
        }

        private bool KeepOnlyFirstEventOfDuplicate(ITableIndex tableIndex, PrepareLogRecord prepare, long eventNumber)
        {
            var result = _readIndex.ReadEvent(prepare.EventStreamId, eventNumber);
            if (result.Result == ReadEventResult.Success && result.Record.LogPosition != prepare.LogPosition)
                return false;

            return true;
        }

        private bool IsSoftDeletedTempStreamWithinSameChunk(string eventStreamId, long chunkStart, long chunkEnd)
        {
            string sh;
            string msh;
            if (SystemStreams.IsMetastream(eventStreamId))
            {
                var originalStreamId = SystemStreams.OriginalStreamOf(eventStreamId);
                var meta = _readIndex.GetStreamMetadata(originalStreamId);
                if (meta.TruncateBefore != EventNumber.DeletedStream || meta.TempStream != true)
                    return false;
                sh = originalStreamId;
                msh = eventStreamId;
            }
            else
            {
                var meta = _readIndex.GetStreamMetadata(eventStreamId);
                if (meta.TruncateBefore != EventNumber.DeletedStream || meta.TempStream != true)
                    return false;
                sh = eventStreamId;
                msh = SystemStreams.MetastreamOf(eventStreamId);
            }

            IndexEntry e;
            var allInChunk = _tableIndex.TryGetOldestEntry(sh, out e) && e.Position >= chunkStart && e.Position < chunkEnd
                             && _tableIndex.TryGetLatestEntry(sh, out e) && e.Position >= chunkStart && e.Position < chunkEnd
                             && _tableIndex.TryGetOldestEntry(msh, out e) && e.Position >= chunkStart && e.Position < chunkEnd
                             && _tableIndex.TryGetLatestEntry(msh, out e) && e.Position >= chunkStart && e.Position < chunkEnd;
            return allInChunk;
        }

        private void TraverseChunkBasic(TFChunk.TFChunk chunk, CancellationToken ct,
            Action<CandidateRecord> process)
        {
            var result = chunk.TryReadFirst();
            while (result.Success)
            {
                process(new CandidateRecord(result.LogRecord, result.RecordLength));

                ct.ThrowIfCancellationRequested();

                result = chunk.TryReadClosestForward(result.NextPosition);
            }
        }

        private static PosMap WriteRecord(TFChunk.TFChunk newChunk, LogRecord record)
        {
            var writeResult = newChunk.TryAppend(record);
            if (!writeResult.Success)
            {
                ThrowHelper.ThrowException_UnableToAppendRecordDuringScavenging(writeResult.OldPosition, record);
            }

            long logPos = newChunk.ChunkHeader.GetLocalLogPosition(record.LogPosition);
            int actualPos = (int)writeResult.OldPosition;
            return new PosMap(logPos, actualPos);
        }

        internal class CommitInfo
        {
            public readonly long EventNumber;

            public bool? KeepCommit;

            public CommitInfo(CommitLogRecord commitRecord)
            {
                EventNumber = commitRecord.FirstEventNumber;
            }

            public override string ToString()
            {
                return string.Format("EventNumber: {0}, KeepCommit: {1}", EventNumber, KeepCommit);
            }
        }

        readonly struct CandidateRecord
        {
            public readonly LogRecord LogRecord;
            public readonly int RecordLength;

            public CandidateRecord(LogRecord logRecord, int recordLength)
            {
                LogRecord = logRecord;
                RecordLength = recordLength;
            }
        }

        private ObjectPool<ThreadLocalScavengeCache> CreateThreadLocalScavengeCachePool(int threads)
        {
            const int initialSizeOfThreadLocalCache = 1024 * 64; // 64k records

            return new ObjectPool<ThreadLocalScavengeCache>(
                ScavengeId,
                0,
                threads,
                () =>
                {
#if DEBUG
                    if (Log.IsTraceLevelEnabled()) Log.AllocatingSpacesInThreadLocalCache(initialSizeOfThreadLocalCache);
#endif
                    return new ThreadLocalScavengeCache(initialSizeOfThreadLocalCache);
                },
                cache => { },
                pool => { });
        }

        class ThreadLocalScavengeCache
        {
            private readonly Dictionary<long, CommitInfo> _commits;
            private readonly List<CandidateRecord> _records;

            public Dictionary<long, CommitInfo> Commits
            {
                get { return _commits; }
            }

            public List<CandidateRecord> Records
            {
                get { return _records; }
            }

            public ThreadLocalScavengeCache(int records)
            {
                // assume max quarter records are commits.
                _commits = new Dictionary<long, CommitInfo>(records / 4);
                _records = new List<CandidateRecord>(records);
            }

            public void Reset()
            {
                Commits.Clear();
                Records.Clear();
            }
        }
    }

    internal static class CommitInfoExtensions
    {
        public static void ForciblyKeep(this TFChunkScavenger.CommitInfo commitInfo)
        {
            if (commitInfo is object)
                commitInfo.KeepCommit = true;
        }

        public static void TryNotToKeep(this TFChunkScavenger.CommitInfo commitInfo)
        {
            // If someone decided definitely to keep corresponding commit then we shouldn't interfere.
            // Otherwise we should point that yes, you can remove commit for this prepare.
            if (commitInfo is object)
                commitInfo.KeepCommit = commitInfo.KeepCommit ?? false;
        }
    }
}
