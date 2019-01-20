using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using CuteAnt.AsyncEx;
using EventStore.ClientAPI.Common.Utils;
using EventStore.ClientAPI.Exceptions;
using EventStore.ClientAPI.Internal;
using EventStore.ClientAPI.SystemData;
using EventStore.ClientAPI.Transport.Tcp;
using EventStore.Transport.Tcp.Messages;
using Microsoft.Extensions.Logging;

namespace EventStore.ClientAPI.ClientOperations
{
    internal abstract class SubscriptionOperation<TSubscription, TResolvedEvent> : ISubscriptionOperation
        where TSubscription : EventStoreSubscription
        where TResolvedEvent : IResolvedEvent
    {
        private static readonly ILogger _log = TraceLogger.GetLogger("EventStore.ClientAPI.SubscriptionOperation");

        private readonly TaskCompletionSource<TSubscription> _source;
        protected readonly string _streamId;
        protected readonly bool _resolveLinkTos;
        protected readonly SubscriptionSettings _subscriptionSettings;
        protected readonly UserCredentials _userCredentials;
        protected readonly Action<TSubscription, TResolvedEvent> _eventAppeared;
        protected readonly Func<TSubscription, TResolvedEvent, Task> _eventAppearedAsync;
        private readonly Action<TSubscription, SubscriptionDropReason, Exception> _subscriptionDropped;
        private readonly bool _verboseLogging;
        protected readonly Func<TcpPackageConnection> _getConnection;
        private readonly int _maxQueueSize = 2000;
        private readonly BufferBlock<ResolvedEventWrapper> _bufferBlock;
        private readonly List<ActionBlock<ResolvedEventWrapper>> _actionBlocks;
        private readonly ITargetBlock<ResolvedEventWrapper> _targetBlock;
        private readonly IDisposable _links;
        protected TSubscription _subscription;
        private int _unsubscribed;
        protected Guid _correlationId;

        /// <summary>Gets the number of items waiting to be processed by this subscription.</summary>
        internal Int32 InputCount { get { return null == _bufferBlock ? _actionBlocks[0].InputCount : _bufferBlock.Count; } }

        protected SubscriptionOperation(
            TaskCompletionSource<TSubscription> source,
            string streamId,
            SubscriptionSettings settings,
            UserCredentials userCredentials,
            Action<TSubscription, TResolvedEvent> eventAppeared,
            Action<TSubscription, SubscriptionDropReason, Exception> subscriptionDropped,
            Func<TcpPackageConnection> getConnection)
            : this(source, streamId, settings.ResolveLinkTos, userCredentials, subscriptionDropped, settings.VerboseLogging, getConnection)
        {
            if (null == eventAppeared) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.eventAppeared); }
            _eventAppeared = eventAppeared;

