
namespace EventStore.ClientAPI.Subscriptions
{
    /// <summary>Represents a volatile subscription to EventStore.</summary>
    public class VolatileSubscription<TEvent> : Subscription<VolatileSubscription<TEvent>, SubscriptionSettings, TEvent>
    {
        public VolatileSubscription() : base(default(string)) { }
    }
}
