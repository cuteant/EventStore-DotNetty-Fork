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
    internal const int ON = 1;
    internal const int OFF = 0;
    internal int _subscribed;

    private static readonly RetryPolicy s_retryPolicy = new RetryPolicy(RetryPolicy.Unbounded, TimeSpan.FromMilliseconds(200));
    protected static readonly RetryPolicy DefaultRetryPolicy = new RetryPolicy(RetryPolicy.Unbounded, TimeSpan.FromMilliseconds(1000), TimeSpan.FromHours(2), TimeSpan.FromMilliseconds(1000), 1.2D);
    private long _processingEventNumber = StreamPosition.End;
    private int _retryAttempts = 0;

    public IEventStoreConnectionBase2 Connection { get; protected set; }
    public TSubscription Subscription { get; protected set; }

    public abstract Task ConnectToSubscriptionAsync();
    public virtual Task ConnectToSubscriptionAsync(long? lastCheckpoint) => ConnectToSubscriptionAsync();

    protected async Task<bool> CanRetryAsync(long eventNumber, SubscriptionDropReason dropReason)
    {
      if (_processingEventNumber != eventNumber)
      {
        Interlocked.Exchange(ref _retryAttempts, 0);
        Interlocked.Exchange(ref _processingEventNumber, eventNumber);
      }
      if (dropReason == SubscriptionDropReason.EventHandlerException)
      {
        Interlocked.Increment(ref _retryAttempts);
        var retryPolicy = Subscription.RetryPolicy;
        if (retryPolicy.MaxNoOfRetries != RetryPolicy.Unbounded && _retryAttempts > retryPolicy.MaxNoOfRetries)
        {
          return false;
        }
        var sleepDurationProvider = retryPolicy.SleepDurationProvider;
        if (sleepDurationProvider != null)
        {
          var delay = sleepDurationProvider(_retryAttempts);
          await Task.Delay(delay).ConfigureAwait(false);
        }
      }
      return true;
    }

    protected async Task HandleDroppedSubscriptionAsync(DroppedSubscription subscriptionDropped)
    {
      if (this.Disposed) { return; }
      await DroppedSubscriptionPolicy.Handle(subscriptionDropped, async () => await ConnectToSubscriptionAsync(), s_retryPolicy);
    }

  }
}
