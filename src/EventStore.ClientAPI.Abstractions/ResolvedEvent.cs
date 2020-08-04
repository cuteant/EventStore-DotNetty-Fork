using System;

namespace EventStore.ClientAPI
{
    /// <summary>A structure representing a single event or an resolved link event.</summary>
    public readonly struct ResolvedEvent : IResolvedEvent
    {
        /// <summary>The event, or the resolved link event if this <see cref="ResolvedEvent"/> is a link event.</summary>
        public readonly RecordedEvent Event;

        /// <summary>The link event if this <see cref="ResolvedEvent"/> is a link event.</summary>
        public readonly RecordedEvent Link;

        /// <summary>Returns the event that was read or which triggered the subscription.
        ///
        /// If this <see cref="ResolvedEvent"/> represents a link event, the Link
        /// will be the <see cref="OriginalEvent"/>, otherwise it will be the event.</summary>
        public RecordedEvent OriginalEvent => Link ?? Event;

        /// <summary>Indicates whether this <see cref="ResolvedEvent"/> is a resolved link event.</summary>
        public bool IsResolved => Link is object && Event is object;

        /// <summary>The logical position of the <see cref="OriginalEvent"/>.</summary>
        public readonly Position? OriginalPosition;

        /// <summary>The stream name of the <see cref="OriginalEvent" />.</summary>
        public string OriginalStreamId => OriginalEvent.EventStreamId;

        /// <summary>The event number in the stream of the <see cref="OriginalEvent"/>.</summary>
        public long OriginalEventNumber => OriginalEvent.EventNumber;

        Position? IResolvedEvent.OriginalPosition => OriginalPosition;
        string IResolvedEvent.OriginalEventType => OriginalEvent.EventType;
        Guid IResolvedEvent.OriginalEventId => OriginalEvent.EventId;

        internal ResolvedEvent(RecordedEvent evnt, RecordedEvent link, Position? position)
        {
            Event = evnt;
            Link = link;
            OriginalPosition = position;
            _isDropping = false;
        }

        internal ResolvedEvent(bool isSystem)
        {
            _isDropping = true;
            Event = null;
            Link = null;
            OriginalPosition = null;
        }

        bool IResolvedEvent.IsDropping => _isDropping;
        private readonly bool _isDropping;
    }
}