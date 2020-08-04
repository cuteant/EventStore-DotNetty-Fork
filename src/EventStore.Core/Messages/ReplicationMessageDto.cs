using System;
using System.Net;
using MessagePack;

namespace EventStore.Core.Messages
{
    public static class ReplicationMessageDto
    {
        [MessagePackObject]
        public sealed class PrepareAck
        {
            [Key(0)]
            public long LogPosition { get; set; }

            [Key(1)]
            public byte Flags { get; set; }

            public PrepareAck()
            {
            }

            [SerializationConstructor]
            public PrepareAck(long logPosition, byte flags)
            {
                LogPosition = logPosition;
                Flags = flags;
            }
        }

        [MessagePackObject]
        public sealed class CommitAck
        {
            [Key(0)]
            public long LogPosition { get; set; }

            [Key(1)]
            public long TransactionPosition { get; set; }

            [Key(2)]
            public long FirstEventNumber { get; set; }

            [Key(3)]
            public long LastEventNumber { get; set; }

            public CommitAck()
            {
            }

            [SerializationConstructor]
            public CommitAck(long logPosition, long transactionPosition, long firstEventNumber, long lastEventNumber)
            {
                LogPosition = logPosition;
                TransactionPosition = transactionPosition;
                FirstEventNumber = firstEventNumber;
                LastEventNumber = lastEventNumber;
            }
        }

        [MessagePackObject]
        public sealed class Epoch
        {
            [Key(0)]
            public long EpochPosition { get; set; }

            [Key(1)]
            public int EpochNumber { get; set; }

            [Key(2)]
            public Guid EpochId { get; set; }

            public Epoch()
            {
            }

            [SerializationConstructor]
            public Epoch(long epochPosition, int epochNumber, Guid epochId)
            {
                EpochPosition = epochPosition;
                EpochNumber = epochNumber;
                EpochId = epochId;
            }
        }

        [MessagePackObject]
        public sealed class SubscribeReplica
        {
            [Key(0)]
            public long LogPosition { get; set; }

            [Key(1)]
            public Guid ChunkId { get; set; }

            [Key(2)]
            public Epoch[] LastEpochs { get; set; }

            [Key(3)]
            public IPAddress Ip { get; set; }

            [Key(4)]
            public int Port { get; set; }

            [Key(5)]
            public Guid MasterId { get; set; }

            [Key(6)]
            public Guid SubscriptionId { get; set; }

            [Key(7)]
            public bool IsPromotable { get; set; }

            public SubscribeReplica()
            {
            }

            [SerializationConstructor]
            public SubscribeReplica(long logPosition, Guid chunkId, Epoch[] lastEpochs, IPAddress ip, int port,
                                    Guid masterId, Guid subscriptionId, bool isPromotable)
            {
                LogPosition = logPosition;
                ChunkId = chunkId;
                LastEpochs = lastEpochs;

                Ip = ip;
                Port = port;
                MasterId = masterId;
                SubscriptionId = subscriptionId;
                IsPromotable = isPromotable;
            }
        }

        [MessagePackObject]
        public sealed class ReplicaSubscriptionRetry
        {
            [Key(0)]
            public Guid MasterId { get; set; }

            [Key(1)]
            public Guid SubscriptionId { get; set; }

            public ReplicaSubscriptionRetry()
            {
            }

            [SerializationConstructor]
            public ReplicaSubscriptionRetry(Guid masterId, Guid subscriptionId)
            {
                //if (masterId is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.masterId); }
                //if (subscriptionId is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.subscriptionId); }
                
                MasterId = masterId;
                SubscriptionId = subscriptionId;
            }
        }

        [MessagePackObject]
        public sealed class ReplicaSubscribed
        {
            [Key(0)]
            public Guid MasterId { get; set; }

            [Key(1)]
            public Guid SubscriptionId { get; set; }

            [Key(2)]
            public long SubscriptionPosition { get; set; }

            public ReplicaSubscribed()
            {
            }

            [SerializationConstructor]
            public ReplicaSubscribed(Guid masterId, Guid subscriptionId, long subscriptionPosition)
            {
                //if (masterId is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.masterId); }
                //if (subscriptionId is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.subscriptionId); }
                if ((ulong)subscriptionPosition > Consts.TooBigOrNegativeUL) { ThrowHelper.ThrowArgumentOutOfRangeException_Nonnegative(ExceptionArgument.subscriptionPosition); }

                MasterId = masterId;
                SubscriptionId = subscriptionId;
                SubscriptionPosition = subscriptionPosition;
            }
        }

        [MessagePackObject]
        public sealed class ReplicaLogPositionAck
        {
            [Key(0)]
            public Guid SubscriptionId { get; set; }

            [Key(1)]
            public long ReplicationLogPosition { get; set; }

            public ReplicaLogPositionAck()
            {
            }

            [SerializationConstructor]
            public ReplicaLogPositionAck(Guid subscriptionId, long replicationLogPosition)
            {
                SubscriptionId = subscriptionId;
                ReplicationLogPosition = replicationLogPosition;
            }
        }

