using System;
using EventStore.Core.Messaging;

namespace EventStore.Core.Bus
{
    public class AdHocHandler<T>: IHandle<T> where T: Message
    {
        private readonly Action<T> _handle;

        public AdHocHandler(Action<T> handle)
        {
            if (null == handle) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.handle); }
            _handle = handle;
        }

        public void Handle(T message)
        {
            _handle(message);
        }
    }
}