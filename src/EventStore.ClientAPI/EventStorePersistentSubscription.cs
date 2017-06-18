using System;
using System.Threading.Tasks;
using EventStore.ClientAPI.Internal;
using EventStore.ClientAPI.SystemData;

namespace EventStore.ClientAPI
{
  /// <summary>Represents a persistent subscription connection.</summary>
  public class EventStorePersistentSubscription : EventStorePersistentSubscriptionBase
  {
    private readonly EventStoreConnectionLogicHandler _handler;

    internal EventStorePersistentSubscription(string subscriptionId, string streamId,
                                              ConnectToPersistentSubscriptionSettings settings,
                                              Action<EventStorePersistentSubscriptionBase, ResolvedEvent> eventAppeared,
                                              Action<EventStorePersistentSubscriptionBase, SubscriptionDropReason, Exception> subscriptionDropped,
                                              UserCredentials userCredentials, ConnectionSettings connSettings,
                                              EventStoreConnectionLogicHandler handler)
      : base(subscriptionId, streamId, settings, eventAppeared, subscriptionDropped, userCredentials, connSettings)
    {
      _handler = handler;
    }

    internal EventStorePersistentSubscription(string subscriptionId, string streamId,
                                              ConnectToPersistentSubscriptionSettings settings,
                                              Func<EventStorePersistentSubscriptionBase, ResolvedEvent, Task> eventAppearedAsync,
                                              Action<EventStorePersistentSubscriptionBase, SubscriptionDropReason, Exception> subscriptionDropped,
                                              UserCredentials userCredentials, ConnectionSettings connSettings,
                                              EventStoreConnectionLogicHandler handler)
      : base(subscriptionId, streamId, settings, eventAppearedAsync, subscriptionDropped, userCredentials, connSettings)
    {
      _handler = handler;
    }

    internal override Task<PersistentEventStoreSubscription> StartSubscriptionAsync(string subscriptionId, string streamId,
      ConnectToPersistentSubscriptionSettings settings, UserCredentials userCredentials,
      Func<EventStoreSubscription, ResolvedEvent, Task> onEventAppearedAsync,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> onSubscriptionDropped,
      ConnectionSettings connSettings)
    {
      var source = new TaskCompletionSource<PersistentEventStoreSubscription>();
      _handler.EnqueueMessage(new StartPersistentSubscriptionRawMessage(source, subscriptionId, streamId, settings, userCredentials,
          onEventAppearedAsync, onSubscriptionDropped, connSettings.MaxRetries, connSettings.OperationTimeout));

      return source.Task;
    }
  }
}