        [MessagePackObject]
        public sealed class CreateChunk
        {
            [Key(0)]
            public Guid MasterId { get; set; }

            [Key(1)]
            public Guid SubscriptionId { get; set; }

            [Key(2)]
            public byte[] ChunkHeaderBytes { get; set; }

            [Key(3)]
            public int FileSize { get; set; }

            [Key(4)]
            public bool IsCompletedChunk { get; set; }

            public CreateChunk()
            {
            }

            [SerializationConstructor]
            public CreateChunk(Guid masterId, Guid subscriptionId, byte[] chunkHeaderBytes, int fileSize, bool isCompletedChunk)
            {
                //if (masterId is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.masterId); }
                //if (subscriptionId is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.subscriptionId); }
                if (chunkHeaderBytes is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.chunkHeaderBytes); }

                MasterId = masterId;
                SubscriptionId = subscriptionId;
                ChunkHeaderBytes = chunkHeaderBytes;
                FileSize = fileSize;
                IsCompletedChunk = isCompletedChunk;
            }
        }

        [MessagePackObject]
        public sealed class RawChunkBulk
        {
            [Key(0)]
            public Guid MasterId { get; set; }

            [Key(1)]
            public Guid SubscriptionId { get; set; }

            [Key(2)]
            public int ChunkStartNumber { get; set; }

            [Key(3)]
            public int ChunkEndNumber { get; set; }

            [Key(4)]
            public int RawPosition { get; set; }

            [Key(5)]
            public byte[] RawBytes { get; set; }

            [Key(6)]
            public bool CompleteChunk { get; set; }

            public RawChunkBulk()
            {
            }

            [SerializationConstructor]
            public RawChunkBulk(Guid masterId,
                                Guid subscriptionId,
                                int chunkStartNumber,
                                int chunkEndNumber,
                                int rawPosition,
                                byte[] rawBytes,
                                bool completeChunk)
            {
                //if (masterId is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.masterId); }
                //if (subscriptionId is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.subscriptionId); }
                if (rawBytes is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.rawBytes); }
                if (0u >= (uint)rawBytes.Length) { ThrowHelper.ThrowArgumentOutOfRangeException_Positive(ExceptionArgument.rawBytes_Length); } // we should never send empty array, NEVER

                MasterId = masterId;
                SubscriptionId = subscriptionId;
                ChunkStartNumber = chunkStartNumber;
                ChunkEndNumber = chunkEndNumber;
                RawPosition = rawPosition;
                RawBytes = rawBytes;
                CompleteChunk = completeChunk;
            }
        }

        [MessagePackObject]
        public sealed class DataChunkBulk
        {
            [Key(0)]
            public Guid MasterId { get; set; }

            [Key(1)]
            public Guid SubscriptionId { get; set; }

            [Key(2)]
            public int ChunkStartNumber { get; set; }

            [Key(3)]
            public int ChunkEndNumber { get; set; }

            [Key(4)]
            public long SubscriptionPosition { get; set; }

            [Key(5)]
            public byte[] DataBytes { get; set; }

            [Key(6)]
            public bool CompleteChunk { get; set; }

            public DataChunkBulk()
            {
            }

            [SerializationConstructor]
            public DataChunkBulk(Guid masterId,
                                 Guid subscriptionId,
                                 int chunkStartNumber,
                                 int chunkEndNumber,
                                 long subscriptionPosition,
                                 byte[] dataBytes,
                                 bool completeChunk)
            {
                //if (masterId is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.masterId); }
                //if (subscriptionId is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.subscriptionId); }
                if (dataBytes is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.dataBytes); }
                if (0u >= (uint)dataBytes.Length) { ThrowHelper.ThrowArgumentOutOfRangeException_Nonnegative(ExceptionArgument.dataBytes_Length); } // we CAN send empty dataBytes array here, unlike as with completed chunks

                MasterId = masterId;
                SubscriptionId = subscriptionId;
                ChunkStartNumber = chunkStartNumber;
                ChunkEndNumber = chunkEndNumber;
                SubscriptionPosition = subscriptionPosition;
                DataBytes = dataBytes;
                CompleteChunk = completeChunk;
            }
        }

        [MessagePackObject]
        public sealed class SlaveAssignment
        {
            [Key(0)]
            public Guid MasterId { get; set; }

            [Key(1)]
            public Guid SubscriptionId { get; set; }

            public SlaveAssignment()
            {
            }

            [SerializationConstructor]
            public SlaveAssignment(Guid masterId, Guid subscriptionId)
            {
                MasterId = masterId;
                SubscriptionId = subscriptionId;
            }
        }

        [MessagePackObject]
        public sealed class CloneAssignment
        {
            [Key(0)]
            public Guid MasterId { get; set; }

            [Key(1)]
            public Guid SubscriptionId { get; set; }

            public CloneAssignment()
            {
            }

            [SerializationConstructor]
            public CloneAssignment(Guid masterId, Guid subscriptionId)
            {
                MasterId = masterId;
                SubscriptionId = subscriptionId;
            }
        }
    }
}
