using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using CuteAnt.Buffers;
using CuteAnt.Pool;
using EventStore.Core.Bus;
using EventStore.Core.Data;
using EventStore.Core.Index;
using EventStore.Core.Messages;
using EventStore.Core.TransactionLog;
using EventStore.Core.TransactionLog.LogRecords;
using Microsoft.Extensions.Logging;

namespace EventStore.Core.Services.Storage.ReaderIndex
{
    public interface IIndexCommitter
    {
        long LastCommitPosition { get; }
        void Init(long buildToPosition);
        void Dispose();
        long Commit(CommitLogRecord commit, bool isTfEof, bool cacheLastEventNumber);
        long Commit(IList<PrepareLogRecord> commitedPrepares, bool isTfEof, bool cacheLastEventNumber);
        long GetCommitLastEventNumber(CommitLogRecord commit);
    }

    public class IndexCommitter : IIndexCommitter
    {
        public static readonly ILogger Log = TraceLogger.GetLogger<IndexCommitter>();

        public long LastCommitPosition { get { return Interlocked.Read(ref _lastCommitPosition); } }

        private readonly IPublisher _bus;
        private readonly IIndexBackend _backend;
        private readonly IIndexReader _indexReader;
        private readonly ITableIndex _tableIndex;
        private readonly bool _additionalCommitChecks;
        private long _persistedPreparePos = -1;
        private long _persistedCommitPos = -1;
        private bool _indexRebuild = true;
        private long _lastCommitPosition = -1;

        public IndexCommitter(IPublisher bus, IIndexBackend backend, IIndexReader indexReader,
                                ITableIndex tableIndex, bool additionalCommitChecks)
        {
            _bus = bus;
            _backend = backend;
            _indexReader = indexReader;
            _tableIndex = tableIndex;
            _additionalCommitChecks = additionalCommitChecks;
        }

        public void Init(long buildToPosition)
        {
            var infoEnabled = Log.IsInformationLevelEnabled();
            if (infoEnabled) Log.TableIndexInitialization();

            _tableIndex.Initialize(buildToPosition);
            _persistedPreparePos = _tableIndex.PrepareCheckpoint;
            _persistedCommitPos = _tableIndex.CommitCheckpoint;
            _lastCommitPosition = _tableIndex.CommitCheckpoint;

            if (_lastCommitPosition >= buildToPosition)
            {
                ThrowHelper.ThrowException_LastCommitPositionIsGreaterThanOrEqualBuildToPosition(_lastCommitPosition, buildToPosition);
            }

            var startTime = DateTime.UtcNow;
            var lastTime = DateTime.UtcNow;
            var reportPeriod = TimeSpan.FromSeconds(5);

            if (infoEnabled) Log.ReadIndexBuilding();

            _indexRebuild = true;
            var debugEnabled = Log.IsDebugLevelEnabled();
            using (var reader = _backend.BorrowReader())
            {
                var startPosition = Math.Max(0, _persistedCommitPos);
                reader.Reposition(startPosition);

                var commitedPrepares = new List<PrepareLogRecord>();

                long processed = 0;
                SeqReadResult result;
                while ((result = reader.TryReadNext()).Success && result.LogRecord.LogPosition < buildToPosition)
                {
                    switch (result.LogRecord.RecordType)
                    {
                        case LogRecordType.Prepare:
                            {
                                var prepare = (PrepareLogRecord)result.LogRecord;
                                if (prepare.Flags.HasAnyOf(PrepareFlags.IsCommitted))
                                {
                                    if (prepare.Flags.HasAnyOf(PrepareFlags.SingleWrite))
                                    {
                                        Commit(commitedPrepares, false, false);
                                        commitedPrepares.Clear();
                                        Commit(new[] { prepare }, result.Eof, false);
                                    }
                                    else
                                    {

                                        if (prepare.Flags.HasAnyOf(PrepareFlags.Data | PrepareFlags.StreamDelete))
                                            commitedPrepares.Add(prepare);
                                        if (prepare.Flags.HasAnyOf(PrepareFlags.TransactionEnd))
                                        {
                                            Commit(commitedPrepares, result.Eof, false);
                                            commitedPrepares.Clear();
                                        }
                                    }
                                }
                                break;
                            }
                        case LogRecordType.Commit:
                            Commit((CommitLogRecord)result.LogRecord, result.Eof, false);
                            break;
                        case LogRecordType.System:
                            break;
                        default:
                            ThrowHelper.ThrowException_UnknownRecordType(result.LogRecord.RecordType); break;
                    }

                    processed += 1;
                    if (DateTime.UtcNow - lastTime > reportPeriod || processed % 100000 == 0)
                    {
                        if (debugEnabled) { Log.ReadIndexRebuilding(processed, result.RecordPostPosition, startPosition, buildToPosition); }
                        lastTime = DateTime.UtcNow;
                    }
                }
                if (debugEnabled) Log.ReadIndex_rebuilding_done(processed, startTime);
                _bus.Publish(new StorageMessage.TfEofAtNonCommitRecord());
                _backend.SetSystemSettings(GetSystemSettings());
            }

            _indexRebuild = false;
        }

