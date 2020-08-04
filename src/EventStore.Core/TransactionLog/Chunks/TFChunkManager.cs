using System;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace EventStore.Core.TransactionLog.Chunks
{
    public class TFChunkManager : IDisposable
    {
        private static readonly ILogger Log = TraceLogger.GetLogger<TFChunkManager>();

        public const int MaxChunksCount = 100000; // that's enough for about 25 Tb of data

        public int ChunksCount { get { return _chunksCount; } }

        private readonly TFChunkDbConfig _config;
        private readonly TFChunk.TFChunk[] _chunks = new TFChunk.TFChunk[MaxChunksCount];
        private volatile int _chunksCount;
        private volatile bool _cachingEnabled;

        private readonly object _chunksLocker = new object();
        private int _backgroundPassesRemaining;
        private int _backgroundRunning;

        public TFChunkManager(TFChunkDbConfig config)
        {
            if (config is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.config); }
            _config = config;
        }

        public void EnableCaching()
        {
            if (0u >= (uint)_chunksCount) { ThrowHelper.ThrowException(ExceptionResource.No_chunks_in_DB); }

            lock (_chunksLocker)
            {
                _cachingEnabled = _config.MaxChunksCacheSize > 0;
                TryCacheChunk(_chunks[_chunksCount - 1]);
            }
        }

        private void BackgroundCachingProcess(object state)
        {
            do
            {
                do
                {
                    CacheUncacheReadOnlyChunks();
                } while (Interlocked.Decrement(ref _backgroundPassesRemaining) > 0);
                Interlocked.Exchange(ref _backgroundRunning, 0);
            } while (Interlocked.CompareExchange(ref _backgroundPassesRemaining, 0, 0) > 0
                     && 0u >= (uint)Interlocked.CompareExchange(ref _backgroundRunning, 1, 0));
        }

        private void CacheUncacheReadOnlyChunks()
        {
            int lastChunkToCache;
            lock (_chunksLocker)
            {
                long totalSize = 0;
                lastChunkToCache = _chunksCount;

                for (int chunkNum = _chunksCount - 1; chunkNum >= 0;)
                {
                    var chunk = _chunks[chunkNum];
                    var chunkSize = chunk.IsReadOnly
                            ? chunk.ChunkFooter.PhysicalDataSize + chunk.ChunkFooter.MapSize + ChunkHeader.Size + ChunkFooter.Size
                            : chunk.ChunkHeader.ChunkSize + ChunkHeader.Size + ChunkFooter.Size;

                    if (totalSize + chunkSize > _config.MaxChunksCacheSize) { break; }

                    totalSize += chunkSize;
                    lastChunkToCache = chunk.ChunkHeader.ChunkStartNumber;

                    chunkNum = chunk.ChunkHeader.ChunkStartNumber - 1;
                }
            }

            for (int chunkNum = lastChunkToCache - 1; chunkNum >= 0;)
            {
                var chunk = _chunks[chunkNum];
                if (chunk.IsReadOnly)
                    chunk.UnCacheFromMemory();
                chunkNum = chunk.ChunkHeader.ChunkStartNumber - 1;
            }

            for (int chunkNum = lastChunkToCache; chunkNum < _chunksCount;)
            {
                var chunk = _chunks[chunkNum];
                if (chunk.IsReadOnly) { chunk.CacheInMemory(); }
                chunkNum = chunk.ChunkHeader.ChunkEndNumber + 1;
            }
        }

        public TFChunk.TFChunk CreateTempChunk(ChunkHeader chunkHeader, int fileSize)
        {
            var chunkFileName = _config.FileNamingStrategy.GetTempFilename();
            return TFChunk.TFChunk.CreateWithHeader(chunkFileName,
                                                    chunkHeader,
                                                    fileSize,
                                                    _config.InMemDb,
                                                    _config.Unbuffered,
                                                    _config.WriteThrough,
                                                    _config.InitialReaderCount,
                                                    _config.ReduceFileCachePressure);
        }

        public TFChunk.TFChunk AddNewChunk()
        {
            lock (_chunksLocker)
            {
                var chunkNumber = _chunksCount;
                var chunkName = _config.FileNamingStrategy.GetFilenameFor(chunkNumber, 0);
                var chunk = TFChunk.TFChunk.CreateNew(chunkName,
                                                      _config.ChunkSize,
                                                      chunkNumber,
                                                      chunkNumber,
                                                      isScavenged: false,
                                                      inMem: _config.InMemDb,
                                                      unbuffered: _config.Unbuffered,
                                                      writethrough: _config.WriteThrough,
                                                      initialReaderCount: _config.InitialReaderCount,
                                                      reduceFileCachePressure: _config.ReduceFileCachePressure);
                AddChunk(chunk);
                return chunk;
            }
        }

        public TFChunk.TFChunk AddNewChunk(ChunkHeader chunkHeader, int fileSize)
        {
            if (chunkHeader is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.chunkHeader); }
            if ((uint)(fileSize - 1) >= Consts.TooBigOrNegative) { ThrowHelper.ThrowArgumentOutOfRangeException_Positive(ExceptionArgument.fileSize); }

            lock (_chunksLocker)
            {
                if (chunkHeader.ChunkStartNumber != _chunksCount)
                {
                    ThrowHelper.ThrowException_ReceivedRequestToCreateAnewOngoingChunk(chunkHeader.ChunkStartNumber, chunkHeader.ChunkEndNumber, _chunksCount);
                }

                var chunkName = _config.FileNamingStrategy.GetFilenameFor(chunkHeader.ChunkStartNumber, 0);
                var chunk = TFChunk.TFChunk.CreateWithHeader(chunkName,
                                                             chunkHeader,
                                                             fileSize,
                                                             _config.InMemDb,
                                                             unbuffered: _config.Unbuffered,
                                                             writethrough: _config.WriteThrough,
                                                             initialReaderCount: _config.InitialReaderCount,
                                                             reduceFileCachePressure: _config.ReduceFileCachePressure);
                AddChunk(chunk);
                return chunk;
            }
        }

        public void AddChunk(TFChunk.TFChunk chunk)
        {
            if (chunk is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.chunk); }

            lock (_chunksLocker)
            {
                for (int i = chunk.ChunkHeader.ChunkStartNumber; i <= chunk.ChunkHeader.ChunkEndNumber; ++i)
                {
                    _chunks[i] = chunk;
                }
                _chunksCount = chunk.ChunkHeader.ChunkEndNumber + 1;

                TryCacheChunk(chunk);
            }
        }

        public TFChunk.TFChunk SwitchChunk(TFChunk.TFChunk chunk, bool verifyHash, bool removeChunksWithGreaterNumbers)
        {
            if (chunk is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.chunk); }
            if (!chunk.IsReadOnly)
            {
                ThrowHelper.ThrowArgumentException_PassedTFChunkIsNotCompleted(chunk);
            }

            var chunkHeader = chunk.ChunkHeader;
            var oldFileName = chunk.FileName;

            var infoEnabled = Log.IsInformationLevelEnabled();
            if (infoEnabled) { Log.SwitchingChunk(chunkHeader.ChunkStartNumber, chunkHeader.ChunkEndNumber, oldFileName); }
            TFChunk.TFChunk newChunk;

            if (_config.InMemDb)
            {
                newChunk = chunk;
            }
            else
            {
                chunk.Dispose();
                try
                {
                    chunk.WaitForDestroy(0); // should happen immediately
                }
                catch (TimeoutException exc)
                {
                    ThrowHelper.ThrowException_TheChunkThatIsBeingSwitched(chunk, exc);
                }
                var newFileName = _config.FileNamingStrategy.DetermineBestVersionFilenameFor(chunkHeader.ChunkStartNumber);
                if (infoEnabled) Log.ChunkFileWillBeMovedToFile(oldFileName, newFileName);
                try
                {
                    File.Move(oldFileName, newFileName);
                }
                catch (IOException)
                {
                    ProcessUtil.PrintWhoIsLocking(oldFileName, Log);
                    ProcessUtil.PrintWhoIsLocking(newFileName, Log);
                    throw;
                }
                newChunk = TFChunk.TFChunk.FromCompletedFile(newFileName, verifyHash, _config.Unbuffered, _config.InitialReaderCount, _config.OptimizeReadSideCache, _config.ReduceFileCachePressure);
            }

            lock (_chunksLocker)
            {
                if (!ReplaceChunksWith(newChunk, "Old"))
                {
                    if (infoEnabled) Log.ChunkWillBeNotSwitchedMarkingForRemove(newChunk);
                    newChunk.MarkForDeletion();
                }

                if (removeChunksWithGreaterNumbers)
                {
                    var oldChunksCount = _chunksCount;
                    _chunksCount = newChunk.ChunkHeader.ChunkEndNumber + 1;
                    RemoveChunks(chunkHeader.ChunkEndNumber + 1, oldChunksCount - 1, "Excessive");
                    if (_chunks[_chunksCount] is object)
                    {
                        ThrowHelper.ThrowException_ExcessiveChunkFoundAfterRawReplicationSwitch(_chunksCount);
                    }
                }

                TryCacheChunk(newChunk);
                return newChunk;
            }
        }

        private bool ReplaceChunksWith(TFChunk.TFChunk newChunk, string chunkExplanation)
        {
            var chunkStartNumber = newChunk.ChunkHeader.ChunkStartNumber;
            var chunkEndNumber = newChunk.ChunkHeader.ChunkEndNumber;
            for (int i = chunkStartNumber; i <= chunkEndNumber;)
            {
                var chunk = _chunks[i];
                if (chunk is object)
                {
                    var chunkHeader = chunk.ChunkHeader;
                    if (chunkHeader.ChunkStartNumber < chunkStartNumber || chunkHeader.ChunkEndNumber > chunkEndNumber)
                    {
                        return false;
                    }

                    i = chunkHeader.ChunkEndNumber + 1;
                }
                else
                {
                    //Cover the case of initial replication of merged chunks where they were never set
                    // in the map in the first place.
                    i += 1;
                }
            }

            var infoEnabled = Log.IsInformationLevelEnabled();
            TFChunk.TFChunk previousRemovedChunk = null;
            for (int i = chunkStartNumber; i <= chunkEndNumber; i += 1)
            {
                var oldChunk = Interlocked.Exchange(ref _chunks[i], newChunk);
                if (!ReferenceEquals(previousRemovedChunk, oldChunk))
                {
                    // Once we've swapped all entries for the previousRemovedChunk we can safely delete it.
                    if (previousRemovedChunk is object)
                    {
                        previousRemovedChunk.MarkForDeletion();
                        if (infoEnabled) Log.ChunkIsMarkedForDeletion(chunkExplanation, previousRemovedChunk);
                    }

                    previousRemovedChunk = oldChunk;
                }
            }

            if (previousRemovedChunk is object)
            {
                // Delete the last chunk swapped out now it's fully replaced.
                previousRemovedChunk.MarkForDeletion();
                if (infoEnabled) Log.ChunkIsMarkedForDeletion(chunkExplanation, previousRemovedChunk);
            }

            return true;
        }

        private void RemoveChunks(int chunkStartNumber, int chunkEndNumber, string chunkExplanation)
        {
            TFChunk.TFChunk lastRemovedChunk = null;
            var infoEnabled = Log.IsInformationLevelEnabled();
            for (int i = chunkStartNumber; i <= chunkEndNumber; i += 1)
            {
                var oldChunk = Interlocked.Exchange(ref _chunks[i], null);
                if (oldChunk is object && !ReferenceEquals(lastRemovedChunk, oldChunk))
                {
                    oldChunk.MarkForDeletion();
                    if (infoEnabled) Log.ChunkIsMarkedForDeletion(chunkExplanation, oldChunk);
                }
                lastRemovedChunk = oldChunk;
            }
        }

        private void TryCacheChunk(TFChunk.TFChunk chunk)
        {
            if (!_cachingEnabled) return;

            Interlocked.Increment(ref _backgroundPassesRemaining);
            if (0u >= (uint)Interlocked.CompareExchange(ref _backgroundRunning, 1, 0))
            {
                ThreadPoolScheduler.Schedule(BackgroundCachingProcess, (object)null);
            }

            if (!chunk.IsReadOnly && chunk.ChunkHeader.ChunkSize + ChunkHeader.Size + ChunkFooter.Size <= _config.MaxChunksCacheSize)
            {
                chunk.CacheInMemory();
            }
        }

        public TFChunk.TFChunk GetChunkFor(long logPosition)
        {
            var chunkNum = (int)(logPosition / _config.ChunkSize);
            if ((uint)chunkNum >= _chunksCount /*chunkNum < 0 || chunkNum >= _chunksCount*/)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException_LogPositionDoesNotHaveCorrespondingChunkInDB(logPosition);
            }

            var chunk = _chunks[chunkNum];
            if (chunk is null)
            {
                ThrowHelper.ThrowException_RequestedChunkForLogPositionWhichIsNotPresentInTFChunkManager(logPosition);
            }

            return chunk;
        }

        public TFChunk.TFChunk GetChunk(int chunkNum)
        {
            if ((uint)chunkNum >= _chunksCount /*chunkNum < 0 || chunkNum >= _chunksCount*/)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException_ChunkIsNotPresentInDB(chunkNum);
            }

            var chunk = _chunks[chunkNum];
            if (chunk is null)
            {
                ThrowHelper.ThrowException_RequestedChunkWhichIsNotPresentInTFChunkManager(chunkNum);
            }

            return chunk;
        }

        public TFChunk.TFChunk GetChunkForOrDefault(string path)
        {
            return _chunks is object ? _chunks.FirstOrDefault(c => c is object && c.FileName == path) : null;
        }

        public void Dispose()
        {
            lock (_chunksLocker)
            {
                for (int i = 0; i < _chunksCount; ++i)
                {
                    if (_chunks[i] is object) { _chunks[i].Dispose(); }
                }
            }
        }
    }
}
