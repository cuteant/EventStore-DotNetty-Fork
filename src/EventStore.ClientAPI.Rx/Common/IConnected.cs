namespace EventStore.ClientAPI.Rx
{
  public interface IConnected<out T>
  {
    T Value { get; }
    bool IsConnected { get; }
  }
}