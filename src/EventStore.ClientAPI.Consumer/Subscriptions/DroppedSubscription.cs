using EventStore.ClientAPI.Resilience;

namespace EventStore.ClientAPI.Subscriptions
{
    /// <summary>
    /// Represents a subscription to EventStore that has been dropped that is used by the
    /// DroppedSubscriptionPolicy to handle it.
    /// </summary>
    public class DroppedSubscription
    {
        public DroppedSubscription(ISubscription subscription, string exceptionMessage, SubscriptionDropReason dropReason)
        {
            StreamId = subscription.StreamId;
            ExceptionMessage = exceptionMessage;
            DropReason = dropReason;
            RetryPolicy = subscription.RetryPolicy;
        }

        public string StreamId { get; }
        public string ExceptionMessage { get; }
        public SubscriptionDropReason DropReason { get; }
        public RetryPolicy RetryPolicy { get; }
    }
}