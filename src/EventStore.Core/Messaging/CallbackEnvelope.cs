using System;

namespace EventStore.Core.Messaging
{
    public class CallbackEnvelope : IEnvelope
    {
        private readonly Action<Message> _callback;

        public CallbackEnvelope(Action<Message> callback)
        {
            if (callback is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.callback); }
            _callback = callback;
        }

        public void ReplyWith<T>(T message) where T : Message
        {
            _callback(message);
        }
    }
}
