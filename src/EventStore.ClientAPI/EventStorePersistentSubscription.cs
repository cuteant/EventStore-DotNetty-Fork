using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using EventStore.ClientAPI.Common.Utils;
using EventStore.ClientAPI.Internal;
using EventStore.ClientAPI.SystemData;

namespace EventStore.ClientAPI
{
  #region -- class EventStorePersistentSubscription --

  /// <summary>Represents a persistent subscription connection.</summary>
  public sealed class EventStorePersistentSubscription : EventStorePersistentSubscriptionBase<EventStorePersistentSubscription, PersistentSubscriptionResolvedEvent<object>, ResolvedEvent<object>>
  {
    static EventStorePersistentSubscription()
    {
      DropSubscriptionEvent = new PersistentSubscriptionResolvedEvent<object>();
    }
    private readonly EventStoreConnectionLogicHandler _handler;

    internal EventStorePersistentSubscription(string subscriptionId, string streamId,
                                              ConnectToPersistentSubscriptionSettings settings,
                                              Action<EventStorePersistentSubscription, ResolvedEvent<object>, int?> eventAppeared,
                                              Action<EventStorePersistentSubscription, SubscriptionDropReason, Exception> subscriptionDropped,
                                              UserCredentials userCredentials, ConnectionSettings connSettings,
                                              EventStoreConnectionLogicHandler handler)
      : base(subscriptionId, streamId, settings, eventAppeared, subscriptionDropped, userCredentials, connSettings)
    {
      _handler = handler;
    }

    internal EventStorePersistentSubscription(string subscriptionId, string streamId,
                                              ConnectToPersistentSubscriptionSettings settings,
                                              Func<EventStorePersistentSubscription, ResolvedEvent<object>, int?, Task> eventAppearedAsync,
                                              Action<EventStorePersistentSubscription, SubscriptionDropReason, Exception> subscriptionDropped,
                                              UserCredentials userCredentials, ConnectionSettings connSettings,
                                              EventStoreConnectionLogicHandler handler)
      : base(subscriptionId, streamId, settings, eventAppearedAsync, subscriptionDropped, userCredentials, connSettings)
    {
      _handler = handler;
    }

    internal override Task<PersistentEventStoreSubscription> StartSubscriptionAsync(string subscriptionId, string streamId,
      ConnectToPersistentSubscriptionSettings settings, UserCredentials userCredentials,
      Func<EventStoreSubscription, PersistentSubscriptionResolvedEvent<object>, Task> onEventAppearedAsync,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> onSubscriptionDropped,
      ConnectionSettings connSettings)
    {
      var source = TaskUtility.CreateTaskCompletionSource<PersistentEventStoreSubscription>();
      _handler.EnqueueMessage(new StartPersistentSubscriptionMessage(source, subscriptionId, streamId, settings, userCredentials,
          onEventAppearedAsync, onSubscriptionDropped, connSettings.MaxRetries, connSettings.OperationTimeout));

      return source.Task;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override ResolvedEvent<object> TransformEvent(PersistentSubscriptionResolvedEvent<object> resolvedEvent) => resolvedEvent.Event;
  }

  #endregion

  #region -- class EventStorePersistentSubscription2 --

  /// <summary>Represents a persistent subscription connection.</summary>
  public sealed class EventStorePersistentSubscription2 : EventStorePersistentSubscriptionBase<EventStorePersistentSubscription2, IPersistentSubscriptionResolvedEvent2, IResolvedEvent2>
  {
    static EventStorePersistentSubscription2()
    {
      DropSubscriptionEvent = new PersistentSubscriptionResolvedEvent<object>();
    }
    private readonly EventStoreConnectionLogicHandler _handler;

