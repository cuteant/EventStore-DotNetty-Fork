using System;
using System.Threading;
using System.Threading.Tasks;
using CuteAnt;
using EventStore.ClientAPI.Resilience;
using EventStore.ClientAPI.Subscriptions;

namespace EventStore.ClientAPI.Consumers
{
    public abstract class StreamConsumerBase<TSubscription, TSettings> : DisposeBase, IStreamConsumer
        where TSubscription : class, ISubscription<TSettings>
        where TSettings : SubscriptionSettings
    {
        protected const int ON = 1;
        protected const int OFF = 0;
        protected int _subscribed;

        protected static readonly RetryPolicy DefaultRetryPolicy = new RetryPolicy(RetryPolicy.Unbounded, TimeSpan.FromMilliseconds(1000), TimeSpan.FromHours(2), TimeSpan.FromMilliseconds(1000), 1.2D);
        private long _lastProcessingEventNumber = StreamPosition.End;
        private int _retryAttempts = 0;

        protected long LastProcessingEventNumber => Volatile.Read(ref _lastProcessingEventNumber);

        public IEventStoreBus Bus { get; protected set; }
        public TSubscription Subscription { get; protected set; }

        public abstract Task ConnectToSubscriptionAsync();

        protected virtual async Task<bool> CanRetryAsync(long eventNumber, SubscriptionDropReason dropReason)
        {
            if (Volatile.Read(ref _lastProcessingEventNumber) != eventNumber)
            {
                Interlocked.Exchange(ref _retryAttempts, 0);
                Interlocked.Exchange(ref _lastProcessingEventNumber, eventNumber);
            }
            if (dropReason == SubscriptionDropReason.EventHandlerException)
            {
                var retryAttempts = Interlocked.Increment(ref _retryAttempts);
                var retryPolicy = Subscription.RetryPolicy;
                if (retryPolicy.MaxNoOfRetries != RetryPolicy.Unbounded && retryAttempts > retryPolicy.MaxNoOfRetries)
                {
                    return false;
                }
                var sleepDurationProvider = retryPolicy.SleepDurationProvider;
                if (sleepDurationProvider != null)
                {
                    var delay = sleepDurationProvider(retryAttempts);
                    await Task.Delay(delay).ConfigureAwait(false);
                }
            }
            return true;
        }

        protected virtual async Task SubscriptionDroppedAsync(long lastEventNum, SubscriptionDropReason dropReason, Exception exception)
        {
            Interlocked.Exchange(ref _subscribed, OFF);

            if (await CanRetryAsync(lastEventNum, dropReason).ConfigureAwait(false))
            {
                var subscriptionDropped = new DroppedSubscription(Subscription, exception, dropReason);
                await HandleDroppedSubscriptionAsync(subscriptionDropped).ConfigureAwait(false);
            }
        }

        protected virtual async Task HandleDroppedSubscriptionAsync(DroppedSubscription subscriptionDropped)
        {
            if (this.Disposed) { return; }
            await DroppedSubscriptionPolicy.Handle(subscriptionDropped, async () => await ConnectToSubscriptionAsync(), subscriptionDropped.RetryPolicy ?? DefaultRetryPolicy);
        }
    }
}
