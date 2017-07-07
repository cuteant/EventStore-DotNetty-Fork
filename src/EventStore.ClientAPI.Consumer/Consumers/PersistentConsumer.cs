using System;
using System.Threading;
using System.Threading.Tasks;
using EventStore.ClientAPI.Subscriptions;
using Microsoft.Extensions.Logging;

namespace EventStore.ClientAPI.Consumers
{
  /// <summary>Represents the consumer of a persistent subscription to EventStore: http://docs.geteventstore.com/introduction/4.0.0/subscriptions/
  /// This kind of consumer supports the competing consumer messaging pattern: http://www.enterpriseintegrationpatterns.com/patterns/messaging/CompetingConsumers.html </summary>
  public class PersistentConsumer : StreamConsumer<PersistentSubscription, ConnectToPersistentSubscriptionSettings>
  {
    private static readonly ILogger s_logger = TraceLogger.GetLogger<VolatileConsumer>();

    private Func<EventStorePersistentSubscription, ResolvedEvent<object>, Task> _eventAppearedAsync;
    private Action<EventStorePersistentSubscription, ResolvedEvent<object>> _eventAppeared;

    private EventStorePersistentSubscription esSubscription;
    private EventStorePersistentSubscription2 esSubscription2;

    protected override void OnDispose(bool disposing)
    {
      base.OnDispose(disposing);
      var subscription = Interlocked.Exchange(ref esSubscription, null);
      subscription?.Stop(TimeSpan.FromMinutes(1));
      var subscription2 = Interlocked.Exchange(ref esSubscription2, null);
      subscription2?.Stop(TimeSpan.FromMinutes(1));
    }

    protected override void Initialize(IEventStoreConnectionBase2 connection, PersistentSubscription subscription)
    {
      if (string.IsNullOrEmpty(subscription.SubscriptionId)) { throw new ArgumentNullException(nameof(subscription.SubscriptionId)); }
      if (null == subscription.PersistentSettings) { throw new ArgumentNullException(nameof(subscription.PersistentSettings)); }

      base.Initialize(connection, subscription);
    }

    public void Initialize(IEventStoreConnectionBase2 connection, PersistentSubscription subscription, Func<EventStorePersistentSubscription, ResolvedEvent<object>, Task> eventAppearedAsync)
    {
      Initialize(connection, subscription);
      _eventAppearedAsync = eventAppearedAsync ?? throw new ArgumentNullException(nameof(eventAppearedAsync));
    }

    public void Initialize(IEventStoreConnectionBase2 connection, PersistentSubscription subscription, Action<EventStorePersistentSubscription, ResolvedEvent<object>> eventAppeared)
    {
      Initialize(connection, subscription);
      _eventAppeared = eventAppeared ?? throw new ArgumentNullException(nameof(eventAppeared));
    }

    public override async Task ConnectToSubscriptionAsync()
    {
      if (Interlocked.CompareExchange(ref _subscribed, ON, OFF) == ON) { return; }

      if (string.IsNullOrEmpty(Subscription.Topic))
      {
        Connection.UpdatePersistentSubscription(Subscription.StreamId, Subscription.SubscriptionId, Subscription.PersistentSettings, Subscription.Credentials);
      }
      else
      {
        Connection.UpdatePersistentSubscription(Subscription.StreamId, Subscription.Topic, Subscription.SubscriptionId, Subscription.PersistentSettings, Subscription.Credentials);
      }

      await InternalConnectToSubscriptionAsync().ConfigureAwait(false);
    }

    public override async Task ConnectToSubscriptionAsync(long? lastCheckpoint)
    {
      if (Interlocked.CompareExchange(ref _subscribed, ON, OFF) == ON) { return; }

      if (string.IsNullOrEmpty(Subscription.Topic))
      {
        Connection.DeletePersistentSubscription(Subscription.StreamId, Subscription.SubscriptionId, Subscription.Credentials);

        await Connection
            .CreatePersistentSubscriptionAsync(
                Subscription.StreamId, Subscription.SubscriptionId,
                Subscription.PersistentSettings.Clone(lastCheckpoint ?? -1), Subscription.Credentials)
            .ConfigureAwait(false);
      }
      else
      {
        Connection.DeletePersistentSubscription(Subscription.StreamId, Subscription.Topic, Subscription.SubscriptionId, Subscription.Credentials);

        await Connection
            .CreatePersistentSubscriptionAsync(
                Subscription.StreamId, Subscription.Topic, Subscription.SubscriptionId,
                Subscription.PersistentSettings.Clone(lastCheckpoint ?? -1), Subscription.Credentials)
            .ConfigureAwait(false);
      }

      await InternalConnectToSubscriptionAsync().ConfigureAwait(false);
    }

