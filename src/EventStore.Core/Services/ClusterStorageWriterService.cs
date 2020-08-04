using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using EventStore.Common.Utils;
using EventStore.Core.Bus;
using EventStore.Core.Data;
using EventStore.Core.Helpers;
using EventStore.Core.Messages;
using EventStore.Core.Services.Replication;
using EventStore.Core.Services.Storage;
using EventStore.Core.Services.Storage.EpochManager;
using EventStore.Core.Services.Storage.ReaderIndex;
using EventStore.Core.TransactionLog.Chunks;
using EventStore.Core.TransactionLog.Chunks.TFChunk;
using EventStore.Core.TransactionLog.LogRecords;
using Microsoft.Extensions.Logging;

namespace EventStore.Core.Services
{
    public class ClusterStorageWriterService :
        StorageWriterService,
        IHandle<ReplicationMessage.ReplicaSubscribed>,
        IHandle<ReplicationMessage.CreateChunk>,
        IHandle<ReplicationMessage.RawChunkBulk>,
        IHandle<ReplicationMessage.DataChunkBulk>
    {
        private static readonly ILogger Log = TraceLogger.GetLogger<ClusterStorageWriterService>();

        private readonly Func<long> _getLastCommitPosition;
        private readonly LengthPrefixSuffixFramer _framer;

        private Guid _subscriptionId;
        private TFChunk _activeChunk;
        private long _subscriptionPos;
        private long _ackedSubscriptionPos;

        public ClusterStorageWriterService(IPublisher bus,
                                               ISubscriber subscribeToBus,
                                               TimeSpan minFlushDelay,
                                               TFChunkDb db,
                                               TFChunkWriter writer,
                                               IIndexWriter indexWriter,
                                               IEpochManager epochManager,
                                               Func<long> getLastCommitPosition)
            : base(bus, subscribeToBus, minFlushDelay, db, writer, indexWriter, epochManager)
        {
            if (getLastCommitPosition is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.getLastCommitPosition); }
            _getLastCommitPosition = getLastCommitPosition;
            _framer = new LengthPrefixSuffixFramer(OnLogRecordUnframed, TFConsts.MaxLogRecordSize);

            SubscribeToMessage<ReplicationMessage.ReplicaSubscribed>();
            SubscribeToMessage<ReplicationMessage.CreateChunk>();
            SubscribeToMessage<ReplicationMessage.RawChunkBulk>();
            SubscribeToMessage<ReplicationMessage.DataChunkBulk>();
        }

        public override void Handle(SystemMessage.StateChangeMessage message)
        {
            if (message.State == VNodeState.PreMaster)
            {
                if (_activeChunk is object)
                {
                    _activeChunk.MarkForDeletion();
                    _activeChunk = null;
                }
                _subscriptionId = Guid.Empty;
                _subscriptionPos = -1;
                _ackedSubscriptionPos = -1;
            }

            base.Handle(message);
        }

        public void Handle(ReplicationMessage.ReplicaSubscribed message)
        {
            if (_activeChunk is object)
            {
                _activeChunk.MarkForDeletion();
                _activeChunk = null;
            }
            _framer.Reset();

            _subscriptionId = message.SubscriptionId;
            _ackedSubscriptionPos = _subscriptionPos = message.SubscriptionPosition;

            var infoEnabled = Log.IsInformationLevelEnabled();
            if (infoEnabled) { Log.SubscribedToMasterAt(message); }

            var writerCheck = Db.Config.WriterCheckpoint.ReadNonFlushed();
            if (message.SubscriptionPosition > writerCheck)
            {
                ReplicationFail(ExceptionResource.Master_subscribed_which_is_greater,
                                message, writerCheck);
            }

            if (message.SubscriptionPosition < writerCheck)
            {
                if (infoEnabled) { Log.MasterSubscribedUsAtWhichIsLessThanOurWriterCheckpoint(message, writerCheck); }

                var lastCommitPosition = _getLastCommitPosition();
                if (infoEnabled)
                {
                    if (message.SubscriptionPosition > lastCommitPosition)
                        Log.OnlineTruncationIsNeededNotImplementedOfflineTruncationWillBePerformed();
                    else
                        Log.OfflineTruncationIsNeededShuttingDownNode(message.SubscriptionPosition, lastCommitPosition);
                }

                EpochRecord lastEpoch = EpochManager.GetLastEpoch();
                if (AreAnyCommittedRecordsTruncatedWithLastEpoch(message.SubscriptionPosition, lastEpoch, lastCommitPosition))
                {
                    Log.MasterSubscribedUsAtWhichIsLessThanOurLastEpochAndLastcommitposition(
                        message.MasterEndPoint, message.MasterId, message.SubscriptionPosition, lastCommitPosition, lastEpoch.EpochPosition);
                    Log.AttemptToTruncateEpochWithCommittedRecords();
                }

                Db.Config.TruncateCheckpoint.Write(message.SubscriptionPosition);
                Db.Config.TruncateCheckpoint.Flush();

                BlockWriter = true;
                Bus.Publish(new ClientMessage.RequestShutdown(exitProcess: true, shutdownHttp: true));
                return;
            }

            // subscription position == writer checkpoint
            // everything is ok
        }

