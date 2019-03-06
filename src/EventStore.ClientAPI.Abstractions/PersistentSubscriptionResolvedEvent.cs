using System;

namespace EventStore.ClientAPI
{
    public readonly struct PersistentSubscriptionResolvedEvent : IPersistentSubscriptionResolvedEvent
    {
        /// <summary>RetryCount</summary>
        public readonly int? RetryCount;

        /// <summary>Event</summary>
        public readonly ResolvedEvent Event;

        /// <summary>Default constructor</summary>
        /// <param name="event"></param>
        /// <param name="retryCount"></param>
        internal PersistentSubscriptionResolvedEvent(ResolvedEvent @event, int? retryCount)
        {
            Event = @event;
            RetryCount = retryCount;
            _isDropping = false;
        }

        bool IResolvedEvent.IsResolved => Event.IsResolved;

        Position? IResolvedEvent.OriginalPosition => Event.OriginalPosition;

        string IResolvedEvent.OriginalStreamId => Event.OriginalStreamId;

        Guid IResolvedEvent.OriginalEventId => Event.OriginalEvent.EventId;

        long IResolvedEvent.OriginalEventNumber => Event.OriginalEventNumber;

        string IResolvedEvent.OriginalEventType => Event.OriginalEvent.EventType;

        int? IPersistentSubscriptionResolvedEvent.RetryCount => this.RetryCount;

        /// <summary>ResolvedEvent</summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public static implicit operator ResolvedEvent(PersistentSubscriptionResolvedEvent x) => x.Event;

        internal PersistentSubscriptionResolvedEvent(bool isSystem)
        {
            _isDropping = true;
            Event = default;
            RetryCount = null;
        }

        bool IResolvedEvent.IsDropping => _isDropping;
        private readonly bool _isDropping;
    }
}
