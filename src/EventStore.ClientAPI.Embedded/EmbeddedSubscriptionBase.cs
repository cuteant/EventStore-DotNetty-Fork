using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using EventStore.ClientAPI.Common.Utils;
using EventStore.ClientAPI.Messages;
using EventStore.ClientAPI.Exceptions;
using EventStore.Core.Bus;
using EventStore.Core.Messages;
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
    private readonly Action<EventStoreSubscription, ResolvedEvent> _eventAppeared;
    private readonly Func<EventStoreSubscription, ResolvedEvent, Task> _eventAppearedAsync;
    private readonly Action<EventStoreSubscription, SubscriptionDropReason, Exception> _subscriptionDropped;
    private readonly ActionBlock<(bool isResolvedEvent, ResolvedEvent resolvedEvent, SubscriptionDropReason dropReason, Exception exc)> _actionQueue;
    private int _unsubscribed;
    private TSubscription _subscription;
    protected IPublisher Publisher;
    protected string StreamId;
    protected Guid CorrelationId;

    protected EmbeddedSubscriptionBase(IPublisher publisher, Guid connectionId, TaskCompletionSource<TSubscription> source,
      string streamId, Action<EventStoreSubscription, ResolvedEvent> eventAppeared,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped)
      : this(publisher, connectionId, source, streamId, subscriptionDropped)
    {
      Ensure.NotNull(eventAppeared, nameof(eventAppeared));

      _eventAppeared = eventAppeared;
      _actionQueue = new ActionBlock<(bool isResolvedEvent, ResolvedEvent resolvedEvent, SubscriptionDropReason dropReason, Exception exc)>(
          e => ProcessItem(e));
    }
    protected EmbeddedSubscriptionBase(IPublisher publisher, Guid connectionId, TaskCompletionSource<TSubscription> source,
      string streamId, Func<EventStoreSubscription, ResolvedEvent, Task> eventAppearedAsync,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped)
      : this(publisher, connectionId, source, streamId, subscriptionDropped)
    {
      Ensure.NotNull(eventAppearedAsync, nameof(eventAppearedAsync));

      _eventAppearedAsync = eventAppearedAsync;
      _actionQueue = new ActionBlock<(bool isResolvedEvent, ResolvedEvent resolvedEvent, SubscriptionDropReason dropReason, Exception exc)>(
          e => ProcessItemAsync(e));
    }
    private EmbeddedSubscriptionBase(IPublisher publisher, Guid connectionId, TaskCompletionSource<TSubscription> source,
      string streamId, Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped)
    {
      Ensure.NotNull(source, nameof(source));
      Ensure.NotNull(streamId, nameof(streamId));
      Ensure.NotNull(publisher, nameof(publisher));

      Publisher = publisher;
      StreamId = streamId;
      ConnectionId = connectionId;
      _log = TraceLogger.GetLogger(this.GetType());
      _source = source;
      _subscriptionDropped = subscriptionDropped ?? ((a, b, c) => { });
    }

    public void DropSubscription(EventStore.Core.Services.SubscriptionDropReason reason, Exception ex)
    {
      switch (reason)
      {
        case EventStore.Core.Services.SubscriptionDropReason.AccessDenied:
          DropSubscription(SubscriptionDropReason.AccessDenied,
              ex ?? new AccessDeniedException(string.Format("Subscription to '{0}' failed due to access denied.",
                  StreamId == string.Empty ? "<all>" : StreamId)));
          break;
        case EventStore.Core.Services.SubscriptionDropReason.Unsubscribed:
          Unsubscribe();
          break;
        case EventStore.Core.Services.SubscriptionDropReason.NotFound:
          DropSubscription(SubscriptionDropReason.NotFound,
              new ArgumentException("Subscription not found"));
          break;
      }
    }

    public async Task EventAppeared(EventStore.Core.Data.ResolvedEvent resolvedEvent)
    {
      var e = resolvedEvent.OriginalPosition == null
          ? resolvedEvent.ConvertToClientResolvedIndexEvent().ToResolvedEvent()
          : resolvedEvent.ConvertToClientResolvedEvent().ToResolvedEvent();
      await EnqueueMessage((true, e, SubscriptionDropReason.Unknown, null)).ConfigureAwait(false);
    }

    public void ConfirmSubscription(long lastCommitPosition, long? lastEventNumber)
    {
      if (lastCommitPosition < -1)
        throw new ArgumentOutOfRangeException("lastCommitPosition", string.Format("Invalid lastCommitPosition {0} on subscription confirmation.", lastCommitPosition));
      if (_subscription != null)
        throw new Exception("Double confirmation of subscription.");

      _subscription = CreateVolatileSubscription(lastCommitPosition, lastEventNumber);
      _source.SetResult(_subscription);
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
          if (exception == null) throw new Exception(string.Format("No exception provided for subscription drop reason '{0}", reason));
          _source.TrySetException(exception);
        }

        if (reason == SubscriptionDropReason.UserInitiated && _subscription != null)
        {
          Publisher.Publish(new CoreClientMessage.UnsubscribeFromStream(Guid.NewGuid(), CorrelationId, new NoopEnvelope(), SystemAccount.Principal));
        }

        if (_subscription != null)
        {
          EnqueueMessage((false, ResolvedEvent.Null, reason, exception)).ConfigureAwait(false).GetAwaiter().GetResult();
        }
      }
    }


    private async Task EnqueueMessage((bool isResolvedEvent, ResolvedEvent resolvedEvent, SubscriptionDropReason dropReason, Exception exc) item)
    {
      await _actionQueue.SendAsync(item).ConfigureAwait(false);
    }

    private void ProcessItem((bool isResolvedEvent, ResolvedEvent resolvedEvent, SubscriptionDropReason dropReason, Exception exc) item)
    {
      try
      {
        if (item.isResolvedEvent)
        {
          _eventAppeared(_subscription, item.resolvedEvent);
        }
        else
        {
          _subscriptionDropped(_subscription, item.dropReason, item.exc);
        }
      }
      catch (Exception exc)
      {
        _log.LogError(exc, "Exception during executing user callback: {0}.", exc.Message);
      }
    }

    private async Task ProcessItemAsync((bool isResolvedEvent, ResolvedEvent resolvedEvent, SubscriptionDropReason dropReason, Exception exc) item)
    {
      try
      {
        if (item.isResolvedEvent)
        {
          await _eventAppearedAsync(_subscription, item.resolvedEvent).ConfigureAwait(false);
        }
        else
        {
          _subscriptionDropped(_subscription, item.dropReason, item.exc);
        }
      }
      catch (Exception exc)
      {
        _log.LogError(exc, "Exception during executing user callback: {0}.", exc.Message);
      }
    }

    public abstract void Start(Guid correlationId);
  }
}
