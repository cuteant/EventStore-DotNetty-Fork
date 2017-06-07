namespace EventStore.ClientAPI
{
  /// <summary>IEventDescriptor</summary>
  public interface IEventDescriptor
  {
    T GetValue<T>(string key);
    T GetValue<T>(string key, T defaultValue);
    bool TryGetValue<T>(string key, out T value);
  }
}
