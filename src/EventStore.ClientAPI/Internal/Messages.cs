using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using EventStore.ClientAPI.ClientOperations;
using EventStore.ClientAPI.Common.Utils;
using EventStore.ClientAPI.SystemData;
using EventStore.ClientAPI.Transport.Tcp;

namespace EventStore.ClientAPI.Internal
{
  internal abstract class Message
  {
  }

  internal class TimerTickMessage : Message
  {
  }

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

  internal class EstablishTcpConnectionMessage : Message
  {
    public readonly NodeEndPoints EndPoints;

    public EstablishTcpConnectionMessage(NodeEndPoints endPoints)
    {
      EndPoints = endPoints;
    }
  }

  internal class TcpConnectionEstablishedMessage : Message
  {
    public readonly TcpPackageConnection Connection;

    public TcpConnectionEstablishedMessage(TcpPackageConnection connection)
    {
      Ensure.NotNull(connection, nameof(connection));
      Connection = connection;
    }
  }

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

  internal class StartSubscriptionMessage : Message
  {
    public readonly TaskCompletionSource<EventStoreSubscription> Source;

    public readonly string StreamId;
    public readonly SubscriptionSettings Settings;
    public readonly UserCredentials UserCredentials;
    public readonly Action<EventStoreSubscription, ResolvedEvent> EventAppeared;
    public readonly Func<EventStoreSubscription, ResolvedEvent, Task> EventAppearedAsync;
    public readonly Action<EventStoreSubscription, SubscriptionDropReason, Exception> SubscriptionDropped;

    public readonly int MaxRetries;
    public readonly TimeSpan Timeout;

    public StartSubscriptionMessage(TaskCompletionSource<EventStoreSubscription> source,
                                        string streamId,
                                        SubscriptionSettings settings,
                                        UserCredentials userCredentials,
                                        Action<EventStoreSubscription, ResolvedEvent> eventAppeared,
                                        Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped,
                                        int maxRetries,
                                        TimeSpan timeout)
      : this(source, streamId, settings, userCredentials, subscriptionDropped, maxRetries, timeout)
    {
      Ensure.NotNull(eventAppeared, nameof(eventAppeared));

      EventAppeared = eventAppeared;
    }
    public StartSubscriptionMessage(TaskCompletionSource<EventStoreSubscription> source,
                                        string streamId,
                                        SubscriptionSettings settings,
                                        UserCredentials userCredentials,
                                        Func<EventStoreSubscription, ResolvedEvent, Task> eventAppearedAsync,
                                        Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped,
                                        int maxRetries,
                                        TimeSpan timeout)
      : this(source, streamId, settings, userCredentials, subscriptionDropped, maxRetries, timeout)
    {
      Ensure.NotNull(eventAppearedAsync, nameof(eventAppearedAsync));

      EventAppearedAsync = eventAppearedAsync;
    }

    private StartSubscriptionMessage(TaskCompletionSource<EventStoreSubscription> source,
                                        string streamId,
                                        SubscriptionSettings settings,
                                        UserCredentials userCredentials,
                                        Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped,
                                        int maxRetries,
                                        TimeSpan timeout)
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

  internal class StartPersistentSubscriptionMessage : Message
  {
    public readonly TaskCompletionSource<PersistentEventStoreSubscription> Source;

    public readonly string SubscriptionId;
    public readonly string StreamId;
    public readonly ConnectToPersistentSubscriptionSettings Settings;
    public readonly UserCredentials UserCredentials;
    public readonly Func<PersistentEventStoreSubscription, ResolvedEvent, Task> EventAppearedAsync;
    public readonly Action<PersistentEventStoreSubscription, SubscriptionDropReason, Exception> SubscriptionDropped;

    public readonly int MaxRetries;
    public readonly TimeSpan Timeout;

    public StartPersistentSubscriptionMessage(TaskCompletionSource<PersistentEventStoreSubscription> source,
      string subscriptionId, string streamId, ConnectToPersistentSubscriptionSettings settings, UserCredentials userCredentials,
      Func<PersistentEventStoreSubscription, ResolvedEvent, Task> eventAppearedAsync,
      Action<PersistentEventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped, int maxRetries, TimeSpan timeout)
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
}
