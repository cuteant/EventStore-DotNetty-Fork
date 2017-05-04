namespace EventStore.ClientAPI.Rx
{
  public interface IEventStoreConfiguration
  {
    string Host { get; }
    int Port { get; }
  }
}