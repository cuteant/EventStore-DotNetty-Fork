using System;

namespace EventStore.ClientAPI
{
    internal interface IConnectToPersistentSubscriptions
    {
        void NotifyEventsProcessed(Guid processedEvent);
        void NotifyEventsProcessed(Guid[] processedEvents);
        void NotifyEventsFailed(Guid processedEvent, PersistentSubscriptionNakEventAction action, string reason);
        void NotifyEventsFailed(Guid[] processedEvents, PersistentSubscriptionNakEventAction action, string reason);
        void Unsubscribe();
    }
}