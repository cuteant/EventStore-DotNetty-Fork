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
            if (null == concreteType) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.concreteType); }
            if (null == interfaceType) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.interfaceType); }
            if (null == consumeMethodName) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.consumeMethodName); }
            ConcreteType = concreteType;
            InterfaceType = interfaceType;
            ConsumeMethodName = consumeMethodName;
            MessageType = messageType;
        }
    }
}