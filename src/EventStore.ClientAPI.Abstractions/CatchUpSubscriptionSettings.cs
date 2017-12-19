using System;
using EventStore.ClientAPI.Common.Utils;

namespace EventStore.ClientAPI
{
  /// <summary>Settings for <see cref="T:EventStore.ClientAPI.EventStoreCatchUpSubscription"/></summary>
  public partial class CatchUpSubscriptionSettings : SubscriptionSettings
  {
    /// <inheritdoc />
    public override int BoundedCapacityPerBlock { get => Unbounded; set { } }

    /// <inheritdoc />
    public override int MaxDegreeOfParallelismPerBlock { get => DefaultDegreeOfParallelism; set { } }

    /// <inheritdoc />
    public override int NumActionBlocks { get => DefaultNumActionBlocks; set { } }

    /// <summary>The maximum amount to cache when processing from live subscription. Going above will drop subscription.</summary>
    public readonly int MaxLiveQueueSize;

    /// <summary>The number of events to read per batch when reading history.</summary>
    public readonly int ReadBatchSize;

    /// <summary>The name of subscription.</summary>
    public readonly string SubscriptionName;

    /// <summary>Returns default settings.</summary>
    public new static readonly CatchUpSubscriptionSettings Default = new CatchUpSubscriptionSettings(
        Consts.CatchUpDefaultMaxPushQueueSize,
        Consts.CatchUpDefaultReadBatchSize,
        verboseLogging: false,
        resolveLinkTos: true,
        subscriptionName: String.Empty);

    /// <summary>Constructs a <see cref="CatchUpSubscriptionSettings"/> object.</summary>
    /// <param name="maxLiveQueueSize">The maximum amount to buffer when processing from live subscription. Going above will drop subscription.</param>
    /// <param name="readBatchSize">The number of events to read per batch when reading history</param>
    /// <param name="verboseLogging">Enables verbose logging on the subscription</param>
    /// <param name="resolveLinkTos">Whether or not to resolve link events</param>
    /// <param name="subscriptionName">The name of subscription.</param>
    public CatchUpSubscriptionSettings(int maxLiveQueueSize, int readBatchSize, bool verboseLogging, bool resolveLinkTos, string subscriptionName = "")
    {
      Ensure.Positive(readBatchSize, nameof(readBatchSize));
      Ensure.Positive(maxLiveQueueSize, nameof(maxLiveQueueSize));
      if (readBatchSize > ClientApiConstants.MaxReadSize) { throw new ArgumentException($"Read batch size should be less than {ClientApiConstants.MaxReadSize}. For larger reads you should page."); }

      MaxLiveQueueSize = maxLiveQueueSize;
      ReadBatchSize = readBatchSize;
      VerboseLogging = verboseLogging;
      ResolveLinkTos = resolveLinkTos;
      SubscriptionName = subscriptionName;
    }

    /// <summary>Creates a <see cref="CatchUpSubscriptionSettings"/>.</summary>
    /// <param name="resolveLinkTos">Whether or not to resolve link events</param>
    /// <param name="subscriptionName">The name of subscription.</param>
    /// <param name="verboseLogging">Enables verbose logging on the subscription</param>
    /// <returns></returns>
    public static CatchUpSubscriptionSettings Create(bool resolveLinkTos, string subscriptionName = "", bool verboseLogging = false)
    {
      return Create(Consts.CatchUpDefaultMaxPushQueueSize, Consts.CatchUpDefaultReadBatchSize, resolveLinkTos, subscriptionName, verboseLogging);
    }

    /// <summary>Creates a <see cref="CatchUpSubscriptionSettings"/>.</summary>
    /// <param name="readBatchSize">The number of events to read per batch when reading history</param>
    /// <param name="resolveLinkTos">Whether or not to resolve link events</param>
    /// <param name="subscriptionName">The name of subscription.</param>
    /// <param name="verboseLogging">Enables verbose logging on the subscription</param>
    /// <returns></returns>
    public static CatchUpSubscriptionSettings Create(int readBatchSize, bool resolveLinkTos, string subscriptionName = "", bool verboseLogging = false)
    {
      return Create(Consts.CatchUpDefaultMaxPushQueueSize, readBatchSize, resolveLinkTos, subscriptionName, verboseLogging);
    }

    /// <summary>Creates a <see cref="CatchUpSubscriptionSettings"/>.</summary>
    /// <param name="maxLiveQueueSize">The maximum amount to buffer when processing from live subscription. Going above will drop subscription.</param>
    /// <param name="readBatchSize">The number of events to read per batch when reading history</param>
    /// <param name="resolveLinkTos">Whether or not to resolve link events</param>
    /// <param name="subscriptionName">The name of subscription.</param>
    /// <param name="verboseLogging">Enables verbose logging on the subscription</param>
    /// <returns></returns>
    public static CatchUpSubscriptionSettings Create(int maxLiveQueueSize, int readBatchSize, bool resolveLinkTos, string subscriptionName = "", bool verboseLogging = false)
    {
      return new CatchUpSubscriptionSettings(maxLiveQueueSize, readBatchSize, verboseLogging, resolveLinkTos, subscriptionName);
    }
  }
}