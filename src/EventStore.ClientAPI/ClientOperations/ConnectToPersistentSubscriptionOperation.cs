using System;
using System.Linq;
using System.Threading.Tasks;
using EventStore.ClientAPI.Common.Utils;
using EventStore.ClientAPI.Exceptions;
using EventStore.ClientAPI.Messages;
using EventStore.ClientAPI.Internal;
using EventStore.ClientAPI.SystemData;
using EventStore.ClientAPI.Transport.Tcp;

namespace EventStore.ClientAPI.ClientOperations
{
  #region == class PersistentSubscriptionOperation ==

  internal sealed class PersistentSubscriptionOperation : ConnectToPersistentSubscriptionOperationBase<ResolvedEvent<object>>
  {
    public PersistentSubscriptionOperation(TaskCompletionSource<PersistentEventStoreSubscription> source,
      string groupName, string streamId, ConnectToPersistentSubscriptionSettings settings, UserCredentials userCredentials,
      Func<PersistentEventStoreSubscription, ResolvedEvent<object>, Task> eventAppearedAsync,
      Action<PersistentEventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped,
      Func<TcpPackageConnection> getConnection)
      : base(source, groupName, streamId, settings, userCredentials, eventAppearedAsync, subscriptionDropped, getConnection)
    {
    }

    protected override ResolvedEvent<object> TransformEvent(ClientMessage.ResolvedEvent rawEvent)
    {
      return rawEvent.ToResolvedEvent();
    }

    protected override ResolvedEvent<object> TransformEvent(ClientMessage.ResolvedIndexedEvent rawEvent)
    {
      return rawEvent.ToResolvedEvent();
    }
  }

  #endregion

  #region == class PersistentSubscriptionOperation ==

  internal sealed class PersistentSubscriptionOperation2 : ConnectToPersistentSubscriptionOperationBase<IResolvedEvent2>
  {
    public PersistentSubscriptionOperation2(TaskCompletionSource<PersistentEventStoreSubscription> source,
      string groupName, string streamId, ConnectToPersistentSubscriptionSettings settings, UserCredentials userCredentials,
      Func<PersistentEventStoreSubscription, IResolvedEvent2, Task> eventAppearedAsync,
      Action<PersistentEventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped,
      Func<TcpPackageConnection> getConnection)
      : base(source, groupName, streamId, settings, userCredentials, eventAppearedAsync, subscriptionDropped, getConnection)
    {
    }

    protected override IResolvedEvent2 TransformEvent(ClientMessage.ResolvedEvent rawEvent)
    {
      return rawEvent.ToResolvedEvent2();
    }

    protected override IResolvedEvent2 TransformEvent(ClientMessage.ResolvedIndexedEvent rawEvent)
    {
      return rawEvent.ToResolvedEvent2();
    }
  }

  #endregion

  #region == class PersistentSubscriptionOperation<TEvent> ==

  internal interface IPersistentSubscriptionOperationWrapper
  {
    ISubscriptionOperation Create(StartPersistentSubscriptionMessageWrapper msgWrapper, TcpPackageConnection connection);
  }
  internal sealed class PersistentSubscriptionOperationWrapper<TEvent> : IPersistentSubscriptionOperationWrapper
    where TEvent : class
  {
    public PersistentSubscriptionOperationWrapper() { }

    public ISubscriptionOperation Create(StartPersistentSubscriptionMessageWrapper msgWrapper, TcpPackageConnection connection)
    {
      var msg = (StartPersistentSubscriptionMessage<TEvent>)msgWrapper.Message;

      return new PersistentSubscriptionOperation<TEvent>(msg.Source, msg.SubscriptionId, msg.StreamId, msg.Settings,
          msg.UserCredentials, msg.EventAppearedAsync, msg.SubscriptionDropped, () => connection);
    }
  }

  internal sealed class PersistentSubscriptionOperation<TEvent> : ConnectToPersistentSubscriptionOperationBase<ResolvedEvent<TEvent>>
    where TEvent : class
  {
    public PersistentSubscriptionOperation(TaskCompletionSource<PersistentEventStoreSubscription> source,
      string groupName, string streamId, ConnectToPersistentSubscriptionSettings settings, UserCredentials userCredentials,
      Func<PersistentEventStoreSubscription, ResolvedEvent<TEvent>, Task> eventAppearedAsync,
      Action<PersistentEventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped,
      Func<TcpPackageConnection> getConnection)
      : base(source, groupName, streamId, settings, userCredentials, eventAppearedAsync, subscriptionDropped, getConnection)
    {
    }

    protected override ResolvedEvent<TEvent> TransformEvent(ClientMessage.ResolvedEvent rawEvent)
    {
      return rawEvent.ToResolvedEvent<TEvent>();
    }

    protected override ResolvedEvent<TEvent> TransformEvent(ClientMessage.ResolvedIndexedEvent rawEvent)
    {
      return rawEvent.ToResolvedEvent<TEvent>();
    }
  }

