using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using EventStore.ClientAPI.Common.Utils;
using EventStore.ClientAPI.Exceptions;
using EventStore.ClientAPI.Messages;
using EventStore.ClientAPI.SystemData;
using EventStore.ClientAPI.Transport.Tcp;
using Microsoft.Extensions.Logging;

namespace EventStore.ClientAPI.ClientOperations
{
  internal abstract class SubscriptionOperation<T> : ISubscriptionOperation where T : EventStoreSubscription
  {
    private readonly ILogger _log;
    private readonly TaskCompletionSource<T> _source;
    protected readonly string _streamId;
    protected readonly bool _resolveLinkTos;
    protected readonly UserCredentials _userCredentials;
    protected readonly Action<T, ResolvedEvent> _eventAppeared;
    private readonly Action<T, SubscriptionDropReason, Exception> _subscriptionDropped;
    private readonly bool _verboseLogging;
    protected readonly Func<TcpPackageConnection> _getConnection;
    private readonly int _maxQueueSize = 2000;
    private readonly ActionBlock<Tuple<Action, ILogger>> _actionQueue =
        new ActionBlock<Tuple<Action, ILogger>>(new Action<Tuple<Action, ILogger>>(ExecuteActions));
    private T _subscription;
    private int _unsubscribed;
    protected Guid _correlationId;

    protected SubscriptionOperation(ILogger log,
                                       TaskCompletionSource<T> source,
                                       string streamId,
                                       bool resolveLinkTos,
                                       UserCredentials userCredentials,
                                       Action<T, ResolvedEvent> eventAppeared,
                                       Action<T, SubscriptionDropReason, Exception> subscriptionDropped,
                                       bool verboseLogging,
                                       Func<TcpPackageConnection> getConnection)
    {
      Ensure.NotNull(log, nameof(log));
      Ensure.NotNull(source, nameof(source));
      Ensure.NotNull(eventAppeared, nameof(eventAppeared));
      Ensure.NotNull(getConnection, nameof(getConnection));

      _log = log;
      _source = source;
      _streamId = string.IsNullOrEmpty(streamId) ? string.Empty : streamId;
      _resolveLinkTos = resolveLinkTos;
      _userCredentials = userCredentials;
      _eventAppeared = eventAppeared;
      _subscriptionDropped = subscriptionDropped ?? ((x, y, z) => { });
      _verboseLogging = verboseLogging;
      if (_verboseLogging) { _verboseLogging = _log.IsDebugLevelEnabled(); }
      _getConnection = getConnection;
    }

    protected void EnqueueSend(TcpPackage package)
    {
      _getConnection().EnqueueSend(package);
    }

    public bool Subscribe(Guid correlationId, TcpPackageConnection connection)
    {
      Ensure.NotNull(connection, nameof(connection));

      if (_subscription != null || _unsubscribed != 0) { return false; }

      _correlationId = correlationId;
      connection.EnqueueSend(CreateSubscriptionPackage());
      return true;
    }

    protected abstract TcpPackage CreateSubscriptionPackage();

    public void Unsubscribe()
    {
      DropSubscription(SubscriptionDropReason.UserInitiated, null, _getConnection());
    }

    private TcpPackage CreateUnsubscriptionPackage()
    {
      return new TcpPackage(TcpCommand.UnsubscribeFromStream, _correlationId, new ClientMessage.UnsubscribeFromStream().Serialize());
    }

    protected abstract bool InspectPackage(TcpPackage package, out InspectionResult result);

    public InspectionResult InspectPackage(TcpPackage package)
    {
      try
      {
        if (InspectPackage(package, out InspectionResult result))
        {
          return result;
        }

        switch (package.Command)
        {
          case TcpCommand.StreamEventAppeared:
            {
              var dto = package.Data.Deserialize<ClientMessage.StreamEventAppeared>();
              EventAppeared(new ResolvedEvent(dto.Event));
              return new InspectionResult(InspectionDecision.DoNothing, "StreamEventAppeared");
            }

          case TcpCommand.SubscriptionDropped:
            {
              var dto = package.Data.Deserialize<ClientMessage.SubscriptionDropped>();
              switch (dto.Reason)
              {
                case ClientMessage.SubscriptionDropped.SubscriptionDropReason.Unsubscribed:
                  DropSubscription(SubscriptionDropReason.UserInitiated, null);
                  break;
                case ClientMessage.SubscriptionDropped.SubscriptionDropReason.AccessDenied:
                  DropSubscription(SubscriptionDropReason.AccessDenied,
                                   new AccessDeniedException(string.Format("Subscription to '{0}' failed due to access denied.", _streamId == string.Empty ? "<all>" : _streamId)));
                  break;
                case ClientMessage.SubscriptionDropped.SubscriptionDropReason.NotFound:
                  DropSubscription(SubscriptionDropReason.NotFound,
                                   new ArgumentException(string.Format("Subscription to '{0}' failed due to not found.", _streamId == string.Empty ? "<all>" : _streamId)));
                  break;
                default:
                  if (_verboseLogging) _log.LogDebug("Subscription dropped by server. Reason: {0}.", dto.Reason);
                  DropSubscription(SubscriptionDropReason.Unknown,
                                   new CommandNotExpectedException(string.Format("Unsubscribe reason: '{0}'.", dto.Reason)));
                  break;
              }
              return new InspectionResult(InspectionDecision.EndOperation, string.Format("SubscriptionDropped: {0}", dto.Reason));
            }

          case TcpCommand.NotAuthenticated:
            {
              string message = Helper.EatException(() => Helper.UTF8NoBom.GetString(package.Data.Array, package.Data.Offset, package.Data.Count));
              DropSubscription(SubscriptionDropReason.NotAuthenticated,
                               new NotAuthenticatedException(string.IsNullOrEmpty(message) ? "Authentication error" : message));
              return new InspectionResult(InspectionDecision.EndOperation, "NotAuthenticated");
            }

          case TcpCommand.BadRequest:
            {
              string message = Helper.EatException(() => Helper.UTF8NoBom.GetString(package.Data.Array, package.Data.Offset, package.Data.Count));
              DropSubscription(SubscriptionDropReason.ServerError,
                               new ServerErrorException(string.IsNullOrEmpty(message) ? "<no message>" : message));
              return new InspectionResult(InspectionDecision.EndOperation, string.Format("BadRequest: {0}", message));
            }

          case TcpCommand.NotHandled:
            {
              if (_subscription != null)
                throw new Exception("NotHandled command appeared while we were already subscribed.");

              var message = package.Data.Deserialize<ClientMessage.NotHandled>();
              switch (message.Reason)
              {
                case ClientMessage.NotHandled.NotHandledReason.NotReady:
                  return new InspectionResult(InspectionDecision.Retry, "NotHandled - NotReady");

                case ClientMessage.NotHandled.NotHandledReason.TooBusy:
                  return new InspectionResult(InspectionDecision.Retry, "NotHandled - TooBusy");

                case ClientMessage.NotHandled.NotHandledReason.NotMaster:
                  var masterInfo = message.AdditionalInfo.Deserialize<ClientMessage.NotHandled.MasterInfo>();
                  return new InspectionResult(InspectionDecision.Reconnect, "NotHandled - NotMaster",
                                              masterInfo.ExternalTcpEndPoint, masterInfo.ExternalSecureTcpEndPoint);

                default:
                  _log.LogError("Unknown NotHandledReason: {0}.", message.Reason);
                  return new InspectionResult(InspectionDecision.Retry, "NotHandled - <unknown>");
              }
            }

          default:
            {
              DropSubscription(SubscriptionDropReason.ServerError,
                               new CommandNotExpectedException(package.Command.ToString()));
              return new InspectionResult(InspectionDecision.EndOperation, package.Command.ToString());
            }
        }
      }
      catch (Exception e)
      {
        DropSubscription(SubscriptionDropReason.Unknown, e);
        return new InspectionResult(InspectionDecision.EndOperation, $"Exception - {e.Message}");
      }
    }

