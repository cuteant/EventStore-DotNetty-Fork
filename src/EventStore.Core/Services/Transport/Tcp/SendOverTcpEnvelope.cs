using EventStore.Common.Utils;
using EventStore.Core.Bus;
using EventStore.Core.Messages;
using EventStore.Core.Messaging;

namespace EventStore.Core.Services.Transport.Tcp
{
    public class SendOverTcpEnvelope : IEnvelope
    {
        private readonly IPublisher _networkSendQueue;
        private readonly TcpConnectionManager _manager;

        public SendOverTcpEnvelope(TcpConnectionManager manager, IPublisher networkSendQueue)
        {
            if (manager is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.manager); }
            if (networkSendQueue is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.networkSendQueue); }
            _networkSendQueue = networkSendQueue;
            _manager = manager;
        }

        public void ReplyWith<T>(T message) where T : Message
        {
            if (_manager is object && !_manager.IsClosed)
            {
                _networkSendQueue.Publish(new TcpMessage.TcpSend(_manager, message));
            }
        }
    }
}