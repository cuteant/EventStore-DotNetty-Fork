﻿using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CuteAnt.Buffers;
using CuteAnt.Text;
using DotNetty.Common;
using EventStore.Common.Utils;
using EventStore.Core.Bus;
using EventStore.Core.Data;
using EventStore.Core.Messages;
using EventStore.Core.Messaging;
using EventStore.Core.Services.Histograms;
using EventStore.Core.Services.Monitoring.Stats;
using EventStore.Core.Services.Storage.EpochManager;
using EventStore.Core.Services.Storage.ReaderIndex;
using EventStore.Core.TransactionLog.Chunks;
using EventStore.Core.TransactionLog.LogRecords;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SpanJson.Serialization;

namespace EventStore.Core.Services.Storage
{
    public class StorageWriterService :
        IHandle<SystemMessage.SystemInit>,
        IHandle<SystemMessage.StateChangeMessage>,
        IHandle<SystemMessage.WriteEpoch>,
        IHandle<SystemMessage.WaitForChaserToCatchUp>,
        IHandle<StorageMessage.WritePrepares>,
        IHandle<StorageMessage.WriteDelete>,
        IHandle<StorageMessage.WriteTransactionStart>,
        IHandle<StorageMessage.WriteTransactionData>,
        IHandle<StorageMessage.WriteTransactionPrepare>,
        IHandle<StorageMessage.WriteCommit>,
        IHandle<MonitoringMessage.InternalStatsRequest>
    {
        private static readonly ILogger Log = TraceLogger.GetLogger<StorageWriterService>();

        private const int c_initialBufferSize = 1024 * 4;
        private static readonly ArrayPool<byte> s_sharedBufferPool = BufferManager.Shared;

        protected static readonly int TicksPerMs = (int)(Stopwatch.Frequency / 1000);
        private static readonly TimeSpan WaitForChaserSingleIterationTimeout = TimeSpan.FromMilliseconds(200);

        protected readonly TFChunkDb Db;
        protected readonly TFChunkWriter Writer;
        private readonly IIndexWriter _indexWriter;
        protected readonly IEpochManager EpochManager;

        protected readonly IPublisher Bus;
        private readonly ISubscriber _subscribeToBus;
        protected readonly IQueuedHandler StorageWriterQueue;
        private readonly InMemoryBus _writerBus;

        private readonly Stopwatch _watch = Stopwatch.StartNew();
        private readonly double _minFlushDelay;
        private long _lastFlushDelay;
        private long _lastFlushTimestamp;

        protected int FlushMessagesInQueue;
        private VNodeState _vnodeState = VNodeState.Initializing;
        protected bool BlockWriter = false;

        private const int LastStatsCount = 1024;
        private readonly long[] _lastFlushDelays = new long[LastStatsCount];
        private readonly long[] _lastFlushSizes = new long[LastStatsCount];
        private int _statIndex;
        private int _statCount;
        private long _sumFlushDelay;
        private long _sumFlushSize;
        private long _lastFlushSize;
        private long _maxFlushSize;
        private long _maxFlushDelay;
        private const string _writerFlushHistogram = "writer-flush";
        private readonly List<Task> _tasks = new List<Task>();
        public IEnumerable<Task> Tasks { get { return _tasks; } }

        public StorageWriterService(IPublisher bus,
            ISubscriber subscribeToBus,
            TimeSpan minFlushDelay,
            TFChunkDb db,
            TFChunkWriter writer,
            IIndexWriter indexWriter,
            IEpochManager epochManager)
        {
            if (bus is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }
            if (subscribeToBus is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.subscribeToBus); }
            if (db is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.db); }
            if (writer is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.writer); }
            if (indexWriter is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.indexWriter); }
            if (epochManager is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.epochManager); }

            Bus = bus;
            _subscribeToBus = subscribeToBus;
            Db = db;
            _indexWriter = indexWriter;
            EpochManager = epochManager;

            _minFlushDelay = minFlushDelay.TotalMilliseconds * TicksPerMs;
            _lastFlushDelay = 0;
            _lastFlushTimestamp = _watch.ElapsedTicks;

            Writer = writer;
            Writer.Open();

            _writerBus = new InMemoryBus("StorageWriterBus", watchSlowMsg: false);
            StorageWriterQueue = QueuedHandler.CreateQueuedHandler(new AdHocHandler<Message>(CommonHandle),
                "StorageWriterQueue",
                true,
                TimeSpan.FromMilliseconds(500));
            _tasks.Add(StorageWriterQueue.Start());

            SubscribeToMessage<SystemMessage.SystemInit>();
            SubscribeToMessage<SystemMessage.StateChangeMessage>();
            SubscribeToMessage<SystemMessage.WriteEpoch>();
            SubscribeToMessage<SystemMessage.WaitForChaserToCatchUp>();
            SubscribeToMessage<StorageMessage.WritePrepares>();
            SubscribeToMessage<StorageMessage.WriteDelete>();
            SubscribeToMessage<StorageMessage.WriteTransactionStart>();
            SubscribeToMessage<StorageMessage.WriteTransactionData>();
            SubscribeToMessage<StorageMessage.WriteTransactionPrepare>();
            SubscribeToMessage<StorageMessage.WriteCommit>();
        }

        protected void SubscribeToMessage<T>() where T : Message
        {
            _writerBus.Subscribe((IHandle<T>)this);
            _subscribeToBus.Subscribe(new AdHocHandler<Message>(EnqueueMessage).WidenFrom<T, Message>());
        }

        private void EnqueueMessage(Message message)
        {
            if (message is StorageMessage.IFlushableMessage)
                Interlocked.Increment(ref FlushMessagesInQueue);

            StorageWriterQueue.Publish(message);

            if (message is SystemMessage.BecomeShuttingDown)
            // we need to handle this message on main thread to stop StorageWriterQueue
            {
                StorageWriterQueue.Stop();
                BlockWriter = true;
                Bus.Publish(new SystemMessage.ServiceShutdown("StorageWriter"));
            }
        }

        private void CommonHandle(Message message)
        {
            if (BlockWriter && !(message is SystemMessage.StateChangeMessage))
            {
#if DEBUG
                if (Log.IsTraceLevelEnabled()) { Log.BlockingMessageInStorageWriterService(message); }
#endif
                return;
            }

            if (_vnodeState != VNodeState.Master && message is StorageMessage.IMasterWriteMessage)
            {
                MessageAppearedInStorageWriter(message);
                return;
            }

            try
            {
                _writerBus.Handle(message);
            }
            catch (Exception exc)
            {
                UnexpectedErrorInStorageWriterService(exc);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void MessageAppearedInStorageWriter(Message message)
        {
            var msg = $"{message.GetType().Name} appeared in StorageWriter during state {_vnodeState}.";
            Log.LogCritical(msg);
            Application.Exit(ExitCode.Error, msg);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void UnexpectedErrorInStorageWriterService(Exception exc)
        {
            BlockWriter = true;
            Log.Unexpected_error_in_StorageWriterService(exc);
            Application.Exit(ExitCode.Error, $"Unexpected error in StorageWriterService: {exc.Message}");
        }

        void IHandle<SystemMessage.SystemInit>.Handle(SystemMessage.SystemInit message)
        {
            Bus.Publish(new SystemMessage.ServiceInitialized("StorageWriter"));
        }

        public virtual void Handle(SystemMessage.StateChangeMessage message)
        {
            _vnodeState = message.State;

            switch (message.State)
            {
                case VNodeState.Master:
                    {
                        _indexWriter.Reset();
                        EpochManager.WriteNewEpoch(); // forces flush
                        break;
                    }
                case VNodeState.ShuttingDown:
                    {
                        Writer.Close();
                        break;
                    }
            }
        }

        void IHandle<SystemMessage.WriteEpoch>.Handle(SystemMessage.WriteEpoch message)
        {
            if (_vnodeState == VNodeState.PreMaster)
                return;
            if (_vnodeState != VNodeState.Master)
            {
                ThrowHelper.ThrowException_NewEpochRequestNotInMasterState(_vnodeState);
            }

            EpochManager.WriteNewEpoch();
            PurgeNotProcessedInfo();
        }

        void IHandle<SystemMessage.WaitForChaserToCatchUp>.Handle(SystemMessage.WaitForChaserToCatchUp message)
        {
            // if we are in states, that doesn't need to wait for chaser, ignore
            if (_vnodeState != VNodeState.PreMaster && _vnodeState != VNodeState.PreReplica)
            {
                ThrowHelper.ThrowException_WaitForChaserToCatchUpAppearedIn(message, _vnodeState);
            }

            if (Writer.Checkpoint.Read() != Writer.Checkpoint.ReadNonFlushed()) { Writer.Flush(); }

            var sw = Stopwatch.StartNew();
            var spinner = new SpinWait();
            while (Db.Config.ChaserCheckpoint.Read() < Db.Config.WriterCheckpoint.Read() &&
                   sw.Elapsed < WaitForChaserSingleIterationTimeout)
            {
                //Thread.Sleep(1);
                spinner.SpinOnce();
            }

            if (Db.Config.ChaserCheckpoint.Read() == Db.Config.WriterCheckpoint.Read())
            {
                Bus.Publish(new SystemMessage.ChaserCaughtUp(message.CorrelationId));
                return;
            }

            var totalTime = message.TotalTimeWasted + sw.Elapsed;
#if DEBUG
            if (Log.IsDebugLevelEnabled() && (totalTime < TimeSpan.FromSeconds(5) || 0u >= (uint)((int)totalTime.TotalSeconds % 30))) // too verbose otherwise
            {
                Log.Still_waiting_for_chaser_to_catch_up_already_for(totalTime);
            }
#endif

            Bus.Publish(new SystemMessage.WaitForChaserToCatchUp(message.CorrelationId, totalTime));
        }

        void IHandle<StorageMessage.WritePrepares>.Handle(StorageMessage.WritePrepares msg)
        {
            Interlocked.Decrement(ref FlushMessagesInQueue);
            ThreadLocalList<PrepareLogRecord> prepares = null;
            try
            {
                if (msg.LiveUntil < DateTime.UtcNow) { return; }

                var msgEvents = msg.Events;
                var eventCount = msgEvents.Length;
                string streamId = msg.EventStreamId;
                var commitCheck = _indexWriter.CheckCommit(streamId, msg.ExpectedVersion,
                    msgEvents.Select(x => x.EventId));
                if (commitCheck.Decision != CommitDecision.Ok)
                {
                    ActOnCommitCheckFailure(msg.Envelope, msg.CorrelationId, commitCheck);
                    return;
                }

                prepares = ThreadLocalList<PrepareLogRecord>.NewInstance();
                var logPosition = Writer.Checkpoint.ReadNonFlushed();
                if (eventCount > 0)
                {
                    var transactionPosition = logPosition;
                    for (int i = 0; i < eventCount; ++i)
                    {
                        var evnt = msgEvents[i];
                        var flags = PrepareFlags.Data | PrepareFlags.IsCommitted;
                        if (0u >= (uint)i) { flags |= PrepareFlags.TransactionBegin; }
                        if (i == eventCount - 1) { flags |= PrepareFlags.TransactionEnd; }
                        if (evnt.IsJson) { flags |= PrepareFlags.IsJson; }

                        // when IsCommitted ExpectedVersion is always explicit
                        var expectedVersion = commitCheck.CurrentVersion + i;
                        var res = WritePrepareWithRetry(
                            LogRecord.Prepare(logPosition, msg.CorrelationId, evnt.EventId,
                                transactionPosition, i, streamId,
                                expectedVersion, flags, evnt.EventType, evnt.Data, evnt.Metadata));
                        logPosition = res.NewPos;
                        if (0u >= (uint)i) { transactionPosition = res.WrittenPos; }
                        // transaction position could be changed due to switching to new chunk
                        prepares.Add(res.Prepare);
                    }
                }
                else
                {
                    WritePrepareWithRetry(
                        LogRecord.Prepare(logPosition, msg.CorrelationId, Guid.NewGuid(), logPosition, -1,
                            streamId, commitCheck.CurrentVersion,
                            PrepareFlags.TransactionBegin | PrepareFlags.TransactionEnd | PrepareFlags.IsCommitted,
                            null, Empty.ByteArray, Empty.ByteArray));
                }

                bool softUndeleteMetastream = SystemStreams.IsMetastream(streamId)
                                              && _indexWriter.IsSoftDeleted(SystemStreams.OriginalStreamOf(streamId));

                _indexWriter.PreCommit(prepares);

                if (commitCheck.IsSoftDeleted)
                {
                    SoftUndeleteStream(streamId, commitCheck.CurrentVersion + 1);
                }
                if (softUndeleteMetastream) { SoftUndeleteMetastream(streamId); }
            }
            catch (Exception exc)
            {
                Log.ExceptionInStorageWriter(exc);
                throw;
            }
            finally
            {
                prepares?.Return();
                Flush();
            }
        }


        private void SoftUndeleteMetastream(string metastreamId)
        {
            var origStreamId = SystemStreams.OriginalStreamOf(metastreamId);
            var rawMetaInfo = _indexWriter.GetStreamRawMeta(origStreamId);
            SoftUndeleteStream(origStreamId, rawMetaInfo.MetaLastEventNumber, rawMetaInfo.RawMeta,
                recreateFrom: _indexWriter.GetStreamLastEventNumber(origStreamId) + 1);
        }

        private void SoftUndeleteStream(string streamId, long recreateFromEventNumber)
        {
            var rawInfo = _indexWriter.GetStreamRawMeta(streamId);
            SoftUndeleteStream(streamId, rawInfo.MetaLastEventNumber, rawInfo.RawMeta, recreateFromEventNumber);
        }

        private void SoftUndeleteStream(string streamId, long metaLastEventNumber, byte[] rawMeta, long recreateFrom)
        {
            if (!SoftUndeleteRawMeta(rawMeta, recreateFrom, out byte[] modifiedMeta)) { return; }

            var logPosition = Writer.Checkpoint.ReadNonFlushed();
            var res = WritePrepareWithRetry(
                LogRecord.Prepare(logPosition, Guid.NewGuid(), Guid.NewGuid(), logPosition, 0,
                    SystemStreams.MetastreamOf(streamId), metaLastEventNumber,
                    PrepareFlags.SingleWrite | PrepareFlags.IsCommitted | PrepareFlags.IsJson,
                    SystemEventTypes.StreamMetadata, modifiedMeta, Empty.ByteArray));

            _indexWriter.PreCommit(new[] { res.Prepare });
        }

        public bool SoftUndeleteRawMeta(byte[] rawMeta, long recreateFromEventNumber, out byte[] modifiedMeta)
        {
            try
            {
                var jobj = JObject.Parse(Encoding.UTF8.GetString(rawMeta));
                jobj[SystemMetadata.TruncateBefore] = recreateFromEventNumber;
                using (var pooledOutputStream = BufferManagerOutputStreamManager.Create())
                {
                    var outputStream = pooledOutputStream.Object;
                    outputStream.Reinitialize(c_initialBufferSize, s_sharedBufferPool);

                    using (var jsonWriter = new JsonTextWriter(new StreamWriterX(outputStream, StringHelper.UTF8NoBOM)))
                    {
                        jsonWriter.ArrayPool = JsonConvertX.GlobalCharacterArrayPool;
                        jsonWriter.CloseOutput = false;

                        jobj.WriteTo(jsonWriter);
                        jsonWriter.Flush();
                    }
                    modifiedMeta =  outputStream.ToByteArray();
                    return true;
                }
            }
            catch (Exception)
            {
                modifiedMeta = null;
                return false;
            }
        }

        void IHandle<StorageMessage.WriteDelete>.Handle(StorageMessage.WriteDelete message)
        {
            Interlocked.Decrement(ref FlushMessagesInQueue);
            try
            {
                if (message.LiveUntil < DateTime.UtcNow) { return; }

                var eventId = Guid.NewGuid();

                var commitCheck = _indexWriter.CheckCommit(message.EventStreamId, message.ExpectedVersion, new[] { eventId });
                if (commitCheck.Decision != CommitDecision.Ok)
                {
                    ActOnCommitCheckFailure(message.Envelope, message.CorrelationId, commitCheck);
                    return;
                }

                if (message.HardDelete)
                {
                    // HARD DELETE
                    const long expectedVersion = EventNumber.DeletedStream - 1;
                    var record = LogRecord.DeleteTombstone(Writer.Checkpoint.ReadNonFlushed(), message.CorrelationId,
                        eventId, message.EventStreamId, expectedVersion, PrepareFlags.IsCommitted);
                    var res = WritePrepareWithRetry(record);
                    _indexWriter.PreCommit(new[] { res.Prepare });
                }
                else
                {
                    // SOFT DELETE
                    var metastreamId = SystemStreams.MetastreamOf(message.EventStreamId);
                    var expectedVersion = _indexWriter.GetStreamLastEventNumber(metastreamId);
                    var logPosition = Writer.Checkpoint.ReadNonFlushed();
                    const PrepareFlags flags = PrepareFlags.SingleWrite | PrepareFlags.IsCommitted | PrepareFlags.IsJson;
                    var data = new StreamMetadata(truncateBefore: EventNumber.DeletedStream).ToJsonBytes();
                    var res = WritePrepareWithRetry(
                        LogRecord.Prepare(logPosition, message.CorrelationId, eventId, logPosition, 0,
                            metastreamId, expectedVersion, flags, SystemEventTypes.StreamMetadata,
                            data, null));
                    _indexWriter.PreCommit(new[] { res.Prepare });
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
        }

        void IHandle<StorageMessage.WriteTransactionStart>.Handle(StorageMessage.WriteTransactionStart message)
        {
            Interlocked.Decrement(ref FlushMessagesInQueue);
            try
            {
                if (message.LiveUntil < DateTime.UtcNow)
                    return;

                var record = LogRecord.TransactionBegin(Writer.Checkpoint.ReadNonFlushed(),
                    message.CorrelationId,
                    message.EventStreamId,
                    message.ExpectedVersion);
                var res = WritePrepareWithRetry(record);

                // we update cache to avoid non-cached look-up on next TransactionWrite
                _indexWriter.UpdateTransactionInfo(res.WrittenPos, res.WrittenPos,
                    new TransactionInfo(-1, message.EventStreamId));
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
        }

        void IHandle<StorageMessage.WriteTransactionData>.Handle(StorageMessage.WriteTransactionData message)
        {
            Interlocked.Decrement(ref FlushMessagesInQueue);
            try
            {
                var logPosition = Writer.Checkpoint.ReadNonFlushed();
                var transactionInfo = _indexWriter.GetTransactionInfo(Writer.Checkpoint.Read(), message.TransactionId);
                if (!CheckTransactionInfo(message.TransactionId, transactionInfo))
                    return;

                var msgEvents = message.Events;
                var eventCount = msgEvents.Length;
                if (eventCount > 0)
                {
                    long lastLogPosition = -1;
                    for (int i = 0; i < eventCount; ++i)
                    {
                        var evnt = msgEvents[i];
                        var record = LogRecord.TransactionWrite(logPosition,
                            message.CorrelationId,
                            evnt.EventId,
                            message.TransactionId,
                            transactionInfo.TransactionOffset + i + 1,
                            transactionInfo.EventStreamId,
                            evnt.EventType,
                            evnt.Data,
                            evnt.Metadata,
                            evnt.IsJson);
                        var res = WritePrepareWithRetry(record);
                        logPosition = res.NewPos;
                        lastLogPosition = res.WrittenPos;
                    }
                    var info = new TransactionInfo(transactionInfo.TransactionOffset + eventCount,
                        transactionInfo.EventStreamId);
                    _indexWriter.UpdateTransactionInfo(message.TransactionId, lastLogPosition, info);
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
        }

        void IHandle<StorageMessage.WriteTransactionPrepare>.Handle(StorageMessage.WriteTransactionPrepare message)
        {
            Interlocked.Decrement(ref FlushMessagesInQueue);
            try
            {
                if (message.LiveUntil < DateTime.UtcNow) { return; }

                var transactionInfo = _indexWriter.GetTransactionInfo(Writer.Checkpoint.Read(), message.TransactionId);
                if (!CheckTransactionInfo(message.TransactionId, transactionInfo)) { return; }

                var record = LogRecord.TransactionEnd(Writer.Checkpoint.ReadNonFlushed(),
                    message.CorrelationId,
                    Guid.NewGuid(),
                    message.TransactionId,
                    transactionInfo.EventStreamId);
                WritePrepareWithRetry(record);
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
        }

        private static bool CheckTransactionInfo(long transactionId, in TransactionInfo transactionInfo)
        {
            if (ThrowHelper.IsInvalidCheckpoint(transactionInfo.TransactionOffset) || transactionInfo.EventStreamId.IsEmptyString())
            {
                Log.InvalidTransactionInfoFoundForTransactionId(transactionId, transactionInfo);
                return false;
            }
            return true;
        }

        void IHandle<StorageMessage.WriteCommit>.Handle(StorageMessage.WriteCommit message)
        {
            Interlocked.Decrement(ref FlushMessagesInQueue);
            try
            {
                var commitPos = Writer.Checkpoint.ReadNonFlushed();
                var commitCheck = _indexWriter.CheckCommitStartingAt(message.TransactionPosition, commitPos);
                if (commitCheck.Decision != CommitDecision.Ok)
                {
                    ActOnCommitCheckFailure(message.Envelope, message.CorrelationId, commitCheck);
                    return;
                }


                var commit = WriteCommitWithRetry(LogRecord.Commit(commitPos,
                    message.CorrelationId,
                    message.TransactionPosition,
                    commitCheck.CurrentVersion + 1));

                bool softUndeleteMetastream = SystemStreams.IsMetastream(commitCheck.EventStreamId)
                                              &&
                                              _indexWriter.IsSoftDeleted(
                                                  SystemStreams.OriginalStreamOf(commitCheck.EventStreamId));

                _indexWriter.PreCommit(commit);

                if (commitCheck.IsSoftDeleted)
                {
                    SoftUndeleteStream(commitCheck.EventStreamId, commitCheck.CurrentVersion + 1);
                }

                if (softUndeleteMetastream)
                {
                    SoftUndeleteMetastream(commitCheck.EventStreamId);
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
        }

        private static void ActOnCommitCheckFailure(IEnvelope envelope, Guid correlationId, in CommitCheckResult result)
        {
            switch (result.Decision)
            {
                case CommitDecision.WrongExpectedVersion:
                    envelope.ReplyWith(new StorageMessage.WrongExpectedVersion(correlationId, result.CurrentVersion));
                    break;
                case CommitDecision.Deleted:
                    envelope.ReplyWith(new StorageMessage.StreamDeleted(correlationId));
                    break;
                case CommitDecision.Idempotent:
                    envelope.ReplyWith(new StorageMessage.AlreadyCommitted(correlationId,
                        result.EventStreamId,
                        result.StartEventNumber,
                        result.EndEventNumber));
                    break;
                case CommitDecision.CorruptedIdempotency:
                    // in case of corrupted idempotency (part of transaction is ok, other is different)
                    // then we can say that the transaction is not idempotent, so WrongExpectedVersion is ok answer
                    envelope.ReplyWith(new StorageMessage.WrongExpectedVersion(correlationId, result.CurrentVersion));
                    break;
                case CommitDecision.InvalidTransaction:
                    envelope.ReplyWith(new StorageMessage.InvalidTransaction(correlationId));
                    break;
                case CommitDecision.IdempotentNotReady:
                    //just drop the write and wait for the client to retry
#if DEBUG
                    if (Log.IsDebugLevelEnabled()) { Log.DroppingIdempotentWriteToStream(result); }
#endif
                    break;
                default:
                    ThrowHelper.ThrowArgumentOutOfRangeException(); break;
            }
        }

        private WriteResult WritePrepareWithRetry(PrepareLogRecord prepare)
        {
            long writtenPos = prepare.LogPosition;
            PrepareLogRecord record = prepare;
            if (!Writer.Write(prepare, out long newPos))
            {
                var transactionPos = prepare.TransactionPosition == prepare.LogPosition
                    ? newPos
                    : prepare.TransactionPosition;
                record = new PrepareLogRecord(newPos,
                    prepare.CorrelationId,
                    prepare.EventId,
                    transactionPos,
                    prepare.TransactionOffset,
                    prepare.EventStreamId,
                    prepare.ExpectedVersion,
                    prepare.TimeStamp,
                    prepare.Flags,
                    prepare.EventType,
                    prepare.Data,
                    prepare.Metadata);
                writtenPos = newPos;
                if (!Writer.Write(record, out newPos))
                {
                    ThrowHelper.ThrowException_SecondWriteTryFailedWhenFirstWritingPrepare(prepare.LogPosition, writtenPos);
                }
            }
            return new WriteResult(writtenPos, newPos, record);
        }

        private CommitLogRecord WriteCommitWithRetry(CommitLogRecord commit)
        {
            if (!Writer.Write(commit, out long newPos))
            {
                var transactionPos = commit.TransactionPosition == commit.LogPosition
                    ? newPos
                    : commit.TransactionPosition;
                var record = new CommitLogRecord(newPos,
                    commit.CorrelationId,
                    transactionPos,
                    commit.TimeStamp,
                    commit.FirstEventNumber);
                long writtenPos = newPos;
                if (!Writer.Write(record, out newPos))
                {
                    ThrowHelper.ThrowException_SecondWriteTryFailedWhenFirstWritingCommit(commit.LogPosition, writtenPos);
                }
                return record;
            }
            return commit;
        }

        protected bool Flush(bool force = false)
        {
            var start = _watch.ElapsedTicks;
            if (force || 0u >= (uint)FlushMessagesInQueue || start - _lastFlushTimestamp >= _lastFlushDelay + _minFlushDelay)
            {
                var flushSize = Writer.Checkpoint.ReadNonFlushed() - Writer.Checkpoint.Read();

                Writer.Flush();
                HistogramService.SetValue(_writerFlushHistogram,
                    (long)((((double)_watch.ElapsedTicks - start) / Stopwatch.Frequency) * 1000000000));
                var end = _watch.ElapsedTicks;
                var flushDelay = end - start;
                Interlocked.Exchange(ref _lastFlushDelay, flushDelay);
                Interlocked.Exchange(ref _lastFlushSize, flushSize);
                _lastFlushTimestamp = end;

                if (_statCount >= LastStatsCount)
                {
                    Interlocked.Add(ref _sumFlushSize, -_lastFlushSizes[_statIndex]);
                    Interlocked.Add(ref _sumFlushDelay, -_lastFlushDelays[_statIndex]);
                }
                else
                {
                    _statCount += 1;
                }
                _lastFlushSizes[_statIndex] = flushSize;
                _lastFlushDelays[_statIndex] = flushDelay;
                Interlocked.Add(ref _sumFlushSize, flushSize);
                Interlocked.Add(ref _sumFlushDelay, flushDelay);
                Interlocked.Exchange(ref _maxFlushSize, Math.Max(Interlocked.Read(ref _maxFlushSize), flushSize));
                Interlocked.Exchange(ref _maxFlushDelay, Math.Max(Interlocked.Read(ref _maxFlushDelay), flushDelay));
                _statIndex = (_statIndex + 1) & (LastStatsCount - 1);

                PurgeNotProcessedInfo();

                return true;
            }
            return false;
        }

        private void PurgeNotProcessedInfo()
        {
            _indexWriter.PurgeNotProcessedCommitsTill(Db.Config.ChaserCheckpoint.Read());
            _indexWriter.PurgeNotProcessedTransactions(Db.Config.WriterCheckpoint.Read());
        }

        private readonly struct WriteResult
        {
            public readonly long WrittenPos;
            public readonly long NewPos;
            public readonly PrepareLogRecord Prepare;

            public WriteResult(long writtenPos, long newPos, PrepareLogRecord prepare)
            {
                WrittenPos = writtenPos;
                NewPos = newPos;
                Prepare = prepare;
            }
        }

        public void Handle(MonitoringMessage.InternalStatsRequest message)
        {
            var lastFlushSize = Interlocked.Read(ref _lastFlushSize);
            var lastFlushDelayMs = Interlocked.Read(ref _lastFlushDelay) / (double)TicksPerMs;
            var statCount = _statCount;
            var meanFlushSize = 0u >= (uint)statCount ? 0L : Interlocked.Read(ref _sumFlushSize) / statCount;
            var meanFlushDelayMs = 0u >= (uint)statCount
                ? 0D
                : Interlocked.Read(ref _sumFlushDelay) / (double)TicksPerMs / statCount;
            var maxFlushSize = Interlocked.Read(ref _maxFlushSize);
            var maxFlushDelayMs = Interlocked.Read(ref _maxFlushDelay) / (double)TicksPerMs;
            var queuedFlushMessages = FlushMessagesInQueue;

            var stats = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                {"es-writer-lastFlushSize", new StatMetadata(lastFlushSize, "Writer Last Flush Size")},
                {"es-writer-lastFlushDelayMs", new StatMetadata(lastFlushDelayMs, "Writer Last Flush Delay, ms")},
                {"es-writer-meanFlushSize", new StatMetadata(meanFlushSize, "Writer Mean Flush Size")},
                {"es-writer-meanFlushDelayMs", new StatMetadata(meanFlushDelayMs, "Writer Mean Flush Delay, ms")},
                {"es-writer-maxFlushSize", new StatMetadata(maxFlushSize, "Writer Max Flush Size")},
                {"es-writer-maxFlushDelayMs", new StatMetadata(maxFlushDelayMs, "Writer Max Flush Delay, ms")},
                {"es-writer-queuedFlushMessages", new StatMetadata(queuedFlushMessages, "Writer Queued Flush Message")}
            };

            message.Envelope.ReplyWith(new MonitoringMessage.InternalStatsRequestResponse(stats));
        }
    }
}