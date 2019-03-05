using System;
using System.Threading.Tasks;
using EventStore.ClientAPI.SystemData;
using EventStore.Core.Authentication;
using EventStore.Core.Bus;
using EventStore.Core.Messages;
using EventStore.Core.Messaging;

namespace EventStore.ClientAPI.Embedded
{
    internal class EmbeddedPersistentSubscription : EmbeddedSubscriptionBase<PersistentEventStoreSubscription>, IConnectToPersistentSubscriptions
    {
        private readonly UserCredentials _userCredentials;
        private readonly IAuthenticationProvider _authenticationProvider;
        private readonly int _bufferSize;
        private string _subscriptionId;

        private readonly Action<EventStoreSubscription, PersistentSubscriptionResolvedEvent> _eventAppeared;
        private readonly Func<EventStoreSubscription, PersistentSubscriptionResolvedEvent, Task> _eventAppearedAsync;

        public EmbeddedPersistentSubscription(IPublisher publisher, Guid connectionId,
            TaskCompletionSource<PersistentEventStoreSubscription> source, string subscriptionId, string streamId,
            UserCredentials userCredentials, IAuthenticationProvider authenticationProvider, int bufferSize,
            Action<EventStoreSubscription, PersistentSubscriptionResolvedEvent> eventAppeared,
            Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped, int maxRetries,
            TimeSpan operationTimeout)
            : base(publisher, connectionId, source, streamId, subscriptionDropped, false)
        {
            _subscriptionId = subscriptionId;
            _userCredentials = userCredentials;
            _authenticationProvider = authenticationProvider;
            _bufferSize = bufferSize;
            _eventAppeared = eventAppeared;
        }
        public EmbeddedPersistentSubscription(IPublisher publisher, Guid connectionId,
            TaskCompletionSource<PersistentEventStoreSubscription> source, string subscriptionId, string streamId,
            UserCredentials userCredentials, IAuthenticationProvider authenticationProvider, int bufferSize,
            Func<EventStoreSubscription, PersistentSubscriptionResolvedEvent, Task> eventAppearedAsync,
            Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped, int maxRetries,
            TimeSpan operationTimeout)
            : base(publisher, connectionId, source, streamId, subscriptionDropped, true)
        {
            _subscriptionId = subscriptionId;
            _userCredentials = userCredentials;
            _authenticationProvider = authenticationProvider;
            _bufferSize = bufferSize;
            _eventAppearedAsync = eventAppearedAsync;
        }

        protected override PersistentEventStoreSubscription CreateVolatileSubscription(long lastCommitPosition, long? lastEventNumber)
        {
            return new PersistentEventStoreSubscription(this, StreamId, lastCommitPosition, lastEventNumber);
        }

        public override void Start(Guid correlationId)
        {
            CorrelationId = correlationId;

            Publisher.PublishWithAuthentication(_authenticationProvider, _userCredentials,
                ex => DropSubscription(EventStore.Core.Services.SubscriptionDropReason.AccessDenied, ex),
                user => new ClientMessage.ConnectToPersistentSubscription(correlationId, correlationId,
                    new PublishEnvelope(Publisher, true), ConnectionId, _subscriptionId, StreamId, _bufferSize,
                    String.Empty,
                    user));
        }

        public void UpdateSubscriptionId(string subscriptionId)
        {
            _subscriptionId = subscriptionId;
        }

        public void NotifyEventsProcessed(Guid processedEvent) => NotifyEventsProcessed(new Guid[] { processedEvent });
        public void NotifyEventsProcessed(Guid[] processedEvents)
        {
            if (null == processedEvents) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.processedEvents); }

            Publisher.PublishWithAuthentication(_authenticationProvider, _userCredentials,
                ex => DropSubscription(EventStore.Core.Services.SubscriptionDropReason.AccessDenied, ex),
                user => new ClientMessage.PersistentSubscriptionAckEvents(CorrelationId, CorrelationId,
                new PublishEnvelope(Publisher, true), _subscriptionId, processedEvents, user));
        }

        public void NotifyEventsFailed(Guid processedEvent, PersistentSubscriptionNakEventAction action, string reason)
        {
            NotifyEventsFailed(new[] { processedEvent }, action, reason);
        }
        public void NotifyEventsFailed(Guid[] processedEvents, PersistentSubscriptionNakEventAction action, string reason)
        {
            if (null == processedEvents) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.processedEvents); }
            if (null == reason) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.reason); }

            Publisher.PublishWithAuthentication(_authenticationProvider, _userCredentials,
                ex => DropSubscription(EventStore.Core.Services.SubscriptionDropReason.AccessDenied, ex),
                user => new ClientMessage.PersistentSubscriptionNackEvents(CorrelationId, CorrelationId,
                    new PublishEnvelope(Publisher, true), _subscriptionId, reason,
                    (ClientMessage.PersistentSubscriptionNackEvents.NakAction)action, processedEvents,
                    user));
        }

        protected override void OnEventAppeared(ResolvedEvent resolvedEvent, int? retryCount) => _eventAppeared(Subscription, new PersistentSubscriptionResolvedEvent(resolvedEvent, retryCount));

        protected override Task OnEventAppearedAsync(ResolvedEvent resolvedEvent, int? retryCount) => _eventAppearedAsync(Subscription, new PersistentSubscriptionResolvedEvent(resolvedEvent, retryCount));
    }
}
