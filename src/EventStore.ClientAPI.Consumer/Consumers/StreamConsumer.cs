using System;
using System.Threading.Tasks;
using CuteAnt;
using EventStore.ClientAPI.Resilience;
using EventStore.ClientAPI.Subscriptions;

namespace EventStore.ClientAPI.Consumers
{
  /// <summary>The abstract base class which supports the different consumer types.</summary>
  public abstract class StreamConsumer<TSubscription, TSettings> : DisposeBase, IStreamConsumer
    where TSubscription : class, ISubscription<TSettings>
    where TSettings : SubscriptionSettings
  {
    internal const int ON = 1;
    internal const int OFF = 0;
    internal int _subscribed;

    public IEventStoreConnectionBase2 Connection { get; private set; }
    public TSubscription Subscription { get; private set; }

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