        public void Dispose()
        {
            try
            {
                _tableIndex.Close(removeFiles: false);
            }
            catch (TimeoutException exc)
            {
                Log.TimeoutExceptionWhenTryingToCloseTableindex(exc);
                throw;
            }
        }

        public long GetCommitLastEventNumber(CommitLogRecord commit)
        {
            long eventNumber = EventNumber.Invalid;

            var lastCommitPosition = Interlocked.Read(ref _lastCommitPosition);
            if (commit.LogPosition < lastCommitPosition || (commit.LogPosition == lastCommitPosition && !_indexRebuild))
                return eventNumber;

            foreach (var prepare in GetTransactionPrepares(commit.TransactionPosition, commit.LogPosition))
            {
                if (prepare.Flags.HasNoneOf(PrepareFlags.StreamDelete | PrepareFlags.Data))
                    continue;
                eventNumber = prepare.Flags.HasAllOf(PrepareFlags.StreamDelete)
                                      ? EventNumber.DeletedStream
                                      : commit.FirstEventNumber + prepare.TransactionOffset;
            }
            return eventNumber;
        }

        public long Commit(CommitLogRecord commit, bool isTfEof, bool cacheLastEventNumber)
        {
            long eventNumber = EventNumber.Invalid;

            var lastCommitPosition = Interlocked.Read(ref _lastCommitPosition);
            if (commit.LogPosition < lastCommitPosition || (commit.LogPosition == lastCommitPosition && !_indexRebuild))
            {
                return eventNumber;  // already committed
            }

            string streamId = null;
            var indexEntries = new List<IndexKey>();
            var prepares = new List<PrepareLogRecord>();

            foreach (var prepare in GetTransactionPrepares(commit.TransactionPosition, commit.LogPosition))
            {
                if (prepare.Flags.HasNoneOf(PrepareFlags.StreamDelete | PrepareFlags.Data)) { continue; }

                if (streamId == null)
                {
                    streamId = prepare.EventStreamId;
                }
                else
                {
                    if (prepare.EventStreamId != streamId)
                    {
                        ThrowHelper.ThrowException_ExpectedStream(streamId, prepare, commit.LogPosition);
                    }
                }
                eventNumber = prepare.Flags.HasAllOf(PrepareFlags.StreamDelete)
                                      ? EventNumber.DeletedStream
                                      : commit.FirstEventNumber + prepare.TransactionOffset;

                if (new TFPos(commit.LogPosition, prepare.LogPosition) > new TFPos(_persistedCommitPos, _persistedPreparePos))
                {
                    indexEntries.Add(new IndexKey(streamId, eventNumber, prepare.LogPosition));
                    prepares.Add(prepare);
                }
            }

            if (indexEntries.Count > 0)
            {
                if (_additionalCommitChecks && cacheLastEventNumber)
                {
                    CheckStreamVersion(streamId, indexEntries[0].Version, commit);
                    CheckDuplicateEvents(streamId, commit, indexEntries, prepares);
                }
                _tableIndex.AddEntries(commit.LogPosition, indexEntries); // atomically add a whole bulk of entries
            }

            if (eventNumber != EventNumber.Invalid)
            {
                if (eventNumber < 0) ThrowHelper.ThrowException_EventNumberIsIncorrect(eventNumber);

                if (cacheLastEventNumber)
                {
                    _backend.SetStreamLastEventNumber(streamId, eventNumber);
                }
                if (SystemStreams.IsMetastream(streamId))
                {
                    _backend.SetStreamMetadata(SystemStreams.OriginalStreamOf(streamId), null); // invalidate cached metadata
                }

                if (streamId == SystemStreams.SettingsStream)
                {
                    _backend.SetSystemSettings(DeserializeSystemSettings(prepares[prepares.Count - 1].Data));
                }
            }

            var newLastCommitPosition = Math.Max(commit.LogPosition, lastCommitPosition);
            if (Interlocked.CompareExchange(ref _lastCommitPosition, newLastCommitPosition, lastCommitPosition) != lastCommitPosition)
            {
                ThrowHelper.ThrowException(ExceptionResource.Concurrency_Error_In_ReadIndex_Commit);
            }

            if(!_indexRebuild)
            for (int i = 0, n = indexEntries.Count; i < n; ++i)
            {
                _bus.Publish(
                    new StorageMessage.EventCommitted(
                        commit.LogPosition,
                        new EventRecord(indexEntries[i].Version, prepares[i]),
                        isTfEof && i == n - 1));
            }

            return eventNumber;
        }

