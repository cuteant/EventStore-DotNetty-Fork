namespace EventStore.ClientAPI.Subscriptions
{
    /// <summary>Represents a persistent subscription to EventSTore.</summary>
    public class PersistentSubscription<TEvent> : Subscription<PersistentSubscription<TEvent>, ConnectToPersistentSubscriptionSettings, TEvent>
    {
        public PersistentSubscription(string subscriptionId)
          : base(subscriptionId)
        {
        }
    }
}