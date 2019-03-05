namespace EventStore.ClientAPI.Subscriptions
{
    /// <summary>Represents a persistent subscription to EventSTore.</summary>
    public class PersistentSubscription<TEvent> : Subscription<PersistentSubscription<TEvent>, ConnectToPersistentSubscriptionSettings, TEvent>
    {
        public PersistentSubscription(string subscriptionId)
          : base()
        {
            SubscriptionId = subscriptionId;
        }

        public string SubscriptionId { get; }

        public PersistentSubscriptionSettings PersistentSettings { get; set; }
    }
}