        public long Commit(IList<PrepareLogRecord> commitedPrepares, bool isTfEof, bool cacheLastEventNumber)
        {
            long eventNumber = EventNumber.Invalid;

            if (commitedPrepares.Count == 0) { return eventNumber; }

            var lastCommitPosition = Interlocked.Read(ref _lastCommitPosition);
            var lastPrepare = commitedPrepares[commitedPrepares.Count - 1];

            string streamId = lastPrepare.EventStreamId;
            var indexEntries = new List<IndexKey>();
            var prepares = new List<PrepareLogRecord>();

            foreach (var prepare in commitedPrepares)
            {
                if (prepare.Flags.HasNoneOf(PrepareFlags.StreamDelete | PrepareFlags.Data)) { continue; }

                if (prepare.EventStreamId != streamId)
                {
                    ThrowHelper.ThrowException_ExpectedStream(streamId, prepare, commitedPrepares);
                }

                if (prepare.LogPosition < lastCommitPosition || (prepare.LogPosition == lastCommitPosition && !_indexRebuild))
                {
                    continue;  // already committed
                }

                eventNumber = prepare.ExpectedVersion + 1; /* for committed prepare expected version is always explicit */

                if (new TFPos(prepare.LogPosition, prepare.LogPosition) > new TFPos(_persistedCommitPos, _persistedPreparePos))
                {
                    indexEntries.Add(new IndexKey(streamId, eventNumber, prepare.LogPosition));
                    prepares.Add(prepare);
                }
            }

            if (indexEntries.Count > 0)
            {
                if (_additionalCommitChecks && cacheLastEventNumber)
                {
                    CheckStreamVersion(streamId, indexEntries[0].Version, null); // TODO AN: bad passing null commit
                    CheckDuplicateEvents(streamId, null, indexEntries, prepares); // TODO AN: bad passing null commit
                }
                _tableIndex.AddEntries(lastPrepare.LogPosition, indexEntries); // atomically add a whole bulk of entries
            }

            if (eventNumber != EventNumber.Invalid)
            {
                if (eventNumber < 0) ThrowHelper.ThrowException_EventNumberIsIncorrect(eventNumber);

                if (cacheLastEventNumber)
                {
                    _backend.SetStreamLastEventNumber(streamId, eventNumber);
                }
                if (SystemStreams.IsMetastream(streamId))
                {
                    _backend.SetStreamMetadata(SystemStreams.OriginalStreamOf(streamId), null); // invalidate cached metadata
                }

                if (streamId == SystemStreams.SettingsStream)
                {
                    _backend.SetSystemSettings(DeserializeSystemSettings(prepares[prepares.Count - 1].Data));
                }
            }

            var newLastCommitPosition = Math.Max(lastPrepare.LogPosition, lastCommitPosition);
            if (Interlocked.CompareExchange(ref _lastCommitPosition, newLastCommitPosition, lastCommitPosition) != lastCommitPosition)
            {
                ThrowHelper.ThrowException(ExceptionResource.Concurrency_Error_In_ReadIndex_Commit);
            }

            if(!_indexRebuild)
            for (int i = 0, n = indexEntries.Count; i < n; ++i)
            {
                _bus.Publish(
                    new StorageMessage.EventCommitted(
                        prepares[i].LogPosition,
                        new EventRecord(indexEntries[i].Version, prepares[i]),
                        isTfEof && i == n - 1));
            }

            return eventNumber;
        }

