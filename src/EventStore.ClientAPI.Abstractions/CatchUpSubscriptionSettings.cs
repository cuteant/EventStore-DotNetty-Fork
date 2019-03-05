using System;
using System.ComponentModel;

namespace EventStore.ClientAPI
{
    /// <summary>Settings for <see cref="T:EventStore.ClientAPI.EventStoreCatchUpSubscription"/>.</summary>
    public partial class CatchUpSubscriptionSettings : SubscriptionSettings
    {
#pragma warning disable CS0809 // 过时成员重写未过时成员
        /// <inheritdoc />
        [Obsolete("No longer used")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int BoundedCapacityPerBlock { get => Unbounded; set { } }

        /// <inheritdoc />
        [Obsolete("No longer used")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int MaxDegreeOfParallelismPerBlock { get => DefaultDegreeOfParallelism; set { } }

        /// <inheritdoc />
        [Obsolete("No longer used")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int NumActionBlocks { get => DefaultNumActionBlocks; set { } }
#pragma warning restore CS0809 // 过时成员重写未过时成员

        /// <summary>The maximum amount of events to cache when processing from a live subscription. Going above this value will drop the subscription.</summary>
        public readonly int MaxLiveQueueSize;

        /// <summary>The number of events to read per batch when reading the history.</summary>
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
        /// <param name="maxLiveQueueSize">The maximum amount of events to buffer when processing from a live subscription. Going above this amount will drop the subscription.</param>
        /// <param name="readBatchSize">The number of events to read per batch when reading through history.</param>
        /// <param name="verboseLogging">Enables verbose logging on the subscription.</param>
        /// <param name="resolveLinkTos">Whether to resolve link events.</param>
        /// <param name="subscriptionName">The name of the subscription.</param>
        public CatchUpSubscriptionSettings(int maxLiveQueueSize, int readBatchSize, bool verboseLogging, bool resolveLinkTos, string subscriptionName = "")
        {
            if (readBatchSize <= 0) { ThrowHelper.ThrowArgumentOutOfRangeException_Positive(ExceptionArgument.readBatchSize); }
            if (maxLiveQueueSize <= 0) { ThrowHelper.ThrowArgumentOutOfRangeException_Positive(ExceptionArgument.maxLiveQueueSize); }
            if (readBatchSize > ClientApiConstants.MaxReadSize) { ThrowHelper.ThrowArgumentException_ReadBatchSizeShouldBeLessThanMaxReadSize(); }

            MaxLiveQueueSize = maxLiveQueueSize;
            ReadBatchSize = readBatchSize;
            VerboseLogging = verboseLogging;
            ResolveLinkTos = resolveLinkTos;
            SubscriptionName = subscriptionName;
        }

        /// <summary>Creates a <see cref="CatchUpSubscriptionSettings"/>.</summary>
        /// <param name="resolveLinkTos">Whether to resolve link events.</param>
        /// <param name="subscriptionName">The name of the subscription.</param>
        /// <param name="verboseLogging">Enables verbose logging on the subscription.</param>
        /// <returns></returns>
        public static CatchUpSubscriptionSettings Create(bool resolveLinkTos, string subscriptionName = "", bool verboseLogging = false)
        {
            return Create(Consts.CatchUpDefaultMaxPushQueueSize, Consts.CatchUpDefaultReadBatchSize, resolveLinkTos, subscriptionName, verboseLogging);
        }

        /// <summary>Creates a <see cref="CatchUpSubscriptionSettings"/>.</summary>
        /// <param name="readBatchSize">The number of events to read per batch when reading through history.</param>
        /// <param name="resolveLinkTos">Whether to resolve link events.</param>
        /// <param name="subscriptionName">The name of the subscription.</param>
        /// <param name="verboseLogging">Enables verbose logging on the subscription.</param>
        /// <returns></returns>
        public static CatchUpSubscriptionSettings Create(int readBatchSize, bool resolveLinkTos, string subscriptionName = "", bool verboseLogging = false)
        {
            return Create(Consts.CatchUpDefaultMaxPushQueueSize, readBatchSize, resolveLinkTos, subscriptionName, verboseLogging);
        }

        /// <summary>Creates a <see cref="CatchUpSubscriptionSettings"/>.</summary>
        /// <param name="maxLiveQueueSize">The maximum amount of events to buffer when processing from a live subscription. Going above this amount will drop the subscription.</param>
        /// <param name="readBatchSize">The number of events to read per batch when reading through history.</param>
        /// <param name="resolveLinkTos">Whether to resolve link events.</param>
        /// <param name="subscriptionName">The name of the subscription.</param>
        /// <param name="verboseLogging">Enables verbose logging on the subscription.</param>
        /// <returns></returns>
        public static CatchUpSubscriptionSettings Create(int maxLiveQueueSize, int readBatchSize, bool resolveLinkTos, string subscriptionName = "", bool verboseLogging = false)
        {
            return new CatchUpSubscriptionSettings(maxLiveQueueSize, readBatchSize, verboseLogging, resolveLinkTos, subscriptionName);
        }
    }
}