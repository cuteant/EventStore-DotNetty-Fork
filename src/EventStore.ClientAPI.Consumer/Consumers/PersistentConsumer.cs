﻿using System;
using System.Threading;
using System.Threading.Tasks;
using EventStore.ClientAPI.Common.Utils.Threading;
using EventStore.ClientAPI.Subscriptions;
using Microsoft.Extensions.Logging;

namespace EventStore.ClientAPI.Consumers
{
    /// <summary>Represents the consumer of a persistent subscription to EventStore: http://docs.geteventstore.com/introduction/4.0.0/subscriptions/
    /// This kind of consumer supports the competing consumer messaging pattern: http://www.enterpriseintegrationpatterns.com/patterns/messaging/CompetingConsumers.html </summary>
    public class PersistentConsumer : StreamConsumer<PersistentSubscription, ConnectToPersistentSubscriptionSettings>
    {
        private static readonly ILogger s_logger = TraceLogger.GetLogger<VolatileConsumer>();

        private Func<EventStorePersistentSubscription, ResolvedEvent<object>, int?, Task> _eventAppearedAsync;
        private Action<EventStorePersistentSubscription, ResolvedEvent<object>, int?> _eventAppeared;

        private EventStorePersistentSubscription esSubscription;
        private EventStorePersistentSubscription2 esSubscription2;

        private int _initialized = OFF;

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

        protected override void Initialize(IEventStoreBus bus, PersistentSubscription subscription)
        {
            if (string.IsNullOrEmpty(subscription.SubscriptionId)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.subscription_SubscriptionId); }
            if (subscription.PersistentSettings is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.subscription_PersistentSettings); }

