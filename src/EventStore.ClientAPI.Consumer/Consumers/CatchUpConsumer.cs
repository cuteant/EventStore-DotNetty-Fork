using System;
using System.Threading.Tasks;
using CuteAnt.AsyncEx;
using EventStore.ClientAPI.Subscriptions;
using Microsoft.Extensions.Logging;

namespace EventStore.ClientAPI.Consumers
{
  /// <summary>Represents the consumer of a catch-up subscription to EventStore: http://docs.geteventstore.com/introduction/4.0.0/subscriptions/ </summary>
  public class CatchUpConsumer : StreamConsumer<CatchUpSubscription, CatchUpSubscriptionSettings>
  {
    private static readonly ILogger s_logger = TraceLogger.GetLogger<VolatileConsumer>();


    private Func<EventStoreCatchUpSubscription, ResolvedEvent<object>, Task> _eventAppearedAsync;
    private Action<EventStoreCatchUpSubscription, ResolvedEvent<object>> _eventAppeared;

    public void Initialize(IEventStoreConnectionBase2 connection, CatchUpSubscription subscription, Func<EventStoreCatchUpSubscription, ResolvedEvent<object>, Task> eventAppearedAsync)
    {
      Initialize(connection, subscription);
      _eventAppearedAsync = eventAppearedAsync ?? throw new ArgumentNullException(nameof(eventAppearedAsync));
    }

    public void Initialize(IEventStoreConnectionBase2 connection, CatchUpSubscription subscription, Action<EventStoreCatchUpSubscription, ResolvedEvent<object>> eventAppeared)
    {
      Initialize(connection, subscription);
      _eventAppeared = eventAppeared ?? throw new ArgumentNullException(nameof(eventAppeared));
    }

    public override Task ConnectToSubscriptionAsync() => ConnectToSubscriptionAsync(StreamPosition.Start);
    public override async Task ConnectToSubscriptionAsync(long? lastCheckpoint)
    {
      try
      {
        if (UsingEventHandlers)
        {
          if (string.IsNullOrEmpty(Subscription.Topic))
          {
            if (RegisterEventHandlers != null)
            {
              Connection.CatchUpSubscribe(Subscription.StreamId, StreamPosition.Start, Subscription.Settings, RegisterEventHandlers,
                      _ => s_logger.LogInformation($"Caught up on {_.StreamId} at {DateTime.Now}"),
                      async (sub, reason, exception) => await SubscriptionDroppedAsync(sub, reason, exception).ConfigureAwait(false),
                      Subscription.Credentials);
            }
            else
            {
              Connection.CatchUpSubscribe(Subscription.StreamId, StreamPosition.Start, Subscription.Settings, RegisterHandlers,
                      _ => s_logger.LogInformation($"Caught up on {_.StreamId} at {DateTime.Now}"),
                      async (sub, reason, exception) => await SubscriptionDroppedAsync(sub, reason, exception).ConfigureAwait(false),
                      Subscription.Credentials);
            }
          }
          else
          {
            if (RegisterEventHandlers != null)
            {
              Connection.CatchUpSubscribe(Subscription.StreamId, Subscription.Topic, StreamPosition.Start, Subscription.Settings, RegisterEventHandlers,
                      _ => s_logger.LogInformation($"Caught up on {_.StreamId} at {DateTime.Now}"),
                      async (sub, reason, exception) => await SubscriptionDroppedAsync(sub, reason, exception).ConfigureAwait(false),
                      Subscription.Credentials);
            }
            else
            {
              Connection.CatchUpSubscribe(Subscription.StreamId, Subscription.Topic, StreamPosition.Start, Subscription.Settings, RegisterHandlers,
                      _ => s_logger.LogInformation($"Caught up on {_.StreamId} at {DateTime.Now}"),
                      async (sub, reason, exception) => await SubscriptionDroppedAsync(sub, reason, exception).ConfigureAwait(false),
                      Subscription.Credentials);
            }
          }
        }
        else
        {
          if (string.IsNullOrEmpty(Subscription.Topic))
          {
            if (_eventAppearedAsync != null)
            {
              Connection.CatchUpSubscribe(Subscription.StreamId, StreamPosition.Start, Subscription.Settings, _eventAppearedAsync,
                      _ => s_logger.LogInformation($"Caught up on {_.StreamId} at {DateTime.Now}"),
                      async (sub, reason, exception) => await SubscriptionDroppedAsync(sub, reason, exception).ConfigureAwait(false),
                      Subscription.Credentials);
            }
            else
            {
              Connection.CatchUpSubscribe(Subscription.StreamId, StreamPosition.Start, Subscription.Settings, _eventAppeared,
                      _ => s_logger.LogInformation($"Caught up on {_.StreamId} at {DateTime.Now}"),
                      async (sub, reason, exception) => await SubscriptionDroppedAsync(sub, reason, exception).ConfigureAwait(false),
                      Subscription.Credentials);
            }
          }
          else
          {
            if (_eventAppearedAsync != null)
            {
              Connection.CatchUpSubscribe(Subscription.StreamId, Subscription.Topic, StreamPosition.Start, Subscription.Settings, _eventAppearedAsync,
                      _ => s_logger.LogInformation($"Caught up on {_.StreamId} at {DateTime.Now}"),
                      async (sub, reason, exception) => await SubscriptionDroppedAsync(sub, reason, exception).ConfigureAwait(false),
                      Subscription.Credentials);
            }
            else
            {
              Connection.CatchUpSubscribe(Subscription.StreamId, Subscription.Topic, StreamPosition.Start, Subscription.Settings, _eventAppeared,
                      _ => s_logger.LogInformation($"Caught up on {_.StreamId} at {DateTime.Now}"),
                      async (sub, reason, exception) => await SubscriptionDroppedAsync(sub, reason, exception).ConfigureAwait(false),
                      Subscription.Credentials);
            }
          }
        }
      }
      catch (Exception exc)
      {
        await TaskConstants.Completed;
        s_logger.LogError(exc.ToString());
      }
    }

    private async Task SubscriptionDroppedAsync(EventStoreCatchUpSubscription subscription, SubscriptionDropReason dropReason, Exception exception)
    {
      var subscriptionDropped = new DroppedSubscription(Subscription, exception.Message, dropReason);
      await HandleDroppedSubscriptionAsync(subscriptionDropped).ConfigureAwait(false);
    }

    private async Task SubscriptionDroppedAsync(EventStoreCatchUpSubscription2 subscription, SubscriptionDropReason dropReason, Exception exception)
    {
      var subscriptionDropped = new DroppedSubscription(Subscription, exception.Message, dropReason);
      await HandleDroppedSubscriptionAsync(subscriptionDropped).ConfigureAwait(false);
    }
  }
}