    public void ConnectionClosed()
    {
      DropSubscription(SubscriptionDropReason.ConnectionClosed, new ConnectionClosedException("Connection was closed."));
    }

    internal bool TimeOutSubscription()
    {
      if (_subscription != null) { return false; }

      DropSubscription(SubscriptionDropReason.SubscribingError, null);
      return true;
    }

    public void DropSubscription(SubscriptionDropReason reason, Exception exc, TcpPackageConnection connection = null)
    {
      if (Interlocked.CompareExchange(ref _unsubscribed, 1, 0) == 0)
      {
        if (_verboseLogging)
        {
          _log.LogDebug("Subscription {0:B} to {1}: closing subscription, reason: {2}, exception: {3}...",
                     _correlationId, _streamId == string.Empty ? "<all>" : _streamId, reason, exc);
        }

        if (reason != SubscriptionDropReason.UserInitiated)
        {
          exc = exc ?? new Exception($"Subscription dropped for {reason}");
          _source.TrySetException(exc);
        }

        if (reason == SubscriptionDropReason.UserInitiated && _subscription != null && connection != null)
        {
          connection.EnqueueSend(CreateUnsubscriptionPackage());
        }

        if (_subscription != null)
        {
          ExecuteActionAsync(() => _subscriptionDropped(_subscription, reason, exc));
        }
      }
    }

    protected void ConfirmSubscription(long lastCommitPosition, long? lastEventNumber)
    {
      if (lastCommitPosition < -1)
      {
        throw new ArgumentOutOfRangeException(nameof(lastCommitPosition), $"Invalid lastCommitPosition {lastCommitPosition} on subscription confirmation.");
      }
      if (_subscription != null) { throw new Exception("Double confirmation of subscription."); }

      if (_verboseLogging)
        _log.LogDebug("Subscription {0:B} to {1}: subscribed at CommitPosition: {2}, EventNumber: {3}.",
                   _correlationId, _streamId == string.Empty ? "<all>" : _streamId, lastCommitPosition, lastEventNumber);
      _subscription = CreateSubscriptionObject(lastCommitPosition, lastEventNumber);
      _source.SetResult(_subscription);
    }

    protected abstract T CreateSubscriptionObject(long lastCommitPosition, long? lastEventNumber);

    protected void EventAppeared(ResolvedEvent e)
    {
      if (_unsubscribed != 0) { return; }

      if (_subscription == null) throw new Exception("Subscription not confirmed, but event appeared!");

      if (_verboseLogging)
        _log.LogDebug("Subscription {0:B} to {1}: event appeared ({2}, {3}, {4} @ {5}).",
                  _correlationId, _streamId == string.Empty ? "<all>" : _streamId,
                  e.OriginalStreamId, e.OriginalEventNumber, e.OriginalEvent.EventType, e.OriginalPosition);

      ExecuteActionAsync(() => _eventAppeared(_subscription, e));
    }

    private void ExecuteActionAsync(Action action)
    {
      _actionQueue.Post(Tuple.Create(action, _log));
      if (_actionQueue.InputCount > _maxQueueSize)
      {
        DropSubscription(SubscriptionDropReason.UserInitiated, new Exception("client buffer too big"));
      }
    }

    private static void ExecuteActions(Tuple<Action, ILogger> actionWapper)
    {
      try
      {
        actionWapper.Item1.Invoke();
      }
      catch (Exception exc)
      {
        actionWapper.Item2.LogError(exc, "Exception during executing user callback: {0}.", exc.Message);
      }
    }
  }

}