    internal EventStorePersistentSubscription2(string subscriptionId, string streamId,
                                               ConnectToPersistentSubscriptionSettings settings,
                                               Action<EventStorePersistentSubscription2, IResolvedEvent2, int?> eventAppeared,
                                               Action<EventStorePersistentSubscription2, SubscriptionDropReason, Exception> subscriptionDropped,
                                               UserCredentials userCredentials, ConnectionSettings connSettings,
                                               EventStoreConnectionLogicHandler handler)
      : base(subscriptionId, streamId, settings, eventAppeared, subscriptionDropped, userCredentials, connSettings)
    {
      _handler = handler;
    }

    internal EventStorePersistentSubscription2(string subscriptionId, string streamId,
                                               ConnectToPersistentSubscriptionSettings settings,
                                               Func<EventStorePersistentSubscription2, IResolvedEvent2, int?, Task> eventAppearedAsync,
                                               Action<EventStorePersistentSubscription2, SubscriptionDropReason, Exception> subscriptionDropped,
                                               UserCredentials userCredentials, ConnectionSettings connSettings,
                                               EventStoreConnectionLogicHandler handler)
      : base(subscriptionId, streamId, settings, eventAppearedAsync, subscriptionDropped, userCredentials, connSettings)
    {
      _handler = handler;
    }

    internal override Task<PersistentEventStoreSubscription> StartSubscriptionAsync(string subscriptionId, string streamId,
      ConnectToPersistentSubscriptionSettings settings, UserCredentials userCredentials,
      Func<EventStoreSubscription, IPersistentSubscriptionResolvedEvent2, Task> onEventAppearedAsync,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> onSubscriptionDropped,
      ConnectionSettings connSettings)
    {
      var source = TaskUtility.CreateTaskCompletionSource<PersistentEventStoreSubscription>();
      _handler.EnqueueMessage(new StartPersistentSubscriptionMessage2(source, subscriptionId, streamId, settings, userCredentials,
          onEventAppearedAsync, onSubscriptionDropped, connSettings.MaxRetries, connSettings.OperationTimeout));

      return source.Task;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override IResolvedEvent2 TransformEvent(IPersistentSubscriptionResolvedEvent2 resolvedEvent) => resolvedEvent;
  }

  #endregion

  #region -- class EventStorePersistentSubscription<TEvent> --

