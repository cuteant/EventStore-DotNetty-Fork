using System;
using EventStore.ClientAPI.Subscriptions;

namespace EventStore.ClientAPI.Consumers
{
    /// <summary>The abstract base class which supports the different consumer types.</summary>
    public abstract class StreamConsumer<TSubscription, TSettings, TEvent> : StreamConsumerBase<TSubscription, TSettings>
        where TSubscription : class, ISubscription<TSettings>
        where TSettings : SubscriptionSettings
    {
        /// <summary>Initializes the external serializer. Called once when the serialization manager creates 
        /// an instance of this type</summary>
        protected virtual void Initialize(IEventStoreBus bus, TSubscription subscription)
        {
            if (bus is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }
            if (subscription is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.subscription); }
            if (subscription.Settings is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.subscription_Settings); }

            Bus = bus;
            Subscription = subscription;

            if (subscription.RetryPolicy is null) { subscription.RetryPolicy = DefaultRetryPolicy; }
            if (subscription.StreamMeta is object)
            {
                if (string.IsNullOrEmpty(Subscription.Topic))
                {
                    bus.SetStreamMetadata<TEvent>(ExpectedVersion.Any, Subscription.StreamMeta, userCredentials: Subscription.Credentials);
                }
                else
                {
                    bus.SetStreamMetadata<TEvent>(Subscription.Topic, ExpectedVersion.Any, Subscription.StreamMeta, userCredentials: Subscription.Credentials);
                }
            }
        }
    }
}
