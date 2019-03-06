using System;
#if DEBUG
using System.Text;
#endif

namespace EventStore.ClientAPI
{
    /// <summary>Represents a previously written event</summary>
    public sealed class RecordedEvent
    {
        /// <summary>The Event Stream that this event belongs to</summary>
        public readonly string EventStreamId;

        /// <summary>The Unique Identifier representing this event</summary>
        public readonly Guid EventId;

        /// <summary>The number of this event in the stream</summary>
        public readonly long EventNumber;

        /// <summary>The type of event this is</summary>
        public readonly string EventType;

        /// <summary>A byte array representing the data of this event</summary>
        public readonly byte[] Data;

        /// <summary>A byte array representing the metadata associated with this event</summary>
        public readonly byte[] Metadata;

        /// <summary>Indicates whether the content is internally marked as json</summary>
        public readonly bool IsJson;

        /// <summary>A datetime representing when this event was created in the system</summary>
        public DateTime Created;

        /// <summary>A long representing the milliseconds since the epoch when the was created in the system</summary>
        public long CreatedEpoch;

#if DEBUG
        /// <summary>Shows the event data interpreted as a UTF8-encoded string.
        /// NOTE: This is only available in DEBUG builds of the client API.</summary>
        public string DebugDataView
        {
            get { return Encoding.UTF8.GetString(Data); }
        }

        /// <summary>Shows the event metadata interpreted as a UTF8-encoded string.
        /// NOTE: This is only available in DEBUG builds of the client API.</summary>
        public string DebugMetadataView
        {
            get { return Encoding.UTF8.GetString(Metadata); }
        }
#endif

        internal RecordedEvent(string streamId, Guid eventId, long eventNumber, string eventType,
            long? created, long? createdEpoch, byte[] data, byte[] metaData, bool isJson)
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
            Data = data;
            Metadata = metaData;
            IsJson = isJson;
        }
    }
}