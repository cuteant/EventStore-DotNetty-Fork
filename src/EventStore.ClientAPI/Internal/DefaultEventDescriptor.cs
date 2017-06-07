using System;
using System.Collections.Generic;
using CuteAnt.Extensions.Serialization;

namespace EventStore.ClientAPI
{
  internal sealed class DefaultEventDescriptor : IEventDescriptor
  {
    private readonly Dictionary<string, object> _eventContext;
    private static readonly IObjectTypeDeserializer _objectTypeDeserializer = JsonObjectTypeDeserializer.Instance;

    public DefaultEventDescriptor(Dictionary<string, object> context)
    {
      _eventContext = context ?? throw new ArgumentNullException(nameof(context));
    }

    public T GetValue<T>(string key)
    {
      return _objectTypeDeserializer.Deserialize<T>(_eventContext, key);
    }

    public T GetValue<T>(string key, T defaultValue)
    {
      return _objectTypeDeserializer.Deserialize<T>(_eventContext, key, defaultValue);
    }

    public bool TryGetValue<T>(string key, out T value)
    {
      try
      {
        value = _objectTypeDeserializer.Deserialize<T>(_eventContext, key);
        return true;
      }
      catch
      {
        value = default(T);
        return false;
      }
    }
  }

  internal sealed class NullEventDescriptor : IEventDescriptor
  {
    internal static readonly IEventDescriptor Instance = new NullEventDescriptor();

    public T GetValue<T>(string key)
    {
      throw new KeyNotFoundException($"The key was not present: {key}");
    }

    public T GetValue<T>(string key, T defaultValue)
    {
      return default(T);
    }

    public bool TryGetValue<T>(string key, out T value)
    {
      value = default(T);
      return false;
    }
  }
}
