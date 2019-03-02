using System;
using System.Threading;
using System.Threading.Tasks;
using EventStore.ClientAPI.Subscriptions;
using Microsoft.Extensions.Logging;

namespace EventStore.ClientAPI.Consumers
{
    /// <summary>Represents the consumer of a persistent subscription to EventStore: http://docs.geteventstore.com/introduction/4.0.0/subscriptions/
    /// This kind of consumer supports the competing consumer messaging pattern: http://www.enterpriseintegrationpatterns.com/patterns/messaging/CompetingConsumers.html </summary>
    public class PersistentConsumer<TEvent> : StreamConsumer<PersistentSubscription<TEvent>, ConnectToPersistentSubscriptionSettings, TEvent>
    {
        private static readonly ILogger s_logger = TraceLogger.GetLogger<PersistentConsumer>();

        private Func<EventStorePersistentSubscription<TEvent>, ResolvedEvent<TEvent>, int?, Task> _resolvedEventAppearedAsync;
        private Action<EventStorePersistentSubscription<TEvent>, ResolvedEvent<TEvent>, int?> _resolvedEventAppeared;

        private EventStorePersistentSubscription<TEvent> esSubscription;

        private int _initialized = OFF;

        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);
            var subscription = Interlocked.Exchange(ref esSubscription, null);
            subscription?.Stop(TimeSpan.FromMinutes(1));
        }

        protected override void Initialize(IEventStoreBus bus, PersistentSubscription<TEvent> subscription)
        {
            if (string.IsNullOrEmpty(subscription.SubscriptionId)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.subscription_SubscriptionId); }
            if (null == subscription.PersistentSettings) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.subscription_PersistentSettings); }

            base.Initialize(bus, subscription);
        }

        public void Initialize(IEventStoreBus bus, PersistentSubscription<TEvent> subscription,
            Func<EventStorePersistentSubscription<TEvent>, ResolvedEvent<TEvent>, int?, Task> resolvedEventAppearedAsync)
        {
            if (null == resolvedEventAppearedAsync) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.resolvedEventAppearedAsync); }
            Initialize(bus, subscription);
            _resolvedEventAppearedAsync = resolvedEventAppearedAsync;
        }

        public void Initialize(IEventStoreBus bus, PersistentSubscription<TEvent> subscription,
            Action<EventStorePersistentSubscription<TEvent>, ResolvedEvent<TEvent>, int?> resolvedEventAppeared)
        {
            if (null == resolvedEventAppeared) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.resolvedEventAppeared); }
            Initialize(bus, subscription);
            _resolvedEventAppeared = resolvedEventAppeared;
        }

        public void Initialize(IEventStoreBus bus, PersistentSubscription<TEvent> subscription, Func<TEvent, Task> eventAppearedAsync)
        {
            if (null == eventAppearedAsync) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.eventAppearedAsync); }
            Initialize(bus, subscription);
            _resolvedEventAppearedAsync = (sub, resolvedEvent, count) => eventAppearedAsync(resolvedEvent.Body);
        }

        public void Initialize(IEventStoreBus bus, PersistentSubscription<TEvent> subscription, Action<TEvent> eventAppeared)
        {
            if (null == eventAppeared) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.eventAppeared); }
            Initialize(bus, subscription);
            _resolvedEventAppeared = (sub, resolvedEvent, count) => eventAppeared(resolvedEvent.Body);
        }

        public override async Task ConnectToSubscriptionAsync()
        {
            if (Volatile.Read(ref _subscribed) == ON) { return; }

            var startFrom = Subscription.PersistentSettings.StartFrom;
            if (startFrom != StreamPosition.End)
            {
                await ConnectToSubscriptionAsync(startFrom).ConfigureAwait(false);
                return;
            }

            if (Interlocked.Exchange(ref _initialized, ON) == OFF)
            {
                try
                {
                    if (string.IsNullOrEmpty(Subscription.Topic))
                    {
                        await Bus.UpdateOrCreatePersistentSubscriptionAsync<TEvent>(Subscription.SubscriptionId, Subscription.PersistentSettings, Subscription.Credentials).ConfigureAwait(false);
                    }
                    else
                    {
                        await Bus.UpdateOrCreatePersistentSubscriptionAsync<TEvent>(Subscription.Topic, Subscription.SubscriptionId, Subscription.PersistentSettings, Subscription.Credentials).ConfigureAwait(false);
                    }
                }
                catch (Exception exc)
                {
                    await SubscriptionDroppedAsync(LastProcessingEventNumber, SubscriptionDropReason.Unknown, exc).ConfigureAwait(false);
                    return;
                }
            }

            await InternalConnectToSubscriptionAsync().ConfigureAwait(false);
        }

        public override async Task ConnectToSubscriptionAsync(long? lastCheckpoint)
        {
            if (Volatile.Read(ref _subscribed) == ON) { return; }

            if (Interlocked.Exchange(ref _initialized, ON) == OFF)
            {
                try
                {
                    if (string.IsNullOrEmpty(Subscription.Topic))
                    {
                        await Bus.DeletePersistentSubscriptionIfExistsAsync<TEvent>(Subscription.SubscriptionId, Subscription.Credentials).ConfigureAwait(false);

                        await Bus
                            .CreatePersistentSubscriptionAsync<TEvent>(Subscription.SubscriptionId,
                                Subscription.PersistentSettings.Clone(lastCheckpoint ?? -1), Subscription.Credentials)
                            .ConfigureAwait(false);
                    }
                    else
                    {
                        await Bus.DeletePersistentSubscriptionIfExistsAsync<TEvent>(Subscription.Topic, Subscription.SubscriptionId, Subscription.Credentials).ConfigureAwait(false);

                        await Bus
                            .CreatePersistentSubscriptionAsync<TEvent>(Subscription.Topic, Subscription.SubscriptionId,
                                Subscription.PersistentSettings.Clone(lastCheckpoint ?? -1), Subscription.Credentials)
                            .ConfigureAwait(false);
                    }
                }
                catch (Exception exc)
                {
                    await SubscriptionDroppedAsync(LastProcessingEventNumber, SubscriptionDropReason.Unknown, exc).ConfigureAwait(false);
                    return;
                }
            }

            await InternalConnectToSubscriptionAsync().ConfigureAwait(false);
        }

        private async Task InternalConnectToSubscriptionAsync()
        {
            try
            {
                if (_resolvedEventAppearedAsync != null)
                {
                    esSubscription = await Bus.PersistentSubscribeAsync<TEvent>(Subscription.Topic, Subscription.SubscriptionId, Subscription.Settings, _resolvedEventAppearedAsync,
                            async (sub, reason, exception) => await SubscriptionDroppedAsync(sub.ProcessingEventNumber, reason, exception).ConfigureAwait(false),
                            Subscription.Credentials).ConfigureAwait(false);
                }
                else
                {
                    esSubscription = await Bus.PersistentSubscribeAsync<TEvent>(Subscription.Topic, Subscription.SubscriptionId, Subscription.Settings, _resolvedEventAppeared,
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
