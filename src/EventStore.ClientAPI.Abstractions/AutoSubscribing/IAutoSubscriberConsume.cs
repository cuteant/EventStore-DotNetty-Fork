namespace EventStore.ClientAPI.AutoSubscribing
{
  public interface IAutoSubscriberConsume<in T> where T : class
  {
    void Consume(T message);
  }
}