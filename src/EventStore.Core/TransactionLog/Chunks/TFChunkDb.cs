using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using DotNetty.Common;
using EventStore.Common.Utils;
using EventStore.Core.Exceptions;
using Microsoft.Extensions.Logging;

namespace EventStore.Core.TransactionLog.Chunks
{
    public class TFChunkDb : IDisposable
    {
        private static readonly ILogger Log = TraceLogger.GetLogger<TFChunkDb>();

        public readonly TFChunkDbConfig Config;
        public readonly TFChunkManager Manager;

        public TFChunkDb(TFChunkDbConfig config)
        {
            if (config is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.config); }

            Config = config;
            Manager = new TFChunkManager(Config);
        }

        readonly struct ChunkInfo
        {
            public readonly int ChunkStartNumber;
            public readonly string ChunkFileName;
            public ChunkInfo(string chunkFileName, int chunkStartNumber) { ChunkFileName = chunkFileName; ChunkStartNumber = chunkStartNumber; }
        }

        IEnumerable<ChunkInfo> GetAllLatestChunkVersions(long checkpoint)
        {
            var lastChunkNum = (int)(checkpoint / Config.ChunkSize);

            for (int chunkNum = 0; chunkNum < lastChunkNum;)
            {
                var versions = Config.FileNamingStrategy.GetAllVersionsFor(chunkNum);
                if (0u >= (uint)versions.Length)
                    ThrowHelper.ThrowCorruptDatabaseException_ChunkNotFound(Config, chunkNum);

                var chunkFileName = versions[0];

                var chunkHeader = ReadChunkHeader(chunkFileName);

                yield return new ChunkInfo(chunkFileName, chunkNum);

                chunkNum = chunkHeader.ChunkEndNumber + 1;
            }
        }

