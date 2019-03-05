
namespace EventStore.ClientAPI.Subscriptions
{
    /// <summary>Represents a volatile subscription to EventStore.</summary>
    public class VolatileSubscription : Subscription<VolatileSubscription, SubscriptionSettings>
    {
        public VolatileSubscription(string streamId) : base(streamId) { }
    }
}
