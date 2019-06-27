using System;
using System.IO;
using System.Linq;
using System.Net;
using EventStore.Common.Utils;
using EventStore.Core.Data;
using EventStore.Core.Messages;
using EventStore.Core.Messaging;
using EventStore.Core.TransactionLog.Chunks;
using EventStore.Core.TransactionLog.LogRecords;
using EventStore.Transport.Tcp.Messages;

namespace EventStore.Core.Services.Transport.Tcp
{
    public class InternalTcpDispatcher : ClientTcpDispatcher
    {
        public InternalTcpDispatcher()
        {
            AddUnwrapper(TcpCommand.PrepareAck, UnwrapPrepareAck, ClientVersion.V2);
            AddWrapper<StorageMessage.PrepareAck>(WrapPrepareAck, ClientVersion.V2);
            AddUnwrapper(TcpCommand.CommitAck, UnwrapCommitAck, ClientVersion.V2);
            AddWrapper<StorageMessage.CommitAck>(WrapCommitAck, ClientVersion.V2);

            AddUnwrapper(TcpCommand.SubscribeReplica, UnwrapReplicaSubscriptionRequest, ClientVersion.V2);
            AddWrapper<ReplicationMessage.SubscribeReplica>(WrapSubscribeReplica, ClientVersion.V2);
            AddUnwrapper(TcpCommand.ReplicaLogPositionAck, UnwrapReplicaLogPositionAck, ClientVersion.V2);
            AddWrapper<ReplicationMessage.AckLogPosition>(WrapAckLogPosition, ClientVersion.V2);
            AddUnwrapper(TcpCommand.CreateChunk, UnwrapCreateChunk, ClientVersion.V2);
            AddWrapper<ReplicationMessage.CreateChunk>(WrapCreateChunk, ClientVersion.V2);
            AddUnwrapper(TcpCommand.RawChunkBulk, UnwrapRawChunkBulk, ClientVersion.V2);
            AddWrapper<ReplicationMessage.RawChunkBulk>(WrapRawChunkBulk, ClientVersion.V2);
            AddUnwrapper(TcpCommand.DataChunkBulk, UnwrapDataChunkBulk, ClientVersion.V2);
            AddWrapper<ReplicationMessage.DataChunkBulk>(WrapDataChunkBulk, ClientVersion.V2);
            AddUnwrapper(TcpCommand.ReplicaSubscriptionRetry, UnwrapReplicaSubscriptionRetry, ClientVersion.V2);
            AddWrapper<ReplicationMessage.ReplicaSubscriptionRetry>(WrapReplicaSubscriptionRetry, ClientVersion.V2);
            AddUnwrapper(TcpCommand.ReplicaSubscribed, UnwrapReplicaSubscribed, ClientVersion.V2);
            AddWrapper<ReplicationMessage.ReplicaSubscribed>(WrapReplicaSubscribed, ClientVersion.V2);

            AddUnwrapper(TcpCommand.SlaveAssignment, UnwrapSlaveAssignment, ClientVersion.V2);
            AddWrapper<ReplicationMessage.SlaveAssignment>(WrapSlaveAssignment, ClientVersion.V2);
            AddUnwrapper(TcpCommand.CloneAssignment, UnwrapCloneAssignment, ClientVersion.V2);
            AddWrapper<ReplicationMessage.CloneAssignment>(WrapCloneAssignment, ClientVersion.V2);
        }

        private static TcpPackage WrapPrepareAck(StorageMessage.PrepareAck msg)
        {
            var dto = new ReplicationMessageDto.PrepareAck(msg.LogPosition, (byte)msg.Flags);
            return new TcpPackage(TcpCommand.PrepareAck, msg.CorrelationId, dto.Serialize());
        }

        private static StorageMessage.PrepareAck UnwrapPrepareAck(TcpPackage package, IEnvelope envelope)
        {
            var dto = package.Data.Deserialize<ReplicationMessageDto.PrepareAck>();
            return new StorageMessage.PrepareAck(package.CorrelationId, dto.LogPosition, (PrepareFlags)dto.Flags);
        }

        private static TcpPackage WrapCommitAck(StorageMessage.CommitAck msg)
        {
            var dto = new ReplicationMessageDto.CommitAck(msg.LogPosition, msg.TransactionPosition, msg.FirstEventNumber, msg.LastEventNumber);
            return new TcpPackage(TcpCommand.CommitAck, msg.CorrelationId, dto.Serialize());
        }