        public void Open(bool verifyHash = true, bool readOnly = false, int threads = 1)
        {
            if ((uint)(threads - 1) >= Consts.TooBigOrNegative) { ThrowHelper.ThrowArgumentOutOfRangeException_Positive(ExceptionArgument.threads); }

            ValidateReaderChecksumsMustBeLess(Config);
            var checkpoint = Config.WriterCheckpoint.Read();

            if (Config.InMemDb)
            {
                Manager.AddNewChunk();
                return;
            }

            var lastChunkNum = (int)(checkpoint / Config.ChunkSize);
            var lastChunkVersions = Config.FileNamingStrategy.GetAllVersionsFor(lastChunkNum);

            try
            {
                Parallel.ForEach(GetAllLatestChunkVersions(checkpoint),
                    new ParallelOptions { MaxDegreeOfParallelism = threads }, LocalAction);
                void LocalAction(ChunkInfo chunkInfo)
                {
                    TFChunk.TFChunk chunk;
                    if (0u >= (uint)lastChunkVersions.Length && (chunkInfo.ChunkStartNumber + 1) * (long)Config.ChunkSize == checkpoint)
                    {
                        // The situation where the logical data size is exactly divisible by ChunkSize,
                        // so it might happen that we have checkpoint indicating one more chunk should exist,
                        // but the actual last chunk is (lastChunkNum-1) one and it could be not completed yet -- perfectly valid situation.
                        var footer = ReadChunkFooter(chunkInfo.ChunkFileName);
                        if (footer.IsCompleted)
                            chunk = TFChunk.TFChunk.FromCompletedFile(chunkInfo.ChunkFileName, verifyHash: false, unbufferedRead: Config.Unbuffered,
                                initialReaderCount: Config.InitialReaderCount, optimizeReadSideCache: Config.OptimizeReadSideCache,
                                reduceFileCachePressure: Config.ReduceFileCachePressure);
                        else
                        {
                            chunk = TFChunk.TFChunk.FromOngoingFile(chunkInfo.ChunkFileName, Config.ChunkSize, checkSize: false,
                                unbuffered: Config.Unbuffered,
                                writethrough: Config.WriteThrough, initialReaderCount: Config.InitialReaderCount,
                                reduceFileCachePressure: Config.ReduceFileCachePressure);
                            // chunk is full with data, we should complete it right here
                            if (!readOnly)
                                chunk.Complete();
                        }
                    }
                    else
                    {
                        chunk = TFChunk.TFChunk.FromCompletedFile(chunkInfo.ChunkFileName, verifyHash: false, unbufferedRead: Config.Unbuffered,
                            initialReaderCount: Config.InitialReaderCount, optimizeReadSideCache: Config.OptimizeReadSideCache,
                            reduceFileCachePressure: Config.ReduceFileCachePressure);
                    }

                    // This call is theadsafe.
                    Manager.AddChunk(chunk);
                }
            }
            catch (AggregateException aggEx)
            {
                // We only really care that *something* is wrong - throw the first inner exception. 
                throw aggEx.InnerException;
            }

            if (0u >= (uint)lastChunkVersions.Length)
            {
                var onBoundary = checkpoint == (Config.ChunkSize * (long)lastChunkNum);
                if (!onBoundary)
                    ThrowHelper.ThrowCorruptDatabaseException_ChunkNotFound(Config, lastChunkNum);
                if (!readOnly)
                    Manager.AddNewChunk();
            }
            else
            {
                var chunkFileName = lastChunkVersions[0];
                var chunkHeader = ReadChunkHeader(chunkFileName);
                var chunkLocalPos = chunkHeader.GetLocalLogPosition(checkpoint);
                if (chunkHeader.IsScavenged)
                {
                    var lastChunk = TFChunk.TFChunk.FromCompletedFile(chunkFileName, verifyHash: false, unbufferedRead: Config.Unbuffered,
                        initialReaderCount: Config.InitialReaderCount, optimizeReadSideCache: Config.OptimizeReadSideCache,
                        reduceFileCachePressure: Config.ReduceFileCachePressure);
                    if (lastChunk.ChunkFooter.LogicalDataSize != chunkLocalPos)
                    {
                        lastChunk.Dispose();
                        ThrowHelper.ThrowCorruptDatabaseException_ChunkIsCorrupted(chunkFileName, chunkLocalPos, lastChunk, checkpoint);
                    }

                    Manager.AddChunk(lastChunk);
                    if (!readOnly)
                    {
                        if (Log.IsInformationLevelEnabled())
                        {
                            Log.MovingWritercheckpointAsItPointsToTheScavengedChunk(checkpoint, lastChunk.ChunkHeader.ChunkEndPosition);
                        }
                        Config.WriterCheckpoint.Write(lastChunk.ChunkHeader.ChunkEndPosition);
                        Config.WriterCheckpoint.Flush();
                        Manager.AddNewChunk();
                    }
                }
                else
                {
                    var lastChunk = TFChunk.TFChunk.FromOngoingFile(chunkFileName, (int)chunkLocalPos, checkSize: false, unbuffered: Config.Unbuffered,
                        writethrough: Config.WriteThrough, initialReaderCount: Config.InitialReaderCount,
                        reduceFileCachePressure: Config.ReduceFileCachePressure);
                    Manager.AddChunk(lastChunk);
                }
            }

            EnsureNoExcessiveChunks(lastChunkNum);

            if (!readOnly)
            {
                RemoveOldChunksVersions(lastChunkNum);
                CleanUpTempFiles();
            }

            if (verifyHash && lastChunkNum > 0)
            {
                var preLastChunk = Manager.GetChunk(lastChunkNum - 1);
                var lastBgChunkNum = preLastChunk.ChunkHeader.ChunkStartNumber;
                ThreadPoolScheduler.Schedule(_ =>
                {
                    for (int chunkNum = lastBgChunkNum; chunkNum >= 0;)
                    {
                        var chunk = Manager.GetChunk(chunkNum);
                        try
                        {
                            chunk.VerifyFileHash();
                        }
                        catch (FileBeingDeletedException exc)
                        {
                            if (Log.IsTraceLevelEnabled()) Log.ExceptionWasThrownWhileDoingBackgroundValidationOfChunk(exc, chunk);
                        }
                        catch (Exception exc)
                        {
                            VerificationOfChunkFailed(exc, chunk);
                            return;
                        }

                        chunkNum = chunk.ChunkHeader.ChunkStartNumber - 1;
                    }
                }, (object)null);
            }

            Manager.EnableCaching();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void VerificationOfChunkFailed(Exception exc, TFChunk.TFChunk chunk)
        {
            var msg = string.Format("Verification of chunk {0} failed, terminating server...", chunk);
            Log.LogCritical(exc, msg);
            Application.Exit(ExitCode.Error, msg);
        }

        private void ValidateReaderChecksumsMustBeLess(TFChunkDbConfig config)
        {
            var current = config.WriterCheckpoint.Read();
            foreach (var checkpoint in new[] { config.ChaserCheckpoint, config.EpochCheckpoint })
            {
                if (checkpoint.Read() > current)
                    ThrowHelper.ThrowCorruptDatabaseException_ValidateReaderChecksumsMustBeLess(checkpoint.Name);
            }
        }

        private static ChunkHeader ReadChunkHeader(string chunkFileName)
        {
            ChunkHeader chunkHeader;
            using (var fs = new FileStream(chunkFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                if ((ulong)fs.Length < ChunkFooter.Size + ChunkHeader.Size)
                {
                    ThrowHelper.ThrowCorruptDatabaseException_ChunkFileIsBad(chunkFileName, fs.Length);
                }

                chunkHeader = ChunkHeader.FromStream(fs);
            }

            return chunkHeader;
        }

        private static ChunkFooter ReadChunkFooter(string chunkFileName)
        {
            ChunkFooter chunkFooter;
            using (var fs = new FileStream(chunkFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                if ((ulong)fs.Length < ChunkFooter.Size + ChunkHeader.Size)
                {
                    ThrowHelper.ThrowCorruptDatabaseException_ChunkFileIsBad(chunkFileName, fs.Length);
                }

                fs.Seek(-ChunkFooter.Size, SeekOrigin.End);
                chunkFooter = ChunkFooter.FromStream(fs);
            }

            return chunkFooter;
        }

        private void EnsureNoExcessiveChunks(int lastChunkNum)
        {
            var allowedFiles = ThreadLocalList<string>.NewInstance();
            try
            {
                int cnt = 0;
                for (int i = 0; i <= lastChunkNum; ++i)
                {
                    var files = Config.FileNamingStrategy.GetAllVersionsFor(i);
                    cnt += files.Length;
                    allowedFiles.AddRange(files);
                }

                var allFiles = Config.FileNamingStrategy.GetAllPresentFiles();
                if ((ulong)allFiles.Length != (ulong)cnt)
                {
                    ThrowHelper.ThrowCorruptDatabaseException_UnexpectedFiles(allFiles, allowedFiles);
                }
            }
            finally
            {
                allowedFiles.Return();
            }
        }

        private void RemoveOldChunksVersions(int lastChunkNum)
        {
#if DEBUG
            var traceEnabled = Log.IsTraceLevelEnabled();
#endif
            for (int chunkNum = 0; chunkNum <= lastChunkNum;)
            {
                var chunk = Manager.GetChunk(chunkNum);
                for (int i = chunk.ChunkHeader.ChunkStartNumber; i <= chunk.ChunkHeader.ChunkEndNumber; ++i)
                {
                    var files = Config.FileNamingStrategy.GetAllVersionsFor(i);
                    for (int j = (i == chunk.ChunkHeader.ChunkStartNumber ? 1 : 0); j < files.Length; ++j)
                    {
                        var filename = files[j];
#if DEBUG
                        if (traceEnabled) Log.Removing_excess_chunk_version(filename);
#endif
                        RemoveFile(filename);
                    }
                }

                chunkNum = chunk.ChunkHeader.ChunkEndNumber + 1;
            }
        }

        private void CleanUpTempFiles()
        {
            var tempFiles = Config.FileNamingStrategy.GetAllTempFiles();
#if DEBUG
            var traceEnabled = Log.IsTraceLevelEnabled();
#endif
            foreach (string tempFile in tempFiles)
            {
                try
                {
#if DEBUG
                    if (traceEnabled) Log.Deleting_temporary_file(tempFile);
#endif
                    RemoveFile(tempFile);
                }
                catch (Exception exc)
                {
                    Log.ErrorWhileTryingToDeleteRemainingTempFile(tempFile, exc);
                }
            }
        }

        private void RemoveFile(string file)
        {
            File.SetAttributes(file, FileAttributes.Normal);
            File.Delete(file);
        }

        public void Dispose()
        {
            Close();
        }

        public void Close()
        {
            if (Manager is object)
                Manager.Dispose();
            Config.WriterCheckpoint.Close();
            Config.ChaserCheckpoint.Close();
            Config.EpochCheckpoint.Close();
            Config.TruncateCheckpoint.Close();
        }
    }
}
