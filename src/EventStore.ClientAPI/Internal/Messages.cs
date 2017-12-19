using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using EventStore.ClientAPI.ClientOperations;
using EventStore.ClientAPI.Common.Utils;
using EventStore.ClientAPI.SystemData;
using EventStore.ClientAPI.Transport.Tcp;

namespace EventStore.ClientAPI.Internal
{
  #region == class Message ==

  internal abstract class Message
  {
  }

  #endregion

  #region == class TimerTickMessage ==

  internal class TimerTickMessage : Message
  {
  }

  #endregion

  #region == class StartConnectionMessage ==

  internal class StartConnectionMessage : Message
  {
    public readonly TaskCompletionSource<object> Task;
    public readonly IEndPointDiscoverer EndPointDiscoverer;

    public StartConnectionMessage(TaskCompletionSource<object> task, IEndPointDiscoverer endPointDiscoverer)
    {
      Ensure.NotNull(task, nameof(task));
      Ensure.NotNull(endPointDiscoverer, nameof(endPointDiscoverer));

      Task = task;
      EndPointDiscoverer = endPointDiscoverer;
    }
  }

  #endregion

  #region == class CloseConnectionMessage ==

  internal class CloseConnectionMessage : Message
  {
    public readonly string Reason;
    public readonly Exception Exception;

    public CloseConnectionMessage(string reason, Exception exception)
    {
      Reason = reason;
      Exception = exception;
    }
  }

  #endregion

  #region == class EstablishTcpConnectionMessage ==

  internal class EstablishTcpConnectionMessage : Message
  {
    public readonly NodeEndPoints EndPoints;

    public EstablishTcpConnectionMessage(NodeEndPoints endPoints)
    {
      EndPoints = endPoints;
    }
  }

  #endregion

  #region == class TcpConnectionEstablishedMessage ==

  internal class TcpConnectionEstablishedMessage : Message
  {
    public readonly TcpPackageConnection Connection;

    public TcpConnectionEstablishedMessage(TcpPackageConnection connection)
    {
      Ensure.NotNull(connection, nameof(connection));
      Connection = connection;
    }
  }

  #endregion

  #region == class TcpConnectionClosedMessage ==

  internal class TcpConnectionClosedMessage : Message
  {
    public readonly TcpPackageConnection Connection;
    public readonly SocketError Error;

    public TcpConnectionClosedMessage(TcpPackageConnection connection, SocketError error)
    {
      Ensure.NotNull(connection, nameof(connection));
      Connection = connection;
      Error = error;
    }
  }

  #endregion

  #region == class StartOperationMessage ==

  internal class StartOperationMessage : Message
  {
    public readonly IClientOperation Operation;
    public readonly int MaxRetries;
    public readonly TimeSpan Timeout;

    public StartOperationMessage(IClientOperation operation, int maxRetries, TimeSpan timeout)
    {
      Ensure.NotNull(operation, nameof(operation));
      Operation = operation;
      MaxRetries = maxRetries;
      Timeout = timeout;
    }
  }

  #endregion


  #region == class StartSubscriptionMessage ==

  internal sealed class StartSubscriptionMessage : StartSubscriptionMessageBase<ResolvedEvent<object>>
  {
    public StartSubscriptionMessage(TaskCompletionSource<EventStoreSubscription> source,
      string streamId, SubscriptionSettings settings, UserCredentials userCredentials,
      Action<EventStoreSubscription, ResolvedEvent<object>> eventAppeared,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped,
      int maxRetries, TimeSpan timeout)
      : base(source, streamId, settings, userCredentials, eventAppeared, subscriptionDropped, maxRetries, timeout)
    {
    }
    public StartSubscriptionMessage(TaskCompletionSource<EventStoreSubscription> source,
      string streamId, SubscriptionSettings settings, UserCredentials userCredentials,
      Func<EventStoreSubscription, ResolvedEvent<object>, Task> eventAppearedAsync,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped,
      int maxRetries, TimeSpan timeout)
      : base(source, streamId, settings, userCredentials, eventAppearedAsync, subscriptionDropped, maxRetries, timeout)
    {
    }
  }

  #endregion

  #region == class StartSubscriptionMessage2 ==

