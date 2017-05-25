using System;
using System.Threading.Tasks;
using EventStore.ClientAPI.SystemData;

namespace EventStore.ClientAPI.Embedded
{
  internal class EmbeddedEventStorePersistentSubscription : EventStorePersistentSubscriptionBase
  {
    private readonly EmbeddedSubscriber _subscriptions;

    internal EmbeddedEventStorePersistentSubscription(string subscriptionId, string streamId,
      Action<EventStorePersistentSubscriptionBase, ResolvedEvent> eventAppeared,
      Action<EventStorePersistentSubscriptionBase, SubscriptionDropReason, Exception> subscriptionDropped,
      UserCredentials userCredentials, bool verboseLogging, ConnectionSettings settings,
      EmbeddedSubscriber subscriptions, int bufferSize = 10, bool autoAck = true)
      : base(subscriptionId, streamId, eventAppeared, subscriptionDropped, userCredentials, verboseLogging, settings, bufferSize, autoAck)
    {
      _subscriptions = subscriptions;
    }
    internal EmbeddedEventStorePersistentSubscription(string subscriptionId, string streamId,
      Func<EventStorePersistentSubscriptionBase, ResolvedEvent, Task> eventAppearedAsync,
      Action<EventStorePersistentSubscriptionBase, SubscriptionDropReason, Exception> subscriptionDropped,
      UserCredentials userCredentials, bool verboseLogging, ConnectionSettings settings,
      EmbeddedSubscriber subscriptions, int bufferSize = 10, bool autoAck = true)
      : base(subscriptionId, streamId, eventAppearedAsync, subscriptionDropped, userCredentials, verboseLogging, settings, bufferSize, autoAck)
    {
      _subscriptions = subscriptions;
    }

    internal override Task<PersistentEventStoreSubscription> StartSubscriptionAsync(
      string subscriptionId, string streamId, int bufferSize, UserCredentials userCredentials,
      Func<EventStoreSubscription, ResolvedEvent, Task> onEventAppearedAsync,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> onSubscriptionDropped,
      ConnectionSettings settings)
    {
      var source = new TaskCompletionSource<PersistentEventStoreSubscription>();

      _subscriptions.StartPersistentSubscription(Guid.NewGuid(), source, subscriptionId, streamId, userCredentials, bufferSize, onEventAppearedAsync,
          onSubscriptionDropped, settings.MaxRetries, settings.OperationTimeout);

      return source.Task;
    }
  }
}