using System;
using System.Threading.Tasks;
using EventStore.ClientAPI.SystemData;

namespace EventStore.ClientAPI.Embedded
{
  internal class EmbeddedEventStorePersistentSubscription : EventStorePersistentSubscriptionBase
  {
    private readonly EmbeddedSubscriber _subscriptions;

    internal EmbeddedEventStorePersistentSubscription(string subscriptionId, string streamId,
      ConnectToPersistentSubscriptionSettings settings,
      Action<EventStorePersistentSubscriptionBase, ResolvedEvent> eventAppeared,
      Action<EventStorePersistentSubscriptionBase, SubscriptionDropReason, Exception> subscriptionDropped,
      UserCredentials userCredentials, ConnectionSettings conSettings, EmbeddedSubscriber subscriptions)
      : base(subscriptionId, streamId, settings, eventAppeared, subscriptionDropped, userCredentials, conSettings)
    {
      _subscriptions = subscriptions;
    }
    internal EmbeddedEventStorePersistentSubscription(string subscriptionId, string streamId,
      ConnectToPersistentSubscriptionSettings settings,
      Func<EventStorePersistentSubscriptionBase, ResolvedEvent, Task> eventAppearedAsync,
      Action<EventStorePersistentSubscriptionBase, SubscriptionDropReason, Exception> subscriptionDropped,
      UserCredentials userCredentials, ConnectionSettings conSettings, EmbeddedSubscriber subscriptions)
      : base(subscriptionId, streamId, settings, eventAppearedAsync, subscriptionDropped, userCredentials, conSettings)
    {
      _subscriptions = subscriptions;
    }

    internal override Task<PersistentEventStoreSubscription> StartSubscriptionAsync(
      string subscriptionId, string streamId, ConnectToPersistentSubscriptionSettings settings, UserCredentials userCredentials,
      Func<EventStoreSubscription, ResolvedEvent, Task> onEventAppearedAsync,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> onSubscriptionDropped,
      ConnectionSettings connSettings)
    {
      var source = new TaskCompletionSource<PersistentEventStoreSubscription>(TaskCreationOptions.RunContinuationsAsynchronously);

      _subscriptions.StartPersistentSubscription(Guid.NewGuid(), source, subscriptionId, streamId, userCredentials, settings.BufferSize,
          onEventAppearedAsync, onSubscriptionDropped, connSettings.MaxRetries, connSettings.OperationTimeout);

      return source.Task;
    }
  }
}