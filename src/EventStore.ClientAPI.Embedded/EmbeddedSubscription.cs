using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using EventStore.ClientAPI.Common.Utils;
using EventStore.ClientAPI.Exceptions;
using EventStore.ClientAPI.SystemData;
using EventStore.Core.Authentication;
using EventStore.Core.Bus;
using EventStore.Core.Messages;
using EventStore.Core.Messaging;

namespace EventStore.ClientAPI.Embedded
{
    internal class EmbeddedSubscription : EmbeddedSubscriptionBase<EventStoreSubscription>
    {
        private readonly UserCredentials _userCredentials;
        private readonly IAuthenticationProvider _authenticationProvider;
        private readonly bool _resolveLinkTos;

        private readonly Action<EventStoreSubscription, ResolvedEvent> _eventAppeared;
        private readonly Func<EventStoreSubscription, ResolvedEvent, Task> _eventAppearedAsync;

        public EmbeddedSubscription(IPublisher publisher, Guid connectionId, TaskCompletionSource<EventStoreSubscription> source,
            string streamId, UserCredentials userCredentials, IAuthenticationProvider authenticationProvider,
            bool resolveLinkTos, Action<EventStoreSubscription, ResolvedEvent> eventAppeared,
            Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped)
            : base(publisher, connectionId, source, streamId, subscriptionDropped, false)
        {
            _userCredentials = userCredentials;
            _authenticationProvider = authenticationProvider;
            _resolveLinkTos = resolveLinkTos;

            _eventAppeared = eventAppeared;
        }
        public EmbeddedSubscription(IPublisher publisher, Guid connectionId, TaskCompletionSource<EventStoreSubscription> source,
            string streamId, UserCredentials userCredentials, IAuthenticationProvider authenticationProvider,
            bool resolveLinkTos, Func<EventStoreSubscription, ResolvedEvent, Task> eventAppearedAsync,
            Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped)
            : base(publisher, connectionId, source, streamId, subscriptionDropped, true)
        {
            _userCredentials = userCredentials;
            _authenticationProvider = authenticationProvider;
            _resolveLinkTos = resolveLinkTos;

            _eventAppearedAsync = eventAppearedAsync;
        }

        protected override EventStoreSubscription CreateVolatileSubscription(long lastCommitPosition, long? lastEventNumber)
        {
            return new EmbeddedVolatileEventStoreSubscription(Unsubscribe, StreamId, lastCommitPosition, lastEventNumber);
        }

        public override void Start(Guid correlationId)
        {
            CorrelationId = correlationId;

            Publisher.PublishWithAuthentication(_authenticationProvider, _userCredentials,
                ex => DropSubscription(EventStore.Core.Services.SubscriptionDropReason.AccessDenied, ex),
                user => new ClientMessage.SubscribeToStream(
                    correlationId,
                    correlationId,
                    new PublishEnvelope(Publisher, true),
                    ConnectionId,
                    StreamId,
                    _resolveLinkTos,
                    user));
        }

        protected override void OnEventAppeared(ResolvedEvent resolvedEvent, int? retryCount) => _eventAppeared(Subscription, resolvedEvent);

        protected override Task OnEventAppearedAsync(ResolvedEvent resolvedEvent, int? retryCount) => _eventAppearedAsync(Subscription, resolvedEvent);
    }
}
