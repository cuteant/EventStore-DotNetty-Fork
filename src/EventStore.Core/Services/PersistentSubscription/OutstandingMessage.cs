using System;
using EventStore.Core.Data;

namespace EventStore.Core.Services.PersistentSubscription
{
    public readonly struct OutstandingMessage
    {
        public readonly ResolvedEvent ResolvedEvent;
        public readonly PersistentSubscriptionClient HandlingClient;
        public readonly int RetryCount;
        public readonly Guid EventId;
        public readonly bool IsReplayedEvent;

        public OutstandingMessage(Guid eventId, PersistentSubscriptionClient handlingClient, in ResolvedEvent resolvedEvent, int retryCount) : this()
        {
            EventId = eventId;
            HandlingClient = handlingClient;
            ResolvedEvent = resolvedEvent;
            RetryCount = retryCount;
            var originalStreamId = resolvedEvent.OriginalStreamId;
            IsReplayedEvent =
                originalStreamId.StartsWith("$persistentsubscription-", StringComparison.Ordinal) &&
                originalStreamId.EndsWith("-parked", StringComparison.Ordinal);
        }
    }
}