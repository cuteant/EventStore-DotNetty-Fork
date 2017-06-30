using System;
using System.Threading.Tasks;
using EventStore.ClientAPI.Subscriptions;
using Microsoft.Extensions.Logging;

namespace EventStore.ClientAPI.Consumers
{
  /// <summary>Represents the consumer of a persistent subscription to EventStore: http://docs.geteventstore.com/introduction/4.0.0/subscriptions/
  /// This kind of consumer supports the competing consumer messaging pattern: http://www.enterpriseintegrationpatterns.com/patterns/messaging/CompetingConsumers.html </summary>
  public class PersistentConsumer<TEvent> : StreamConsumer<PersistentSubscription<TEvent>, ConnectToPersistentSubscriptionSettings, TEvent>
    where TEvent : class
  {
    private static readonly ILogger s_logger = TraceLogger.GetLogger<VolatileConsumer>();

    private bool processingResolvedEvent;
    private Func<EventStorePersistentSubscription<TEvent>, ResolvedEvent<TEvent>, Task> _resolvedEventAppearedAsync;
    private Action<EventStorePersistentSubscription<TEvent>, ResolvedEvent<TEvent>> _resolvedEventAppeared;
    private Func<EventStorePersistentSubscription<TEvent>, ResolvedEvent<TEvent>, Task> _eventAppearedAsync;
    private Action<EventStorePersistentSubscription<TEvent>, ResolvedEvent<TEvent>> _eventAppeared;

    protected override void Initialize(IEventStoreConnectionBase2 connection, PersistentSubscription<TEvent> subscription)
    {
      if (string.IsNullOrEmpty(subscription.SubscriptionId)) { throw new ArgumentNullException(nameof(subscription.SubscriptionId)); }
      if (null == subscription.PersistentSettings) { throw new ArgumentNullException(nameof(subscription.PersistentSettings)); }

      base.Initialize(connection, subscription);
    }

    public void Initialize(IEventStoreConnectionBase2 connection, PersistentSubscription<TEvent> subscription,
      Func<EventStorePersistentSubscription<TEvent>, ResolvedEvent<TEvent>, Task> resolvedEventAppearedAsync)
    {
      Initialize(connection, subscription);
      _resolvedEventAppearedAsync = resolvedEventAppearedAsync ?? throw new ArgumentNullException(nameof(resolvedEventAppearedAsync));
      processingResolvedEvent = true;
    }

    public void Initialize(IEventStoreConnectionBase2 connection, PersistentSubscription<TEvent> subscription,
      Action<EventStorePersistentSubscription<TEvent>, ResolvedEvent<TEvent>> resolvedEventAppeared)
    {
      Initialize(connection, subscription);
      _resolvedEventAppeared = resolvedEventAppeared ?? throw new ArgumentNullException(nameof(resolvedEventAppeared));
      processingResolvedEvent = true;
    }

    public void Initialize(IEventStoreConnectionBase2 connection, PersistentSubscription<TEvent> subscription, Func<TEvent, Task> eventAppearedAsync)
    {
      if (null == eventAppearedAsync) { throw new ArgumentNullException(nameof(eventAppearedAsync)); }
      Initialize(connection, subscription);
      _eventAppearedAsync = (sub, resolvedEvent) => eventAppearedAsync(resolvedEvent.Body);
      processingResolvedEvent = false;
    }

    public void Initialize(IEventStoreConnectionBase2 connection, PersistentSubscription<TEvent> subscription, Action<TEvent> eventAppeared)
    {
      if (null == eventAppeared) { throw new ArgumentNullException(nameof(eventAppeared)); }
      Initialize(connection, subscription);
      _eventAppeared = (sub, resolvedEvent) => eventAppeared(resolvedEvent.Body);
      processingResolvedEvent = false;
    }

    public override async Task ConnectToSubscriptionAsync()
    {

      if (string.IsNullOrEmpty(Subscription.Topic))
      {
        Connection.UpdatePersistentSubscription<TEvent>(Subscription.SubscriptionId, Subscription.PersistentSettings, Subscription.Credentials);
      }
      else
      {
        Connection.UpdatePersistentSubscription<TEvent>(Subscription.Topic, Subscription.SubscriptionId, Subscription.PersistentSettings, Subscription.Credentials);
      }

      await InternalConnectToSubscriptionAsync().ConfigureAwait(false);
    }

    public override async Task ConnectToSubscriptionAsync(long? lastCheckpoint)
    {
      if (string.IsNullOrEmpty(Subscription.Topic))
      {
        Connection.DeletePersistentSubscription<TEvent>(Subscription.SubscriptionId, Subscription.Credentials);

        await Connection
            .CreatePersistentSubscriptionAsync<TEvent>(Subscription.SubscriptionId,
                Subscription.PersistentSettings.Clone(lastCheckpoint ?? -1), Subscription.Credentials)
            .ConfigureAwait(false);
      }
      else
      {
        Connection.DeletePersistentSubscription<TEvent>(Subscription.Topic, Subscription.SubscriptionId, Subscription.Credentials);

        await Connection
            .CreatePersistentSubscriptionAsync<TEvent>(Subscription.Topic, Subscription.SubscriptionId,
                Subscription.PersistentSettings.Clone(lastCheckpoint ?? -1), Subscription.Credentials)
            .ConfigureAwait(false);
      }

      await InternalConnectToSubscriptionAsync().ConfigureAwait(false);
    }

    private async Task InternalConnectToSubscriptionAsync()
    {
      try
      {
        if (processingResolvedEvent)
        {
          if (_resolvedEventAppearedAsync != null)
          {
            await Connection.PersistentSubscribeAsync<TEvent>(Subscription.Topic, Subscription.SubscriptionId, Subscription.Settings, _resolvedEventAppearedAsync,
                    async (sub, reason, exception) => await SubscriptionDroppedAsync(sub, reason, exception).ConfigureAwait(false),
                    Subscription.Credentials).ConfigureAwait(false);
          }
          else
          {
            await Connection.PersistentSubscribeAsync<TEvent>(Subscription.Topic, Subscription.SubscriptionId, Subscription.Settings, _resolvedEventAppeared,
                    async (sub, reason, exception) => await SubscriptionDroppedAsync(sub, reason, exception).ConfigureAwait(false),
                    Subscription.Credentials).ConfigureAwait(false);
          }
        }
        else
        {
          if (_eventAppearedAsync != null)
          {
            await Connection.PersistentSubscribeAsync<TEvent>(Subscription.Topic, Subscription.SubscriptionId, Subscription.Settings, _eventAppearedAsync,
                    async (sub, reason, exception) => await SubscriptionDroppedAsync(sub, reason, exception).ConfigureAwait(false),
                    Subscription.Credentials).ConfigureAwait(false);
          }
          else
          {
            await Connection.PersistentSubscribeAsync<TEvent>(Subscription.Topic, Subscription.SubscriptionId, Subscription.Settings, _eventAppeared,
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

    private async Task SubscriptionDroppedAsync(EventStorePersistentSubscription<TEvent> subscription, SubscriptionDropReason dropReason, Exception exception)
    {
      var subscriptionDropped = new DroppedSubscription(Subscription, exception.Message, dropReason);

      await HandleDroppedSubscriptionAsync(subscriptionDropped).ConfigureAwait(false);
    }
  }
}
