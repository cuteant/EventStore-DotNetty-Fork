using System;

namespace EventStore.ClientAPI
{
    public readonly struct PersistentSubscriptionResolvedEvent<T> : IPersistentSubscriptionResolvedEvent<T>
    {
        /// <summary>RetryCount</summary>
        public readonly int? RetryCount;

        /// <summary>Event</summary>
        public readonly ResolvedEvent<T> Event;

        /// <summary>Default constructor</summary>
        /// <param name="event"></param>
        /// <param name="retryCount"></param>
        internal PersistentSubscriptionResolvedEvent(ResolvedEvent<T> @event, int? retryCount)
        {
            Event = @event;
            RetryCount = retryCount;
        }

        int? IPersistentSubscriptionResolvedEvent.RetryCount => RetryCount;

        IRecordedEvent<T> IResolvedEvent<T>.OriginalEvent => Event.OriginalEvent;

        T IResolvedEvent<T>.Body => Event.Body;

        bool IResolvedEvent.IsResolved => Event.IsResolved;

        Position? IResolvedEvent.OriginalPosition => Event.OriginalPosition;

        string IResolvedEvent.OriginalStreamId => Event.OriginalStreamId;

        Guid IResolvedEvent.OriginalEventId => Event.OriginalEvent.EventId;

        long IResolvedEvent.OriginalEventNumber => Event.OriginalEventNumber;

        string IResolvedEvent.OriginalEventType => Event.OriginalEvent.EventType;

        IEventDescriptor IResolvedEvent2.OriginalEventDescriptor => Event.OriginalEventDescriptor;

        object IResolvedEvent2.GetBody() => Event.Body;

        IRecordedEvent IResolvedEvent2.GetOriginalEvent() => Event.OriginalEvent;

        /// <summary>ResolvedEvent</summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public static implicit operator ResolvedEvent<T>(PersistentSubscriptionResolvedEvent<T> x) => x.Event;
    }
}
