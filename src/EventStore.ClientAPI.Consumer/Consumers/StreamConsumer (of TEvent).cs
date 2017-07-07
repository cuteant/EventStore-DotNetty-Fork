using System;
using System.Threading.Tasks;
using CuteAnt;
using EventStore.ClientAPI.Resilience;
using EventStore.ClientAPI.Subscriptions;

namespace EventStore.ClientAPI.Consumers
{
  /// <summary>The abstract base class which supports the different consumer types.</summary>
  public abstract class StreamConsumer<TSubscription, TSettings, TEvent> : DisposeBase, IStreamConsumer
    where TSubscription : class, ISubscription<TSettings>
    where TSettings : SubscriptionSettings
    where TEvent : class
  {
    internal const int ON = 1;
    internal const int OFF = 0;
    internal int _subscribed;

    public IEventStoreConnectionBase2 Connection { get; private set; }
    public TSubscription Subscription { get; private set; }

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

    //set the default RetryPolicy for each subscription - max 5 retries with exponential backoff
    protected static readonly RetryPolicy DefaultRetryPolicy = new RetryPolicy(5.Retries(), retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

    public abstract Task ConnectToSubscriptionAsync();
    public virtual Task ConnectToSubscriptionAsync(long? lastCheckpoint) => ConnectToSubscriptionAsync();

    protected async Task HandleDroppedSubscriptionAsync(DroppedSubscription subscriptionDropped)
    {
      if (this.Disposed) { return; }
      await DroppedSubscriptionPolicy.Handle(subscriptionDropped, async () => await ConnectToSubscriptionAsync());
    }
  }
}
