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
    protected virtual void Initialize(IEventStoreConnectionBase2 connection, TSubscription subscription)
    {
      Connection = connection ?? throw new ArgumentNullException(nameof(connection));
      Subscription = subscription ?? throw new ArgumentNullException(nameof(subscription));
      if (string.IsNullOrEmpty(Subscription.StreamId)) { throw new ArgumentNullException(nameof(Subscription.StreamId)); }
      if (null == Subscription.Settings) { throw new ArgumentNullException(nameof(Subscription.Settings)); }

      if (subscription.RetryPolicy == null) { subscription.RetryPolicy = DefaultRetryPolicy; }
      if (subscription.StreamMeta != null)
      {
        if (string.IsNullOrEmpty(Subscription.Topic))
        {
          connection.SetStreamMetadata(Subscription.StreamId, ExpectedVersion.Any, Subscription.StreamMeta, Subscription.Credentials);
        }
        else
        {
          connection.SetStreamMetadata(Subscription.StreamId, Subscription.Topic, ExpectedVersion.Any, Subscription.StreamMeta, Subscription.Credentials);
        }
      }
    }

    public void Initialize(IEventStoreConnectionBase2 connection, TSubscription subscription, Action<IHandlerRegistration> registerEventHandlers)
    {
      Initialize(connection, subscription);
      RegisterEventHandlers = registerEventHandlers ?? throw new ArgumentNullException(nameof(registerEventHandlers));
      UsingEventHandlers = true;
    }

    public void Initialize(IEventStoreConnectionBase2 connection, TSubscription subscription, Action<IConsumerRegistration> registerHandlers)
    {
      Initialize(connection, subscription);
      RegisterHandlers = registerHandlers ?? throw new ArgumentNullException(nameof(registerHandlers));
      UsingEventHandlers = true;
    }
  }
}
