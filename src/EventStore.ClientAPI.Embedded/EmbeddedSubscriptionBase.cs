using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using EventStore.ClientAPI.Exceptions;
using EventStore.ClientAPI.Messages;
using EventStore.Core.Bus;
using EventStore.Core.Messaging;
using EventStore.Core.Services.UserManagement;
using Microsoft.Extensions.Logging;
using CoreClientMessage = EventStore.Core.Messages.ClientMessage;

namespace EventStore.ClientAPI.Embedded
{
    internal abstract class EmbeddedSubscriptionBase<TSubscription> : IEmbeddedSubscription
        where TSubscription : EventStoreSubscription
    {
        private readonly ILogger _log;
        protected readonly Guid ConnectionId;
        private readonly TaskCompletionSource<TSubscription> _source;
        private readonly Action<EventStoreSubscription, SubscriptionDropReason, Exception> _subscriptionDropped;
        private readonly ActionBlock<(bool isResolvedEvent, ResolvedEvent resolvedEvent, int? retryCount, SubscriptionDropReason dropReason, Exception exc)> _actionQueue;
        private int _unsubscribed;
        protected TSubscription Subscription;
        protected IPublisher Publisher;
        protected string StreamId;
        protected Guid CorrelationId;

        protected EmbeddedSubscriptionBase(IPublisher publisher, Guid connectionId, TaskCompletionSource<TSubscription> source,
            string streamId, Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped, bool asynchronous)
        {
            if (source is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source); }
            if (streamId is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.streamId); }
            if (publisher is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.publisher); }

            Publisher = publisher;
            StreamId = streamId;
            ConnectionId = connectionId;
            _log = TraceLogger.GetLogger(this.GetType());
            _source = source;
            _subscriptionDropped = subscriptionDropped ?? ((a, b, c) => { });
            if (asynchronous)
            {
                _actionQueue = new ActionBlock<(bool isResolvedEvent, ResolvedEvent resolvedEvent, int? retryCount, SubscriptionDropReason dropReason, Exception exc)>(
                    e => ProcessItemAsync(e));
            }
            else
            {
                _actionQueue = new ActionBlock<(bool isResolvedEvent, ResolvedEvent resolvedEvent, int? retryCount, SubscriptionDropReason dropReason, Exception exc)>(
                    e => ProcessItem(e));
            }
        }

        public void DropSubscription(EventStore.Core.Services.SubscriptionDropReason reason, Exception ex)
        {
            switch (reason)
            {
                case EventStore.Core.Services.SubscriptionDropReason.AccessDenied:
                    DropSubscription(SubscriptionDropReason.AccessDenied,
                        ex ?? CoreThrowHelper.GetAccessDeniedException_All(StreamId));
                    break;
                case EventStore.Core.Services.SubscriptionDropReason.Unsubscribed:
                    Unsubscribe();
                    break;
                case EventStore.Core.Services.SubscriptionDropReason.NotFound:
                    DropSubscription(SubscriptionDropReason.NotFound,
                        ThrowHelper.GetArgumentException(ExceptionResource.SubscriptionNotFound));
                    break;
            }
        }

        public async Task EventAppeared((EventStore.Core.Data.ResolvedEvent resolvedEvent, int? retryCount) resolvedEventWrapper)
        {
            var resolvedEvent = resolvedEventWrapper.resolvedEvent;
            var e = resolvedEvent.OriginalPosition is null
                ? resolvedEvent.ConvertToClientResolvedIndexEvent().ToRawResolvedEvent()
                : resolvedEvent.ConvertToClientResolvedEvent().ToRawResolvedEvent();
            await EnqueueMessage((true, e, resolvedEventWrapper.retryCount, SubscriptionDropReason.Unknown, null)).ConfigureAwait(false);
        }

        public void ConfirmSubscription(long lastCommitPosition, long? lastEventNumber)
        {
            if (lastCommitPosition < -1)
                EmbeddedThrowHelper.ThrowArgumentOutOfRangeException_InvalidLastCommitPosition(lastCommitPosition);
            if (Subscription is object)
                EmbeddedThrowHelper.ThrowException_DoubleConfirmationOfSubscription();

            Subscription = CreateVolatileSubscription(lastCommitPosition, lastEventNumber);
            _source.SetResult(Subscription);
        }

        protected abstract TSubscription CreateVolatileSubscription(long lastCommitPosition, long? lastEventNumber);

        public void Unsubscribe()
        {
            DropSubscription(SubscriptionDropReason.UserInitiated, null);
        }

        private void DropSubscription(SubscriptionDropReason reason, Exception exception)
        {
            if (Interlocked.CompareExchange(ref _unsubscribed, 1, 0) == 0)
            {

                if (reason != SubscriptionDropReason.UserInitiated)
                {
                    if (exception is null) EmbeddedThrowHelper.ThrowException_NoExceptionProvidedForSubscriptionDropReason(reason);
                    _source.TrySetException(exception);
                }

                if (reason == SubscriptionDropReason.UserInitiated && Subscription is object)
                {
                    Publisher.Publish(new CoreClientMessage.UnsubscribeFromStream(Guid.NewGuid(), CorrelationId, new NoopEnvelope(), SystemAccount.Principal));
                }

                if (Subscription is object)
                {
                    EnqueueMessage((false, new ResolvedEvent(true), default(int?), reason, exception)).ConfigureAwait(false).GetAwaiter().GetResult();
                }
            }
        }


        private async Task EnqueueMessage((bool isResolvedEvent, ResolvedEvent resolvedEvent, int? retryCount, SubscriptionDropReason dropReason, Exception exc) item)
        {
            await _actionQueue.SendAsync(item).ConfigureAwait(false);
        }

        private void ProcessItem((bool isResolvedEvent, ResolvedEvent resolvedEvent, int? retryCount, SubscriptionDropReason dropReason, Exception exc) item)
        {
            try
            {
                if (item.isResolvedEvent)
                {
                    OnEventAppeared(item.resolvedEvent, item.retryCount);
                }
                else
                {
                    _subscriptionDropped(Subscription, item.dropReason, item.exc);
                }
            }
            catch (Exception exc)
            {
                _log.ExceptionDuringExecutingUserCallback(exc);
            }
        }
        protected abstract void OnEventAppeared(ResolvedEvent resolvedEvent, int? retryCount);

        private async Task ProcessItemAsync((bool isResolvedEvent, ResolvedEvent resolvedEvent, int? retryCount, SubscriptionDropReason dropReason, Exception exc) item)
        {
            try
            {
                if (item.isResolvedEvent)
                {
                    await OnEventAppearedAsync(item.resolvedEvent, item.retryCount).ConfigureAwait(false);
                }
                else
                {
                    _subscriptionDropped(Subscription, item.dropReason, item.exc);
                }
            }
            catch (Exception exc)
            {
                _log.ExceptionDuringExecutingUserCallback(exc);
            }
        }
        protected abstract Task OnEventAppearedAsync(ResolvedEvent resolvedEvent, int? retryCount);

        public abstract void Start(Guid correlationId);
    }
}
