using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using EventStore.ClientAPI.Common.Utils;
using EventStore.ClientAPI.Exceptions;
using EventStore.ClientAPI.Internal;
using EventStore.ClientAPI.Messages;
using EventStore.ClientAPI.SystemData;
using EventStore.ClientAPI.Transport.Tcp;

namespace EventStore.ClientAPI.ClientOperations
{
  #region == class PersistentSubscriptionOperation ==

  internal sealed class PersistentSubscriptionOperation : ConnectToPersistentSubscriptionOperationBase<PersistentSubscriptionResolvedEvent<object>>
  {
    public PersistentSubscriptionOperation(TaskCompletionSource<PersistentEventStoreSubscription> source,
      string groupName, string streamId, ConnectToPersistentSubscriptionSettings settings, UserCredentials userCredentials,
      Func<PersistentEventStoreSubscription, PersistentSubscriptionResolvedEvent<object>, Task> eventAppearedAsync,
      Action<PersistentEventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped,
      Func<TcpPackageConnection> getConnection)
      : base(source, groupName, streamId, settings, userCredentials, eventAppearedAsync, subscriptionDropped, getConnection)
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override PersistentSubscriptionResolvedEvent<object> TransformEvent(ClientMessage.ResolvedIndexedEvent rawEvent, int? retryCount)
    {
      return new PersistentSubscriptionResolvedEvent<object>(rawEvent.ToResolvedEvent(), retryCount);
    }
  }

  #endregion

  #region == class PersistentSubscriptionOperation ==

