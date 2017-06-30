
namespace EventStore.ClientAPI.Subscriptions
{
  /// <summary>Represents a volatile subscription to EventStore.</summary>
  public class VolatileSubscription<TEvent> : Subscription<VolatileSubscription, SubscriptionSettings, TEvent>
    where TEvent : class
  {
    public VolatileSubscription() : base(default(string)) { }
  }
}
