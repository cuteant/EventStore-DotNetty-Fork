namespace EventStore.ClientAPI.AutoSubscribing
{
  public interface IAutoSubscriberPersistentConsume
  {
    void Consume(EventStorePersistentSubscription subscription, ResolvedEvent<object> resolvedEvent, int? retryCount);
  }

  public interface IAutoSubscriberPersistentConsume<T>
  {
    void Consume(EventStorePersistentSubscription<T> subscription, ResolvedEvent<T> resolvedEvent, int? retryCount);
  }
}
