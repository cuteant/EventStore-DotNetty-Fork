using System;
using System.IO;
using CuteAnt.IO;
using CuteAnt.Text;
using EventStore.Common.Utils;
using EventStore.Core.Data;
using EventStore.Core.Services;

namespace EventStore.Core.TransactionLog.LogRecords
{
    public enum LogRecordType
    {
        Prepare = 0,
        Commit = 1,
        System = 2
    }

    public class LogRecordVersion
    {
        public const byte LogRecordV0 = 0;
        public const byte LogRecordV1 = 1;
    }

    public abstract class LogRecord
    {
        public static readonly byte[] NoData = Empty.ByteArray;

        public readonly LogRecordType RecordType;
        public readonly byte Version;
        public readonly long LogPosition;

        public long GetNextLogPosition(long logicalPosition, int length)
        {
            return logicalPosition + length + 2 * sizeof(int);
        }

        public long GetPrevLogPosition(long logicalPosition, int length)
        {
            return logicalPosition - length - 2 * sizeof(int);
        }

        public static LogRecord ReadFrom(BinaryReader reader)
        {
            var recordType = (LogRecordType)reader.ReadByte();
            var version = reader.ReadByte();
            var logPosition = reader.ReadInt64();

            if (logPosition < 0) { ThrowHelper.ThrowArgumentOutOfRangeException_Nonnegative(ExceptionArgument.logPosition); }

            switch (recordType)
            {
                case LogRecordType.Prepare:
                    return new PrepareLogRecord(reader, version, logPosition);
                case LogRecordType.Commit:
                    return new CommitLogRecord(reader, version, logPosition);
                case LogRecordType.System:
                    return new SystemLogRecord(reader, version, logPosition);
                default:
                    ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.recordType); return null;
            }
        }

        public static PrepareLogRecord Prepare(long logPosition, Guid correlationId, Guid eventId, long transactionPos, int transactionOffset,
                                               string eventStreamId, long expectedVersion, PrepareFlags flags, string eventType,
                                               byte[] data, byte[] metadata, DateTime? timeStamp = null)
        {
            return new PrepareLogRecord(logPosition, correlationId, eventId, transactionPos, transactionOffset,
                                        eventStreamId, expectedVersion, timeStamp ?? DateTime.UtcNow, flags, eventType,
                                        data, metadata);
        }

        public static CommitLogRecord Commit(long logPosition, Guid correlationId, long startPosition, long eventNumber)
        {
            return new CommitLogRecord(logPosition, correlationId, startPosition, DateTime.UtcNow, eventNumber);
        }

        public static PrepareLogRecord SingleWrite(long logPosition, Guid correlationId, Guid eventId, string eventStreamId,
                                                   long expectedVersion, string eventType, byte[] data, byte[] metadata,
                                                   DateTime? timestamp = null, PrepareFlags? additionalFlags = null)
        {
            return new PrepareLogRecord(logPosition, correlationId, eventId, logPosition, 0, eventStreamId, expectedVersion,
                                        timestamp ?? DateTime.UtcNow,
                                        PrepareFlags.Data | PrepareFlags.TransactionBegin | PrepareFlags.TransactionEnd | (additionalFlags ?? PrepareFlags.None),
                                        eventType, data, metadata);
        }

        public static PrepareLogRecord TransactionBegin(long logPos, Guid correlationId, string eventStreamId, long expectedVersion)
        {
            return new PrepareLogRecord(logPos, correlationId, Guid.NewGuid(), logPos, -1, eventStreamId, expectedVersion,
                                        DateTime.UtcNow, PrepareFlags.TransactionBegin, null, NoData, NoData);
        }

        public static PrepareLogRecord TransactionWrite(long logPosition, Guid correlationId, Guid eventId, long transactionPos, int transactionOffset, string eventStreamId, string eventType, byte[] data, byte[] metadata, bool isJson)
        {
            return new PrepareLogRecord(logPosition, correlationId, eventId, transactionPos, transactionOffset,
                                        eventStreamId, ExpectedVersion.Any, DateTime.UtcNow, PrepareFlags.Data | (isJson ? PrepareFlags.IsJson : PrepareFlags.None),
                                        eventType, data, metadata);
        }

        public static PrepareLogRecord TransactionEnd(long logPos, Guid correlationId, Guid eventId, long transactionPos, string eventStreamId)
        {
            return new PrepareLogRecord(logPos, correlationId, eventId, transactionPos, -1, eventStreamId, ExpectedVersion.Any,
                                        DateTime.UtcNow, PrepareFlags.TransactionEnd, null, NoData, NoData);
        }

        public static PrepareLogRecord DeleteTombstone(long logPosition, Guid correlationId, Guid eventId,
                                                       string eventStreamId, long expectedVersion, PrepareFlags additionalFlags = PrepareFlags.None)
        {
            return new PrepareLogRecord(logPosition, correlationId, eventId, logPosition, 0, eventStreamId,
                                        expectedVersion, DateTime.UtcNow,
                                        PrepareFlags.StreamDelete | PrepareFlags.TransactionBegin | PrepareFlags.TransactionEnd | additionalFlags,
                                        SystemEventTypes.StreamDeleted, NoData, NoData);
        }

        protected LogRecord(LogRecordType recordType, byte version, long logPosition)
        {
            if (logPosition < 0) { ThrowHelper.ThrowArgumentOutOfRangeException_Nonnegative(ExceptionArgument.logPosition); }
            RecordType = recordType;
            Version = version;
            LogPosition = logPosition;
        }

        public virtual void WriteTo(BinaryWriter writer)
        {
            writer.Write((byte)RecordType);
            writer.Write(Version);
            writer.Write(LogPosition);
        }

        public int GetSizeWithLengthPrefixAndSuffix()
        {
            using (var memoryStream = MemoryStreamManager.GetStream())
            {
                WriteTo(new BinaryWriter(memoryStream, StringHelper.SecureUTF8NoBOM, true));
                return 8 + (int)memoryStream.Length;
            }
        }

        internal void WriteWithLengthPrefixAndSuffixTo(BinaryWriter writer)
        {
            using (var memoryStream = MemoryStreamManager.GetStream())
            {
                WriteTo(new BinaryWriter(memoryStream, StringHelper.SecureUTF8NoBOM, true));
                var length = (int)memoryStream.Length;
                writer.Write(length);
                writer.Write(memoryStream.GetBuffer(), 0, (int)memoryStream.Length);
                writer.Write(length);
            }
        }
    }
}