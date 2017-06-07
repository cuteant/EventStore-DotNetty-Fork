using EventStore.ClientAPI.Common.Utils;

namespace EventStore.ClientAPI
{
  /// <summary>Settings for <see cref="T:EventStore.ClientAPI.EventStoreCatchUpSubscription"/></summary>
  public partial class ConnectToPersistentSubscriptionSettings : SubscriptionSettings
  {
    /// <inheritdoc />
    public override int BoundedCapacityPerBlock { get => Unbounded; set { } }

    /// <inheritdoc />
    public override int MaxDegreeOfParallelismPerBlock { get => DefaultDegreeOfParallelism; set { } }

    /// <inheritdoc />
    public override int NumActionBlocks { get => DefaultNumActionBlocks; set { } }

    /// <summary>The buffer size to use for the persistent subscription.</summary>
    public readonly int BufferSize;

    /// <summary>Whether the subscription should automatically acknowledge messages processed.
    /// If not set the receiver is required to explicitly acknowledge messages through the subscription.</summary>
    public readonly bool AutoAck;

    /// <summary>Whether or not to resolve link events.</summary>
    public override bool ResolveLinkTos { get => false; set { } }

    /// <summary>Returns default settings.</summary>
    public new static readonly ConnectToPersistentSubscriptionSettings Default = new ConnectToPersistentSubscriptionSettings();

    /// <summary>Constructs a <see cref="ConnectToPersistentSubscriptionSettings"/> object.</summary>
    /// <param name="bufferSize">The buffer size to use for the persistent subscription</param>
    /// <param name="autoAck">Whether the subscription should automatically acknowledge messages processed.
    /// If not set the receiver is required to explicitly acknowledge messages through the subscription.</param>
    /// <param name="verboseLogging">Enables verbose logging on the subscription</param>
    public ConnectToPersistentSubscriptionSettings(int bufferSize = 10, bool autoAck = true, bool verboseLogging = false)
    {
      Ensure.Positive(bufferSize, nameof(bufferSize));

      BufferSize = bufferSize;
      AutoAck = autoAck;
      VerboseLogging = verboseLogging;
    }
  }
}