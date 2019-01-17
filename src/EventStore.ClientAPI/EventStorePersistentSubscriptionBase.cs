using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using CuteAnt.AsyncEx;
using EventStore.ClientAPI.Internal;
using EventStore.ClientAPI.SystemData;
using Microsoft.Extensions.Logging;

namespace EventStore.ClientAPI
{
    public abstract class EventStorePersistentSubscriptionBase : EventStorePersistentSubscriptionBase<EventStorePersistentSubscriptionBase, PersistentSubscriptionResolvedEvent, ResolvedEvent>
    {
        internal EventStorePersistentSubscriptionBase(string subscriptionId, string streamId,
                                                      ConnectToPersistentSubscriptionSettings settings,
                                                      Action<EventStorePersistentSubscriptionBase, ResolvedEvent, int?> eventAppeared,
                                                      Action<EventStorePersistentSubscriptionBase, SubscriptionDropReason, Exception> subscriptionDropped,
                                                      UserCredentials userCredentials, ConnectionSettings connSettings)
          : base(subscriptionId, streamId, settings, eventAppeared, subscriptionDropped, userCredentials, connSettings)
        {
        }

        internal EventStorePersistentSubscriptionBase(string subscriptionId, string streamId,
                                                      ConnectToPersistentSubscriptionSettings settings,
                                                      Func<EventStorePersistentSubscriptionBase, ResolvedEvent, int?, Task> eventAppearedAsync,
                                                      Action<EventStorePersistentSubscriptionBase, SubscriptionDropReason, Exception> subscriptionDropped,
                                                      UserCredentials userCredentials, ConnectionSettings connSettings)
          : base(subscriptionId, streamId, settings, eventAppearedAsync, subscriptionDropped, userCredentials, connSettings)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override ResolvedEvent TransformEvent(in PersistentSubscriptionResolvedEvent resolvedEvent) => resolvedEvent.Event;
    }

