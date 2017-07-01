using System;

namespace EventStore.ClientAPI.AutoSubscribing
{
  public class AutoSubscriberConsumerInfo
  {
    public readonly Type ConcreteType;
    public readonly Type InterfaceType;
    public readonly Type MessageType;

    public AutoSubscriberConsumerInfo(Type concreteType, Type interfaceType, Type messageType)
    {
      ConcreteType = concreteType ?? throw new ArgumentNullException(nameof(concreteType));
      InterfaceType = interfaceType ?? throw new ArgumentNullException(nameof(interfaceType));
      MessageType = messageType;
    }
  }
}