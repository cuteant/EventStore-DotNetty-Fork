using System;

namespace EventStore.ClientAPI.AutoSubscribing
{
  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = false)]
  public class ConnectToVolatileSubscriptionConfigurationAttribute : Attribute
  {
    /// <summary>Gets or sets the maximum number of messages that may be processed per task.</summary>
    public Int32? MaxMessagesPerTask { get; set; }

    /// <summary>Gets or sets the maximum number of messages that may be buffered by the block.</summary>
    public Int32? BoundedCapacityPerBlock { get; set; }

    /// <summary>Gets the maximum number of messages that may be processed by the block concurrently.</summary>
    public Int32? MaxDegreeOfParallelismPerBlock { get; set; }

    public Int32? NumActionBlocks { get; set; }

    /// <summary>Whether or not to resolve link events.</summary>
    public bool? ResolveLinkTos { get; set; }

    /// <summary>Enables verbose logging on the subscription.</summary>
    public bool? VerboseLogging { get; set; }
  }
}
