namespace EventStore.ClientAPI.AutoSubscribing
{
  public interface IAutoSubscriberVolatileConsume
  {
    void Consume(EventStoreSubscription subscription, ResolvedEvent<object> resolvedEvent);
  }

  public interface IAutoSubscriberVolatileConsume<T>
  {
    void Consume(EventStoreSubscription subscription, ResolvedEvent<T> resolvedEvent);
  }
}