    private async Task InternalConnectToSubscriptionAsync()
    {
      try
      {
        if (UsingEventHandlers)
        {
          if (string.IsNullOrEmpty(Subscription.Topic))
          {
            if (RegisterEventHandlers != null)
            {
              esSubscription2 = await Connection.PersistentSubscribeAsync(Subscription.StreamId, Subscription.SubscriptionId, Subscription.Settings, RegisterEventHandlers,
                      async (sub, reason, exception) => await SubscriptionDroppedAsync(sub, reason, exception).ConfigureAwait(false),
                      Subscription.Credentials).ConfigureAwait(false);
            }
            else
            {
              esSubscription2 = await Connection.PersistentSubscribeAsync(Subscription.StreamId, Subscription.SubscriptionId, Subscription.Settings, RegisterHandlers,
                      async (sub, reason, exception) => await SubscriptionDroppedAsync(sub, reason, exception).ConfigureAwait(false),
                      Subscription.Credentials).ConfigureAwait(false);
            }
          }
          else
          {
            if (RegisterEventHandlers != null)
            {
              esSubscription2 = await Connection.PersistentSubscribeAsync(Subscription.StreamId, Subscription.Topic, Subscription.SubscriptionId, Subscription.Settings, RegisterEventHandlers,
                      async (sub, reason, exception) => await SubscriptionDroppedAsync(sub, reason, exception).ConfigureAwait(false),
                      Subscription.Credentials).ConfigureAwait(false);
            }
            else
            {
              esSubscription2 = await Connection.PersistentSubscribeAsync(Subscription.StreamId, Subscription.Topic, Subscription.SubscriptionId, Subscription.Settings, RegisterHandlers,
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
              esSubscription = await Connection.PersistentSubscribeAsync(Subscription.StreamId, Subscription.SubscriptionId, Subscription.Settings, _eventAppearedAsync,
                      async (sub, reason, exception) => await SubscriptionDroppedAsync(sub, reason, exception).ConfigureAwait(false),
                      Subscription.Credentials).ConfigureAwait(false);
            }
            else
            {
              esSubscription = await Connection.PersistentSubscribeAsync(Subscription.StreamId, Subscription.SubscriptionId, Subscription.Settings, _eventAppeared,
                      async (sub, reason, exception) => await SubscriptionDroppedAsync(sub, reason, exception).ConfigureAwait(false),
                      Subscription.Credentials).ConfigureAwait(false);
            }
          }
          else
          {
            if (_eventAppearedAsync != null)
            {
              esSubscription = await Connection.PersistentSubscribeAsync(Subscription.StreamId, Subscription.Topic, Subscription.SubscriptionId, Subscription.Settings, _eventAppearedAsync,
                      async (sub, reason, exception) => await SubscriptionDroppedAsync(sub, reason, exception).ConfigureAwait(false),
                      Subscription.Credentials).ConfigureAwait(false);
            }
            else
            {
              esSubscription = await Connection.PersistentSubscribeAsync(Subscription.StreamId, Subscription.Topic, Subscription.SubscriptionId, Subscription.Settings, _eventAppeared,
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

    private async Task SubscriptionDroppedAsync(EventStorePersistentSubscription subscription, SubscriptionDropReason dropReason, Exception exception)
    {
      var subscriptionDropped = new DroppedSubscription(Subscription, exception.Message, dropReason);

      await HandleDroppedSubscriptionAsync(subscriptionDropped).ConfigureAwait(false);
    }

    private async Task SubscriptionDroppedAsync(EventStorePersistentSubscription2 subscription, SubscriptionDropReason dropReason, Exception exception)
    {
      var subscriptionDropped = new DroppedSubscription(Subscription, exception.Message, dropReason);

      await HandleDroppedSubscriptionAsync(subscriptionDropped).ConfigureAwait(false);
    }
  }
}
