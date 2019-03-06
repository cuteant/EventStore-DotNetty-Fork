using System;
using System.Threading;
using System.Threading.Tasks;
using CuteAnt.AsyncEx;
using EventStore.ClientAPI.Common.Utils.Threading;
using EventStore.ClientAPI.Resilience;
using EventStore.ClientAPI.Subscriptions;
using Microsoft.Extensions.Logging;

namespace EventStore.ClientAPI.Consumers
{
    /// <summary>Represents the consumer of a catch-up subscription to EventStore: http://docs.geteventstore.com/introduction/4.0.0/subscriptions/ </summary>
    public class CatchUpConsumer<TEvent> : StreamConsumer<CatchUpSubscription<TEvent>, CatchUpSubscriptionSettings, TEvent>
    {
        private static readonly ILogger s_logger = TraceLogger.GetLogger<CatchUpConsumer>();

        private Func<EventStoreCatchUpSubscription<TEvent>, ResolvedEvent<TEvent>, Task> _resolvedEventAppearedAsync;
        private Action<EventStoreCatchUpSubscription<TEvent>, ResolvedEvent<TEvent>> _resolvedEventAppeared;

        private EventStoreCatchUpSubscription<TEvent> esSubscription;

        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);
            Task.Run(() =>
            {
                var subscription = Interlocked.Exchange(ref esSubscription, null);
                subscription?.Stop(TimeSpan.FromMinutes(1));
            }).Ignore();
        }

        public void Initialize(IEventStoreBus bus, CatchUpSubscription<TEvent> subscription,
            Func<EventStoreCatchUpSubscription<TEvent>, ResolvedEvent<TEvent>, Task> resolvedEventAppearedAsync)
        {
            if (null == resolvedEventAppearedAsync) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.resolvedEventAppearedAsync); }
            Initialize(bus, subscription);
            _resolvedEventAppearedAsync = resolvedEventAppearedAsync;
        }

        public void Initialize(IEventStoreBus bus, CatchUpSubscription<TEvent> subscription,
            Action<EventStoreCatchUpSubscription<TEvent>, ResolvedEvent<TEvent>> resolvedEventAppeared)
        {
            if (null == resolvedEventAppeared) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.resolvedEventAppeared); }
            Initialize(bus, subscription);
            _resolvedEventAppeared = resolvedEventAppeared;
        }

        public void Initialize(IEventStoreBus bus, CatchUpSubscription<TEvent> subscription, Func<TEvent, Task> eventAppearedAsync)
        {
            if (null == eventAppearedAsync) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.eventAppearedAsync); }
            Initialize(bus, subscription);
            _resolvedEventAppearedAsync = (sub, resolvedEvent) => eventAppearedAsync(resolvedEvent.Body);
        }

        public void Initialize(IEventStoreBus bus, CatchUpSubscription<TEvent> subscription, Action<TEvent> eventAppeared)
        {
            if (null == eventAppeared) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.eventAppeared); }
            Initialize(bus, subscription);
            _resolvedEventAppeared = (sub, resolvedEvent) => eventAppeared(resolvedEvent.Body);
        }

        public override Task ConnectToSubscriptionAsync() => ConnectToSubscriptionAsync(Subscription.LastCheckpoint);
        public virtual async Task ConnectToSubscriptionAsync(long? lastCheckpoint)
        {
            if (Volatile.Read(ref _subscribed) == ON) { return; }

            try
            {
                if (lastCheckpoint.HasValue && lastCheckpoint.Value < 0L) // StreamPosition.End
                {
                    var readResult = await Bus.ReadLastEventAsync(Subscription.StreamId, Subscription.Settings.ResolveLinkTos, Subscription.Credentials);
                    if (EventReadStatus.Success == readResult.Status)
                    {
                        lastCheckpoint = readResult.EventNumber;
                    }
                }
            }
            catch (Exception exc)
            {
                await SubscriptionDroppedAsync(LastProcessingEventNumber, SubscriptionDropReason.Unknown, exc).ConfigureAwait(false);
                return;
            }

            try
            {
                if (_resolvedEventAppearedAsync != null)
                {
                    esSubscription = Bus.CatchUpSubscribe<TEvent>(Subscription.Topic, lastCheckpoint, Subscription.Settings, _resolvedEventAppearedAsync,
                            _ => s_logger.CaughtUpOnStreamAt(_.StreamId),
                            async (sub, reason, exception) => await SubscriptionDroppedAsync(sub.LastProcessedEventNumber, reason, exception).ConfigureAwait(false),
                            Subscription.Credentials);
                }
                else
                {
                    esSubscription = Bus.CatchUpSubscribe<TEvent>(Subscription.Topic, lastCheckpoint, Subscription.Settings, _resolvedEventAppeared,
                            _ => s_logger.CaughtUpOnStreamAt(_.StreamId),
                            async (sub, reason, exception) => await SubscriptionDroppedAsync(sub.LastProcessedEventNumber, reason, exception).ConfigureAwait(false),
                            Subscription.Credentials);
                }

                Interlocked.Exchange(ref _subscribed, ON);
            }
            catch (Exception exc)
            {
                await TaskConstants.Completed;
                s_logger.LogError(exc.ToString());
                throw exc;
            }
        }

        protected override async Task HandleDroppedSubscriptionAsync(DroppedSubscription subscriptionDropped)
        {
            if (this.Disposed) { return; }
            var lastCheckpoint = StreamCheckpoint.StreamStart;
            if (LastProcessingEventNumber >= 0L) { lastCheckpoint = LastProcessingEventNumber; }
            await DroppedSubscriptionPolicy.Handle(subscriptionDropped, async () => await ConnectToSubscriptionAsync(lastCheckpoint), subscriptionDropped.RetryPolicy ?? DefaultRetryPolicy);
        }
    }
}
