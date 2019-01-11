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
            if (getLastCommitPosition == null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.getLastCommitPosition); }
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
                if (_activeChunk != null)
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
            if (_activeChunk != null)
            {
                _activeChunk.MarkForDeletion();
                _activeChunk = null;
            }
            _framer.Reset();

            _subscriptionId = message.SubscriptionId;
            _ackedSubscriptionPos = _subscriptionPos = message.SubscriptionPosition;

            var infoEnabled = Log.IsInformationLevelEnabled();
            if (infoEnabled)
            {
                Log.LogInformation(string.Format("=== SUBSCRIBED to [{0},{1:B}] at {2} (0x{2:X}). SubscriptionId: {3:B}.",
                         message.MasterEndPoint, message.MasterId, message.SubscriptionPosition, message.SubscriptionId));
            }

            var writerCheck = Db.Config.WriterCheckpoint.ReadNonFlushed();
            if (message.SubscriptionPosition > writerCheck)
            {
                ReplicationFail(ExceptionResource.Master_subscribed_which_is_greater,
                                message, writerCheck);
            }

            if (message.SubscriptionPosition < writerCheck)
            {
                if (infoEnabled)
                {
                    Log.LogInformation(string.Format("Master [{0},{1:B}] subscribed us at {2} (0x{2:X}), which is less than our writer checkpoint {3} (0x{3:X}). TRUNCATION IS NEEDED.",
                           message.MasterEndPoint, message.MasterId, message.SubscriptionPosition, writerCheck));
                }

                var lastCommitPosition = _getLastCommitPosition();
                if (infoEnabled)
                {
                    if (message.SubscriptionPosition > lastCommitPosition)
                        Log.LogInformation("ONLINE TRUNCATION IS NEEDED. NOT IMPLEMENTED. OFFLINE TRUNCATION WILL BE PERFORMED. SHUTTING DOWN NODE.");
                    else
                        Log.LogInformation(string.Format("OFFLINE TRUNCATION IS NEEDED (SubscribedAt {0} (0x{0:X}) <= LastCommitPosition {1} (0x{1:X})). SHUTTING DOWN NODE.", message.SubscriptionPosition, lastCommitPosition));
                }

                EpochRecord lastEpoch = EpochManager.GetLastEpoch();
                if (AreAnyCommittedRecordsTruncatedWithLastEpoch(message.SubscriptionPosition, lastEpoch, lastCommitPosition))
                {
                    Log.LogError(string.Format("Master [{0},{1:B}] subscribed us at {2} (0x{2:X}), which is less than our last epoch and LastCommitPosition {3} (0x{3:X}) >= lastEpoch.EpochPosition {4} (0x{4:X}). That might be bad, especially if the LastCommitPosition is way beyond EpochPosition.",
                                message.MasterEndPoint, message.MasterId, message.SubscriptionPosition, lastCommitPosition, lastEpoch.EpochPosition));
                    Log.LogError("ATTEMPT TO TRUNCATE EPOCH WITH COMMITTED RECORDS. THIS MAY BE BAD, BUT IT IS OK IF JUST-ELECTED MASTER FAILS IMMEDIATELY AFTER ITS ELECTION.");
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
            return lastEpoch != null && subscriptionPosition <= lastEpoch.EpochPosition && lastCommitPosition >= lastEpoch.EpochPosition;
        }

        public void Handle(ReplicationMessage.CreateChunk message)
        {
            if (_subscriptionId != message.SubscriptionId) return;

            if (_activeChunk != null)
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
            if (_activeChunk == null) ReplicationFail(ExceptionResource.Physical_chunk_bulk_received_but);

            if (_activeChunk.ChunkHeader.ChunkStartNumber != message.ChunkStartNumber || _activeChunk.ChunkHeader.ChunkEndNumber != message.ChunkEndNumber)
            {
                Log.LogError("Received RawChunkBulk for TFChunk {0}-{1}, but active chunk is {2}.",
                          message.ChunkStartNumber, message.ChunkEndNumber, _activeChunk);
                return;
            }
            if (_activeChunk.RawWriterPosition != message.RawPosition)
            {
                Log.LogError(string.Format("Received RawChunkBulk at raw pos {0} (0x{0:X}) while current writer raw pos is {1} (0x{1:X}).",
                          message.RawPosition, _activeChunk.RawWriterPosition));
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
                if (Log.IsTraceLevelEnabled()) Log.LogTrace("Completing raw chunk {0}-{1}...", message.ChunkStartNumber, message.ChunkEndNumber);
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
                if (_activeChunk != null) ReplicationFail(ExceptionResource.Data_chunk_bulk_received_but);

                var chunk = Writer.CurrentChunk;
                if (chunk.ChunkHeader.ChunkStartNumber != message.ChunkStartNumber || chunk.ChunkHeader.ChunkEndNumber != message.ChunkEndNumber)
                {
                    Log.LogError("Received DataChunkBulk for TFChunk {0}-{1}, but active chunk is {2}-{3}.",
                              message.ChunkStartNumber, message.ChunkEndNumber, chunk.ChunkHeader.ChunkStartNumber, chunk.ChunkHeader.ChunkEndNumber);
                    return;
                }

                if (_subscriptionPos != message.SubscriptionPosition)
                {
                    Log.LogError(string.Format("Received DataChunkBulk at SubscriptionPosition {0} (0x{0:X}) while current SubscriptionPosition is {1} (0x{1:X}).",
                              message.SubscriptionPosition, _subscriptionPos));
                    return;
                }

                _framer.UnFrameData(new ArraySegment<byte>(message.DataBytes));
                _subscriptionPos += message.DataBytes.Length;

                if (message.CompleteChunk)
                {
                    if (Log.IsTraceLevelEnabled()) Log.LogTrace("Completing data chunk {0}-{1}...", message.ChunkStartNumber, message.ChunkEndNumber);
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
                Log.LogError(exc, "Exception in writer.");
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
            var msg = args.Length == 0 ? message : string.Format(message, args);
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
