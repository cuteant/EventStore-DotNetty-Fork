using System;
using EventStore.ClientAPI.Resilience;

namespace EventStore.ClientAPI.Subscriptions
{
    /// <summary>
    /// Represents a subscription to EventStore that has been dropped that is used by the
    /// DroppedSubscriptionPolicy to handle it.
    /// </summary>
    public class DroppedSubscription
    {
        public DroppedSubscription(ISubscription subscription, Exception exception, SubscriptionDropReason dropReason)
        {
            StreamId = subscription.StreamId;
            Error = exception;
            DropReason = dropReason;
            RetryPolicy = subscription.RetryPolicy;
        }

        public string StreamId { get; }
        public Exception Error { get; }
        public SubscriptionDropReason DropReason { get; }
        public RetryPolicy RetryPolicy { get; }
    }
}