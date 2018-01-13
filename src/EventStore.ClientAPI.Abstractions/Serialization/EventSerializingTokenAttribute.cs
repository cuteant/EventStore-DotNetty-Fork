using System;

namespace EventStore.ClientAPI.Serialization
{
  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false)]
  public class EventSerializingTokenAttribute : Attribute
  {
    public readonly EventSerializingToken Token;

    public EventSerializingTokenAttribute(EventSerializingToken token) => Token = token;
  }
}