            var numActionBlocks = settings.NumActionBlocks;
            if (SubscriptionSettings.Unbounded == settings.BoundedCapacityPerBlock)
            {
                // 如果没有设定 ActionBlock 的容量，设置多个 ActionBlock 没有意义
                numActionBlocks = 1;
            }
            _actionBlocks = new List<ActionBlock<ResolvedEventWrapper>>(numActionBlocks);
            for (var idx = 0; idx < numActionBlocks; idx++)
            {
                _actionBlocks.Add(new ActionBlock<ResolvedEventWrapper>(
                    e => ProcessItem(e),
                    settings.ToExecutionDataflowBlockOptions(true)));
            }
            if (numActionBlocks > 1)
            {
                var links = new CompositeDisposable();
                _bufferBlock = new BufferBlock<ResolvedEventWrapper>(settings.ToBufferBlockOptions());
                for (var idx = 0; idx < numActionBlocks; idx++)
                {
                    var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };
                    links.Add(_bufferBlock.LinkTo(_actionBlocks[idx], linkOptions));
                }
                _links = links;
                _targetBlock = _bufferBlock;
            }
            else
            {
                _targetBlock = _actionBlocks[0];
            }
        }

        protected SubscriptionOperation(
            TaskCompletionSource<TSubscription> source,
            string streamId,
            SubscriptionSettings settings,
            UserCredentials userCredentials,
            Func<TSubscription, TResolvedEvent, Task> eventAppearedAsync,
            Action<TSubscription, SubscriptionDropReason, Exception> subscriptionDropped,
            Func<TcpPackageConnection> getConnection)
            : this(source, streamId, settings.ResolveLinkTos, userCredentials, subscriptionDropped, settings.VerboseLogging, getConnection)
        {
            if (null == eventAppearedAsync) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.eventAppearedAsync); }
            _eventAppearedAsync = eventAppearedAsync;

            var numActionBlocks = settings.NumActionBlocks;
            if (SubscriptionSettings.Unbounded == settings.BoundedCapacityPerBlock)
            {
                // 如果没有设定 ActionBlock 的容量，设置多个 ActionBlock 没有意义
                numActionBlocks = 1;
            }
            _actionBlocks = new List<ActionBlock<ResolvedEventWrapper>>(numActionBlocks);
            for (var idx = 0; idx < numActionBlocks; idx++)
            {
                _actionBlocks.Add(new ActionBlock<ResolvedEventWrapper>(
                  e => ProcessItemAsync(e),
                  settings.ToExecutionDataflowBlockOptions()));
            }
            if (numActionBlocks > 1)
            {
                var links = new CompositeDisposable();
                _bufferBlock = new BufferBlock<ResolvedEventWrapper>(settings.ToBufferBlockOptions());
                for (var idx = 0; idx < numActionBlocks; idx++)
                {
                    var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };
                    links.Add(_bufferBlock.LinkTo(_actionBlocks[idx], linkOptions));
                }
                _links = links;
                _targetBlock = _bufferBlock;
            }
            else
            {
                _targetBlock = _actionBlocks[0];
            }
        }

        private SubscriptionOperation(
            TaskCompletionSource<TSubscription> source,
            string streamId,
            bool resolveLinkTos,
            UserCredentials userCredentials,
            Action<TSubscription, SubscriptionDropReason, Exception> subscriptionDropped,
            bool verboseLogging,
            Func<TcpPackageConnection> getConnection)
        {
            if (null == source) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source); }
            if (null == getConnection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.getConnection); }

            _source = source;
            _streamId = string.IsNullOrEmpty(streamId) ? string.Empty : streamId;
            _resolveLinkTos = resolveLinkTos;
            _userCredentials = userCredentials;
            _subscriptionDropped = subscriptionDropped ?? ((x, y, z) => { });
            _verboseLogging = verboseLogging && _log.IsDebugLevelEnabled();
            _getConnection = getConnection;
        }

        protected void EnqueueSend(TcpPackage package)
        {
            _getConnection().EnqueueSend(package);
        }

        public bool Subscribe(Guid correlationId, TcpPackageConnection connection)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }

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
            return new TcpPackage(TcpCommand.UnsubscribeFromStream, _correlationId, new TcpClientMessageDto.UnsubscribeFromStream().Serialize());
        }

        protected abstract Task<InspectionResult> TryInspectPackageAsync(TcpPackage package);

        public async Task<InspectionResult> InspectPackageAsync(TcpPackage package)
        {
            try
            {
                var result = await TryInspectPackageAsync(package).ConfigureAwait(false);
                if (result != null) { return result; }

                switch (package.Command)
                {
                    case TcpCommand.SubscriptionDropped:
                        return HandleSubscriptionDroppedCommand(package);

                    case TcpCommand.NotAuthenticated:
                        return HandleNotAuthenticatedCommand(package);

                    case TcpCommand.BadRequest:
                        return HandleBadRequestCommand(package);

                    case TcpCommand.NotHandled:
                        return HandleNotHandledCommand(package);

                    default:
                        return HandleOtherCommand(package);
                }
            }
            catch (Exception e) { return HandleInspectPackageError(e); }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private InspectionResult HandleInspectPackageError(Exception e)
        {
            DropSubscription(SubscriptionDropReason.Unknown, e);
            return new InspectionResult(InspectionDecision.EndOperation, $"Exception - {e.Message}");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private InspectionResult HandleSubscriptionDroppedCommand(TcpPackage package)
        {
            var dto = package.Data.Deserialize<TcpClientMessageDto.SubscriptionDropped>();
            switch (dto.Reason)
            {
                case TcpClientMessageDto.SubscriptionDropped.SubscriptionDropReason.Unsubscribed:
                    DropSubscription(SubscriptionDropReason.UserInitiated, null);
                    break;
                case TcpClientMessageDto.SubscriptionDropped.SubscriptionDropReason.AccessDenied:
                    DropSubscription(SubscriptionDropReason.AccessDenied,
                                     CoreThrowHelper.GetAccessDeniedException_All(_streamId));
                    break;
                case TcpClientMessageDto.SubscriptionDropped.SubscriptionDropReason.NotFound:
                    DropSubscription(SubscriptionDropReason.NotFound,
                                     CoreThrowHelper.GetArgumentException_All(_streamId));
                    break;
                default:
                    if (_verboseLogging) _log.SubscriptionDroppedByServerReason(dto.Reason);
                    DropSubscription(SubscriptionDropReason.Unknown,
                                     CoreThrowHelper.GetCommandNotExpectedException(dto.Reason));
                    break;
            }
            return new InspectionResult(InspectionDecision.EndOperation, $"SubscriptionDropped: {dto.Reason}");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private InspectionResult HandleNotAuthenticatedCommand(TcpPackage package)
        {
            DropSubscription(SubscriptionDropReason.NotAuthenticated,
                             CoreThrowHelper.GetNotAuthenticatedException(package));
            return new InspectionResult(InspectionDecision.EndOperation, "NotAuthenticated");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private InspectionResult HandleBadRequestCommand(TcpPackage package)
        {
            string message = Helper.EatException(() => Helper.UTF8NoBom.GetString(package.Data));
            DropSubscription(SubscriptionDropReason.ServerError,
                             new ServerErrorException(string.IsNullOrEmpty(message) ? "<no message>" : message));
            return new InspectionResult(InspectionDecision.EndOperation, $"BadRequest: {message}");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private InspectionResult HandleNotHandledCommand(TcpPackage package)
        {
            if (_subscription != null) { CoreThrowHelper.ThrowException_NotHandledCommandAppeared(); }

            var message = package.Data.Deserialize<TcpClientMessageDto.NotHandled>();
            switch (message.Reason)
            {
                case TcpClientMessageDto.NotHandled.NotHandledReason.NotReady:
                    return new InspectionResult(InspectionDecision.Retry, "NotHandled - NotReady");

                case TcpClientMessageDto.NotHandled.NotHandledReason.TooBusy:
                    return new InspectionResult(InspectionDecision.Retry, "NotHandled - TooBusy");

                case TcpClientMessageDto.NotHandled.NotHandledReason.NotMaster:
                    var masterInfo = message.AdditionalInfo.Deserialize<TcpClientMessageDto.NotHandled.MasterInfo>();
                    return new InspectionResult(InspectionDecision.Reconnect, "NotHandled - NotMaster",
                                                masterInfo.ExternalTcpEndPoint, masterInfo.ExternalSecureTcpEndPoint);

                default:
                    _log.UnknownNotHandledReason(message.Reason);
                    return new InspectionResult(InspectionDecision.Retry, "NotHandled - <unknown>");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private InspectionResult HandleOtherCommand(TcpPackage package)
        {
            DropSubscription(SubscriptionDropReason.ServerError,
                             CoreThrowHelper.GetCommandNotExpectedException(package.Command));
            return new InspectionResult(InspectionDecision.EndOperation, package.Command.ToString());
        }

        public void ConnectionClosed()
        {
            DropSubscription(SubscriptionDropReason.ConnectionClosed, CoreThrowHelper.GetConnectionClosedException());
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
                if (_verboseLogging) { _log.ClosingSubscription(_correlationId, _streamId, reason, exc); }

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
                    EnqueueMessage(new ResolvedEventWrapper(reason, exc));
                }
            }
        }

        protected void ConfirmSubscription(long lastCommitPosition, long? lastEventNumber)
        {
            if (lastCommitPosition < -1)
            {
                CoreThrowHelper.ThrowArgumentOutOfRangeException_InvalidLastCommitPositionOnSubscriptionConfirmation(lastCommitPosition);
            }
            if (_subscription != null) { CoreThrowHelper.ThrowException_DoubleConfirmationOfSubscription(); }

            if (_verboseLogging)
            {
                _log.SubscribedAtCommitPosition(_correlationId, _streamId, lastCommitPosition, lastEventNumber);
            }
            _subscription = CreateSubscriptionObject(lastCommitPosition, lastEventNumber);
            _source.SetResult(_subscription);
        }

        protected abstract TSubscription CreateSubscriptionObject(long lastCommitPosition, long? lastEventNumber);

        protected void EventAppeared(in TResolvedEvent e)
        {
            if (_unsubscribed != 0) { return; }

            if (_subscription == null) { CoreThrowHelper.ThrowException_SubscriptionNotConfirmedButEventAppeared(); }

            if (_verboseLogging) { _log.SubscribedEventAppeared(_correlationId, _streamId, e); }
            EnqueueMessage(new ResolvedEventWrapper(e));
        }

        protected Task EventAppearedAsync(in TResolvedEvent e)
        {
            if (_unsubscribed != 0) { return TaskConstants.Completed; }

            if (_subscription == null) { CoreThrowHelper.ThrowException_SubscriptionNotConfirmedButEventAppeared(); }

            if (_verboseLogging) { _log.SubscribedEventAppeared(_correlationId, _streamId, e); }
            return EnqueueMessageAsync(new ResolvedEventWrapper(e));
        }

        private void EnqueueMessage(ResolvedEventWrapper item)
        {
            _targetBlock.Post(item);
            //AsyncContext.Run(s_sendToQueueFunc, _targetBlock, item);
            if (InputCount > _maxQueueSize)
            {
                DropSubscription(SubscriptionDropReason.ProcessingQueueOverflow, new Exception("client buffer too big"));
            }
        }

        private static readonly Func<ITargetBlock<ResolvedEventWrapper>, ResolvedEventWrapper, Task<bool>> s_sendToQueueFunc = SendToQueueAsync;
        private static async Task<bool> SendToQueueAsync(
            ITargetBlock<ResolvedEventWrapper> targetBlock,
            ResolvedEventWrapper message)
        {
            return await targetBlock.SendAsync(message).ConfigureAwait(false);
        }

        private async Task EnqueueMessageAsync(ResolvedEventWrapper item)
        {
            await _targetBlock.SendAsync(item).ConfigureAwait(false);
            if (InputCount > _maxQueueSize)
            {
                DropSubscription(SubscriptionDropReason.ProcessingQueueOverflow, new Exception("client buffer too big"));
            }
        }

        protected virtual void ProcessResolvedEvent(in TResolvedEvent resolvedEvent)
        {
            _eventAppeared(_subscription, resolvedEvent);
        }

        private void ProcessItem(ResolvedEventWrapper item)
        {
            try
            {
                if (item.IsResolvedEvent)
                {
                    ProcessResolvedEvent(item.ResolvedEvent);
                }
                else
                {
                    if (_bufferBlock != null)
                    {
                        _bufferBlock.Complete();
                        _links?.Dispose();
                    }
                    else if (_actionBlocks != null && _actionBlocks.Count > 0)
                    {
                        _actionBlocks[0]?.Complete();
                    }
                    _subscriptionDropped(_subscription, item.DropReason, item.Error);
                }
            }
            catch (Exception exc)
            {
                _log.ExceptionDuringExecutingUserCallback(exc);
            }
        }

        protected virtual Task ProcessResolvedEventAsync(in TResolvedEvent resolvedEvent)
        {
            return _eventAppearedAsync(_subscription, resolvedEvent);
        }

        private async Task ProcessItemAsync(ResolvedEventWrapper item)
        {
            try
            {
                if (item.IsResolvedEvent)
                {
                    await ProcessResolvedEventAsync(item.ResolvedEvent).ConfigureAwait(false);
                }
                else
                {
                    if (_bufferBlock != null)
                    {
                        _bufferBlock.Complete();
                        _links?.Dispose();
                    }
                    else if (_actionBlocks != null && _actionBlocks.Count > 0)
                    {
                        _actionBlocks[0]?.Complete();
                    }
                    _subscriptionDropped(_subscription, item.DropReason, item.Error);
                }
            }
            catch (Exception exc)
            {
                _log.ExceptionDuringExecutingUserCallback(exc);
            }
        }

        sealed class ResolvedEventWrapper
        {
            public readonly bool IsResolvedEvent;
            public readonly TResolvedEvent ResolvedEvent;
            public readonly SubscriptionDropReason DropReason;
            public readonly Exception Error;

            public ResolvedEventWrapper(TResolvedEvent resolvedEvent)
            {
                IsResolvedEvent = true;
                ResolvedEvent = resolvedEvent;
                DropReason = SubscriptionDropReason.Unknown;
                Error = null;
            }

            public ResolvedEventWrapper(SubscriptionDropReason dropReason, Exception exc)
            {
                IsResolvedEvent = false;
                ResolvedEvent = default;
                DropReason = dropReason;
                Error = exc;
            }
        }
    }
}