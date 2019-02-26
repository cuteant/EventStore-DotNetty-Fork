using System;

namespace EventStore.ClientAPI
{
    public interface IPersistentSubscriptionResolvedEvent<out T> : IResolvedEvent<T>, IPersistentSubscriptionResolvedEvent2
    {
    }

    public interface IPersistentSubscriptionResolvedEvent2 : IPersistentSubscriptionResolvedEvent, IResolvedEvent2
    {
    }

    public interface IPersistentSubscriptionResolvedEvent : IResolvedEvent
    {
        int? RetryCount { get; }
    }
}
