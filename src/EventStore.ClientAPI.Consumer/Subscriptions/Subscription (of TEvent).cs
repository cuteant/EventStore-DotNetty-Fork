using System;
using EventStore.ClientAPI.Resilience;
using EventStore.ClientAPI.SystemData;

namespace EventStore.ClientAPI.Subscriptions
{
  /// <summary>Represents a subscription to EventStore incl. StreamId, SubscriptionId and RetryPolicy.</summary>
  public abstract class Subscription<TSubscription, TSettings, TEvent> : ISubscription<TSettings>, ISubscription
    where TSubscription : Subscription<TSubscription, TSettings>
    where TSettings : SubscriptionSettings
    where TEvent : class
  {
    public Subscription(string subscriptionId)
    {
      SubscriptionId = subscriptionId;
    }

    public string StreamId => typeof(TEvent).ToStreamId(Topic);
    public string SubscriptionId { get; }
    public string Topic { get; set; }

    public TSettings Settings { get; set; }
    public StreamMetadata StreamMeta { get; set; }
    public PersistentSubscriptionSettings PersistentSettings { get; set; }

    public RetryPolicy RetryPolicy { get; set; }
    public UserCredentials Credentials { get; set; }

    public TSubscription SetRetryPolicy(params TimeSpan[] durations)
    {
      RetryPolicy = new RetryPolicy(durations);
      return this as TSubscription;
    }

    public TSubscription SetRetryPolicy(Retries maxNoOfRetries, Func<int, TimeSpan> provider)
    {
      RetryPolicy = new RetryPolicy(maxNoOfRetries, provider);
      return this as TSubscription;
    }
  }
}