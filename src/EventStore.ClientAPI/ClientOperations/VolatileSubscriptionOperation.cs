using System;
using System.Threading.Tasks;
using EventStore.ClientAPI.Internal;
using EventStore.ClientAPI.Messages;
using EventStore.ClientAPI.SystemData;
using EventStore.ClientAPI.Transport.Tcp;

namespace EventStore.ClientAPI.ClientOperations
{
  #region == class SubscriptionOperation ==

  internal sealed class SubscriptionOperation : VolatileSubscriptionOperationBase<ResolvedEvent<object>>
  {
    public SubscriptionOperation(TaskCompletionSource<EventStoreSubscription> source,
      string streamId, SubscriptionSettings settings, UserCredentials userCredentials,
      Action<EventStoreSubscription, ResolvedEvent<object>> eventAppeared,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped,
      Func<TcpPackageConnection> getConnection)
      : base(source, streamId, settings, userCredentials, eventAppeared, subscriptionDropped, getConnection)
    {
    }
    public SubscriptionOperation(TaskCompletionSource<EventStoreSubscription> source,
      string streamId, SubscriptionSettings settings, UserCredentials userCredentials,
      Func<EventStoreSubscription, ResolvedEvent<object>, Task> eventAppearedAsync,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped,
      Func<TcpPackageConnection> getConnection)
      : base(source, streamId, settings, userCredentials, eventAppearedAsync, subscriptionDropped, getConnection)
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

  #region == class SubscriptionOperation2 ==

  internal sealed class SubscriptionOperation2 : VolatileSubscriptionOperationBase<IResolvedEvent2>
  {
    public SubscriptionOperation2(TaskCompletionSource<EventStoreSubscription> source,
      string streamId, SubscriptionSettings settings, UserCredentials userCredentials,
      Action<EventStoreSubscription, IResolvedEvent2> eventAppeared,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped,
      Func<TcpPackageConnection> getConnection)
      : base(source, streamId, settings, userCredentials, eventAppeared, subscriptionDropped, getConnection)
    {
    }
    public SubscriptionOperation2(TaskCompletionSource<EventStoreSubscription> source,
      string streamId, SubscriptionSettings settings, UserCredentials userCredentials,
      Func<EventStoreSubscription, IResolvedEvent2, Task> eventAppearedAsync,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped,
      Func<TcpPackageConnection> getConnection)
      : base(source, streamId, settings, userCredentials, eventAppearedAsync, subscriptionDropped, getConnection)
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

  #region == class SubscriptionOperation<TEvent> ==

