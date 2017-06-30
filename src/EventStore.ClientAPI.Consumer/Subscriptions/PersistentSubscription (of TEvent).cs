
namespace EventStore.ClientAPI.Subscriptions
{
  /// <summary>Represents a persistent subscription to EventSTore.</summary>
  public class PersistentSubscription<TEvent> : Subscription<PersistentSubscription, ConnectToPersistentSubscriptionSettings, TEvent>
    where TEvent : class
  {
    public PersistentSubscription(string subscriptionId)
      : base(subscriptionId)
    {
    }
  }
}