  internal sealed class StartSubscriptionMessage2 : StartSubscriptionMessageBase<IResolvedEvent2>
  {
    public StartSubscriptionMessage2(TaskCompletionSource<EventStoreSubscription> source,
      string streamId, SubscriptionSettings settings, UserCredentials userCredentials,
      Action<EventStoreSubscription, IResolvedEvent2> eventAppeared,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped,
      int maxRetries, TimeSpan timeout)
      : base(source, streamId, settings, userCredentials, eventAppeared, subscriptionDropped, maxRetries, timeout)
    {
    }
    public StartSubscriptionMessage2(TaskCompletionSource<EventStoreSubscription> source,
      string streamId, SubscriptionSettings settings, UserCredentials userCredentials,
      Func<EventStoreSubscription, IResolvedEvent2, Task> eventAppearedAsync,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped,
      int maxRetries, TimeSpan timeout)
      : base(source, streamId, settings, userCredentials, eventAppearedAsync, subscriptionDropped, maxRetries, timeout)
    {
    }
  }

  #endregion

  #region == class StartSubscriptionMessage<TEvent> ==

  internal sealed class StartSubscriptionMessageWrapper : Message
  {
    public TaskCompletionSource<EventStoreSubscription> Source { get; set; }

    public Type EventType { get; set; }
    public Object Message { get; set; }

    public int MaxRetries { get; set; }
    public TimeSpan Timeout { get; set; }
  }

  internal sealed class StartSubscriptionMessage<TEvent> : StartSubscriptionMessageBase<ResolvedEvent<TEvent>>
    where TEvent : class
  {
    public StartSubscriptionMessage(TaskCompletionSource<EventStoreSubscription> source,
      string streamId, SubscriptionSettings settings, UserCredentials userCredentials,
      Action<EventStoreSubscription, ResolvedEvent<TEvent>> eventAppeared,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped,
      int maxRetries, TimeSpan timeout)
      : base(source, streamId, settings, userCredentials, eventAppeared, subscriptionDropped, maxRetries, timeout)
    {
    }
    public StartSubscriptionMessage(TaskCompletionSource<EventStoreSubscription> source,
      string streamId, SubscriptionSettings settings, UserCredentials userCredentials,
      Func<EventStoreSubscription, ResolvedEvent<TEvent>, Task> eventAppearedAsync,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped,
      int maxRetries, TimeSpan timeout)
      : base(source, streamId, settings, userCredentials, eventAppearedAsync, subscriptionDropped, maxRetries, timeout)
    {
    }
  }

  #endregion

  #region == class StartSubscriptionRawMessage ==

  internal sealed class StartSubscriptionRawMessage : StartSubscriptionMessageBase<ResolvedEvent>
  {
    public StartSubscriptionRawMessage(TaskCompletionSource<EventStoreSubscription> source,
      string streamId, SubscriptionSettings settings, UserCredentials userCredentials,
      Action<EventStoreSubscription, ResolvedEvent> eventAppeared,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped,
      int maxRetries, TimeSpan timeout)
      : base(source, streamId, settings, userCredentials, eventAppeared, subscriptionDropped, maxRetries, timeout)
    {
    }
    public StartSubscriptionRawMessage(TaskCompletionSource<EventStoreSubscription> source,
      string streamId, SubscriptionSettings settings, UserCredentials userCredentials,
      Func<EventStoreSubscription, ResolvedEvent, Task> eventAppearedAsync,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped,
      int maxRetries, TimeSpan timeout)
      : base(source, streamId, settings, userCredentials, eventAppearedAsync, subscriptionDropped, maxRetries, timeout)
    {
    }
  }

  #endregion

  #region == class StartSubscriptionMessageBase<TResolvedEvent> ==

