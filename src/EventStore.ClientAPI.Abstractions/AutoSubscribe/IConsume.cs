namespace EventStore.ClientAPI.AutoSubscribe
{
  public interface IConsume<in T> where T : class
  {
    void Consume(T message);
  }
}