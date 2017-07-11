using System;
using System.Threading;
using System.Threading.Tasks;
using EventStore.ClientAPI.Subscriptions;
using Microsoft.Extensions.Logging;

namespace EventStore.ClientAPI.Consumers
{
  /// <summary>Represents a consumer to a volatile subscription to EventStore: http://docs.geteventstore.com/introduction/4.0.0/subscriptions/ .</summary>
  public class VolatileConsumer<TEvent> : StreamConsumer<VolatileSubscription<TEvent>, SubscriptionSettings, TEvent>
    where TEvent : class
  {
    private static readonly ILogger s_logger = TraceLogger.GetLogger<VolatileConsumer>();

    private bool processingResolvedEvent;
    private Func<EventStoreSubscription, ResolvedEvent<TEvent>, Task> _resolvedEventAppearedAsync;
    private Action<EventStoreSubscription, ResolvedEvent<TEvent>> _resolvedEventAppeared;
    private Func<EventStoreSubscription, ResolvedEvent<TEvent>, Task> _eventAppearedAsync;
    private Action<EventStoreSubscription, ResolvedEvent<TEvent>> _eventAppeared;

    private EventStoreSubscription _esSubscription;

    protected override void OnDispose(bool disposing)
    {
      base.OnDispose(disposing);
      var subscription = Interlocked.Exchange(ref _esSubscription, null);
      subscription?.Dispose();
    }

    public void Initialize(IEventStoreConnectionBase2 connection, VolatileSubscription<TEvent> subscription,
      Func<EventStoreSubscription, ResolvedEvent<TEvent>, Task> resolvedEventAppearedAsync)
    {
      Initialize(connection, subscription);
      _resolvedEventAppearedAsync = resolvedEventAppearedAsync ?? throw new ArgumentNullException(nameof(resolvedEventAppearedAsync));
      processingResolvedEvent = true;
    }

    public void Initialize(IEventStoreConnectionBase2 connection, VolatileSubscription<TEvent> subscription,
      Action<EventStoreSubscription, ResolvedEvent<TEvent>> resolvedEventAppeared)
    {
      Initialize(connection, subscription);
      _resolvedEventAppeared = resolvedEventAppeared ?? throw new ArgumentNullException(nameof(resolvedEventAppeared));
      processingResolvedEvent = true;
    }

    public void Initialize(IEventStoreConnectionBase2 connection, VolatileSubscription<TEvent> subscription, Func<TEvent, Task> eventAppearedAsync)
    {
      if (null == eventAppearedAsync) { throw new ArgumentNullException(nameof(eventAppearedAsync)); }
      Initialize(connection, subscription);
      _eventAppearedAsync = (sub, resolvedEvent) => eventAppearedAsync(resolvedEvent.Body);
      processingResolvedEvent = false;
    }

    public void Initialize(IEventStoreConnectionBase2 connection, VolatileSubscription<TEvent> subscription, Action<TEvent> eventAppeared)
    {
      if (null == eventAppeared) { throw new ArgumentNullException(nameof(eventAppeared)); }
      Initialize(connection, subscription);
      _eventAppeared = (sub, resolvedEvent) => eventAppeared(resolvedEvent.Body);
      processingResolvedEvent = false;
    }

    public override async Task ConnectToSubscriptionAsync()
    {
      if (Interlocked.CompareExchange(ref _subscribed, ON, OFF) == ON) { return; }

      try
      {
        if (processingResolvedEvent)
        {
          if (_resolvedEventAppearedAsync != null)
          {
            _esSubscription = await Connection.VolatileSubscribeAsync<TEvent>(Subscription.Topic, Subscription.Settings, _resolvedEventAppearedAsync,
                    async (sub, reason, exception) => await SubscriptionDroppedAsync(sub, reason, exception).ConfigureAwait(false),
                    Subscription.Credentials).ConfigureAwait(false);
          }
          else
          {
            _esSubscription = await Connection.VolatileSubscribeAsync<TEvent>(Subscription.Topic, Subscription.Settings, _resolvedEventAppeared,
                    async (sub, reason, exception) => await SubscriptionDroppedAsync(sub, reason, exception).ConfigureAwait(false),
                    Subscription.Credentials).ConfigureAwait(false);
          }
        }
        else
        {
          if (_eventAppearedAsync != null)
          {
            _esSubscription = await Connection.VolatileSubscribeAsync<TEvent>(Subscription.Topic, Subscription.Settings, _eventAppearedAsync,
                    async (sub, reason, exception) => await SubscriptionDroppedAsync(sub, reason, exception).ConfigureAwait(false),
                    Subscription.Credentials).ConfigureAwait(false);
          }
          else
          {
            _esSubscription = await Connection.VolatileSubscribeAsync<TEvent>(Subscription.Topic, Subscription.Settings, _eventAppeared,
                    async (sub, reason, exception) => await SubscriptionDroppedAsync(sub, reason, exception).ConfigureAwait(false),
                    Subscription.Credentials).ConfigureAwait(false);
          }
        }
      }
      catch (Exception exc)
      {
        s_logger.LogError(exc.ToString());
      }
    }

    private async Task SubscriptionDroppedAsync(EventStoreSubscription subscription, SubscriptionDropReason dropReason, Exception exception)
    {
      if (await CanRetryAsync(subscription.ProcessingEventNumber, dropReason).ConfigureAwait(false))
      {
        var subscriptionDropped = new DroppedSubscription(Subscription, exception.Message, dropReason);
        await HandleDroppedSubscriptionAsync(subscriptionDropped).ConfigureAwait(false);
      }
    }
  }
}