  /// <summary>Represents a persistent subscription connection.</summary>
  public sealed class EventStorePersistentSubscription<TEvent> : EventStorePersistentSubscriptionBase<EventStorePersistentSubscription<TEvent>, PersistentSubscriptionResolvedEvent<TEvent>, ResolvedEvent<TEvent>>
    where TEvent : class
  {
    static EventStorePersistentSubscription()
    {
      DropSubscriptionEvent = new PersistentSubscriptionResolvedEvent<TEvent>();
    }
    private readonly EventStoreConnectionLogicHandler _handler;

    internal EventStorePersistentSubscription(string subscriptionId, string streamId,
                                              ConnectToPersistentSubscriptionSettings settings,
                                              Action<EventStorePersistentSubscription<TEvent>, ResolvedEvent<TEvent>, int?> eventAppeared,
                                              Action<EventStorePersistentSubscription<TEvent>, SubscriptionDropReason, Exception> subscriptionDropped,
                                              UserCredentials userCredentials, ConnectionSettings connSettings,
                                              EventStoreConnectionLogicHandler handler)
      : base(subscriptionId, streamId, settings, eventAppeared, subscriptionDropped, userCredentials, connSettings)
    {
      _handler = handler;
    }

    internal EventStorePersistentSubscription(string subscriptionId, string streamId,
                                              ConnectToPersistentSubscriptionSettings settings,
                                              Func<EventStorePersistentSubscription<TEvent>, ResolvedEvent<TEvent>, int?, Task> eventAppearedAsync,
                                              Action<EventStorePersistentSubscription<TEvent>, SubscriptionDropReason, Exception> subscriptionDropped,
                                              UserCredentials userCredentials, ConnectionSettings connSettings,
                                              EventStoreConnectionLogicHandler handler)
      : base(subscriptionId, streamId, settings, eventAppearedAsync, subscriptionDropped, userCredentials, connSettings)
    {
      _handler = handler;
    }

    internal override Task<PersistentEventStoreSubscription> StartSubscriptionAsync(string subscriptionId, string streamId,
      ConnectToPersistentSubscriptionSettings settings, UserCredentials userCredentials,
      Func<EventStoreSubscription, PersistentSubscriptionResolvedEvent<TEvent>, Task> onEventAppearedAsync,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> onSubscriptionDropped,
      ConnectionSettings connSettings)
    {
      var source = TaskUtility.CreateTaskCompletionSource<PersistentEventStoreSubscription>();

      _handler.EnqueueMessage(new StartPersistentSubscriptionMessageWrapper
      {
        Source = source,
        EventType = typeof(TEvent),
        MaxRetries = connSettings.MaxRetries,
        Timeout = connSettings.OperationTimeout,
        Message = new StartPersistentSubscriptionMessage<TEvent>(source, subscriptionId, streamId, settings, userCredentials,
            onEventAppearedAsync, onSubscriptionDropped, connSettings.MaxRetries, connSettings.OperationTimeout)
      });

      return source.Task;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override ResolvedEvent<TEvent> TransformEvent(PersistentSubscriptionResolvedEvent<TEvent> resolvedEvent) => resolvedEvent.Event;
  }

  #endregion

  #region -- class EventStorePersistentRawSubscription --

  /// <summary>Represents a persistent subscription connection.</summary>
  internal sealed class EventStorePersistentRawSubscription : EventStorePersistentSubscriptionBase
  {
    static EventStorePersistentRawSubscription()
    {
      DropSubscriptionEvent = new PersistentSubscriptionResolvedEvent();
    }
    private readonly EventStoreConnectionLogicHandler _handler;

    internal EventStorePersistentRawSubscription(string subscriptionId, string streamId,
                                              ConnectToPersistentSubscriptionSettings settings,
                                              Action<EventStorePersistentSubscriptionBase, ResolvedEvent, int?> eventAppeared,
                                              Action<EventStorePersistentSubscriptionBase, SubscriptionDropReason, Exception> subscriptionDropped,
                                              UserCredentials userCredentials, ConnectionSettings connSettings,
                                              EventStoreConnectionLogicHandler handler)
      : base(subscriptionId, streamId, settings, eventAppeared, subscriptionDropped, userCredentials, connSettings)
    {
      _handler = handler;
    }

    internal EventStorePersistentRawSubscription(string subscriptionId, string streamId,
                                              ConnectToPersistentSubscriptionSettings settings,
                                              Func<EventStorePersistentSubscriptionBase, ResolvedEvent, int?, Task> eventAppearedAsync,
                                              Action<EventStorePersistentSubscriptionBase, SubscriptionDropReason, Exception> subscriptionDropped,
                                              UserCredentials userCredentials, ConnectionSettings connSettings,
                                              EventStoreConnectionLogicHandler handler)
      : base(subscriptionId, streamId, settings, eventAppearedAsync, subscriptionDropped, userCredentials, connSettings)
    {
      _handler = handler;
    }

    internal override Task<PersistentEventStoreSubscription> StartSubscriptionAsync(string subscriptionId, string streamId,
      ConnectToPersistentSubscriptionSettings settings, UserCredentials userCredentials,
      Func<EventStoreSubscription, PersistentSubscriptionResolvedEvent, Task> onEventAppearedAsync,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> onSubscriptionDropped,
      ConnectionSettings connSettings)
    {
      var source = TaskUtility.CreateTaskCompletionSource<PersistentEventStoreSubscription>();
      _handler.EnqueueMessage(new StartPersistentSubscriptionRawMessage(source, subscriptionId, streamId, settings, userCredentials,
          onEventAppearedAsync, onSubscriptionDropped, connSettings.MaxRetries, connSettings.OperationTimeout));

      return source.Task;
    }
  }

  #endregion
}
