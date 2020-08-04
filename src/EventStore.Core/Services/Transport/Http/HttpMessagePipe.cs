using System;
using System.Collections.Concurrent;
using System.Net;
using EventStore.Core.Messaging;

namespace EventStore.Core.Services.Transport.Http
{
    public class HttpMessagePipe
    {
        private readonly ConcurrentDictionary<Type, IMessageSender> _senders = new ConcurrentDictionary<Type, IMessageSender>();

        public void RegisterSender<T>(ISender<T> sender) where T : Message
        {
            if (sender is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.sender); }
            _senders.TryAdd(typeof (T), new MessageSender<T>(sender));
        }

        public void Push(Message message, IPEndPoint endPoint)
        {
            if (message is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.message); }
            if (endPoint is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.endPoint); }

            var type = message.GetType();
            IMessageSender sender;

            if (_senders.TryGetValue(type, out sender))
                sender.Send(message, endPoint);
        }
    }

    public interface ISender<in T> where T : Message
    {
        void Send(T message, IPEndPoint endPoint);
    }

    public interface IMessageSender
    {
        void Send(Message message, IPEndPoint endPoint);
    }

    public class MessageSender<T> : IMessageSender 
        where T : Message
    {
        private readonly ISender<T> _sender;

        public MessageSender(ISender<T> sender)
        {
            if (sender is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.sender); }
            _sender = sender;
        }

        public void Send(Message message, IPEndPoint endPoint)
        {
            if (message is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.message); }
            if (endPoint is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.endPoint); }

            _sender.Send((T) message, endPoint);
        }
    }
}