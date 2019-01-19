
namespace EventStore.ClientAPI.Subscriptions
{
    /// <summary>Represents a volatile subscription to EventStore.</summary>
    public class VolatileSubscription<TEvent> : Subscription<VolatileSubscription<TEvent>, SubscriptionSettings, TEvent>
        where TEvent : class
    {
        public VolatileSubscription() : base(default(string)) { }
    }
}