        private static StorageMessage.CommitAck UnwrapCommitAck(TcpPackage package, IEnvelope envelope)
        {
            var dto = package.Data.Deserialize<ReplicationMessageDto.CommitAck>();
            return new StorageMessage.CommitAck(package.CorrelationId,
                                                dto.LogPosition,
                                                dto.TransactionPosition,
                                                dto.FirstEventNumber,
                                                dto.LastEventNumber);
        }

        private static ReplicationMessage.ReplicaSubscriptionRequest UnwrapReplicaSubscriptionRequest(TcpPackage package, IEnvelope envelope, TcpConnectionManager connection)
        {
            var dto = package.Data.Deserialize<ReplicationMessageDto.SubscribeReplica>();
            var vnodeTcpEndPoint = new IPEndPoint(dto.Ip, dto.Port);
            var lastEpochs = dto.LastEpochs.Safe().Select(x => new Epoch(x.EpochPosition, x.EpochNumber, x.EpochId)).ToArray();
            return new ReplicationMessage.ReplicaSubscriptionRequest(package.CorrelationId,
                                                                     envelope,
                                                                     connection,
                                                                     dto.LogPosition,
                                                                     dto.ChunkId,
                                                                     lastEpochs,
                                                                     vnodeTcpEndPoint,
                                                                     dto.MasterId,
                                                                     dto.SubscriptionId,
                                                                     dto.IsPromotable);
        }

        private static TcpPackage WrapSubscribeReplica(ReplicationMessage.SubscribeReplica msg)
        {
            var epochs = msg.LastEpochs.Select(x => new ReplicationMessageDto.Epoch(x.EpochPosition, x.EpochNumber, x.EpochId)).ToArray();
            var dto = new ReplicationMessageDto.SubscribeReplica(msg.LogPosition,
                                                                 msg.ChunkId,
                                                                 epochs,
                                                                 msg.ReplicaEndPoint.Address,
                                                                 msg.ReplicaEndPoint.Port,
                                                                 msg.MasterId,
                                                                 msg.SubscriptionId,
                                                                 msg.IsPromotable);
            return new TcpPackage(TcpCommand.SubscribeReplica, Guid.NewGuid(), dto.Serialize());
        }

        private static ReplicationMessage.ReplicaLogPositionAck UnwrapReplicaLogPositionAck(TcpPackage package, IEnvelope envelope, TcpConnectionManager connection)
        {
            var dto = package.Data.Deserialize<ReplicationMessageDto.ReplicaLogPositionAck>();
            return new ReplicationMessage.ReplicaLogPositionAck(dto.SubscriptionId, dto.ReplicationLogPosition);
        }

        private static TcpPackage WrapAckLogPosition(ReplicationMessage.AckLogPosition msg)
        {
            var dto = new ReplicationMessageDto.ReplicaLogPositionAck(msg.SubscriptionId, msg.ReplicationLogPosition);
            return new TcpPackage(TcpCommand.ReplicaLogPositionAck, Guid.NewGuid(), dto.Serialize());
        }

        private static ReplicationMessage.CreateChunk UnwrapCreateChunk(TcpPackage package, IEnvelope envelope)
        {
            var dto = package.Data.Deserialize<ReplicationMessageDto.CreateChunk>();
            ChunkHeader chunkHeader;
            using (var memStream = new MemoryStream(dto.ChunkHeaderBytes))
            {
                chunkHeader = ChunkHeader.FromStream(memStream);
            }
            return new ReplicationMessage.CreateChunk(dto.MasterId, dto.SubscriptionId, chunkHeader, dto.FileSize, dto.IsCompletedChunk);
        }

        private static TcpPackage WrapCreateChunk(ReplicationMessage.CreateChunk msg)
        {
            var dto = new ReplicationMessageDto.CreateChunk(msg.MasterId,
                                                            msg.SubscriptionId,
                                                            msg.ChunkHeader.AsByteArray(),
                                                            msg.FileSize,
                                                            msg.IsCompletedChunk);
            return new TcpPackage(TcpCommand.CreateChunk, Guid.NewGuid(), dto.Serialize());
        }

        private static ReplicationMessage.RawChunkBulk UnwrapRawChunkBulk(TcpPackage package, IEnvelope envelope)
        {
            var dto = package.Data.Deserialize<ReplicationMessageDto.RawChunkBulk>();
            return new ReplicationMessage.RawChunkBulk(dto.MasterId,
                                                       dto.SubscriptionId,
                                                       dto.ChunkStartNumber,
                                                       dto.ChunkEndNumber,
                                                       dto.RawPosition,
                                                       dto.RawBytes,
                                                       dto.CompleteChunk);
        }

