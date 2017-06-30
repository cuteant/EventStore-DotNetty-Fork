using System;
using EventStore.ClientAPI.Serialization;

namespace EventStore.ClientAPI
{
  public static class TypeExtensions
  {
    public static string ToStreamId(this Type actualType, Type expectedType = null)
    {
      return SerializationManager.GetStreamId(actualType, expectedType);
    }

    public static string ToStreamId(this Type actualType, string topic, Type expectedType = null)
    {
      return string.IsNullOrEmpty(topic)
           ? SerializationManager.GetStreamId(actualType, expectedType)
           : IEventStoreConnectionExtensions.CombineStreamId(SerializationManager.GetStreamId(actualType, expectedType), topic);
    }
  }
}
