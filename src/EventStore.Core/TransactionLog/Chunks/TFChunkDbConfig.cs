using EventStore.Common.Utils;
using EventStore.Core.TransactionLog.Checkpoint;
using EventStore.Core.TransactionLog.FileNamingStrategy;

namespace EventStore.Core.TransactionLog.Chunks
{
    public class TFChunkDbConfig
    {
        public readonly string Path;
        public readonly int ChunkSize;
        public readonly long MaxChunksCacheSize;
        public readonly ICheckpoint WriterCheckpoint;
        public readonly ICheckpoint ChaserCheckpoint;
        public readonly ICheckpoint EpochCheckpoint;
        public readonly ICheckpoint TruncateCheckpoint;
        public readonly ICheckpoint ReplicationCheckpoint;
        public readonly IFileNamingStrategy FileNamingStrategy;
        public readonly bool InMemDb;
        public readonly bool Unbuffered;
        public readonly bool WriteThrough;
        public readonly int InitialReaderCount;
        public readonly bool OptimizeReadSideCache;
        public readonly bool ReduceFileCachePressure;

        public TFChunkDbConfig(string path, 
                               IFileNamingStrategy fileNamingStrategy, 
                               int chunkSize,
                               long maxChunksCacheSize,
                               ICheckpoint writerCheckpoint, 
                               ICheckpoint chaserCheckpoint,
                               ICheckpoint epochCheckpoint,
                               ICheckpoint truncateCheckpoint,
                               ICheckpoint replicationCheckpoint,
                               int initialReaderCount,
                               bool inMemDb = false,
                               bool unbuffered = false,
                               bool writethrough = false,
                               bool optimizeReadSideCache = false,
                               bool reduceFileCachePressure = false)
        {
            if (string.IsNullOrEmpty(path)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.path); }
            if (null == fileNamingStrategy) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.fileNamingStrategy); }
            if (chunkSize <= 0) { ThrowHelper.ThrowArgumentOutOfRangeException_Positive(ExceptionArgument.chunkSize); }
            if (maxChunksCacheSize < 0) { ThrowHelper.ThrowArgumentOutOfRangeException_Nonnegative(ExceptionArgument.maxChunksCacheSize); }
            if (null == writerCheckpoint) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.writerCheckpoint); }
            if (null == chaserCheckpoint) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.chaserCheckpoint); }
            if (null == epochCheckpoint) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.epochCheckpoint); }
            if (null == truncateCheckpoint) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.truncateCheckpoint); }
            if (null == replicationCheckpoint) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.replicationCheckpoint); }
            if (initialReaderCount <= 0) { ThrowHelper.ThrowArgumentOutOfRangeException_Positive(ExceptionArgument.initialReaderCount); }

            Path = path;
            ChunkSize = chunkSize;
            MaxChunksCacheSize = maxChunksCacheSize;
            WriterCheckpoint = writerCheckpoint;
            ChaserCheckpoint = chaserCheckpoint;
            EpochCheckpoint = epochCheckpoint;
            TruncateCheckpoint = truncateCheckpoint;
            ReplicationCheckpoint = replicationCheckpoint;
            FileNamingStrategy = fileNamingStrategy;
            InMemDb = inMemDb;
            Unbuffered = unbuffered;
            WriteThrough = writethrough;
            InitialReaderCount = initialReaderCount;
            OptimizeReadSideCache = optimizeReadSideCache;
            ReduceFileCachePressure = reduceFileCachePressure;
        }
    }
}