        private static TcpPackage WrapRawChunkBulk(ReplicationMessage.RawChunkBulk msg)
        {
            var dto = new ReplicationMessageDto.RawChunkBulk(msg.MasterId,
                                                             msg.SubscriptionId,
                                                             msg.ChunkStartNumber,
                                                             msg.ChunkEndNumber,
                                                             msg.RawPosition,
                                                             msg.RawBytes,
                                                             msg.CompleteChunk);
            return new TcpPackage(TcpCommand.RawChunkBulk, Guid.NewGuid(), dto.Serialize());
        }

        private static ReplicationMessage.DataChunkBulk UnwrapDataChunkBulk(TcpPackage package, IEnvelope envelope)
        {
            var dto = package.Data.Deserialize<ReplicationMessageDto.DataChunkBulk>();
            return new ReplicationMessage.DataChunkBulk(dto.MasterId,
                                                        dto.SubscriptionId,
                                                        dto.ChunkStartNumber,
                                                        dto.ChunkEndNumber,
                                                        dto.SubscriptionPosition,
                                                        dto.DataBytes,
                                                        dto.CompleteChunk);
        }

        private static TcpPackage WrapDataChunkBulk(ReplicationMessage.DataChunkBulk msg)
        {
            var dto = new ReplicationMessageDto.DataChunkBulk(msg.MasterId,
                                                              msg.SubscriptionId,
                                                              msg.ChunkStartNumber,
                                                              msg.ChunkEndNumber,
                                                              msg.SubscriptionPosition,
                                                              msg.DataBytes,
                                                              msg.CompleteChunk);
            return new TcpPackage(TcpCommand.DataChunkBulk, Guid.NewGuid(), dto.Serialize());
        }

        private static ReplicationMessage.ReplicaSubscriptionRetry UnwrapReplicaSubscriptionRetry(TcpPackage package, IEnvelope envelope)
        {
            var dto = package.Data.Deserialize<ReplicationMessageDto.ReplicaSubscriptionRetry>();
            return new ReplicationMessage.ReplicaSubscriptionRetry(dto.MasterId, dto.SubscriptionId);
        }

        private static TcpPackage WrapReplicaSubscriptionRetry(ReplicationMessage.ReplicaSubscriptionRetry msg)
        {
            var dto = new ReplicationMessageDto.ReplicaSubscriptionRetry(msg.MasterId, msg.SubscriptionId);
            return new TcpPackage(TcpCommand.ReplicaSubscriptionRetry, Guid.NewGuid(), dto.Serialize());
        }

        private static ReplicationMessage.ReplicaSubscribed UnwrapReplicaSubscribed(TcpPackage package, IEnvelope envelope, TcpConnectionManager connection)
        {
            var dto = package.Data.Deserialize<ReplicationMessageDto.ReplicaSubscribed>();
            return new ReplicationMessage.ReplicaSubscribed(dto.MasterId,
                                                            dto.SubscriptionId,
                                                            dto.SubscriptionPosition,
                                                            connection.RemoteEndPoint);
        }

        private static TcpPackage WrapReplicaSubscribed(ReplicationMessage.ReplicaSubscribed msg)
        {
            var dto = new ReplicationMessageDto.ReplicaSubscribed(msg.MasterId,
                                                                  msg.SubscriptionId,
                                                                  msg.SubscriptionPosition);
            return new TcpPackage(TcpCommand.ReplicaSubscribed, Guid.NewGuid(), dto.Serialize());
        }

        private static ReplicationMessage.SlaveAssignment UnwrapSlaveAssignment(TcpPackage package, IEnvelope envelope)
        {
            var dto = package.Data.Deserialize<ReplicationMessageDto.SlaveAssignment>();
            return new ReplicationMessage.SlaveAssignment(dto.MasterId, dto.SubscriptionId);
        }

        private static TcpPackage WrapSlaveAssignment(ReplicationMessage.SlaveAssignment msg)
        {
            var dto = new ReplicationMessageDto.SlaveAssignment(msg.MasterId, msg.SubscriptionId);
            return new TcpPackage(TcpCommand.SlaveAssignment, Guid.NewGuid(), dto.Serialize());
        }

        private static ReplicationMessage.CloneAssignment UnwrapCloneAssignment(TcpPackage package, IEnvelope envelope)
        {
            var dto = package.Data.Deserialize<ReplicationMessageDto.CloneAssignment>();
            return new ReplicationMessage.CloneAssignment(dto.MasterId, dto.SubscriptionId);
        }

        private static TcpPackage WrapCloneAssignment(ReplicationMessage.CloneAssignment msg)
        {
            var dto = new ReplicationMessageDto.CloneAssignment(msg.MasterId, msg.SubscriptionId);
            return new TcpPackage(TcpCommand.CloneAssignment, Guid.NewGuid(), dto.Serialize());
        }
    }
}