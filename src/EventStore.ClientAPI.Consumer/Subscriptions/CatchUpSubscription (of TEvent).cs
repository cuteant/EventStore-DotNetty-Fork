
namespace EventStore.ClientAPI.Subscriptions
{
  /// <summary>Represents a catch-up subscription to EventStore.</summary>
  public class CatchUpSubscription<TEvent> : Subscription<CatchUpSubscription, CatchUpSubscriptionSettings, TEvent>
    where TEvent : class
  {
    public CatchUpSubscription() : base(default(string)) { }
  }
}