    /// <summary>Represents a persistent subscription connection.</summary>
    public abstract class EventStorePersistentSubscriptionBase<TSubscription, TPersistentSubscriptionResolvedEvent, TResolvedEvent> : IStreamCheckpointer
      where TSubscription : EventStorePersistentSubscriptionBase<TSubscription, TPersistentSubscriptionResolvedEvent, TResolvedEvent>
      where TPersistentSubscriptionResolvedEvent : IPersistentSubscriptionResolvedEvent
      where TResolvedEvent : IResolvedEvent
    {
        internal static /*readonly*/ TPersistentSubscriptionResolvedEvent DropSubscriptionEvent;// = new TResolvedEvent();
        private static readonly ILogger _log = TraceLogger.GetLogger("EventStore.ClientAPI.PersistentSubscription");

        ///<summary>The default buffer size for the persistent subscription</summary>
        public const int DefaultBufferSize = 10;

        private readonly string _subscriptionId;
        private readonly string _streamId;
        internal readonly Action<TSubscription, TResolvedEvent, int?> _eventAppeared;
        internal readonly Func<TSubscription, TResolvedEvent, int?, Task> _eventAppearedAsync;
        private readonly Action<TSubscription, SubscriptionDropReason, Exception> _subscriptionDropped;
        private readonly UserCredentials _userCredentials;
        private readonly bool _verbose;
        private readonly ConnectionSettings _connSettings;
        private readonly bool _autoAck;

        private PersistentEventStoreSubscription _subscription;
        private BufferBlock<TPersistentSubscriptionResolvedEvent> _bufferBlock;
        private List<ActionBlock<TPersistentSubscriptionResolvedEvent>> _actionBlocks;
        private ITargetBlock<TPersistentSubscriptionResolvedEvent> _targetBlock;
        private ConnectToPersistentSubscriptionSettings _settings;
        private IDisposable _links;
        private DropData _dropData;

        private int _isDropped;
        private readonly ManualResetEventSlim _stopped = new ManualResetEventSlim(true);
        private readonly int _bufferSize;
        private long _processingEventNumber;

        /// <summary>Gets the number of items waiting to be processed by this subscription.</summary>
        internal Int32 InputCount { get { return null == _bufferBlock ? _actionBlocks[0].InputCount : _bufferBlock.Count; } }

        public long ProcessingEventNumber => _processingEventNumber;

        internal EventStorePersistentSubscriptionBase(string subscriptionId, string streamId,
                                                      ConnectToPersistentSubscriptionSettings settings,
                                                      Action<TSubscription, TResolvedEvent, int?> eventAppeared,
                                                      Action<TSubscription, SubscriptionDropReason, Exception> subscriptionDropped,
                                                      UserCredentials userCredentials, ConnectionSettings connSettings)
          : this(subscriptionId, streamId, settings, subscriptionDropped, userCredentials, connSettings)
        {
            _eventAppeared = eventAppeared;
        }

        internal EventStorePersistentSubscriptionBase(string subscriptionId, string streamId,
                                                      ConnectToPersistentSubscriptionSettings settings,
                                                      Func<TSubscription, TResolvedEvent, int?, Task> eventAppearedAsync,
                                                      Action<TSubscription, SubscriptionDropReason, Exception> subscriptionDropped,
                                                      UserCredentials userCredentials, ConnectionSettings connSettings)
          : this(subscriptionId, streamId, settings, subscriptionDropped, userCredentials, connSettings)
        {
            _eventAppearedAsync = eventAppearedAsync;
        }

        private EventStorePersistentSubscriptionBase(string subscriptionId, string streamId,
                                                     ConnectToPersistentSubscriptionSettings settings,
                                                     Action<TSubscription, SubscriptionDropReason, Exception> subscriptionDropped,
                                                     UserCredentials userCredentials, ConnectionSettings connSettings)
        {
            _subscriptionId = subscriptionId;
            _streamId = streamId;
            _settings = settings;
            _subscriptionDropped = subscriptionDropped;
            _userCredentials = userCredentials;
            _verbose = settings.VerboseLogging && _log.IsDebugLevelEnabled();
            _connSettings = connSettings;
            _bufferSize = settings.BufferSize;
            _autoAck = settings.AutoAck;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected abstract TResolvedEvent TransformEvent(in TPersistentSubscriptionResolvedEvent resolvedEvent);

        internal async Task<TSubscription> StartAsync()
        {
            _stopped.Reset();

            _subscription = await StartSubscriptionAsync(_subscriptionId, _streamId, _settings, _userCredentials, OnEventAppearedAsync, OnSubscriptionDropped, _connSettings)
                                  .ConfigureAwait(false);

            var numActionBlocks = _settings.NumActionBlocks;
            if (SubscriptionSettings.Unbounded == _settings.BoundedCapacityPerBlock)
            {
                // 如果没有设定 ActionBlock 的容量，设置多个 ActionBlock 没有意义
                numActionBlocks = 1;
            }
            _actionBlocks = new List<ActionBlock<TPersistentSubscriptionResolvedEvent>>(numActionBlocks);
            if (_eventAppeared != null)
            {
                for (var idx = 0; idx < numActionBlocks; idx++)
                {
                    _actionBlocks.Add(new ActionBlock<TPersistentSubscriptionResolvedEvent>(e => ProcessResolvedEvent(e), _settings.ToExecutionDataflowBlockOptions(true)));
                }
            }
            else
            {
                for (var idx = 0; idx < numActionBlocks; idx++)
                {
                    _actionBlocks.Add(new ActionBlock<TPersistentSubscriptionResolvedEvent>(e => ProcessResolvedEventAsync(e), _settings.ToExecutionDataflowBlockOptions()));
                }
            }
            if (numActionBlocks > 1)
            {
                var links = new CompositeDisposable();
                _bufferBlock = new BufferBlock<TPersistentSubscriptionResolvedEvent>(_settings.ToBufferBlockOptions());
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

            return this as TSubscription;
        }

        internal abstract Task<PersistentEventStoreSubscription> StartSubscriptionAsync(string subscriptionId, string streamId,
          ConnectToPersistentSubscriptionSettings settings, UserCredentials userCredentials,
          Func<EventStoreSubscription, TPersistentSubscriptionResolvedEvent, Task> onEventAppearedAsync,
          Action<EventStoreSubscription, SubscriptionDropReason, Exception> onSubscriptionDropped,
          ConnectionSettings connSettings);

        /// <summary>Acknowledge that a message have completed processing (this will tell the server it has been processed).</summary>
        /// <remarks>There is no need to ack a message if you have Auto Ack enabled</remarks>
        /// <param name="event">The <see cref="ResolvedEvent"></see> to acknowledge</param>
        public void Acknowledge(in TResolvedEvent @event)
        {
            _subscription.NotifyEventsProcessed(new[] { @event.OriginalEventId });
        }

        /// <summary>Acknowledge that a message have completed processing (this will tell the server it has been processed).</summary>
        /// <remarks>There is no need to ack a message if you have Auto Ack enabled</remarks>
        /// <param name="events">The <see cref="ResolvedEvent"></see>s to acknowledge there should be less than 2000 to ack at a time.</param>
        public void Acknowledge(IEnumerable<TResolvedEvent> events)
        {
            var ids = events.Select(x => x.OriginalEventId).ToArray();
            if (ids.Length > 2000) CoreThrowHelper.ThrowArgumentOutOfRangeException_EventsIsLimitedTo2000ToAckAtATime();
            _subscription.NotifyEventsProcessed(ids);
        }

        /// <summary>Acknowledge a message by event id (this will tell the server it has been processed).</summary>
        /// <remarks>There is no need to ack a message if you have Auto Ack enabled</remarks>
        /// <param name="eventId">The <see cref="ResolvedEvent"></see> OriginalEvent.EventId to acknowledge</param>
        public void Acknowledge(Guid eventId)
        {
            _subscription.NotifyEventsProcessed(new[] { eventId });
        }

        /// <summary>Acknowledge a group of messages by event id (this will tell the server it has been processed).</summary>
        /// <remarks>There is no need to ack a message if you have Auto Ack enabled</remarks>
        /// <param name="events">The <see cref="ResolvedEvent"></see> OriginalEvent.EventIds to acknowledge there should be less than 2000 to ack at a time.</param>
        public void Acknowledge(IEnumerable<Guid> events)
        {
            var ids = events.ToArray();
            if (ids.Length > 2000) CoreThrowHelper.ThrowArgumentOutOfRangeException_EventsIsLimitedTo2000ToAckAtATime();
            _subscription.NotifyEventsProcessed(ids);
        }
        /// <summary>Mark a message failed processing. The server will be take action based upon the action paramter.</summary>
        /// <param name="event">The event to mark as failed</param>
        /// <param name="action">The <see cref="PersistentSubscriptionNakEventAction"></see> action to take</param>
        /// <param name="reason">A string with a message as to why the failure is occurring</param>
        public void Fail(in TResolvedEvent @event, PersistentSubscriptionNakEventAction action, string reason)
        {
            _subscription.NotifyEventsFailed(new[] { @event.OriginalEventId }, action, reason);
        }

        /// <summary>Mark nmessages that have failed processing. The server will take action based upon the action parameter.</summary>
        /// <param name="events">The events to mark as failed</param>
        /// <param name="action">The <see cref="PersistentSubscriptionNakEventAction"></see> action to take</param>
        /// <param name="reason">A string with a message as to why the failure is occurring</param>
        public void Fail(IEnumerable<TResolvedEvent> events, PersistentSubscriptionNakEventAction action, string reason)
        {
            var ids = events.Select(x => x.OriginalEventId).ToArray();
            if (ids.Length > 2000) CoreThrowHelper.ThrowArgumentOutOfRangeException_EventsIsLimitedTo2000ToAckAtATime();
            _subscription.NotifyEventsFailed(ids, action, reason);
        }

        /// <summary>Disconnects this client from the persistent subscriptions.</summary>
        /// <param name="timeout"></param>
        /// <exception cref="TimeoutException"></exception>
        public void Stop(TimeSpan timeout)
        {
            if (_verbose) { _log.PersistentSubscriptionRequestingStop(_streamId); }

            EnqueueSubscriptionDropNotification(SubscriptionDropReason.UserInitiated, null);

            if (!_stopped.Wait(timeout))
            {
                CoreThrowHelper.ThrowTimeoutException_CouldNotStopSubscriptionsInTime(GetType());
            }

            if (_bufferBlock != null)
            {
                _bufferBlock.Complete();
                _links?.Dispose();
            }
            else if (_actionBlocks != null && _actionBlocks.Count > 0)
            {
                _actionBlocks[0]?.Complete();
            }
        }

        private void EnqueueSubscriptionDropNotification(SubscriptionDropReason reason, Exception error)
        {
            // if drop data was already set -- no need to enqueue drop again, somebody did that already
            var dropData = new DropData(reason, error);
            if (Interlocked.CompareExchange(ref _dropData, dropData, null) == null)
            {
                AsyncContext.Run(s_sendToQueueFunc, _targetBlock, DropSubscriptionEvent);
            }
        }

        private static readonly Func<ITargetBlock<TPersistentSubscriptionResolvedEvent>, TPersistentSubscriptionResolvedEvent, Task<bool>> s_sendToQueueFunc = SendToQueueAsync;
        private static async Task<bool> SendToQueueAsync(ITargetBlock<TPersistentSubscriptionResolvedEvent> targetBlock, TPersistentSubscriptionResolvedEvent message)
        {
            return await targetBlock.SendAsync(message).ConfigureAwait(false);
        }

        private void OnSubscriptionDropped(EventStoreSubscription subscription, SubscriptionDropReason reason, Exception exception)
        {
            EnqueueSubscriptionDropNotification(reason, exception);
        }

        private Task OnEventAppearedAsync(EventStoreSubscription subscription, TPersistentSubscriptionResolvedEvent resolvedEvent)
        {
            return _targetBlock.SendAsync(resolvedEvent);
        }

        private void ProcessResolvedEvent(in TPersistentSubscriptionResolvedEvent resolvedEvent)
        {
            if (resolvedEvent.Equals(DropSubscriptionEvent)) // drop subscription artificial ResolvedEvent
            {
                if (_dropData == null) { CoreThrowHelper.ThrowException_DropReasonNotSpecified(); }
                DropSubscription(_dropData.Reason, _dropData.Error);
                return;
            }

            Interlocked.Exchange(ref _processingEventNumber, resolvedEvent.OriginalEventNumber);

            if (_dropData != null)
            {
                DropSubscription(_dropData.Reason, _dropData.Error);
                return;
            }
            try
            {
                _eventAppeared(this as TSubscription, TransformEvent(resolvedEvent), resolvedEvent.RetryCount);
                if (_autoAck)
                {
                    _subscription.NotifyEventsProcessed(new[] { resolvedEvent.OriginalEventId });
                }
                if (_verbose) { _log.PersistentSubscriptionProcessedEvent(_streamId, resolvedEvent); }
            }
            catch (Exception exc)
            {
                // TODO GFY should we autonak here?
                DropSubscription(SubscriptionDropReason.EventHandlerException, exc);
                return;
            }
        }

        private async Task ProcessResolvedEventAsync(TPersistentSubscriptionResolvedEvent resolvedEvent)
        {
            if (resolvedEvent.Equals(DropSubscriptionEvent)) // drop subscription artificial ResolvedEvent
            {
                if (_dropData == null) { CoreThrowHelper.ThrowException_DropReasonNotSpecified(); }
                DropSubscription(_dropData.Reason, _dropData.Error);
                return;
            }

            Interlocked.Exchange(ref _processingEventNumber, resolvedEvent.OriginalEventNumber);

            if (_dropData != null)
            {
                DropSubscription(_dropData.Reason, _dropData.Error);
                return;
            }
            try
            {
                await _eventAppearedAsync(this as TSubscription, TransformEvent(resolvedEvent), resolvedEvent.RetryCount).ConfigureAwait(false);
                if (_autoAck)
                {
                    _subscription.NotifyEventsProcessed(new[] { resolvedEvent.OriginalEventId });
                }
                if (_verbose) { _log.PersistentSubscriptionProcessedEvent(_streamId, resolvedEvent); }
            }
            catch (Exception exc)
            {
                // TODO GFY should we autonak here?
                DropSubscription(SubscriptionDropReason.EventHandlerException, exc);
                return;
            }
        }

        private void DropSubscription(SubscriptionDropReason reason, Exception error)
        {
            if (Interlocked.CompareExchange(ref _isDropped, 1, 0) == 0)
            {
                if (_verbose) { _log.PersistentDroppingSubscriptionReason(_streamId, reason, error); }

                _subscription?.Unsubscribe();
                _subscriptionDropped?.Invoke(this as TSubscription, reason, error);
                _stopped.Set();
            }
        }

        private sealed class DropData
        {
            public readonly SubscriptionDropReason Reason;
            public readonly Exception Error;

            public DropData(SubscriptionDropReason reason, Exception error)
            {
                Reason = reason;
                Error = error;
            }
        }
    }
}
