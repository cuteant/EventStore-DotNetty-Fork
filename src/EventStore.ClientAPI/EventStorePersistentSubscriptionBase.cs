using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
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

        private readonly ActionBlock<TPersistentSubscriptionResolvedEvent> _targetBlock;
        private PersistentEventStoreSubscription _subscription;
        private readonly ConnectToPersistentSubscriptionSettings _settings;
        private DropData _dropData;

        private int _isDropped;
        private readonly ManualResetEventSlim _stopped = new ManualResetEventSlim(true);
        private long _processingEventNumber;

        /// <summary>Gets the number of items waiting to be processed by this subscription.</summary>
        internal Int32 InputCount => _targetBlock.InputCount;

        public long ProcessingEventNumber => Volatile.Read(ref _processingEventNumber);

        internal EventStorePersistentSubscriptionBase(string subscriptionId, string streamId,
                                                      ConnectToPersistentSubscriptionSettings settings,
                                                      Action<TSubscription, TResolvedEvent, int?> eventAppeared,
                                                      Action<TSubscription, SubscriptionDropReason, Exception> subscriptionDropped,
                                                      UserCredentials userCredentials, ConnectionSettings connSettings)
          : this(subscriptionId, streamId, settings, subscriptionDropped, userCredentials, connSettings)
        {
            _targetBlock = new ActionBlock<TPersistentSubscriptionResolvedEvent>(ProcessResolvedEvent, _settings.ToExecutionDataflowBlockOptions(true));
            _eventAppeared = eventAppeared;
        }

        internal EventStorePersistentSubscriptionBase(string subscriptionId, string streamId,
                                                      ConnectToPersistentSubscriptionSettings settings,
                                                      Func<TSubscription, TResolvedEvent, int?, Task> eventAppearedAsync,
                                                      Action<TSubscription, SubscriptionDropReason, Exception> subscriptionDropped,
                                                      UserCredentials userCredentials, ConnectionSettings connSettings)
          : this(subscriptionId, streamId, settings, subscriptionDropped, userCredentials, connSettings)
        {
            _targetBlock = new ActionBlock<TPersistentSubscriptionResolvedEvent>(ProcessResolvedEventAsync, _settings.ToExecutionDataflowBlockOptions());
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
            _autoAck = settings.AutoAck;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected abstract TResolvedEvent TransformEvent(in TPersistentSubscriptionResolvedEvent resolvedEvent);

        internal async Task<TSubscription> StartAsync()
        {
            _stopped.Reset();

            var subscription = await StartSubscriptionAsync(_subscriptionId, _streamId, _settings, _userCredentials, OnEventAppearedAsync, OnSubscriptionDropped, _connSettings)
                                  .ConfigureAwait(false);
            Interlocked.Exchange(ref _subscription, subscription);
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
            _subscription.NotifyEventsProcessed(@event.OriginalEventId);
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
            _subscription.NotifyEventsProcessed(eventId);
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
            _subscription.NotifyEventsFailed(@event.OriginalEventId, action, reason);
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

            _targetBlock.Complete();
        }

        private void EnqueueSubscriptionDropNotification(SubscriptionDropReason reason, Exception error)
        {
            // if drop data was already set -- no need to enqueue drop again, somebody did that already
            var dropData = new DropData(reason, error);
            if (Interlocked.CompareExchange(ref _dropData, dropData, null) == null)
            {
                _targetBlock.SendAsync(DropSubscriptionEvent);
            }
        }

        private void OnSubscriptionDropped(EventStoreSubscription subscription, SubscriptionDropReason reason, Exception exception)
        {
            EnqueueSubscriptionDropNotification(reason, exception);
        }

        private Task OnEventAppearedAsync(EventStoreSubscription subscription, TPersistentSubscriptionResolvedEvent resolvedEvent)
        {
            return _targetBlock.SendAsync(resolvedEvent);
        }

        private void ProcessResolvedEvent(TPersistentSubscriptionResolvedEvent resolvedEvent)
        {
            var isArtificialEvent = resolvedEvent.IsDropping;  // drop subscription artificial ResolvedEvent
            var dropData = Volatile.Read(ref _dropData);
            if (isArtificialEvent || dropData != null)
            {
                ProcessDropSubscription(isArtificialEvent, dropData);
                return;
            }

            Interlocked.Exchange(ref _processingEventNumber, resolvedEvent.OriginalEventNumber);
            try
            {
                _eventAppeared(this as TSubscription, TransformEvent(resolvedEvent), resolvedEvent.RetryCount);
                if (_autoAck)
                {
                    _subscription.NotifyEventsProcessed(resolvedEvent.OriginalEventId);
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

        private void ProcessDropSubscription(bool isArtificialEvent, DropData dropData)
        {
            if (isArtificialEvent && dropData == null) { CoreThrowHelper.ThrowException_DropReasonNotSpecified(); }
            DropSubscription(dropData.Reason, dropData.Error);
        }

        private async Task ProcessResolvedEventAsync(TPersistentSubscriptionResolvedEvent resolvedEvent)
        {
            var isArtificialEvent = resolvedEvent.IsDropping;  // drop subscription artificial ResolvedEvent
            var dropData = Volatile.Read(ref _dropData);
            if (isArtificialEvent || dropData != null)
            {
                ProcessDropSubscription(isArtificialEvent, dropData);
                return;
            }

            Interlocked.Exchange(ref _processingEventNumber, resolvedEvent.OriginalEventNumber);
            try
            {
                await _eventAppearedAsync(this as TSubscription, TransformEvent(resolvedEvent), resolvedEvent.RetryCount).ConfigureAwait(false);
                if (_autoAck)
                {
                    _subscription.NotifyEventsProcessed(resolvedEvent.OriginalEventId);
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
