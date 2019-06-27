using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using EventStore.ClientAPI.Exceptions;
using EventStore.ClientAPI.Internal;
using EventStore.ClientAPI.Messages;
using EventStore.ClientAPI.SystemData;
using EventStore.ClientAPI.Transport.Tcp;
using EventStore.Transport.Tcp.Messages;

namespace EventStore.ClientAPI.ClientOperations
{
    #region == class PersistentSubscriptionOperation ==

    internal sealed class PersistentSubscriptionOperation : ConnectToPersistentSubscriptionOperationBase<PersistentSubscriptionResolvedEvent<object>>
    {
        private readonly IEventAdapter _eventAdapter;

        public PersistentSubscriptionOperation(TaskCompletionSource<PersistentEventStoreSubscription> source,
          string groupName, string streamId, ConnectToPersistentSubscriptionSettings settings, UserCredentials userCredentials,
          Func<PersistentEventStoreSubscription, PersistentSubscriptionResolvedEvent<object>, Task> eventAppearedAsync,
          Action<PersistentEventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped,
          IHasTcpPackageConnection getConnection, IEventAdapter eventAdapter)
          : base(source, groupName, streamId, settings, userCredentials, eventAppearedAsync, subscriptionDropped, getConnection)
        {
            if (null == eventAdapter) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.eventAdapter); }
            _eventAdapter = eventAdapter;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override PersistentSubscriptionResolvedEvent<object> TransformEvent(TcpClientMessageDto.ResolvedIndexedEvent rawEvent, int? retryCount)
        {
            return new PersistentSubscriptionResolvedEvent<object>(rawEvent.ToResolvedEvent(_eventAdapter), retryCount);
        }
    }

    #endregion

    #region == class PersistentSubscriptionOperation ==

    internal sealed class PersistentSubscriptionOperation2 : ConnectToPersistentSubscriptionOperationBase<IPersistentSubscriptionResolvedEvent2>
    {
        private readonly IEventAdapter _eventAdapter;

        public PersistentSubscriptionOperation2(TaskCompletionSource<PersistentEventStoreSubscription> source,
          string groupName, string streamId, ConnectToPersistentSubscriptionSettings settings, UserCredentials userCredentials,
          Func<PersistentEventStoreSubscription, IPersistentSubscriptionResolvedEvent2, Task> eventAppearedAsync,
          Action<PersistentEventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped,
          IHasTcpPackageConnection getConnection, IEventAdapter eventAdapter)
          : base(source, groupName, streamId, settings, userCredentials, eventAppearedAsync, subscriptionDropped, getConnection)
        {
            if (null == eventAdapter) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.eventAdapter); }
            _eventAdapter = eventAdapter;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override IPersistentSubscriptionResolvedEvent2 TransformEvent(TcpClientMessageDto.ResolvedIndexedEvent rawEvent, int? retryCount)
        {
            return rawEvent.ToPersistentSubscriptionResolvedEvent2(retryCount, _eventAdapter);
        }
    }

    #endregion

    #region == class PersistentSubscriptionOperation<TEvent> ==

    internal interface IPersistentSubscriptionOperationWrapper
    {
        ISubscriptionOperation Create(StartPersistentSubscriptionMessageWrapper msgWrapper, IHasTcpPackageConnection connection, IEventAdapter eventAdapter);
    }
    internal sealed class PersistentSubscriptionOperationWrapper<TEvent> : IPersistentSubscriptionOperationWrapper
    {
        public PersistentSubscriptionOperationWrapper() { }

        public ISubscriptionOperation Create(StartPersistentSubscriptionMessageWrapper msgWrapper, IHasTcpPackageConnection connection, IEventAdapter eventAdapter)
        {
            var msg = (StartPersistentSubscriptionMessage<TEvent>)msgWrapper.Message;

            return new PersistentSubscriptionOperation<TEvent>(msg.Source, msg.SubscriptionId, msg.StreamId, msg.Settings,
                msg.UserCredentials, msg.EventAppearedAsync, msg.SubscriptionDropped, connection, eventAdapter);
        }
    }

    internal sealed class PersistentSubscriptionOperation<TEvent> : ConnectToPersistentSubscriptionOperationBase<PersistentSubscriptionResolvedEvent<TEvent>>
    {
        private readonly IEventAdapter _eventAdapter;

        public PersistentSubscriptionOperation(TaskCompletionSource<PersistentEventStoreSubscription> source,
          string groupName, string streamId, ConnectToPersistentSubscriptionSettings settings, UserCredentials userCredentials,
          Func<PersistentEventStoreSubscription, PersistentSubscriptionResolvedEvent<TEvent>, Task> eventAppearedAsync,
          Action<PersistentEventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped,
          IHasTcpPackageConnection getConnection, IEventAdapter eventAdapter)
          : base(source, groupName, streamId, settings, userCredentials, eventAppearedAsync, subscriptionDropped, getConnection)
        {
            if (null == eventAdapter) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.eventAdapter); }
            _eventAdapter = eventAdapter;
        }

        protected override PersistentSubscriptionResolvedEvent<TEvent> TransformEvent(TcpClientMessageDto.ResolvedIndexedEvent rawEvent, int? retryCount)
        {
            return new PersistentSubscriptionResolvedEvent<TEvent>(rawEvent.ToResolvedEvent<TEvent>(_eventAdapter), retryCount);
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
          IHasTcpPackageConnection getConnection)
          : base(source, groupName, streamId, settings, userCredentials, eventAppearedAsync, subscriptionDropped, getConnection)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override PersistentSubscriptionResolvedEvent TransformEvent(TcpClientMessageDto.ResolvedIndexedEvent rawEvent, int? retryCount)
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
        //  IHasTcpPackageConnection getConnection)
        //  : base(source, streamId, new SubscriptionSettings { ResolveLinkTos = false }, userCredentials, eventAppeared, subscriptionDropped, getConnection)
        //{
        //  _groupName = groupName;
        //  _bufferSize = settings.BufferSize;
        //}
        public ConnectToPersistentSubscriptionOperationBase(TaskCompletionSource<PersistentEventStoreSubscription> source,
          string groupName, string streamId, ConnectToPersistentSubscriptionSettings settings, UserCredentials userCredentials,
          Func<PersistentEventStoreSubscription, TResolvedEvent, Task> eventAppearedAsync,
          Action<PersistentEventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped,
          IHasTcpPackageConnection getConnection)
          : base(source, streamId,
              //new SubscriptionSettings { ResolveLinkTos = false, TaskScheduler = settings.TaskScheduler, CancellationToken = settings.CancellationToken }, 
              SubscriptionSettings.Default,
              userCredentials, eventAppearedAsync, subscriptionDropped, getConnection)
        {
            _groupName = groupName;
            _bufferSize = settings.BufferSize;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected abstract TResolvedEvent TransformEvent(TcpClientMessageDto.ResolvedIndexedEvent rawEvent, int? retryCount);

        protected override TcpPackage CreateSubscriptionPackage()
        {
            var dto = new TcpClientMessageDto.ConnectToPersistentSubscription(_groupName, _streamId, _bufferSize);
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
                var dto = package.Data.Deserialize<TcpClientMessageDto.PersistentSubscriptionConfirmation>();
                ConfirmSubscription(dto.LastCommitPosition, dto.LastEventNumber);
                _subscriptionId = dto.SubscriptionId;
                return new InspectionResult(InspectionDecision.Subscribed, "SubscriptionConfirmation");
            }
            if (package.Command == TcpCommand.PersistentSubscriptionStreamEventAppeared)
            {
                var dto = package.Data.Deserialize<TcpClientMessageDto.PersistentSubscriptionStreamEventAppeared>();
                await EventAppearedAsync(TransformEvent(dto.Event, dto.RetryCount)).ConfigureAwait(false);
                return new InspectionResult(InspectionDecision.DoNothing, "StreamEventAppeared");
            }
            if (package.Command == TcpCommand.SubscriptionDropped)
            {
                var dto = package.Data.Deserialize<TcpClientMessageDto.SubscriptionDropped>();
                if (dto.Reason == TcpClientMessageDto.SubscriptionDropped.SubscriptionDropReason.AccessDenied)
                {
                    DropSubscription(SubscriptionDropReason.AccessDenied, CoreThrowHelper.GetAccessDeniedException(ExceptionResource.YouDoNotHaveAccessToTheStream));
                    return new InspectionResult(InspectionDecision.EndOperation, "SubscriptionDropped");
                }
                if (dto.Reason == TcpClientMessageDto.SubscriptionDropped.SubscriptionDropReason.NotFound)
                {
                    DropSubscription(SubscriptionDropReason.NotFound, ThrowHelper.GetArgumentException(ExceptionResource.SubscriptionNotFound));
                    return new InspectionResult(InspectionDecision.EndOperation, "SubscriptionDropped");
                }
                if (dto.Reason == TcpClientMessageDto.SubscriptionDropped.SubscriptionDropReason.PersistentSubscriptionDeleted)
                {
                    DropSubscription(SubscriptionDropReason.PersistentSubscriptionDeleted, new PersistentSubscriptionDeletedException());
                    return new InspectionResult(InspectionDecision.EndOperation, "SubscriptionDropped");
                }
                if (dto.Reason == TcpClientMessageDto.SubscriptionDropped.SubscriptionDropReason.SubscriberMaxCountReached)
                {
                    DropSubscription(SubscriptionDropReason.MaxSubscribersReached, new MaximumSubscribersReachedException());
                    return new InspectionResult(InspectionDecision.EndOperation, "SubscriptionDropped");
                }
                DropSubscription((SubscriptionDropReason)dto.Reason, null, _getConnection.Connection);
                return new InspectionResult(InspectionDecision.EndOperation, "SubscriptionDropped");
            }
            return null;
        }

        protected override PersistentEventStoreSubscription CreateSubscriptionObject(long lastCommitPosition, long? lastEventNumber)
        {
            return new PersistentEventStoreSubscription(this, _streamId, lastCommitPosition, lastEventNumber);
        }

        public void NotifyEventsProcessed(Guid processedEventId)
        {
            var dto = new TcpClientMessageDto.PersistentSubscriptionAckEvents(
                _subscriptionId,
                new[] { processedEventId });

            NotifyEventsProcessed(dto);
        }

        public void NotifyEventsProcessed(Guid[] processedEvents)
        {
            if (null == processedEvents) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.processedEvents); }

            var count = processedEvents.Length;
            var rawIds = new Guid[count];
            for (var idx = 0; idx < count; idx++)
            {
                rawIds[idx] = processedEvents[idx];
            }

            var dto = new TcpClientMessageDto.PersistentSubscriptionAckEvents(_subscriptionId, rawIds);
            NotifyEventsProcessed(dto);
        }

        private void NotifyEventsProcessed(TcpClientMessageDto.PersistentSubscriptionAckEvents dto)
        {
            var package = new TcpPackage(TcpCommand.PersistentSubscriptionAckEvents,
                                  _userCredentials != null ? TcpFlags.Authenticated : TcpFlags.None,
                                  _correlationId,
                                  _userCredentials?.Username,
                                  _userCredentials?.Password,
                                  dto.Serialize());
            EnqueueSend(package);
        }

        public void NotifyEventsFailed(Guid processedEvent, PersistentSubscriptionNakEventAction action, string reason)
        {
            if (null == reason) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.reason); }

            var dto = new TcpClientMessageDto.PersistentSubscriptionNakEvents(
                _subscriptionId,
                new[] { processedEvent },
                reason,
                (TcpClientMessageDto.PersistentSubscriptionNakEvents.NakAction)action);
            NotifyEventsFailed(dto);
        }

        public void NotifyEventsFailed(Guid[] processedEvents, PersistentSubscriptionNakEventAction action, string reason)
        {
            if (null == processedEvents) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.processedEvents); }
            if (null == reason) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.reason); }

            var count = processedEvents.Length;
            var rawIds = new Guid[count];
            for (var idx = 0; idx < count; idx++)
            {
                rawIds[idx] = processedEvents[idx];
            }

            var dto = new TcpClientMessageDto.PersistentSubscriptionNakEvents(
                _subscriptionId,
                rawIds,
                reason,
                (TcpClientMessageDto.PersistentSubscriptionNakEvents.NakAction)action);
            NotifyEventsFailed(dto);
        }

        private void NotifyEventsFailed(TcpClientMessageDto.PersistentSubscriptionNakEvents dto)
        {
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