        private IEnumerable<PrepareLogRecord> GetTransactionPrepares(long transactionPos, long commitPos)
        {
            using (var reader = _backend.BorrowReader())
            {
                reader.Reposition(transactionPos);

                // in case all prepares were scavenged, we should not read past Commit LogPosition
                SeqReadResult result;
                while ((result = reader.TryReadNext()).Success && result.RecordPrePosition <= commitPos)
                {
                    if (result.LogRecord.RecordType != LogRecordType.Prepare) { continue; }

                    var prepare = (PrepareLogRecord)result.LogRecord;
                    if (prepare.TransactionPosition == transactionPos)
                    {
                        yield return prepare;
                        if (prepare.Flags.HasAnyOf(PrepareFlags.TransactionEnd)) { yield break; }
                    }
                }
            }
        }

        private void CheckStreamVersion(string streamId, long newEventNumber, CommitLogRecord commit)
        {
            if (newEventNumber == EventNumber.DeletedStream) { return; }

            long lastEventNumber = _indexReader.GetStreamLastEventNumber(streamId);
            if (newEventNumber != lastEventNumber + 1)
            {
                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }
                else
                    ThrowHelper.ThrowException_CommitInvariantViolation(newEventNumber, lastEventNumber, streamId, commit);
            }
        }

        private void CheckDuplicateEvents(string streamId, CommitLogRecord commit, IList<IndexKey> indexEntries, IList<PrepareLogRecord> prepares)
        {
            using (var reader = _backend.BorrowReader())
            {
                var entries = _tableIndex.GetRange(streamId, indexEntries[0].Version, indexEntries[indexEntries.Count - 1].Version);
                foreach (var indexEntry in entries)
                {
                    int prepareIndex = (int)(indexEntry.Version - indexEntries[0].Version);
                    var prepare = prepares[prepareIndex];
                    PrepareLogRecord indexedPrepare = GetPrepare(reader, indexEntry.Position);
                    if (indexedPrepare != null && indexedPrepare.EventStreamId == prepare.EventStreamId)
                    {
                        if (Debugger.IsAttached)
                        {
                            Debugger.Break();
                        }
                        else
                        {
                            ThrowHelper.ThrowException_TryingToAddDuplicateEventToStream(indexEntry.Version, prepare, commit, indexedPrepare);
                        }
                    }
                }
            }
        }

        private SystemSettings GetSystemSettings()
        {
            var res = _indexReader.ReadEvent(SystemStreams.SettingsStream, -1);
            return res.Result == ReadEventResult.Success ? DeserializeSystemSettings(res.Record.Data) : null;
        }

        private static SystemSettings DeserializeSystemSettings(byte[] settingsData)
        {
            try
            {
                return SystemSettings.FromJsonBytes(settingsData);
            }
            catch (Exception exc)
            {
                Log.ErrorDeserializingSystemsettingsRecord(exc);
            }
            return null;
        }

        private static PrepareLogRecord GetPrepare(in TFReaderLease reader, long logPosition)
        {
            var result = reader.TryReadAt(logPosition);
            if (!result.Success) { return null; }
            if (result.LogRecord.RecordType != LogRecordType.Prepare)
            {
                ThrowHelper.ThrowException_IncorrectTypeOfLogRecord(result.LogRecord.RecordType);
            }

            return (PrepareLogRecord)result.LogRecord;
        }
    }
}
