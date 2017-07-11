using System;
using EventStore.ClientAPI.Subscriptions;

namespace EventStore.ClientAPI.Consumers
{
  /// <summary>The abstract base class which supports the different consumer types.</summary>
  public abstract class StreamConsumer<TSubscription, TSettings, TEvent> : StreamConsumerBase<TSubscription, TSettings>
    where TSubscription : class, ISubscription<TSettings>
    where TSettings : SubscriptionSettings
    where TEvent : class
  {
    /// <summary>Initializes the external serializer. Called once when the serialization manager creates 
    /// an instance of this type</summary>
    protected virtual void Initialize(IEventStoreConnectionBase2 connection, TSubscription subscription)
    {
      Connection = connection ?? throw new ArgumentNullException(nameof(connection));
      Subscription = subscription ?? throw new ArgumentNullException(nameof(subscription));
      if (null == Subscription.Settings) { throw new ArgumentNullException(nameof(Subscription.Settings)); }

      if (subscription.RetryPolicy == null) { subscription.RetryPolicy = DefaultRetryPolicy; }
      if (subscription.StreamMeta != null)
      {
        if (string.IsNullOrEmpty(Subscription.Topic))
        {
          connection.SetStreamMetadata<TEvent>(ExpectedVersion.Any, Subscription.StreamMeta, userCredentials: Subscription.Credentials);
        }
        else
        {
          connection.SetStreamMetadata<TEvent>(Subscription.Topic, ExpectedVersion.Any, Subscription.StreamMeta, userCredentials: Subscription.Credentials);
        }
      }
    }
  }
}
