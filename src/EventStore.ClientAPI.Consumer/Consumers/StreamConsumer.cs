using System;
using EventStore.ClientAPI.Subscriptions;

namespace EventStore.ClientAPI.Consumers
{
    /// <summary>The abstract base class which supports the different consumer types.</summary>
    public abstract class StreamConsumer<TSubscription, TSettings> : StreamConsumerBase<TSubscription, TSettings>
        where TSubscription : class, ISubscription<TSettings>
        where TSettings : SubscriptionSettings
    {
        protected Action<IHandlerRegistration> RegisterEventHandlers { get; private set; }
        protected Action<IConsumerRegistration> RegisterHandlers { get; private set; }
        protected bool UsingEventHandlers { get; private set; }

        /// <summary>Initializes the external serializer. Called once when the serialization manager creates 
        /// an instance of this type</summary>
        protected virtual void Initialize(IEventStoreBus bus, TSubscription subscription)
        {
            if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }
            if (null == subscription) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.subscription); }
            if (string.IsNullOrEmpty(subscription.StreamId)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.subscription_StreamId); }
            if (null == subscription.Settings) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.subscription_Settings); }

            Bus = bus;
            Subscription = subscription;

            if (subscription.RetryPolicy == null) { subscription.RetryPolicy = DefaultRetryPolicy; }
            if (subscription.StreamMeta != null)
            {
                if (string.IsNullOrEmpty(Subscription.Topic))
                {
                    bus.SetStreamMetadata(Subscription.StreamId, ExpectedVersion.Any, Subscription.StreamMeta, Subscription.Credentials);
                }
                else
                {
                    bus.SetStreamMetadata(Subscription.StreamId, Subscription.Topic, ExpectedVersion.Any, Subscription.StreamMeta, Subscription.Credentials);
                }
            }
        }

        public void Initialize(IEventStoreBus bus, TSubscription subscription, Action<IHandlerRegistration> addEventHandlers)
        {
            if (null == addEventHandlers) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.addEventHandlers); }
            if (RegisterHandlers != null) { ConsumerThrowHelper.ThrowArgumentException_HandlerAlreadyRegistered(); }
            Initialize(bus, subscription);
            RegisterEventHandlers = addEventHandlers;
            UsingEventHandlers = true;
        }

        public void Initialize(IEventStoreBus bus, TSubscription subscription, Action<IConsumerRegistration> addHandlers)
        {
            if (null == addHandlers) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.addHandlers); }
            if (RegisterEventHandlers != null) { ConsumerThrowHelper.ThrowArgumentException_HandlerAlreadyRegistered(); }
            Initialize(bus, subscription);
            RegisterHandlers = addHandlers;
            UsingEventHandlers = true;
        }
    }
}
