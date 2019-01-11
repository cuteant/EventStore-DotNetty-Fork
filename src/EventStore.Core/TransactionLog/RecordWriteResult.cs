using System;

namespace EventStore.Core.TransactionLog
{
    public readonly struct RecordWriteResult
    {
        public readonly bool Success;
        public readonly long OldPosition;
        public readonly long NewPosition;

        public RecordWriteResult(bool success, long oldPosition, long newPosition)
        {
            if (newPosition < oldPosition)
                ThrowHelper.ThrowArgumentException(ExceptionResource.NewPositionIsLessThanOldPosition);

            Success = success;
            OldPosition = oldPosition;
            NewPosition = newPosition;
        }

        public static RecordWriteResult Failed(long position)
        {
            return new RecordWriteResult(false, position, position);
        }

        public static RecordWriteResult Successful(long oldPosition, long newPosition)
        {
            return new RecordWriteResult(true, oldPosition, newPosition);
        }

        public override string ToString()
        {
            return string.Format("Success: {0}, OldPosition: {1}, NewPosition: {2}", Success, OldPosition, NewPosition);
        }
    }
}