using System;
using System.Threading;
using System.Threading.Tasks;
using EventStore.ClientAPI.Subscriptions;
using Microsoft.Extensions.Logging;

namespace EventStore.ClientAPI.Consumers
{
  /// <summary>Represents a consumer to a volatile subscription to EventStore: http://docs.geteventstore.com/introduction/4.0.0/subscriptions/ .</summary>
  public class VolatileConsumer : StreamConsumer<VolatileSubscription, SubscriptionSettings>
  {
    private static readonly ILogger s_logger = TraceLogger.GetLogger<VolatileConsumer>();

    private Func<EventStoreSubscription, ResolvedEvent<object>, Task> _eventAppearedAsync;
    private Action<EventStoreSubscription, ResolvedEvent<object>> _eventAppeared;

    private EventStoreSubscription _esSubscription;

    protected override void OnDispose(bool disposing)
    {
      base.OnDispose(disposing);
      var subscription = Interlocked.Exchange(ref _esSubscription, null);
      subscription?.Dispose();
    }

    public void Initialize(IEventStoreBus bus, VolatileSubscription subscription, Func<EventStoreSubscription, ResolvedEvent<object>, Task> eventAppearedAsync)
    {
      Initialize(bus, subscription);
      _eventAppearedAsync = eventAppearedAsync ?? throw new ArgumentNullException(nameof(eventAppearedAsync));
    }

    public void Initialize(IEventStoreBus bus, VolatileSubscription subscription, Action<EventStoreSubscription, ResolvedEvent<object>> eventAppeared)
    {
      Initialize(bus, subscription);
      _eventAppeared = eventAppeared ?? throw new ArgumentNullException(nameof(eventAppeared));
    }

    public override async Task ConnectToSubscriptionAsync()
    {
      if (Interlocked.CompareExchange(ref _subscribed, ON, OFF) == ON) { return; }

      try
      {
        if (UsingEventHandlers)
        {
          if (string.IsNullOrEmpty(Subscription.Topic))
          {
            if (RegisterEventHandlers != null)
            {
              _esSubscription = await Bus.VolatileSubscribeAsync(Subscription.StreamId, Subscription.Settings, RegisterEventHandlers,
                      async (sub, reason, exception) => await SubscriptionDroppedAsync(sub, reason, exception).ConfigureAwait(false),
                      Subscription.Credentials).ConfigureAwait(false);
            }
            else
            {
              _esSubscription = await Bus.VolatileSubscribeAsync(Subscription.StreamId, Subscription.Settings, RegisterHandlers,
                      async (sub, reason, exception) => await SubscriptionDroppedAsync(sub, reason, exception).ConfigureAwait(false),
                      Subscription.Credentials).ConfigureAwait(false);
            }
          }
          else
          {
            if (RegisterEventHandlers != null)
            {
              _esSubscription = await Bus.VolatileSubscribeAsync(Subscription.StreamId, Subscription.Topic, Subscription.Settings, RegisterEventHandlers,
                      async (sub, reason, exception) => await SubscriptionDroppedAsync(sub, reason, exception).ConfigureAwait(false),
                      Subscription.Credentials).ConfigureAwait(false);
            }
            else
            {
              _esSubscription = await Bus.VolatileSubscribeAsync(Subscription.StreamId, Subscription.Topic, Subscription.Settings, RegisterHandlers,
                      async (sub, reason, exception) => await SubscriptionDroppedAsync(sub, reason, exception).ConfigureAwait(false),
                      Subscription.Credentials).ConfigureAwait(false);
            }
          }
        }
        else
        {
          if (string.IsNullOrEmpty(Subscription.Topic))
          {
            if (_eventAppearedAsync != null)
            {
              _esSubscription = await Bus.VolatileSubscribeAsync(Subscription.StreamId, Subscription.Settings, _eventAppearedAsync,
                      async (sub, reason, exception) => await SubscriptionDroppedAsync(sub, reason, exception).ConfigureAwait(false),
                      Subscription.Credentials).ConfigureAwait(false);
            }
            else
            {
              _esSubscription = await Bus.VolatileSubscribeAsync(Subscription.StreamId, Subscription.Settings, _eventAppeared,
                      async (sub, reason, exception) => await SubscriptionDroppedAsync(sub, reason, exception).ConfigureAwait(false),
                      Subscription.Credentials).ConfigureAwait(false);
            }
          }
          else
          {
            if (_eventAppearedAsync != null)
            {
              _esSubscription = await Bus.VolatileSubscribeAsync(Subscription.StreamId, Subscription.Topic, Subscription.Settings, _eventAppearedAsync,
                      async (sub, reason, exception) => await SubscriptionDroppedAsync(sub, reason, exception).ConfigureAwait(false),
                      Subscription.Credentials).ConfigureAwait(false);
            }
            else
            {
              _esSubscription = await Bus.VolatileSubscribeAsync(Subscription.StreamId, Subscription.Topic, Subscription.Settings, _eventAppeared,
                      async (sub, reason, exception) => await SubscriptionDroppedAsync(sub, reason, exception).ConfigureAwait(false),
                      Subscription.Credentials).ConfigureAwait(false);
            }
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
