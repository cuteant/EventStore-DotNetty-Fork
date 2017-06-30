
namespace EventStore.ClientAPI.Subscriptions
{
  /// <summary>Represents a persistent subscription to EventSTore.</summary>
  public class PersistentSubscription : Subscription<PersistentSubscription, ConnectToPersistentSubscriptionSettings>
  {
    public PersistentSubscription(string streamId, string subscriptionId)
      : base(streamId, subscriptionId)
    {
    }
  }
}