            base.Initialize(bus, subscription);
        }

        public void Initialize(IEventStoreBus bus, PersistentSubscription subscription, Func<EventStorePersistentSubscription, ResolvedEvent<object>, int?, Task> eventAppearedAsync)
        {
            if (eventAppearedAsync is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.eventAppearedAsync); }
            Initialize(bus, subscription);
            _eventAppearedAsync = eventAppearedAsync;
        }

        public void Initialize(IEventStoreBus bus, PersistentSubscription subscription, Action<EventStorePersistentSubscription, ResolvedEvent<object>, int?> eventAppeared)
        {
            if (eventAppeared is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.eventAppeared); }
            Initialize(bus, subscription);
            _eventAppeared = eventAppeared;
        }

        public override async Task ConnectToSubscriptionAsync()
        {
            if (Volatile.Read(ref _subscribed) == ON) { return; }

            if (Interlocked.Exchange(ref _initialized, ON) == OFF)
            {
                try
                {
                    if (string.IsNullOrEmpty(Subscription.Topic))
                    {
                        await Bus.UpdateOrCreatePersistentSubscriptionAsync(Subscription.StreamId, Subscription.SubscriptionId, Subscription.PersistentSettings, Subscription.Credentials).ConfigureAwait(false);
                    }
                    else
                    {
                        await Bus.UpdateOrCreatePersistentSubscriptionAsync(Subscription.StreamId, Subscription.Topic, Subscription.SubscriptionId, Subscription.PersistentSettings, Subscription.Credentials).ConfigureAwait(false);
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

        public virtual async Task ConnectToSubscriptionAsync(long? startFrom)
        {
            if (Volatile.Read(ref _subscribed) == ON) { return; }

            if (!startFrom.HasValue)
            {
                await ConnectToSubscriptionAsync().ConfigureAwait(false);
                return;
            }

            long realStartFrom = StreamPosition.End;
            if (startFrom.Value >= 0L) { realStartFrom = startFrom.Value; }

            if (Interlocked.Exchange(ref _initialized, ON) == OFF)
            {
                try
                {
                    if (string.IsNullOrEmpty(Subscription.Topic))
                    {
                        await Bus.DeletePersistentSubscriptionIfExistsAsync(Subscription.StreamId, Subscription.SubscriptionId, Subscription.Credentials).ConfigureAwait(false);

                        await Bus
                            .CreatePersistentSubscriptionAsync(
                                Subscription.StreamId, Subscription.SubscriptionId,
                                Subscription.PersistentSettings.Clone(realStartFrom), Subscription.Credentials)
                            .ConfigureAwait(false);
                    }
                    else
                    {
                        await Bus.DeletePersistentSubscriptionIfExistsAsync(Subscription.StreamId, Subscription.Topic, Subscription.SubscriptionId, Subscription.Credentials).ConfigureAwait(false);

                        await Bus
                            .CreatePersistentSubscriptionAsync(
                                Subscription.StreamId, Subscription.Topic, Subscription.SubscriptionId,
                                Subscription.PersistentSettings.Clone(realStartFrom), Subscription.Credentials)
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
                if (UsingEventHandlers)
                {
                    if (string.IsNullOrEmpty(Subscription.Topic))
                    {
                        if (RegisterEventHandlers is object)
                        {
                            esSubscription2 = await Bus.PersistentSubscribeAsync(Subscription.StreamId, Subscription.SubscriptionId, Subscription.Settings, RegisterEventHandlers,
                                    async (sub, reason, exception) => await SubscriptionDroppedAsync(sub.ProcessingEventNumber, reason, exception).ConfigureAwait(false),
                                    Subscription.Credentials).ConfigureAwait(false);
                        }
                        else
                        {
                            esSubscription2 = await Bus.PersistentSubscribeAsync(Subscription.StreamId, Subscription.SubscriptionId, Subscription.Settings, RegisterHandlers,
                                    async (sub, reason, exception) => await SubscriptionDroppedAsync(sub.ProcessingEventNumber, reason, exception).ConfigureAwait(false),
                                    Subscription.Credentials).ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        if (RegisterEventHandlers is object)
                        {
                            esSubscription2 = await Bus.PersistentSubscribeAsync(Subscription.StreamId, Subscription.Topic, Subscription.SubscriptionId, Subscription.Settings, RegisterEventHandlers,
                                    async (sub, reason, exception) => await SubscriptionDroppedAsync(sub.ProcessingEventNumber, reason, exception).ConfigureAwait(false),
                                    Subscription.Credentials).ConfigureAwait(false);
                        }
                        else
                        {
                            esSubscription2 = await Bus.PersistentSubscribeAsync(Subscription.StreamId, Subscription.Topic, Subscription.SubscriptionId, Subscription.Settings, RegisterHandlers,
                                    async (sub, reason, exception) => await SubscriptionDroppedAsync(sub.ProcessingEventNumber, reason, exception).ConfigureAwait(false),
                                    Subscription.Credentials).ConfigureAwait(false);
                        }
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(Subscription.Topic))
                    {
                        if (_eventAppearedAsync is object)
                        {
                            esSubscription = await Bus.PersistentSubscribeAsync(Subscription.StreamId, Subscription.SubscriptionId, Subscription.Settings, _eventAppearedAsync,
                                    async (sub, reason, exception) => await SubscriptionDroppedAsync(sub.ProcessingEventNumber, reason, exception).ConfigureAwait(false),
                                    Subscription.Credentials).ConfigureAwait(false);
                        }
                        else
                        {
                            esSubscription = await Bus.PersistentSubscribeAsync(Subscription.StreamId, Subscription.SubscriptionId, Subscription.Settings, _eventAppeared,
                                    async (sub, reason, exception) => await SubscriptionDroppedAsync(sub.ProcessingEventNumber, reason, exception).ConfigureAwait(false),
                                    Subscription.Credentials).ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        if (_eventAppearedAsync is object)
                        {
                            esSubscription = await Bus.PersistentSubscribeAsync(Subscription.StreamId, Subscription.Topic, Subscription.SubscriptionId, Subscription.Settings, _eventAppearedAsync,
                                    async (sub, reason, exception) => await SubscriptionDroppedAsync(sub.ProcessingEventNumber, reason, exception).ConfigureAwait(false),
                                    Subscription.Credentials).ConfigureAwait(false);
                        }
                        else
                        {
                            esSubscription = await Bus.PersistentSubscribeAsync(Subscription.StreamId, Subscription.Topic, Subscription.SubscriptionId, Subscription.Settings, _eventAppeared,
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
