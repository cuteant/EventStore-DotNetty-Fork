namespace EventStore.ClientAPI.AutoSubscribing
{
  public interface IAutoSubscriberCatchUpConsume
  {
    void Consume(EventStoreCatchUpSubscription subscription, ResolvedEvent<object> resolvedEvent);
  }

  public interface IAutoSubscriberCatchUpConsume<T>
  {
    void Consume(EventStoreCatchUpSubscription<T> subscription, ResolvedEvent<T> resolvedEvent);
  }
}
