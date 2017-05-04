namespace EventStore.ClientAPI.Rx
{
  public interface IEventStore
  {
    IEventStoreConnection Connection { get; }
  }
}