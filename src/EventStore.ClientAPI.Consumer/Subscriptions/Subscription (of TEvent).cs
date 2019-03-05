using System;
using EventStore.ClientAPI.Resilience;
using EventStore.ClientAPI.SystemData;

namespace EventStore.ClientAPI.Subscriptions
{
    /// <summary>Represents a subscription to EventStore incl. StreamId, SubscriptionId and RetryPolicy.</summary>
    public abstract class Subscription<TSubscription, TSettings, TEvent> : ISubscription<TSettings>, ISubscription
        where TSubscription : Subscription<TSubscription, TSettings, TEvent>
        where TSettings : SubscriptionSettings
    {
        public Subscription() { }

        public string StreamId => EventManager.GetStreamId<TEvent>(Topic);
        public string Topic { get; set; }

        public TSettings Settings { get; set; }
        public StreamMetadata StreamMeta { get; set; }

        public RetryPolicy RetryPolicy { get; set; }
        public UserCredentials Credentials { get; set; }

        public TSubscription SetRetryPolicy(int maxNoOfRetries, TimeSpan duration)
        {
            RetryPolicy = new RetryPolicy(maxNoOfRetries, duration);
            return this as TSubscription;
        }

        public TSubscription SetRetryPolicy(int maxNoOfRetries, TimeSpan minDelay, TimeSpan maxDelay, TimeSpan step, double powerFactor)
        {
            RetryPolicy = new RetryPolicy(maxNoOfRetries, minDelay, maxDelay, step, powerFactor);
            return this as TSubscription;
        }

        public TSubscription SetRetryPolicy(int maxNoOfRetries, Func<int, TimeSpan> provider)
        {
            RetryPolicy = new RetryPolicy(maxNoOfRetries, provider);
            return this as TSubscription;
        }
    }
}