using System;
using System.Threading;
using System.Threading.Tasks;
using EventStore.ClientAPI.Common.Utils.Threading;
using EventStore.ClientAPI.Subscriptions;
using Microsoft.Extensions.Logging;

namespace EventStore.ClientAPI.Consumers
{
    /// <summary>Represents a consumer to a volatile subscription to EventStore: http://docs.geteventstore.com/introduction/4.0.0/subscriptions/ .</summary>
    public class VolatileConsumer : StreamConsumer<VolatileSubscription, SubscriptionSettings>
    {
        private static readonly ILogger s_logger = TraceLogger.GetLogger<VolatileConsumer>();

        private Func<EventStoreSubscription, ResolvedEvent<object>, Task> _eventAppearedAsync;
        private Action<EventStoreSubscription, ResolvedEvent<object>> _eventAppeared;

        private EventStoreSubscription _esSubscription;

        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);
            Task.Run(() =>
            {
                var subscription = Interlocked.Exchange(ref _esSubscription, null);
                subscription?.Dispose();
            }).Ignore();
        }

        public void Initialize(IEventStoreBus bus, VolatileSubscription subscription, Func<EventStoreSubscription, ResolvedEvent<object>, Task> eventAppearedAsync)
        {
            if (null == eventAppearedAsync) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.eventAppearedAsync); }
            Initialize(bus, subscription);
            _eventAppearedAsync = eventAppearedAsync;
        }

        public void Initialize(IEventStoreBus bus, VolatileSubscription subscription, Action<EventStoreSubscription, ResolvedEvent<object>> eventAppeared)
        {
            if (null == eventAppeared) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.eventAppeared); }
            Initialize(bus, subscription);
            _eventAppeared = eventAppeared;
        }

        public override async Task ConnectToSubscriptionAsync()
        {
            if (Volatile.Read(ref _subscribed) == ON) { return; }

            try
            {
                if (UsingEventHandlers)
                {
                    if (string.IsNullOrEmpty(Subscription.Topic))
                    {
                        if (RegisterEventHandlers != null)
                        {
                            _esSubscription = await Bus.VolatileSubscribeAsync(Subscription.StreamId, Subscription.Settings, RegisterEventHandlers,
                                    async (sub, reason, exception) => await SubscriptionDroppedAsync(sub.ProcessingEventNumber, reason, exception).ConfigureAwait(false),
                                    Subscription.Credentials).ConfigureAwait(false);
                        }
                        else
                        {
                            _esSubscription = await Bus.VolatileSubscribeAsync(Subscription.StreamId, Subscription.Settings, RegisterHandlers,
                                    async (sub, reason, exception) => await SubscriptionDroppedAsync(sub.ProcessingEventNumber, reason, exception).ConfigureAwait(false),
                                    Subscription.Credentials).ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        if (RegisterEventHandlers != null)
                        {
                            _esSubscription = await Bus.VolatileSubscribeAsync(Subscription.StreamId, Subscription.Topic, Subscription.Settings, RegisterEventHandlers,
                                    async (sub, reason, exception) => await SubscriptionDroppedAsync(sub.ProcessingEventNumber, reason, exception).ConfigureAwait(false),
                                    Subscription.Credentials).ConfigureAwait(false);
                        }
                        else
                        {
                            _esSubscription = await Bus.VolatileSubscribeAsync(Subscription.StreamId, Subscription.Topic, Subscription.Settings, RegisterHandlers,
                                    async (sub, reason, exception) => await SubscriptionDroppedAsync(sub.ProcessingEventNumber, reason, exception).ConfigureAwait(false),
                                    Subscription.Credentials).ConfigureAwait(false);
                        }
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(Subscription.Topic))
                    {
                        if (_eventAppearedAsync != null)
                        {
                            _esSubscription = await Bus.VolatileSubscribeAsync(Subscription.StreamId, Subscription.Settings, _eventAppearedAsync,
                                    async (sub, reason, exception) => await SubscriptionDroppedAsync(sub.ProcessingEventNumber, reason, exception).ConfigureAwait(false),
                                    Subscription.Credentials).ConfigureAwait(false);
                        }
                        else
                        {
                            _esSubscription = await Bus.VolatileSubscribeAsync(Subscription.StreamId, Subscription.Settings, _eventAppeared,
                                    async (sub, reason, exception) => await SubscriptionDroppedAsync(sub.ProcessingEventNumber, reason, exception).ConfigureAwait(false),
                                    Subscription.Credentials).ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        if (_eventAppearedAsync != null)
                        {
                            _esSubscription = await Bus.VolatileSubscribeAsync(Subscription.StreamId, Subscription.Topic, Subscription.Settings, _eventAppearedAsync,
                                    async (sub, reason, exception) => await SubscriptionDroppedAsync(sub.ProcessingEventNumber, reason, exception).ConfigureAwait(false),
                                    Subscription.Credentials).ConfigureAwait(false);
                        }
                        else
                        {
                            _esSubscription = await Bus.VolatileSubscribeAsync(Subscription.StreamId, Subscription.Topic, Subscription.Settings, _eventAppeared,
                                    async (sub, reason, exception) => await SubscriptionDroppedAsync(sub.ProcessingEventNumber, reason, exception).ConfigureAwait(false),
                                    Subscription.Credentials).ConfigureAwait(false);
                        }
                    }
                }

                Interlocked.Exchange(ref _subscribed, ON);
            }
            catch (Exception exc)
            {
                s_logger.LogError(exc.ToString());
                throw exc;
            }
        }
    }
}
