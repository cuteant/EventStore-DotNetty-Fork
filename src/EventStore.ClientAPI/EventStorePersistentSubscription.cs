﻿using System;
using System.Threading.Tasks;
using EventStore.ClientAPI.Internal;
using EventStore.ClientAPI.SystemData;
using Microsoft.Extensions.Logging;

namespace EventStore.ClientAPI
{
  /// <summary>Represents a persistent subscription connection.</summary>
  public class EventStorePersistentSubscription : EventStorePersistentSubscriptionBase
  {
    private readonly EventStoreConnectionLogicHandler _handler;

    internal EventStorePersistentSubscription(
        string subscriptionId, string streamId,
        Action<EventStorePersistentSubscriptionBase, ResolvedEvent> eventAppeared,
        Action<EventStorePersistentSubscriptionBase, SubscriptionDropReason, Exception> subscriptionDropped,
        UserCredentials userCredentials, bool verboseLogging, ConnectionSettings settings,
        EventStoreConnectionLogicHandler handler, int bufferSize = 10, bool autoAck = true)
        : base(
            subscriptionId, streamId, eventAppeared, subscriptionDropped, userCredentials, verboseLogging,
            settings, bufferSize, autoAck)
    {
      _handler = handler;
    }

    internal EventStorePersistentSubscription(
        string subscriptionId, string streamId,
        Func<EventStorePersistentSubscriptionBase, ResolvedEvent, Task> eventAppearedAsync,
        Action<EventStorePersistentSubscriptionBase, SubscriptionDropReason, Exception> subscriptionDropped,
        UserCredentials userCredentials, bool verboseLogging, ConnectionSettings settings,
        EventStoreConnectionLogicHandler handler, int bufferSize = 10, bool autoAck = true)
        : base(
            subscriptionId, streamId, eventAppearedAsync, subscriptionDropped, userCredentials, verboseLogging,
            settings, bufferSize, autoAck)
    {
      _handler = handler;
    }

    internal override Task<PersistentEventStoreSubscription> StartSubscription(
        string subscriptionId, string streamId, int bufferSize, UserCredentials userCredentials, Action<EventStoreSubscription, ResolvedEvent> onEventAppeared,
        Action<EventStoreSubscription, SubscriptionDropReason, Exception> onSubscriptionDropped, ConnectionSettings settings)
    {
      var source = new TaskCompletionSource<PersistentEventStoreSubscription>();
      _handler.EnqueueMessage(new StartPersistentSubscriptionMessage(source, subscriptionId, streamId, bufferSize,
          userCredentials, onEventAppeared,
          onSubscriptionDropped, settings.MaxRetries, settings.OperationTimeout));

      return source.Task;
    }
  }
}
