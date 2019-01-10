using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using EventStore.ClientAPI.Internal;
using EventStore.ClientAPI.Messages;
using EventStore.ClientAPI.SystemData;
using EventStore.ClientAPI.Transport.Tcp;
using EventStore.Transport.Tcp.Messages;

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override ResolvedEvent<object> TransformEvent(TcpClientMessageDto.ResolvedEvent rawEvent)
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override IResolvedEvent2 TransformEvent(TcpClientMessageDto.ResolvedEvent rawEvent)
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override ResolvedEvent<TEvent> TransformEvent(TcpClientMessageDto.ResolvedEvent rawEvent)
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override ResolvedEvent TransformEvent(TcpClientMessageDto.ResolvedEvent rawEvent)
    {
      return rawEvent.ToRawResolvedEvent();
    }
  }

  #endregion

  #region == class VolatileSubscriptionOperationBase<TResolvedEvent> ==

  internal abstract class VolatileSubscriptionOperationBase<TResolvedEvent> : SubscriptionOperation<EventStoreSubscription, TResolvedEvent>, IVolatileSubscriptionOperation
    where TResolvedEvent : IResolvedEvent
  {
    private long _processingEventNumber;
    public long ProcessingEventNumber => _processingEventNumber;

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected abstract TResolvedEvent TransformEvent(TcpClientMessageDto.ResolvedEvent rawEvent);

    protected override TcpPackage CreateSubscriptionPackage()
    {
      var dto = new TcpClientMessageDto.SubscribeToStream(_streamId, _resolveLinkTos);
      return new TcpPackage(
          TcpCommand.SubscribeToStream, _userCredentials != null ? TcpFlags.Authenticated : TcpFlags.None,
          _correlationId, _userCredentials?.Username, _userCredentials?.Password, dto.Serialize());
    }

    protected override async Task<InspectionResult> TryInspectPackageAsync(TcpPackage package)
    {
      if (package.Command == TcpCommand.SubscriptionConfirmation)
      {
        var dto = package.Data.Deserialize<TcpClientMessageDto.SubscriptionConfirmation>();
        ConfirmSubscription(dto.LastCommitPosition, dto.LastEventNumber);
        return new InspectionResult(InspectionDecision.Subscribed, "SubscriptionConfirmation");
      }
      if (package.Command == TcpCommand.StreamEventAppeared)
      {
        var dto = package.Data.Deserialize<TcpClientMessageDto.StreamEventAppeared>();
        await EventAppearedAsync(TransformEvent(dto.Event)).ConfigureAwait(false);
        return new InspectionResult(InspectionDecision.DoNothing, "StreamEventAppeared");
      }
      return null;
    }

    protected override EventStoreSubscription CreateSubscriptionObject(long lastCommitPosition, long? lastEventNumber)
    {
      return new VolatileEventStoreSubscription(this, _streamId, lastCommitPosition, lastEventNumber);
    }

    protected override void ProcessResolvedEvent(in TResolvedEvent resolvedEvent)
    {
      Interlocked.Exchange(ref _processingEventNumber, resolvedEvent.OriginalEventNumber);
      _eventAppeared(_subscription, resolvedEvent);
    }

    protected override Task ProcessResolvedEventAsync(in TResolvedEvent resolvedEvent)
    {
      Interlocked.Exchange(ref _processingEventNumber, resolvedEvent.OriginalEventNumber);
      return _eventAppearedAsync(_subscription, resolvedEvent);
    }
  }

  #endregion

  #region == interface IVolatileSubscriptionOperation ==

  internal interface IVolatileSubscriptionOperation
  {
    long ProcessingEventNumber { get; }

    void Unsubscribe();
  }

  #endregion
}