  #endregion

  #region == class ConnectToPersistentSubscriptionOperation ==

  internal sealed class ConnectToPersistentSubscriptionOperation : ConnectToPersistentSubscriptionOperationBase<ResolvedEvent>
  {
    public ConnectToPersistentSubscriptionOperation(TaskCompletionSource<PersistentEventStoreSubscription> source,
      string groupName, string streamId, ConnectToPersistentSubscriptionSettings settings, UserCredentials userCredentials,
      Func<PersistentEventStoreSubscription, ResolvedEvent, Task> eventAppearedAsync,
      Action<PersistentEventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped,
      Func<TcpPackageConnection> getConnection)
      : base(source, groupName, streamId, settings, userCredentials, eventAppearedAsync, subscriptionDropped, getConnection)
    {
    }

    protected override ResolvedEvent TransformEvent(ClientMessage.ResolvedEvent rawEvent)
    {
      return rawEvent.ToRawResolvedEvent();
    }

    protected override ResolvedEvent TransformEvent(ClientMessage.ResolvedIndexedEvent rawEvent)
    {
      return rawEvent.ToRawResolvedEvent();
    }
  }

  #endregion

  #region == class ConnectToPersistentSubscriptionOperationBase<TResolvedEvent> ==

  internal abstract class ConnectToPersistentSubscriptionOperationBase<TResolvedEvent> : SubscriptionOperation<PersistentEventStoreSubscription, TResolvedEvent>, IConnectToPersistentSubscriptions
    where TResolvedEvent : IResolvedEvent
  {
    private readonly string _groupName;
    private readonly int _bufferSize;
    private string _subscriptionId;

    //public ConnectToPersistentSubscriptionOperation(TaskCompletionSource<PersistentEventStoreSubscription> source,
    //  string groupName, string streamId, ConnectToPersistentSubscriptionSettings settings, UserCredentials userCredentials,
    //  Action<PersistentEventStoreSubscription, TResolvedEvent> eventAppeared,
    //  Action<PersistentEventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped,
    //  Func<TcpPackageConnection> getConnection)
    //  : base(source, streamId, new SubscriptionSettings { ResolveLinkTos = false }, userCredentials, eventAppeared, subscriptionDropped, getConnection)
    //{
    //  _groupName = groupName;
    //  _bufferSize = settings.BufferSize;
    //}
    public ConnectToPersistentSubscriptionOperationBase(TaskCompletionSource<PersistentEventStoreSubscription> source,
      string groupName, string streamId, ConnectToPersistentSubscriptionSettings settings, UserCredentials userCredentials,
      Func<PersistentEventStoreSubscription, TResolvedEvent, Task> eventAppearedAsync,
      Action<PersistentEventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped,
      Func<TcpPackageConnection> getConnection)
      : base(source, streamId,
          //new SubscriptionSettings { ResolveLinkTos = false, TaskScheduler = settings.TaskScheduler, CancellationToken = settings.CancellationToken }, 
          SubscriptionSettings.Default,
          userCredentials, eventAppearedAsync, subscriptionDropped, getConnection)
    {
      _groupName = groupName;
      _bufferSize = settings.BufferSize;
    }

    protected override TcpPackage CreateSubscriptionPackage()
    {
      var dto = new ClientMessage.ConnectToPersistentSubscription(_groupName, _streamId, _bufferSize);
      return new TcpPackage(TcpCommand.ConnectToPersistentSubscription,
                            _userCredentials != null ? TcpFlags.Authenticated : TcpFlags.None,
                            _correlationId,
                            _userCredentials?.Username,
                            _userCredentials?.Password,
                            dto.Serialize());
    }

    protected override bool InspectPackage(TcpPackage package, out InspectionResult result)
    {
      if (package.Command == TcpCommand.PersistentSubscriptionConfirmation)
      {
        var dto = package.Data.Deserialize<ClientMessage.PersistentSubscriptionConfirmation>();
        ConfirmSubscription(dto.LastCommitPosition, dto.LastEventNumber);
        result = new InspectionResult(InspectionDecision.Subscribed, "SubscriptionConfirmation");
        _subscriptionId = dto.SubscriptionId;
        return true;
      }
      if (package.Command == TcpCommand.PersistentSubscriptionStreamEventAppeared)
      {
        var dto = package.Data.Deserialize<ClientMessage.PersistentSubscriptionStreamEventAppeared>();
        EventAppeared(TransformEvent(dto.Event));
        result = new InspectionResult(InspectionDecision.DoNothing, "StreamEventAppeared");
        return true;
      }
      if (package.Command == TcpCommand.SubscriptionDropped)
      {
        var dto = package.Data.Deserialize<ClientMessage.SubscriptionDropped>();
        if (dto.Reason == ClientMessage.SubscriptionDropped.SubscriptionDropReason.AccessDenied)
        {
          DropSubscription(SubscriptionDropReason.AccessDenied, new AccessDeniedException("You do not have access to the stream."));
          result = new InspectionResult(InspectionDecision.EndOperation, "SubscriptionDropped");
          return true;
        }
        if (dto.Reason == ClientMessage.SubscriptionDropped.SubscriptionDropReason.NotFound)
        {
          DropSubscription(SubscriptionDropReason.NotFound, new ArgumentException("Subscription not found"));
          result = new InspectionResult(InspectionDecision.EndOperation, "SubscriptionDropped");
          return true;
        }
        if (dto.Reason == ClientMessage.SubscriptionDropped.SubscriptionDropReason.PersistentSubscriptionDeleted)
        {
          DropSubscription(SubscriptionDropReason.PersistentSubscriptionDeleted, new PersistentSubscriptionDeletedException());
          result = new InspectionResult(InspectionDecision.EndOperation, "SubscriptionDropped");
          return true;
        }
        if (dto.Reason == ClientMessage.SubscriptionDropped.SubscriptionDropReason.SubscriberMaxCountReached)
        {
          DropSubscription(SubscriptionDropReason.MaxSubscribersReached, new MaximumSubscribersReachedException());
          result = new InspectionResult(InspectionDecision.EndOperation, "SubscriptionDropped");
          return true;
        }
        DropSubscription((SubscriptionDropReason)dto.Reason, null, _getConnection());
        result = new InspectionResult(InspectionDecision.EndOperation, "SubscriptionDropped");
        return true;
      }
      result = null;
      return false;
    }

    protected override PersistentEventStoreSubscription CreateSubscriptionObject(long lastCommitPosition, long? lastEventNumber)
    {
      return new PersistentEventStoreSubscription(this, _streamId, lastCommitPosition, lastEventNumber);
    }

    public void NotifyEventsProcessed(Guid[] processedEvents)
    {
      Ensure.NotNull(processedEvents, nameof(processedEvents));
      var dto = new ClientMessage.PersistentSubscriptionAckEvents(
          _subscriptionId,
          processedEvents.Select(x => x.ToByteArray()).ToArray());

      var package = new TcpPackage(TcpCommand.PersistentSubscriptionAckEvents,
                            _userCredentials != null ? TcpFlags.Authenticated : TcpFlags.None,
                            _correlationId,
                            _userCredentials?.Username,
                            _userCredentials?.Password,
                            dto.Serialize());
      EnqueueSend(package);
    }

    public void NotifyEventsFailed(Guid[] processedEvents, PersistentSubscriptionNakEventAction action, string reason)
    {
      Ensure.NotNull(processedEvents, nameof(processedEvents));
      Ensure.NotNull(reason, nameof(reason));
      var dto = new ClientMessage.PersistentSubscriptionNakEvents(
          _subscriptionId,
          processedEvents.Select(x => x.ToByteArray()).ToArray(),
          reason,
          (ClientMessage.PersistentSubscriptionNakEvents.NakAction)action);

      var package = new TcpPackage(TcpCommand.PersistentSubscriptionNakEvents,
                            _userCredentials != null ? TcpFlags.Authenticated : TcpFlags.None,
                            _correlationId,
                            _userCredentials?.Username,
                            _userCredentials?.Password,
                            dto.Serialize());
      EnqueueSend(package);
    }
  }

  #endregion

  #region -- class MaximumSubscribersReachedException --

  /// <summary>Thrown when max subscribers is set on subscription and it has been reached</summary>
  public class MaximumSubscribersReachedException : Exception
  {
    const string _errorMsg = "Maximum subscriptions reached.";
    /// <summary>Constructs a <see cref="MaximumSubscribersReachedException" />.</summary>
    public MaximumSubscribersReachedException()
      : base(_errorMsg)
    {
    }
  }

  #endregion

  #region -- class PersistentSubscriptionDeletedException --

  /// <summary>Thrown when the persistent subscription has been deleted to subscribers connected to it.</summary>
  public class PersistentSubscriptionDeletedException : Exception
  {
    const string _errorMsg = "The subscription has been deleted.";

    /// <summary>Constructs a <see cref="PersistentSubscriptionDeletedException" />.</summary>
    public PersistentSubscriptionDeletedException()
      : base(_errorMsg)
    {
    }
  }

  #endregion
}