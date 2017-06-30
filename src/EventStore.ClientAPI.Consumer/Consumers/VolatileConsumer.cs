using System;
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

    public void Initialize(IEventStoreConnectionBase2 connection, VolatileSubscription subscription, Func<EventStoreSubscription, ResolvedEvent<object>, Task> eventAppearedAsync)
    {
      Initialize(connection, subscription);
      _eventAppearedAsync = eventAppearedAsync ?? throw new ArgumentNullException(nameof(eventAppearedAsync));
    }

    public void Initialize(IEventStoreConnectionBase2 connection, VolatileSubscription subscription, Action<EventStoreSubscription, ResolvedEvent<object>> eventAppeared)
    {
      Initialize(connection, subscription);
      _eventAppeared = eventAppeared ?? throw new ArgumentNullException(nameof(eventAppeared));
    }

    public override async Task ConnectToSubscriptionAsync()
    {
      try
      {
        if (UsingEventHandlers)
        {
          if (string.IsNullOrEmpty(Subscription.Topic))
          {
            if (RegisterEventHandlers != null)
            {
              await Connection.VolatileSubscribeAsync(Subscription.StreamId, Subscription.Settings, RegisterEventHandlers,
                      async (sub, reason, exception) => await SubscriptionDroppedAsync(sub, reason, exception).ConfigureAwait(false),
                      Subscription.Credentials).ConfigureAwait(false);
            }
            else
            {
              await Connection.VolatileSubscribeAsync(Subscription.StreamId, Subscription.Settings, RegisterHandlers,
                      async (sub, reason, exception) => await SubscriptionDroppedAsync(sub, reason, exception).ConfigureAwait(false),
                      Subscription.Credentials).ConfigureAwait(false);
            }
          }
          else
          {
            if (RegisterEventHandlers != null)
            {
              await Connection.VolatileSubscribeAsync(Subscription.StreamId, Subscription.Topic, Subscription.Settings, RegisterEventHandlers,
                      async (sub, reason, exception) => await SubscriptionDroppedAsync(sub, reason, exception).ConfigureAwait(false),
                      Subscription.Credentials).ConfigureAwait(false);
            }
            else
            {
              await Connection.VolatileSubscribeAsync(Subscription.StreamId, Subscription.Topic, Subscription.Settings, RegisterHandlers,
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
              await Connection.VolatileSubscribeAsync(Subscription.StreamId, Subscription.Settings, _eventAppearedAsync,
                      async (sub, reason, exception) => await SubscriptionDroppedAsync(sub, reason, exception).ConfigureAwait(false),
                      Subscription.Credentials).ConfigureAwait(false);
            }
            else
            {
              await Connection.VolatileSubscribeAsync(Subscription.StreamId, Subscription.Settings, _eventAppeared,
                      async (sub, reason, exception) => await SubscriptionDroppedAsync(sub, reason, exception).ConfigureAwait(false),
                      Subscription.Credentials).ConfigureAwait(false);
            }
          }
          else
          {
            if (_eventAppearedAsync != null)
            {
              await Connection.VolatileSubscribeAsync(Subscription.StreamId, Subscription.Topic, Subscription.Settings, _eventAppearedAsync,
                      async (sub, reason, exception) => await SubscriptionDroppedAsync(sub, reason, exception).ConfigureAwait(false),
                      Subscription.Credentials).ConfigureAwait(false);
            }
            else
            {
              await Connection.VolatileSubscribeAsync(Subscription.StreamId, Subscription.Topic, Subscription.Settings, _eventAppeared,
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
      var subscriptionDropped = new DroppedSubscription(Subscription, exception.Message, dropReason);

      await HandleDroppedSubscriptionAsync(subscriptionDropped).ConfigureAwait(false);
    }
  }
}
