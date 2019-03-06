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
    public class CatchUpConsumer : StreamConsumer<CatchUpSubscription, CatchUpSubscriptionSettings>
    {
        private static readonly ILogger s_logger = TraceLogger.GetLogger<VolatileConsumer>();

        private Func<EventStoreCatchUpSubscription, ResolvedEvent<object>, Task> _eventAppearedAsync;
        private Action<EventStoreCatchUpSubscription, ResolvedEvent<object>> _eventAppeared;

        private EventStoreCatchUpSubscription esSubscription;
        private EventStoreCatchUpSubscription2 esSubscription2;

        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);
            Task.Run(() =>
            {
                var subscription = Interlocked.Exchange(ref esSubscription, null);
                subscription?.Stop(TimeSpan.FromMinutes(1));
                var subscription2 = Interlocked.Exchange(ref esSubscription2, null);
                subscription2?.Stop(TimeSpan.FromMinutes(1));
            }).Ignore();
        }

        public void Initialize(IEventStoreBus bus, CatchUpSubscription subscription, Func<EventStoreCatchUpSubscription, ResolvedEvent<object>, Task> eventAppearedAsync)
        {
            if (null == eventAppearedAsync) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.eventAppearedAsync); }
            Initialize(bus, subscription);
            _eventAppearedAsync = eventAppearedAsync;
        }

        public void Initialize(IEventStoreBus bus, CatchUpSubscription subscription, Action<EventStoreCatchUpSubscription, ResolvedEvent<object>> eventAppeared)
        {
            if (null == eventAppeared) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.eventAppeared); }
            Initialize(bus, subscription);
            _eventAppeared = eventAppeared;
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
                if (UsingEventHandlers)
                {
                    if (string.IsNullOrEmpty(Subscription.Topic))
                    {
                        if (RegisterEventHandlers != null)
                        {
                            esSubscription2 = Bus.CatchUpSubscribe(Subscription.StreamId, lastCheckpoint, Subscription.Settings, RegisterEventHandlers,
                                    _ => s_logger.CaughtUpOnStreamAt(_.StreamId),
                                    async (sub, reason, exception) => await SubscriptionDroppedAsync(sub.LastProcessedEventNumber, reason, exception).ConfigureAwait(false),
                                    Subscription.Credentials);
                        }
                        else
                        {
                            esSubscription2 = Bus.CatchUpSubscribe(Subscription.StreamId, lastCheckpoint, Subscription.Settings, RegisterHandlers,
                                    _ => s_logger.CaughtUpOnStreamAt(_.StreamId),
                                    async (sub, reason, exception) => await SubscriptionDroppedAsync(sub.LastProcessedEventNumber, reason, exception).ConfigureAwait(false),
                                    Subscription.Credentials);
                        }
                    }
                    else
                    {
                        if (RegisterEventHandlers != null)
                        {
                            esSubscription2 = Bus.CatchUpSubscribe(Subscription.StreamId, Subscription.Topic, lastCheckpoint, Subscription.Settings, RegisterEventHandlers,
                                    _ => s_logger.CaughtUpOnStreamAt(_.StreamId),
                                    async (sub, reason, exception) => await SubscriptionDroppedAsync(sub.LastProcessedEventNumber, reason, exception).ConfigureAwait(false),
                                    Subscription.Credentials);
                        }
                        else
                        {
                            esSubscription2 = Bus.CatchUpSubscribe(Subscription.StreamId, Subscription.Topic, lastCheckpoint, Subscription.Settings, RegisterHandlers,
                                    _ => s_logger.CaughtUpOnStreamAt(_.StreamId),
                                    async (sub, reason, exception) => await SubscriptionDroppedAsync(sub.LastProcessedEventNumber, reason, exception).ConfigureAwait(false),
                                    Subscription.Credentials);
                        }
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(Subscription.Topic))
                    {
                        if (_eventAppearedAsync != null)
                        {
                            esSubscription = Bus.CatchUpSubscribe(Subscription.StreamId, lastCheckpoint, Subscription.Settings, _eventAppearedAsync,
                                    _ => s_logger.CaughtUpOnStreamAt(_.StreamId),
                                    async (sub, reason, exception) => await SubscriptionDroppedAsync(sub.LastProcessedEventNumber, reason, exception).ConfigureAwait(false),
                                    Subscription.Credentials);
                        }
                        else
                        {
                            esSubscription = Bus.CatchUpSubscribe(Subscription.StreamId, lastCheckpoint, Subscription.Settings, _eventAppeared,
                                    _ => s_logger.CaughtUpOnStreamAt(_.StreamId),
                                    async (sub, reason, exception) => await SubscriptionDroppedAsync(sub.LastProcessedEventNumber, reason, exception).ConfigureAwait(false),
                                    Subscription.Credentials);
                        }
                    }
                    else
                    {
                        if (_eventAppearedAsync != null)
                        {
                            esSubscription = Bus.CatchUpSubscribe(Subscription.StreamId, Subscription.Topic, lastCheckpoint, Subscription.Settings, _eventAppearedAsync,
                                    _ => s_logger.CaughtUpOnStreamAt(_.StreamId),
                                    async (sub, reason, exception) => await SubscriptionDroppedAsync(sub.LastProcessedEventNumber, reason, exception).ConfigureAwait(false),
                                    Subscription.Credentials);
                        }
                        else
                        {
                            esSubscription = Bus.CatchUpSubscribe(Subscription.StreamId, Subscription.Topic, lastCheckpoint, Subscription.Settings, _eventAppeared,
                                    _ => s_logger.CaughtUpOnStreamAt(_.StreamId),
                                    async (sub, reason, exception) => await SubscriptionDroppedAsync(sub.LastProcessedEventNumber, reason, exception).ConfigureAwait(false),
                                    Subscription.Credentials);
                        }
                    }
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
