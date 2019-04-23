using System;
using System.Collections.Generic;
using DotNetty.Common;
using EventStore.Core.Bus;
using EventStore.Core.Messages;
using EventStore.Core.DataStructures;
using EventStore.Core.TransactionLog;
using EventStore.Core.TransactionLog.Checkpoint;
using EventStore.Core.TransactionLog.LogRecords;
using Microsoft.Extensions.Logging;

namespace EventStore.Core.Services.Storage.EpochManager
{
    public class EpochManager : IEpochManager
    {
        private static readonly ILogger Log = TraceLogger.GetLogger<EpochManager>();
        private readonly IPublisher _bus;

        public readonly int CachedEpochCount;
        public int LastEpochNumber { get { return _lastEpochNumber; } }

        private readonly ICheckpoint _checkpoint;
        private readonly ObjectPool<ITransactionFileReader> _readers;
        private readonly ITransactionFileWriter _writer;

        private readonly object _locker = new object();
        private readonly Dictionary<int, EpochRecord> _epochs = new Dictionary<int, EpochRecord>();
        private volatile int _lastEpochNumber = -1;
        private long _lastEpochPosition = -1;
        private int _minCachedEpochNumber = -1;

        public EpochManager(IPublisher bus,
                              int cachedEpochCount,
                              ICheckpoint checkpoint,
                              ITransactionFileWriter writer,
                              int initialReaderCount,
                              int maxReaderCount,
                              Func<ITransactionFileReader> readerFactory)
        {
            if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }
            if (cachedEpochCount < 0) { ThrowHelper.ThrowArgumentOutOfRangeException_Nonnegative(ExceptionArgument.cachedEpochCount); }
            if (null == checkpoint) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.checkpoint); }
            if (null == writer) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.writer); }
            if (initialReaderCount < 0) { ThrowHelper.ThrowArgumentOutOfRangeException_Nonnegative(ExceptionArgument.initialReaderCount); }
            if (maxReaderCount <= 0) { ThrowHelper.ThrowArgumentOutOfRangeException_Positive(ExceptionArgument.maxReaderCount); }
            if (initialReaderCount > maxReaderCount)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException_InitialReaderCountIsGreaterThanMaxReaderCount();
            }
            if (null == readerFactory) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.readerFactory); }

            _bus = bus;
            CachedEpochCount = cachedEpochCount;
            _checkpoint = checkpoint;
            _readers = new ObjectPool<ITransactionFileReader>("EpochManager readers pool", initialReaderCount, maxReaderCount, readerFactory);
            _writer = writer;
        }

        public void Init()
        {
            ReadEpochs(CachedEpochCount);
        }

        public EpochRecord GetLastEpoch()
        {
            lock (_locker)
            {
                return _lastEpochNumber < 0 ? null : GetEpoch(_lastEpochNumber, throwIfNotFound: true);
            }
        }

        private void ReadEpochs(int maxEpochCount)
        {
            lock (_locker)
            {
                var reader = _readers.Get();
                try
                {
                    long epochPos = _checkpoint.Read();
                    if (epochPos < 0) // we probably have lost/uninitialized epoch checkpoint
                    {
                        reader.Reposition(_writer.Checkpoint.Read());

                        SeqReadResult result;
                        while ((result = reader.TryReadPrev()).Success)
                        {
                            var rec = result.LogRecord;
                            if (rec.RecordType != LogRecordType.System || ((SystemLogRecord)rec).SystemRecordType != SystemRecordType.Epoch)
                            {
                                continue;
                            }
                            epochPos = rec.LogPosition;
                            break;
                        }
                    }

                    int cnt = 0;
                    while (epochPos >= 0 && cnt < maxEpochCount)
                    {
                        var result = reader.TryReadAt(epochPos);
                        if (!result.Success)
                        {
                            ThrowHelper.ThrowException_CouldNotFindEpochRecordAtLogPosition(epochPos);
                        }

                        if (result.LogRecord.RecordType != LogRecordType.System)
                        {
                            ThrowHelper.ThrowException_LogRecordIsNotSystemLogRecord(result);
                        }

                        var sysRec = (SystemLogRecord)result.LogRecord;
                        if (sysRec.SystemRecordType != SystemRecordType.Epoch)
                        {
                            ThrowHelper.ThrowException_SystemLogRecordIsNotOfEpochSubType(result);
                        }

                        var epoch = sysRec.GetEpochRecord();
                        _epochs[epoch.EpochNumber] = epoch;
                        _lastEpochNumber = Math.Max(_lastEpochNumber, epoch.EpochNumber);
                        _lastEpochPosition = Math.Max(_lastEpochPosition, epoch.EpochPosition);
                        _minCachedEpochNumber = epoch.EpochNumber;

                        epochPos = epoch.PrevEpochPosition;
                        cnt += 1;
                    }
                }
                finally
                {
                    _readers.Return(reader);
                }
            }
        }

        public EpochRecord[] GetLastEpochs(int maxCount)
        {
            lock (_locker)
            {
                var res = ThreadLocalList<EpochRecord>.NewInstance();
                try
                {
                    for (int epochNum = _lastEpochNumber, n = maxCount; epochNum >= 0 && n > 0; --epochNum, --n)
                    {
                        if (!_epochs.TryGetValue(epochNum, out EpochRecord epoch)) { break; }
                        res.Add(epoch);
                    }
                    return res.ToArray();
                }
                finally
                {
                    res.Return();
                }
            }
        }

        public EpochRecord GetEpoch(int epochNumber, bool throwIfNotFound)
        {
            lock (_locker)
            {
                if (epochNumber < _minCachedEpochNumber)
                {
                    if (!throwIfNotFound) { return null; }
                    ThrowHelper.ThrowArgumentOutOfRangeException_EpochNumberRequestedShouldNotBeCached(epochNumber, _minCachedEpochNumber);
                }
                if (!_epochs.TryGetValue(epochNumber, out EpochRecord epoch) && throwIfNotFound)
                {
                    ThrowHelper.ThrowException_ConcurrencyFailureEpochIsNull(epochNumber);
                }

                return epoch;
            }
        }

        public bool IsCorrectEpochAt(long epochPosition, int epochNumber, Guid epochId)
        {
            if (epochPosition < 0) { ThrowHelper.ThrowArgumentOutOfRangeException_Nonnegative(ExceptionArgument.epochPosition); }
            if (epochNumber < 0) { ThrowHelper.ThrowArgumentOutOfRangeException_Nonnegative(ExceptionArgument.epochNumber); }
            if (Guid.Empty == epochId) { ThrowHelper.ThrowArgumentException_NotEmptyGuid(ExceptionArgument.epochId); }

            lock (_locker)
            {
                if (epochNumber > _lastEpochNumber) { return false; }
                if (epochNumber >= _minCachedEpochNumber)
                {
                    var epoch = _epochs[epochNumber];
                    return epoch.EpochId == epochId && epoch.EpochPosition == epochPosition;
                }
            }

            // epochNumber < _minCachedEpochNumber
            var reader = _readers.Get();
            try
            {
                var res = reader.TryReadAt(epochPosition);
                if (!res.Success || res.LogRecord.RecordType != LogRecordType.System) { return false; }
                var sysRec = (SystemLogRecord)res.LogRecord;
                if (sysRec.SystemRecordType != SystemRecordType.Epoch) { return false; }

                var epoch = sysRec.GetEpochRecord();
                return epoch.EpochNumber == epochNumber && epoch.EpochId == epochId;
            }
            finally
            {
                _readers.Return(reader);
            }
        }

        // This method should be called from single thread.
        public void WriteNewEpoch()
        {
            // Set epoch checkpoint to -1, so if we crash after new epoch record was written, 
            // but epoch checkpoint wasn't updated, on restart we don't miss the latest epoch.
            // So on node start, if there is no epoch checkpoint or it contains negative position, 
            // we do sequential scan from the end of TF to find the latest epoch record.
            //NOTE AN: It seems we don't need to pessimistically set epoch checkpoint to -1, because
            //NOTE AN: if crash occurs in the middle of writing epoch or updating epoch checkpoint,
            //NOTE AN: then on restart we'll start from chaser checkpoint (which is not updated yet)
            //NOTE AN: and process all records till the writer checkpoint, so all epochs will be processed 
            //NOTE AN: and epoch checkpoint will ultimately contain correct last epoch position. This process
            //NOTE AN: is similar to index rebuild process.
            //_checkpoint.Write(-1);
            //_checkpoint.Flush();

            // Now we write epoch record (with possible retry, if we are at the end of chunk) 
            // and update EpochManager's state, by adjusting cache of records, epoch count and un-caching 
            // excessive record, if present.
            // If we are writing the very first epoch, last position will be -1.
            var epoch = WriteEpochRecordWithRetry(_lastEpochNumber + 1, Guid.NewGuid(), _lastEpochPosition);
            UpdateLastEpoch(epoch, flushWriter: true);
        }

        private EpochRecord WriteEpochRecordWithRetry(int epochNumber, Guid epochId, long lastEpochPosition)
        {
            long pos = _writer.Checkpoint.ReadNonFlushed();
            var epoch = new EpochRecord(pos, epochNumber, epochId, lastEpochPosition, DateTime.UtcNow);
            var rec = new SystemLogRecord(epoch.EpochPosition, epoch.TimeStamp, SystemRecordType.Epoch, SystemRecordSerialization.Json, epoch.AsSerialized());

            if (!_writer.Write(rec, out pos))
            {
                epoch = new EpochRecord(pos, epochNumber, epochId, lastEpochPosition, DateTime.UtcNow);
                rec = new SystemLogRecord(epoch.EpochPosition, epoch.TimeStamp, SystemRecordType.Epoch, SystemRecordSerialization.Json, epoch.AsSerialized());
                if (!_writer.Write(rec, out pos))
                {
                    ThrowHelper.ThrowException_SecondWriteTryFailed(epoch.EpochPosition);
                }
            }
            if (Log.IsDebugLevelEnabled()) Log.Writing_Epoch_previous_epoch_at(epochNumber, epoch.EpochPosition, epochId, lastEpochPosition);

            _bus.Publish(new SystemMessage.EpochWritten(epoch));
            return epoch;
        }

        public void SetLastEpoch(EpochRecord epoch)
        {
            if (null == epoch) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.epoch); }

            lock (_locker)
            {
                if (epoch.EpochPosition > _lastEpochPosition)
                {
                    UpdateLastEpoch(epoch, flushWriter: false);
                    return;
                }
            }

            // Epoch record must have been already written, so we need to make sure it is where we expect it to be.
            // If this check fails, then there is something very wrong with epochs, data corruption is possible.
            if (!IsCorrectEpochAt(epoch.EpochPosition, epoch.EpochNumber, epoch.EpochId))
            {
                ThrowHelper.ThrowException_NotFoundEpoch(epoch.EpochPosition, epoch.EpochNumber, epoch.EpochId);
            }
        }

        private void UpdateLastEpoch(EpochRecord epoch, bool flushWriter)
        {
            lock (_locker)
            {
                _epochs[epoch.EpochNumber] = epoch;
                _lastEpochNumber = epoch.EpochNumber;
                _lastEpochPosition = epoch.EpochPosition;
                _minCachedEpochNumber = Math.Max(_minCachedEpochNumber, epoch.EpochNumber - CachedEpochCount + 1);
                _epochs.Remove(_minCachedEpochNumber - 1);

                if (flushWriter) { _writer.Flush(); }
                // Now update epoch checkpoint, so on restart we don't scan sequentially TF.
                _checkpoint.Write(epoch.EpochPosition);
                _checkpoint.Flush();

                if (Log.IsDebugLevelEnabled()) { Log.Update_Last_Epoch(epoch); }
            }
        }

        public EpochRecord GetEpochWithAllEpochs(int epochNumber, bool throwIfNotFound)
        {
            ReadEpochs(int.MaxValue);
            return GetEpoch(epochNumber, throwIfNotFound);
        }
    }
}
