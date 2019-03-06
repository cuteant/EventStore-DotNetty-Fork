using System;

namespace EventStore.ClientAPI
{
    /// <summary>Represents a previously written event</summary>
    public sealed class RecordedEvent<T> : IRecordedEvent<T>
    {
        /// <summary>The Event Stream that this event belongs to</summary>
        public readonly string EventStreamId;

        /// <summary>The Unique Identifier representing this event</summary>
        public readonly Guid EventId;

        /// <summary>The number of this event in the stream</summary>
        public readonly long EventNumber;

        /// <summary>The type of event this is</summary>
        public readonly string EventType;

        /// <summary>The full event info.</summary>
        public readonly IFullEvent<T> FullEvent;

        /// <summary>Indicates whether the content is internally marked as json</summary>
        public readonly bool IsJson;

        /// <summary>A datetime representing when this event was created in the system</summary>
        public DateTime Created;

        /// <summary>A long representing the milliseconds since the epoch when the was created in the system</summary>
        public long CreatedEpoch;

        internal RecordedEvent(string streamId, Guid eventId, long eventNumber, string eventType,
            long? created, long? createdEpoch, IFullEvent<T> fullEvent, bool isJson)
        {
            EventStreamId = streamId;

            EventId = eventId;
            EventNumber = eventNumber;

            EventType = eventType;
            if (created.HasValue)
            {
                Created = DateTime.FromBinary(created.Value);
            }
            if (createdEpoch.HasValue)
            {
                CreatedEpoch = createdEpoch.Value;
            }
            FullEvent = fullEvent;
            IsJson = isJson;
        }

        #region -- IRecordedEvent Members --

        string IRecordedEvent.EventStreamId => EventStreamId;
        Guid IRecordedEvent.EventId => EventId;
        long IRecordedEvent.EventNumber => EventNumber;
        string IRecordedEvent.EventType => EventType;
        bool IRecordedEvent.IsJson => IsJson;
        DateTime IRecordedEvent.Created => Created;
        long IRecordedEvent.CreatedEpoch => CreatedEpoch;
        IFullEvent<T> IRecordedEvent<T>.FullEvent => FullEvent;

        #endregion
    }
}