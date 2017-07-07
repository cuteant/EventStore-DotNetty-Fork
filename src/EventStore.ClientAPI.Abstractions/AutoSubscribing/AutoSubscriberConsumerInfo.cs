using System;

namespace EventStore.ClientAPI.AutoSubscribing
{
  public class AutoSubscriberConsumerInfo
  {
    public readonly Type ConcreteType;
    public readonly Type InterfaceType;
    public readonly string ConsumeMethodName;
    public readonly Type MessageType;

    public AutoSubscriberConsumerInfo(Type concreteType, Type interfaceType, string consumeMethodName, Type messageType = null)
    {
      ConcreteType = concreteType ?? throw new ArgumentNullException(nameof(concreteType));
      InterfaceType = interfaceType ?? throw new ArgumentNullException(nameof(interfaceType));
      ConsumeMethodName = consumeMethodName ?? throw new ArgumentNullException(nameof(consumeMethodName));
      MessageType = messageType;
    }
  }
}