using System;
using System.Threading;
using System.Threading.Tasks;
using EventStore.ClientAPI.Common.Utils.Threading;
using EventStore.ClientAPI.Subscriptions;
using Microsoft.Extensions.Logging;

namespace EventStore.ClientAPI.Consumers
{
    /// <summary>Represents a consumer to a volatile subscription to EventStore: http://docs.geteventstore.com/introduction/4.0.0/subscriptions/ .</summary>
    public class VolatileConsumer<TEvent> : StreamConsumer<VolatileSubscription<TEvent>, SubscriptionSettings, TEvent>
    {
        private static readonly ILogger s_logger = TraceLogger.GetLogger<VolatileConsumer>();

        private Func<EventStoreSubscription, ResolvedEvent<TEvent>, Task> _resolvedEventAppearedAsync;
        private Action<EventStoreSubscription, ResolvedEvent<TEvent>> _resolvedEventAppeared;

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

        public void Initialize(IEventStoreBus bus, VolatileSubscription<TEvent> subscription,
          Func<EventStoreSubscription, ResolvedEvent<TEvent>, Task> resolvedEventAppearedAsync)
        {
            if (null == resolvedEventAppearedAsync) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.resolvedEventAppearedAsync); }
            Initialize(bus, subscription);
            _resolvedEventAppearedAsync = resolvedEventAppearedAsync;
        }

        public void Initialize(IEventStoreBus bus, VolatileSubscription<TEvent> subscription,
          Action<EventStoreSubscription, ResolvedEvent<TEvent>> resolvedEventAppeared)
        {
            if (null == resolvedEventAppeared) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.resolvedEventAppeared); }
            Initialize(bus, subscription);
            _resolvedEventAppeared = resolvedEventAppeared;
        }

        public void Initialize(IEventStoreBus bus, VolatileSubscription<TEvent> subscription, Func<TEvent, Task> eventAppearedAsync)
        {
            if (null == eventAppearedAsync) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.eventAppearedAsync); }
            Initialize(bus, subscription);
            _resolvedEventAppearedAsync = (sub, resolvedEvent) => eventAppearedAsync(resolvedEvent.Body);
        }

        public void Initialize(IEventStoreBus bus, VolatileSubscription<TEvent> subscription, Action<TEvent> eventAppeared)
        {
            if (null == eventAppeared) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.eventAppeared); }
            Initialize(bus, subscription);
            _resolvedEventAppeared = (sub, resolvedEvent) => eventAppeared(resolvedEvent.Body);
        }

        public override async Task ConnectToSubscriptionAsync()
        {
            if (Volatile.Read(ref _subscribed) == ON) { return; }

            try
            {
                if (_resolvedEventAppearedAsync != null)
                {
                    _esSubscription = await Bus.VolatileSubscribeAsync<TEvent>(Subscription.Topic, Subscription.Settings, _resolvedEventAppearedAsync,
                            async (sub, reason, exception) => await SubscriptionDroppedAsync(sub.ProcessingEventNumber, reason, exception).ConfigureAwait(false),
                            Subscription.Credentials).ConfigureAwait(false);
                }
                else
                {
                    _esSubscription = await Bus.VolatileSubscribeAsync<TEvent>(Subscription.Topic, Subscription.Settings, _resolvedEventAppeared,
                            async (sub, reason, exception) => await SubscriptionDroppedAsync(sub.ProcessingEventNumber, reason, exception).ConfigureAwait(false),
                            Subscription.Credentials).ConfigureAwait(false);
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