  internal abstract class StartSubscriptionMessageBase<TResolvedEvent> : Message
    where TResolvedEvent : IResolvedEvent
  {
    public readonly TaskCompletionSource<EventStoreSubscription> Source;

    public readonly string StreamId;
    public readonly SubscriptionSettings Settings;
    public readonly UserCredentials UserCredentials;
    public readonly Action<EventStoreSubscription, TResolvedEvent> EventAppeared;
    public readonly Func<EventStoreSubscription, TResolvedEvent, Task> EventAppearedAsync;
    public readonly Action<EventStoreSubscription, SubscriptionDropReason, Exception> SubscriptionDropped;

    public readonly int MaxRetries;
    public readonly TimeSpan Timeout;

    public StartSubscriptionMessageBase(TaskCompletionSource<EventStoreSubscription> source,
      string streamId, SubscriptionSettings settings, UserCredentials userCredentials,
      Action<EventStoreSubscription, TResolvedEvent> eventAppeared,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped,
      int maxRetries, TimeSpan timeout)
      : this(source, streamId, settings, userCredentials, subscriptionDropped, maxRetries, timeout)
    {
      Ensure.NotNull(eventAppeared, nameof(eventAppeared));

      EventAppeared = eventAppeared;
    }
    public StartSubscriptionMessageBase(TaskCompletionSource<EventStoreSubscription> source,
      string streamId, SubscriptionSettings settings, UserCredentials userCredentials,
      Func<EventStoreSubscription, TResolvedEvent, Task> eventAppearedAsync,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped,
      int maxRetries, TimeSpan timeout)
      : this(source, streamId, settings, userCredentials, subscriptionDropped, maxRetries, timeout)
    {
      Ensure.NotNull(eventAppearedAsync, nameof(eventAppearedAsync));

      EventAppearedAsync = eventAppearedAsync;
    }

    private StartSubscriptionMessageBase(TaskCompletionSource<EventStoreSubscription> source,
      string streamId, SubscriptionSettings settings, UserCredentials userCredentials,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped,
      int maxRetries, TimeSpan timeout)
    {
      Ensure.NotNull(source, nameof(source));
      Ensure.NotNull(settings, nameof(settings));

      Source = source;
      StreamId = streamId;
      Settings = settings;
      UserCredentials = userCredentials;
      SubscriptionDropped = subscriptionDropped;
      MaxRetries = maxRetries;
      Timeout = timeout;
    }
  }

  #endregion


  #region == class StartPersistentSubscriptionMessage ==

  internal sealed class StartPersistentSubscriptionMessage : StartPersistentSubscriptionMessageBase<PersistentSubscriptionResolvedEvent<object>>
  {
    public StartPersistentSubscriptionMessage(TaskCompletionSource<PersistentEventStoreSubscription> source,
      string subscriptionId, string streamId, ConnectToPersistentSubscriptionSettings settings, UserCredentials userCredentials,
      Func<PersistentEventStoreSubscription, PersistentSubscriptionResolvedEvent<object>, Task> eventAppearedAsync,
      Action<PersistentEventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped,
      int maxRetries, TimeSpan timeout)
      : base(source, subscriptionId, streamId, settings, userCredentials,
             eventAppearedAsync, subscriptionDropped, maxRetries, timeout)
    {
    }
  }

  #endregion

  #region == class StartPersistentSubscriptionMessage2 ==

  internal sealed class StartPersistentSubscriptionMessage2 : StartPersistentSubscriptionMessageBase<IPersistentSubscriptionResolvedEvent2>
  {
    public StartPersistentSubscriptionMessage2(TaskCompletionSource<PersistentEventStoreSubscription> source,
      string subscriptionId, string streamId, ConnectToPersistentSubscriptionSettings settings, UserCredentials userCredentials,
      Func<PersistentEventStoreSubscription, IPersistentSubscriptionResolvedEvent2, Task> eventAppearedAsync,
      Action<PersistentEventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped,
      int maxRetries, TimeSpan timeout)
      : base(source, subscriptionId, streamId, settings, userCredentials,
             eventAppearedAsync, subscriptionDropped, maxRetries, timeout)
    {
    }
  }

  #endregion

  #region == class StartPersistentSubscriptionMessage<TEvent> ==

  internal sealed class StartPersistentSubscriptionMessageWrapper : Message
  {
    public TaskCompletionSource<PersistentEventStoreSubscription> Source { get; set; }

    public Type EventType { get; set; }
    public Object Message { get; set; }

    public int MaxRetries { get; set; }
    public TimeSpan Timeout { get; set; }
  }

