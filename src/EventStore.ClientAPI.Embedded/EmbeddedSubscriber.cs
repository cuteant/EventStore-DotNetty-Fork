using System;
using System.Threading.Tasks;
using EventStore.ClientAPI.SystemData;
using EventStore.Core.Authentication;
using EventStore.Core.Bus;
using EventStore.Core.Messages;
using Microsoft.Extensions.Logging;

namespace EventStore.ClientAPI.Embedded
{
    internal partial class EmbeddedSubscriber :
        IHandle<ClientMessage.SubscriptionConfirmation>,
        IHandle<ClientMessage.StreamEventAppeared>,
        IHandle<ClientMessage.SubscriptionDropped>,
        IHandle<ClientMessage.PersistentSubscriptionConfirmation>,
        IHandle<ClientMessage.PersistentSubscriptionStreamEventAppeared>
    {
        private readonly EmbeddedSubcriptionsManager _subscriptions;
        private readonly IPublisher _publisher;
        private readonly IAuthenticationProvider _authenticationProvider;
        private readonly Guid _connectionId;


        public EmbeddedSubscriber(IPublisher publisher, IAuthenticationProvider authenticationProvider, Guid connectionId)
        {
            _publisher = publisher;
            _authenticationProvider = authenticationProvider;
            _connectionId = connectionId;
            _subscriptions = new EmbeddedSubcriptionsManager();
        }

        public void Handle(ClientMessage.StreamEventAppeared message)
        {
            StreamEventAppeared(message.CorrelationId, message.Event);
        }

        public void Handle(ClientMessage.SubscriptionConfirmation message)
        {
            ConfirmSubscription(message.CorrelationId, message.LastCommitPosition, message.LastEventNumber);
        }

        public void Handle(ClientMessage.SubscriptionDropped message)
        {
            _subscriptions.TryGetActiveSubscription(message.CorrelationId, out IEmbeddedSubscription subscription);
            subscription.DropSubscription(message.Reason);
        }

        public void Handle(ClientMessage.PersistentSubscriptionConfirmation message)
        {
            ConfirmSubscription(message.SubscriptionId, message.CorrelationId, message.LastCommitPosition, message.LastEventNumber);
        }

        public void Handle(ClientMessage.PersistentSubscriptionStreamEventAppeared message)
        {
            StreamEventAppeared(message.CorrelationId, message.Event);
        }

        private void StreamEventAppeared(Guid correlationId, EventStore.Core.Data.ResolvedEvent resolvedEvent)
        {
            _subscriptions.TryGetActiveSubscription(correlationId, out IEmbeddedSubscription subscription);
            subscription.EventAppeared(resolvedEvent);
        }

        private void ConfirmSubscription(Guid correlationId, long lastCommitPosition, long? lastEventNumber)
        {
            _subscriptions.TryGetActiveSubscription(correlationId, out IEmbeddedSubscription subscription);
            subscription.ConfirmSubscription(lastCommitPosition, lastEventNumber);
        }

        private void ConfirmSubscription(string subscriptionId, Guid correlationId, long lastCommitPosition, long? lastEventNumber)
        {
            _subscriptions.TryGetActiveSubscription(correlationId, out IEmbeddedSubscription subscription);
            ((EmbeddedPersistentSubscription)subscription).UpdateSubscriptionId(subscriptionId);
            subscription.ConfirmSubscription(lastCommitPosition, lastEventNumber);
        }

        public void StartSubscription(Guid correlationId, TaskCompletionSource<EventStoreSubscription> source, string stream, UserCredentials userCredentials, bool resolveLinkTos, Action<EventStoreSubscription, ResolvedEvent> eventAppeared, Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped)
        {
            var subscription = new EmbeddedSubscription(
                _publisher, _connectionId, source, stream, userCredentials, _authenticationProvider,
                resolveLinkTos, eventAppeared,
                subscriptionDropped);

            _subscriptions.StartSubscription(correlationId, subscription);
        }
        public void StartSubscription(Guid correlationId, TaskCompletionSource<EventStoreSubscription> source, string stream, UserCredentials userCredentials, bool resolveLinkTos, Func<EventStoreSubscription, ResolvedEvent, Task> eventAppearedAsync, Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped)
        {
            var subscription = new EmbeddedSubscription(
                _publisher, _connectionId, source, stream, userCredentials, _authenticationProvider,
                resolveLinkTos, eventAppearedAsync,
                subscriptionDropped);

            _subscriptions.StartSubscription(correlationId, subscription);
        }

        public void StartPersistentSubscription(Guid correlationId, TaskCompletionSource<PersistentEventStoreSubscription> source, string subscriptionId, string streamId, UserCredentials userCredentials, int bufferSize, Action<EventStoreSubscription, PersistentSubscriptionResolvedEvent> eventAppeared, Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped, int maxRetries, TimeSpan operationTimeout)
        {
            var subscription = new EmbeddedPersistentSubscription(_publisher, _connectionId, source,
                subscriptionId, streamId, userCredentials, _authenticationProvider, bufferSize, eventAppeared,
                subscriptionDropped, maxRetries, operationTimeout);

            _subscriptions.StartSubscription(correlationId, subscription);
        }
        public void StartPersistentSubscription(Guid correlationId, TaskCompletionSource<PersistentEventStoreSubscription> source, string subscriptionId, string streamId, UserCredentials userCredentials, int bufferSize, Func<EventStoreSubscription, PersistentSubscriptionResolvedEvent, Task> eventAppearedAsync, Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped, int maxRetries, TimeSpan operationTimeout)
        {
            var subscription = new EmbeddedPersistentSubscription(_publisher, _connectionId, source,
                subscriptionId, streamId, userCredentials, _authenticationProvider, bufferSize, eventAppearedAsync,
                subscriptionDropped, maxRetries, operationTimeout);

            _subscriptions.StartSubscription(correlationId, subscription);
        }
    }
}
