using System;
using System.ComponentModel;

namespace EventStore.ClientAPI
{
    /// <summary>Settings for <see cref="T:EventStore.ClientAPI.EventStoreCatchUpSubscription"/></summary>
    public partial class ConnectToPersistentSubscriptionSettings : SubscriptionSettings
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
            if (bufferSize <= 0) { ThrowHelper.ThrowArgumentOutOfRangeException_Positive(ExceptionArgument.bufferSize); }

            BufferSize = bufferSize;
            AutoAck = autoAck;
            VerboseLogging = verboseLogging;
        }
    }
}