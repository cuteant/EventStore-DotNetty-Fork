using EventStore.Common.Utils;
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
            public byte[] EpochId { get; set; }

            public Epoch()
            {
            }

            [SerializationConstructor]
            public Epoch(long epochPosition, int epochNumber, byte[] epochId)
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
            public byte[] ChunkId { get; set; }

            [Key(2)]
            public Epoch[] LastEpochs { get; set; }

            [Key(3)]
            public byte[] Ip { get; set; }

            [Key(4)]
            public int Port { get; set; }

            [Key(5)]
            public byte[] MasterId { get; set; }

            [Key(6)]
            public byte[] SubscriptionId { get; set; }

            [Key(7)]
            public bool IsPromotable { get; set; }

            public SubscribeReplica()
            {
            }

            [SerializationConstructor]
            public SubscribeReplica(long logPosition, byte[] chunkId, Epoch[] lastEpochs, byte[] ip, int port,
                                    byte[] masterId, byte[] subscriptionId, bool isPromotable)
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
            public byte[] MasterId { get; set; }

            [Key(1)]
            public byte[] SubscriptionId { get; set; }

            public ReplicaSubscriptionRetry()
            {
            }

            [SerializationConstructor]
            public ReplicaSubscriptionRetry(byte[] masterId, byte[] subscriptionId)
            {
                Ensure.NotNull(masterId, "masterId");
                Ensure.NotNull(subscriptionId, "subscriptionId");
                
                MasterId = masterId;
                SubscriptionId = subscriptionId;
            }
        }

        [MessagePackObject]
        public sealed class ReplicaSubscribed
        {
            [Key(0)]
            public byte[] MasterId { get; set; }

            [Key(1)]
            public byte[] SubscriptionId { get; set; }

            [Key(2)]
            public long SubscriptionPosition { get; set; }

            public ReplicaSubscribed()
            {
            }

            [SerializationConstructor]
            public ReplicaSubscribed(byte[] masterId, byte[] subscriptionId, long subscriptionPosition)
            {
                Ensure.NotNull(masterId, "masterId");
                Ensure.NotNull(subscriptionId, "subscriptionId");
                Ensure.Nonnegative(subscriptionPosition, "subscriptionPosition");

                MasterId = masterId;
                SubscriptionId = subscriptionId;
                SubscriptionPosition = subscriptionPosition;
            }
        }

        [MessagePackObject]
        public sealed class ReplicaLogPositionAck
        {
            [Key(0)]
            public byte[] SubscriptionId { get; set; }

            [Key(1)]
            public long ReplicationLogPosition { get; set; }

            public ReplicaLogPositionAck()
            {
            }

            [SerializationConstructor]
            public ReplicaLogPositionAck(byte[] subscriptionId, long replicationLogPosition)
            {
                SubscriptionId = subscriptionId;
                ReplicationLogPosition = replicationLogPosition;
            }
        }

        [MessagePackObject]
        public sealed class CreateChunk
        {
            [Key(0)]
            public byte[] MasterId { get; set; }

            [Key(1)]
            public byte[] SubscriptionId { get; set; }

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
            public CreateChunk(byte[] masterId, byte[] subscriptionId, byte[] chunkHeaderBytes, int fileSize, bool isCompletedChunk)
            {
                Ensure.NotNull(masterId, "masterId");
                Ensure.NotNull(subscriptionId, "subscriptionId");
                Ensure.NotNull(chunkHeaderBytes, "chunkHeaderBytes");

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
            public byte[] MasterId { get; set; }

            [Key(1)]
            public byte[] SubscriptionId { get; set; }

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
            public RawChunkBulk(byte[] masterId,
                                byte[] subscriptionId,
                                int chunkStartNumber,
                                int chunkEndNumber,
                                int rawPosition,
                                byte[] rawBytes,
                                bool completeChunk)
            {
                Ensure.NotNull(masterId, "masterId");
                Ensure.NotNull(subscriptionId, "subscriptionId");
                Ensure.NotNull(rawBytes, "rawBytes");
                Ensure.Positive(rawBytes.Length, "rawBytes.Length"); // we should never send empty array, NEVER

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
            public byte[] MasterId { get; set; }

            [Key(1)]
            public byte[] SubscriptionId { get; set; }

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
            public DataChunkBulk(byte[] masterId,
                                 byte[] subscriptionId,
                                 int chunkStartNumber,
                                 int chunkEndNumber,
                                 long subscriptionPosition,
                                 byte[] dataBytes,
                                 bool completeChunk)
            {
                Ensure.NotNull(masterId, "masterId");
                Ensure.NotNull(subscriptionId, "subscriptionId");
                Ensure.NotNull(dataBytes, "rawBytes");
                Ensure.Nonnegative(dataBytes.Length, "dataBytes.Length"); // we CAN send empty dataBytes array here, unlike as with completed chunks

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
            public byte[] MasterId { get; set; }

            [Key(1)]
            public byte[] SubscriptionId { get; set; }

            public SlaveAssignment()
            {
            }

            [SerializationConstructor]
            public SlaveAssignment(byte[] masterId, byte[] subscriptionId)
            {
                MasterId = masterId;
                SubscriptionId = subscriptionId;
            }
        }

        [MessagePackObject]
        public sealed class CloneAssignment
        {
            [Key(0)]
            public byte[] MasterId { get; set; }

            [Key(1)]
            public byte[] SubscriptionId { get; set; }

            public CloneAssignment()
            {
            }

            [SerializationConstructor]
            public CloneAssignment(byte[] masterId, byte[] subscriptionId)
            {
                MasterId = masterId;
                SubscriptionId = subscriptionId;
            }
        }
    }
}