  internal sealed class PersistentSubscriptionOperation2 : ConnectToPersistentSubscriptionOperationBase<IPersistentSubscriptionResolvedEvent2>
  {
    public PersistentSubscriptionOperation2(TaskCompletionSource<PersistentEventStoreSubscription> source,
      string groupName, string streamId, ConnectToPersistentSubscriptionSettings settings, UserCredentials userCredentials,
      Func<PersistentEventStoreSubscription, IPersistentSubscriptionResolvedEvent2, Task> eventAppearedAsync,
      Action<PersistentEventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped,
      Func<TcpPackageConnection> getConnection)
      : base(source, groupName, streamId, settings, userCredentials, eventAppearedAsync, subscriptionDropped, getConnection)
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override IPersistentSubscriptionResolvedEvent2 TransformEvent(ClientMessage.ResolvedIndexedEvent rawEvent, int? retryCount)
    {
      return rawEvent.ToPersistentSubscriptionResolvedEvent2(retryCount);
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

  internal sealed class PersistentSubscriptionOperation<TEvent> : ConnectToPersistentSubscriptionOperationBase<PersistentSubscriptionResolvedEvent<TEvent>>
    where TEvent : class
  {
    public PersistentSubscriptionOperation(TaskCompletionSource<PersistentEventStoreSubscription> source,
      string groupName, string streamId, ConnectToPersistentSubscriptionSettings settings, UserCredentials userCredentials,
      Func<PersistentEventStoreSubscription, PersistentSubscriptionResolvedEvent<TEvent>, Task> eventAppearedAsync,
      Action<PersistentEventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped,
      Func<TcpPackageConnection> getConnection)
      : base(source, groupName, streamId, settings, userCredentials, eventAppearedAsync, subscriptionDropped, getConnection)
    {
    }

    protected override PersistentSubscriptionResolvedEvent<TEvent> TransformEvent(ClientMessage.ResolvedIndexedEvent rawEvent, int? retryCount)
    {
      return new PersistentSubscriptionResolvedEvent<TEvent>(rawEvent.ToResolvedEvent<TEvent>(), retryCount);
    }
  }

  #endregion

  #region == class ConnectToPersistentSubscriptionOperation ==

  internal sealed class ConnectToPersistentSubscriptionOperation : ConnectToPersistentSubscriptionOperationBase<PersistentSubscriptionResolvedEvent>
  {
    public ConnectToPersistentSubscriptionOperation(TaskCompletionSource<PersistentEventStoreSubscription> source,
      string groupName, string streamId, ConnectToPersistentSubscriptionSettings settings, UserCredentials userCredentials,
      Func<PersistentEventStoreSubscription, PersistentSubscriptionResolvedEvent, Task> eventAppearedAsync,
      Action<PersistentEventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped,
      Func<TcpPackageConnection> getConnection)
      : base(source, groupName, streamId, settings, userCredentials, eventAppearedAsync, subscriptionDropped, getConnection)
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override PersistentSubscriptionResolvedEvent TransformEvent(ClientMessage.ResolvedIndexedEvent rawEvent, int? retryCount)
    {
      return new PersistentSubscriptionResolvedEvent(rawEvent.ToRawResolvedEvent(), retryCount);
    }
  }

  #endregion

  #region == class ConnectToPersistentSubscriptionOperationBase<TResolvedEvent> ==

  internal abstract class ConnectToPersistentSubscriptionOperationBase<TResolvedEvent> : SubscriptionOperation<PersistentEventStoreSubscription, TResolvedEvent>, IConnectToPersistentSubscriptions
    where TResolvedEvent : IPersistentSubscriptionResolvedEvent
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected abstract TResolvedEvent TransformEvent(ClientMessage.ResolvedIndexedEvent rawEvent, int? retryCount);

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

    protected override async Task<InspectionResult> TryInspectPackageAsync(TcpPackage package)
    {
      if (package.Command == TcpCommand.PersistentSubscriptionConfirmation)
      {
        var dto = package.Data.Deserialize<ClientMessage.PersistentSubscriptionConfirmation>();
        ConfirmSubscription(dto.LastCommitPosition, dto.LastEventNumber);
        _subscriptionId = dto.SubscriptionId;
        return new InspectionResult(InspectionDecision.Subscribed, "SubscriptionConfirmation");
      }
      if (package.Command == TcpCommand.PersistentSubscriptionStreamEventAppeared)
      {
        var dto = package.Data.Deserialize<ClientMessage.PersistentSubscriptionStreamEventAppeared>();
        await EventAppearedAsync(TransformEvent(dto.Event, dto.RetryCount)).ConfigureAwait(false);
        return new InspectionResult(InspectionDecision.DoNothing, "StreamEventAppeared");
      }
      if (package.Command == TcpCommand.SubscriptionDropped)
      {
        var dto = package.Data.Deserialize<ClientMessage.SubscriptionDropped>();
        if (dto.Reason == ClientMessage.SubscriptionDropped.SubscriptionDropReason.AccessDenied)
        {
          DropSubscription(SubscriptionDropReason.AccessDenied, new AccessDeniedException("You do not have access to the stream."));
          return new InspectionResult(InspectionDecision.EndOperation, "SubscriptionDropped");
        }
        if (dto.Reason == ClientMessage.SubscriptionDropped.SubscriptionDropReason.NotFound)
        {
          DropSubscription(SubscriptionDropReason.NotFound, new ArgumentException("Subscription not found"));
          return new InspectionResult(InspectionDecision.EndOperation, "SubscriptionDropped");
        }
        if (dto.Reason == ClientMessage.SubscriptionDropped.SubscriptionDropReason.PersistentSubscriptionDeleted)
        {
          DropSubscription(SubscriptionDropReason.PersistentSubscriptionDeleted, new PersistentSubscriptionDeletedException());
          return new InspectionResult(InspectionDecision.EndOperation, "SubscriptionDropped");
        }
        if (dto.Reason == ClientMessage.SubscriptionDropped.SubscriptionDropReason.SubscriberMaxCountReached)
        {
          DropSubscription(SubscriptionDropReason.MaxSubscribersReached, new MaximumSubscribersReachedException());
          return new InspectionResult(InspectionDecision.EndOperation, "SubscriptionDropped");
        }
        DropSubscription((SubscriptionDropReason)dto.Reason, null, _getConnection());
        return new InspectionResult(InspectionDecision.EndOperation, "SubscriptionDropped");
      }
      return null;
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