using System;

namespace EventStore.ClientAPI.Serialization
{
  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false)]
  public class SerializationTokenAttribute : Attribute
  {
    public readonly SerializationToken Token;

    public SerializationTokenAttribute(SerializationToken token)
    {
      Token = token;
    }
  }
}