  internal interface IVolatileSubscriptionOperationWrapper
  {
    ISubscriptionOperation Create(StartSubscriptionMessageWrapper msgWrapper, TcpPackageConnection connection);
  }
  internal sealed class SubscriptionOperationWrapper<TEvent> : IVolatileSubscriptionOperationWrapper
    where TEvent : class
  {
    public SubscriptionOperationWrapper() { }

    public ISubscriptionOperation Create(StartSubscriptionMessageWrapper msgWrapper, TcpPackageConnection connection)
    {
      var msg = (StartSubscriptionMessage<TEvent>)msgWrapper.Message;

      return msg.EventAppeared != null
           ? new SubscriptionOperation<TEvent>(msg.Source, msg.StreamId, msg.Settings, msg.UserCredentials,
                                               msg.EventAppeared, msg.SubscriptionDropped, () => connection)
           : new SubscriptionOperation<TEvent>(msg.Source, msg.StreamId, msg.Settings, msg.UserCredentials,
                                               msg.EventAppearedAsync, msg.SubscriptionDropped, () => connection);
    }
  }
  internal sealed class SubscriptionOperation<TEvent> : VolatileSubscriptionOperationBase<ResolvedEvent<TEvent>>
    where TEvent : class
  {
    public SubscriptionOperation(TaskCompletionSource<EventStoreSubscription> source,
      string streamId, SubscriptionSettings settings, UserCredentials userCredentials,
      Action<EventStoreSubscription, ResolvedEvent<TEvent>> eventAppeared,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped,
      Func<TcpPackageConnection> getConnection)
      : base(source, streamId, settings, userCredentials, eventAppeared, subscriptionDropped, getConnection)
    {
    }
    public SubscriptionOperation(TaskCompletionSource<EventStoreSubscription> source,
      string streamId, SubscriptionSettings settings, UserCredentials userCredentials,
      Func<EventStoreSubscription, ResolvedEvent<TEvent>, Task> eventAppearedAsync,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped,
      Func<TcpPackageConnection> getConnection)
      : base(source, streamId, settings, userCredentials, eventAppearedAsync, subscriptionDropped, getConnection)
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

  #region == class VolatileSubscriptionOperation ==

  internal sealed class VolatileSubscriptionOperation : VolatileSubscriptionOperationBase<ResolvedEvent>
  {
    public VolatileSubscriptionOperation(TaskCompletionSource<EventStoreSubscription> source,
      string streamId, SubscriptionSettings settings, UserCredentials userCredentials,
      Action<EventStoreSubscription, ResolvedEvent> eventAppeared,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped,
      Func<TcpPackageConnection> getConnection)
      : base(source, streamId, settings, userCredentials, eventAppeared, subscriptionDropped, getConnection)
    {
    }
    public VolatileSubscriptionOperation(TaskCompletionSource<EventStoreSubscription> source,
      string streamId, SubscriptionSettings settings, UserCredentials userCredentials,
      Func<EventStoreSubscription, ResolvedEvent, Task> eventAppearedAsync,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped,
      Func<TcpPackageConnection> getConnection)
      : base(source, streamId, settings, userCredentials, eventAppearedAsync, subscriptionDropped, getConnection)
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

  #region == class VolatileSubscriptionOperationBase<TResolvedEvent> ==

  internal abstract class VolatileSubscriptionOperationBase<TResolvedEvent> : SubscriptionOperation<EventStoreSubscription, TResolvedEvent>, IVolatileSubscriptionOperation
    where TResolvedEvent : IResolvedEvent
  {
    public VolatileSubscriptionOperationBase(TaskCompletionSource<EventStoreSubscription> source,
      string streamId, SubscriptionSettings settings, UserCredentials userCredentials,
      Action<EventStoreSubscription, TResolvedEvent> eventAppeared,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped,
      Func<TcpPackageConnection> getConnection)
      : base(source, streamId, settings, userCredentials, eventAppeared, subscriptionDropped, getConnection)
    {
    }
    public VolatileSubscriptionOperationBase(TaskCompletionSource<EventStoreSubscription> source,
      string streamId, SubscriptionSettings settings, UserCredentials userCredentials,
      Func<EventStoreSubscription, TResolvedEvent, Task> eventAppearedAsync,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped,
      Func<TcpPackageConnection> getConnection)
      : base(source, streamId, settings, userCredentials, eventAppearedAsync, subscriptionDropped, getConnection)
    {
    }

    protected override TcpPackage CreateSubscriptionPackage()
    {
      var dto = new ClientMessage.SubscribeToStream(_streamId, _resolveLinkTos);
      return new TcpPackage(
          TcpCommand.SubscribeToStream, _userCredentials != null ? TcpFlags.Authenticated : TcpFlags.None,
          _correlationId, _userCredentials?.Username, _userCredentials?.Password, dto.Serialize());
    }

    protected override bool InspectPackage(TcpPackage package, out InspectionResult result)
    {
      if (package.Command == TcpCommand.SubscriptionConfirmation)
      {
        var dto = package.Data.Deserialize<ClientMessage.SubscriptionConfirmation>();
        ConfirmSubscription(dto.LastCommitPosition, dto.LastEventNumber);
        result = new InspectionResult(InspectionDecision.Subscribed, "SubscriptionConfirmation");
        return true;
      }
      if (package.Command == TcpCommand.StreamEventAppeared)
      {
        var dto = package.Data.Deserialize<ClientMessage.StreamEventAppeared>();
        EventAppeared(TransformEvent(dto.Event));
        result = new InspectionResult(InspectionDecision.DoNothing, "StreamEventAppeared");
        return true;
      }
      result = null;
      return false;
    }

    protected override EventStoreSubscription CreateSubscriptionObject(long lastCommitPosition, long? lastEventNumber)
    {
      return new VolatileEventStoreSubscription(this, _streamId, lastCommitPosition, lastEventNumber);
    }
  }

  #endregion

  #region == interface IVolatileSubscriptionOperation ==

  internal interface IVolatileSubscriptionOperation
  {
    void Unsubscribe();
  }

  #endregion
}