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
            if (concreteType is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.concreteType); }
            if (interfaceType is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.interfaceType); }
            if (consumeMethodName is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.consumeMethodName); }
            ConcreteType = concreteType;
            InterfaceType = interfaceType;
            ConsumeMethodName = consumeMethodName;
            MessageType = messageType;
        }
    }
}