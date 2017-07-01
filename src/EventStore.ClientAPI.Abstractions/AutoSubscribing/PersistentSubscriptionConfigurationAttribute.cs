using System;

namespace EventStore.ClientAPI.AutoSubscribing
{
  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = false)]
  public class PersistentSubscriptionConfigurationAttribute : Attribute
  {
    /// <summary>Whether or not the <see cref="T:EventStore.ClientAPI.PersistentEventStoreSubscription"/> should resolve linkTo events to their linked events.</summary>
    public bool? ResolveLinkTos { get; set; }

    /// <summary>Where the subscription should start from (position).</summary>
    public long? StartFrom { get; set; }

    /// <summary>Whether or not in depth latency statistics should be tracked on this subscription.</summary>
    public bool? ExtraStatistics { get; set; }

    /// <summary>The amount of time after which a message should be considered to be timedout and retried.</summary>
    public TimeSpan? MessageTimeout { get; set; }

    /// <summary>The maximum number of retries (due to timeout) before a message get considered to be parked.</summary>
    public int? MaxRetryCount { get; set; }

    /// <summary>The size of the buffer listening to live messages as they happen.</summary>
    public int? LiveBufferSize { get; set; }

    /// <summary>The number of events read at a time when paging in history.</summary>
    public int? ReadBatchSize { get; set; }

    /// <summary>The number of events to cache when paging through history.</summary>
    public int? HistoryBufferSize { get; set; }

    /// <summary>The amount of time to try to checkpoint after.</summary>
    public TimeSpan? CheckPointAfter { get; set; }

    /// <summary>The minimum number of messages to checkpoint.</summary>
    public int? MinCheckPointCount { get; set; }

    /// <summary>The maximum number of messages to checkpoint if this number is a reached a checkpoint will be forced.</summary>
    public int? MaxCheckPointCount { get; set; }

    /// <summary>The maximum number of subscribers allowed.</summary>
    public int? MaxSubscriberCount { get; set; }

    /// <summary>The strategy to use for distributing events to client consumers. See <see cref="EventStore.ClientAPI.Common.SystemConsumerStrategies"/> for system supported strategies.</summary>
    public string NamedConsumerStrategy { get; set; } = EventStore.ClientAPI.Common.SystemConsumerStrategies.RoundRobin;
  }
}