        private bool AreAnyCommittedRecordsTruncatedWithLastEpoch(long subscriptionPosition, EpochRecord lastEpoch, long lastCommitPosition)
        {
            return lastEpoch is object && subscriptionPosition <= lastEpoch.EpochPosition && lastCommitPosition >= lastEpoch.EpochPosition;
        }

        public void Handle(ReplicationMessage.CreateChunk message)
        {
            if (_subscriptionId != message.SubscriptionId) return;

            if (_activeChunk is object)
            {
                _activeChunk.MarkForDeletion();
                _activeChunk = null;
            }
            _framer.Reset();

            if (message.IsCompletedChunk)
            {
                _activeChunk = Db.Manager.CreateTempChunk(message.ChunkHeader, message.FileSize);
            }
            else
            {
                if (message.ChunkHeader.ChunkStartNumber != Db.Manager.ChunksCount)
                {
                    ReplicationFail(ExceptionResource.Received_request_to_create_chunk,
                                    message, Db.Manager.ChunksCount);
                }
                Db.Manager.AddNewChunk(message.ChunkHeader, message.FileSize);
            }

            _subscriptionPos = message.ChunkHeader.ChunkStartPosition;
            _ackedSubscriptionPos = _subscriptionPos;
            Bus.Publish(new ReplicationMessage.AckLogPosition(_subscriptionId, _ackedSubscriptionPos));
        }

        public void Handle(ReplicationMessage.RawChunkBulk message)
        {
            if (_subscriptionId != message.SubscriptionId) return;
            if (_activeChunk is null) ReplicationFail(ExceptionResource.Physical_chunk_bulk_received_but);

            if (_activeChunk.ChunkHeader.ChunkStartNumber != message.ChunkStartNumber || _activeChunk.ChunkHeader.ChunkEndNumber != message.ChunkEndNumber)
            {
                Log.ReceivedRawChunkBulkForTFChunkButActiveChunkIs(message.ChunkStartNumber, message.ChunkEndNumber, _activeChunk);
                return;
            }
            if (_activeChunk.RawWriterPosition != message.RawPosition)
            {
                Log.ReceivedRawchunkbulkAtRawPosWhileCurrentWriterRawPosIs(message.RawPosition, _activeChunk.RawWriterPosition);
                return;
            }

            if (!_activeChunk.TryAppendRawData(message.RawBytes))
            {
                ReplicationFail(ExceptionResource.Could_not_append_raw_bytes_to_chunk,
                                message, _activeChunk.FileSize);
            }

            _subscriptionPos += message.RawBytes.Length;

            if (message.CompleteChunk)
            {
#if DEBUG
                if (Log.IsTraceLevelEnabled()) Log.CompletingRawChu1nk(message.ChunkStartNumber, message.ChunkEndNumber);
#endif
                Writer.CompleteReplicatedRawChunk(_activeChunk);

                _subscriptionPos = _activeChunk.ChunkHeader.ChunkEndPosition;
                _framer.Reset();
                _activeChunk = null;
            }

            if (message.CompleteChunk || _subscriptionPos - _ackedSubscriptionPos >= MasterReplicationService.ReplicaAckWindow)
            {
                _ackedSubscriptionPos = _subscriptionPos;
                Bus.Publish(new ReplicationMessage.AckLogPosition(_subscriptionId, _ackedSubscriptionPos));
            }
        }