  internal sealed class StartPersistentSubscriptionMessage<TEvent> : StartPersistentSubscriptionMessageBase<PersistentSubscriptionResolvedEvent<TEvent>>
    where TEvent : class
  {
    public StartPersistentSubscriptionMessage(TaskCompletionSource<PersistentEventStoreSubscription> source,
      string subscriptionId, string streamId, ConnectToPersistentSubscriptionSettings settings, UserCredentials userCredentials,
      Func<PersistentEventStoreSubscription, PersistentSubscriptionResolvedEvent<TEvent>, Task> eventAppearedAsync,
      Action<PersistentEventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped,
      int maxRetries, TimeSpan timeout)
      : base(source, subscriptionId, streamId, settings, userCredentials,
             eventAppearedAsync, subscriptionDropped, maxRetries, timeout)
    {
    }
  }

  #endregion

  #region == class StartPersistentSubscriptionRawMessage ==

  internal sealed class StartPersistentSubscriptionRawMessage : StartPersistentSubscriptionMessageBase<PersistentSubscriptionResolvedEvent>
  {
    public StartPersistentSubscriptionRawMessage(TaskCompletionSource<PersistentEventStoreSubscription> source,
      string subscriptionId, string streamId, ConnectToPersistentSubscriptionSettings settings, UserCredentials userCredentials,
      Func<PersistentEventStoreSubscription, PersistentSubscriptionResolvedEvent, Task> eventAppearedAsync,
      Action<PersistentEventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped,
      int maxRetries, TimeSpan timeout)
      : base(source, subscriptionId, streamId, settings, userCredentials,
             eventAppearedAsync, subscriptionDropped, maxRetries, timeout)
    {
    }
  }

  #endregion

  #region == class class StartPersistentSubscriptionMessageBase<TResolvedEvent> ==

  internal abstract class StartPersistentSubscriptionMessageBase<TResolvedEvent> : Message
    where TResolvedEvent : IPersistentSubscriptionResolvedEvent
  {
    public readonly TaskCompletionSource<PersistentEventStoreSubscription> Source;

    public readonly string SubscriptionId;
    public readonly string StreamId;
    public readonly ConnectToPersistentSubscriptionSettings Settings;
    public readonly UserCredentials UserCredentials;
    public readonly Func<PersistentEventStoreSubscription, TResolvedEvent, Task> EventAppearedAsync;
    public readonly Action<PersistentEventStoreSubscription, SubscriptionDropReason, Exception> SubscriptionDropped;

    public readonly int MaxRetries;
    public readonly TimeSpan Timeout;

    public StartPersistentSubscriptionMessageBase(TaskCompletionSource<PersistentEventStoreSubscription> source,
      string subscriptionId, string streamId, ConnectToPersistentSubscriptionSettings settings, UserCredentials userCredentials,
      Func<PersistentEventStoreSubscription, TResolvedEvent, Task> eventAppearedAsync,
      Action<PersistentEventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped,
      int maxRetries, TimeSpan timeout)
    {
      Ensure.NotNull(source, nameof(source));
      Ensure.NotNull(eventAppearedAsync, nameof(eventAppearedAsync));
      Ensure.NotNullOrEmpty(subscriptionId, nameof(subscriptionId));
      Ensure.NotNull(settings, nameof(settings));

      SubscriptionId = subscriptionId;
      Settings = settings;
      Source = source;
      StreamId = streamId;
      UserCredentials = userCredentials;
      EventAppearedAsync = eventAppearedAsync;
      SubscriptionDropped = subscriptionDropped;
      MaxRetries = maxRetries;
      Timeout = timeout;
    }
  }

  #endregion


  #region == class HandleTcpPackageMessage ==

  internal class HandleTcpPackageMessage : Message
  {
    public readonly TcpPackageConnection Connection;
    public readonly TcpPackage Package;

    public HandleTcpPackageMessage(TcpPackageConnection connection, TcpPackage package)
    {
      Connection = connection;
      Package = package;
    }
  }

  #endregion

  #region == class TcpConnectionErrorMessage ==

  internal class TcpConnectionErrorMessage : Message
  {
    public readonly TcpPackageConnection Connection;
    public readonly Exception Exception;

    public TcpConnectionErrorMessage(TcpPackageConnection connection, Exception exception)
    {
      Connection = connection;
      Exception = exception;
    }
  }

  #endregion
}
