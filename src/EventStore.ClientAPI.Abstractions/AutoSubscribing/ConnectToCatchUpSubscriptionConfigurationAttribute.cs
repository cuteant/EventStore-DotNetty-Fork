using System;

namespace EventStore.ClientAPI.AutoSubscribing
{
  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = false)]
  public class ConnectToCatchUpSubscriptionConfigurationAttribute : Attribute
  {
    /// <summary>Gets or sets the maximum number of messages that may be processed per task.</summary>
    public Int32? MaxMessagesPerTask { get; set; }

    /// <summary>Whether or not to resolve link events.</summary>
    public bool? ResolveLinkTos { get; set; }

    /// <summary>Enables verbose logging on the subscription.</summary>
    public bool? VerboseLogging { get; set; }

    /// <summary>The maximum amount to cache when processing from live subscription. Going above will drop subscription.</summary>
    public int? MaxLiveQueueSize { get; set; }

    /// <summary>The number of events to read per batch when reading history.</summary>
    public int? ReadBatchSize { get; set; }

    /// <summary>The name of subscription.</summary>
    public string SubscriptionName { get; set; }
  }
}