        public void Handle(ReplicationMessage.DataChunkBulk message)
        {
            Interlocked.Decrement(ref FlushMessagesInQueue);
            try
            {
                if (_subscriptionId != message.SubscriptionId) return;
                if (_activeChunk is object) ReplicationFail(ExceptionResource.Data_chunk_bulk_received_but);

                var chunk = Writer.CurrentChunk;
                if (chunk.ChunkHeader.ChunkStartNumber != message.ChunkStartNumber || chunk.ChunkHeader.ChunkEndNumber != message.ChunkEndNumber)
                {
                    Log.ReceivedDataChunkBulkForTFChunkButActiveChunkIs(
                              message.ChunkStartNumber, message.ChunkEndNumber, chunk.ChunkHeader.ChunkStartNumber, chunk.ChunkHeader.ChunkEndNumber);
                    return;
                }

                if (_subscriptionPos != message.SubscriptionPosition)
                {
                    Log.ReceivedDatachunkbulkAtSubscriptionpositionWhileCurrentSubscriptionpositionIs(message.SubscriptionPosition, _subscriptionPos);
                    return;
                }

                _framer.UnFrameData(new ArraySegment<byte>(message.DataBytes));
                _subscriptionPos += message.DataBytes.Length;

                if (message.CompleteChunk)
                {
#if DEBUG
                    if (Log.IsTraceLevelEnabled()) Log.CompletingDataChunk(message.ChunkStartNumber, message.ChunkEndNumber);
#endif
                    Writer.CompleteChunk();

                    if (_framer.HasData)
                    {
                        ReplicationFail(ExceptionResource.There_is_some_data_left_in_framer_when_completing_chunk);
                    }

                    _subscriptionPos = chunk.ChunkHeader.ChunkEndPosition;
                    _framer.Reset();
                }
            }
            catch (Exception exc)
            {
                Log.ExceptionInStorageWriter(exc);
                throw;
            }
            finally
            {
                Flush();
            }

            if (message.CompleteChunk || _subscriptionPos - _ackedSubscriptionPos >= MasterReplicationService.ReplicaAckWindow)
            {
                _ackedSubscriptionPos = _subscriptionPos;
                Bus.Publish(new ReplicationMessage.AckLogPosition(_subscriptionId, _ackedSubscriptionPos));
            }
        }

        private void OnLogRecordUnframed(BinaryReader reader)
        {
            var record = LogRecord.ReadFrom(reader);
            if (!Writer.Write(record, out long newPos))
            {
                ReplicationFail(ExceptionResource.First_write_failed_when_writing_replicated_record, record);
            }
        }

        private void ReplicationFail(string message, params object[] args)
        {
            var msg = 0u >= (uint)args.Length ? message : string.Format(message, args);
            Log.LogCritical(msg);
            BlockWriter = true;
            Application.Exit(ExitCode.Error, msg);
            throw new Exception(msg);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ReplicationFail(ExceptionResource resource)
        {
            ReplicationFail(ThrowHelper.GetResourceString(resource));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ReplicationFail(ExceptionResource resource, LogRecord record)
        {
            ReplicationFail(ThrowHelper.GetResourceString(resource), record);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ReplicationFail(ExceptionResource resource, ReplicationMessage.CreateChunk message, int chunksCount)
        {
            ReplicationFail(ThrowHelper.GetResourceString(resource), message.ChunkHeader.ChunkStartNumber, message.ChunkHeader.ChunkEndNumber, chunksCount);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ReplicationFail(ExceptionResource resource, ReplicationMessage.RawChunkBulk message, int fileSize)
        {
            ReplicationFail(ThrowHelper.GetResourceString(resource), message.ChunkStartNumber, message.ChunkEndNumber, message.RawPosition, message.RawBytes.Length, _activeChunk.FileSize);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ReplicationFail(ExceptionResource resource, ReplicationMessage.ReplicaSubscribed message, long writerCheck)
        {
            ReplicationFail(ThrowHelper.GetResourceString(resource), message.MasterEndPoint, message.MasterId, message.SubscriptionPosition, writerCheck);
        }
    }
}
