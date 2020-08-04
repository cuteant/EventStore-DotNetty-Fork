using System;
using System.Collections.Concurrent;
using EventStore.Core.Bus;

namespace EventStore.Core.Messaging
{
    public sealed class RequestResponseDispatcher<TRequest, TResponse> : IHandle<TResponse>
        where TRequest : Message where TResponse : Message
    {
        //NOTE: this class is not intended to be used from multiple threads except from the QueuedHandlerThreadPool
        //however it supports count requests from other threads for statistics purposes
        private readonly ConcurrentDictionary<Guid, Action<TResponse>> _map = new ConcurrentDictionary<Guid, Action<TResponse>>();
        private readonly IPublisher _publisher;
        private readonly Func<TRequest, Guid> _getRequestCorrelationId;
        private readonly Func<TResponse, Guid> _getResponseCorrelationId;
        private readonly IEnvelope _defaultReplyEnvelope;
        private readonly Func<Guid, Message> _cancelMessageFactory;

        public RequestResponseDispatcher(
            IPublisher publisher,
            Func<TRequest, Guid> getRequestCorrelationId,
            Func<TResponse, Guid> getResponseCorrelationId,
            IEnvelope defaultReplyEnvelope,
            Func<Guid, Message> cancelMessageFactory = null)
        {
            _publisher = publisher;
            _getRequestCorrelationId = getRequestCorrelationId;
            _getResponseCorrelationId = getResponseCorrelationId;
            _defaultReplyEnvelope = defaultReplyEnvelope;
            _cancelMessageFactory = cancelMessageFactory;
        }

        public Guid Publish(TRequest request, Action<TResponse> action)
        {
            //TODO: expiration?
            var requestCorrelationId = _getRequestCorrelationId(request);
            _map.TryAdd(requestCorrelationId, action);
            _publisher.Publish(request);
            //NOTE: the following condition is required as publishing the message could also process the message 
            // and the correlationId is already invalid here
            return _map.ContainsKey(requestCorrelationId) ? requestCorrelationId : Guid.Empty;
        }

        void IHandle<TResponse>.Handle(TResponse message)
        {
            Handle(message);
        }

        public bool Handle(TResponse message)
        {
            var correlationId = _getResponseCorrelationId(message);

            // We mustn't call the handler inside the lock as we have no
            // knowledge of it's behaviour, which might result in dead locks.
            if (_map.TryRemove(correlationId, out Action<TResponse> action))
            {
                action(message);
                return true;
            }

            return false;

        }

        public IEnvelope Envelope => _defaultReplyEnvelope;

        public void Cancel(Guid requestId)
        {
            if (_cancelMessageFactory is object)
            {
                _publisher.Publish(_cancelMessageFactory(requestId));
            }

            _map.TryRemove(requestId, out Action<TResponse> action);
        }

        public void CancelAll()
        {
            if (_cancelMessageFactory is object)
            {
                var keys = _map.Keys;
                foreach (var id in keys)
                {
                    _publisher.Publish(_cancelMessageFactory(id));
                    _map.TryRemove(id, out Action<TResponse> action);
                }
            }
            else
            {
                _map.Clear();
            }
        }
    }
}
