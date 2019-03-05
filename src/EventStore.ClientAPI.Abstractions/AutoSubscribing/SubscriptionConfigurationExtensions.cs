using System;
using EventStore.ClientAPI.Resilience;
using EventStore.ClientAPI.SystemData;

namespace EventStore.ClientAPI.AutoSubscribing
{
    internal static class SubscriptionConfigurationExtensions
    {
        public static PersistentSubscriptionSettings ToSettings(this PersistentSubscriptionConfigurationAttribute attr)
        {
            var builder = PersistentSubscriptionSettings.Create();
            if (null == attr) { return builder.Build(); }

            var resolveLinkTos = ConfigUtils.ToNullableBool(attr.ResolveLinkTos);
            if (resolveLinkTos.HasValue)
            {
                if (resolveLinkTos.Value) { builder.ResolveLinkTos(); } else { builder.DoNotResolveLinkTos(); }
            }

            var startFrom = ConfigUtils.ToNullableInt64(attr.StartFrom);
            if (startFrom.HasValue)
            {
                if (startFrom.Value < 0L)
                {
                    builder.StartFromCurrent();
                }
                else if (startFrom.Value == 0L)
                {
                    builder.StartFromBeginning();
                }
                else
                {
                    builder.StartFrom(startFrom.Value);
                }
            }
            else
            {
                builder.StartFromBeginning();
            }

            var extraStatistics = ConfigUtils.ToNullableBool(attr.ExtraStatistics);
            if (extraStatistics.HasValue && extraStatistics.Value) { builder.WithExtraStatistics(); }

            var messageTimeout = ConfigUtils.ToNullableTimeSpan(attr.MessageTimeout);
            if (messageTimeout.HasValue) { builder.WithMessageTimeoutOf(messageTimeout.Value); }

            var maxRetryCount = ConfigUtils.ToNullableInt(attr.MaxRetryCount);
            if (maxRetryCount.HasValue) { builder.WithMaxRetriesOf(maxRetryCount.Value); }

            var liveBufferSize = ConfigUtils.ToNullableInt(attr.LiveBufferSize);
            if (liveBufferSize.HasValue) { builder.WithLiveBufferSizeOf(liveBufferSize.Value); }

            var readBatchSize = ConfigUtils.ToNullableInt(attr.ReadBatchSize);
            if (readBatchSize.HasValue) { builder.WithReadBatchOf(readBatchSize.Value); }

            var historyBufferSize = ConfigUtils.ToNullableInt(attr.HistoryBufferSize);
            if (historyBufferSize.HasValue) { builder.WithBufferSizeOf(historyBufferSize.Value); }

            var checkPointAfter = ConfigUtils.ToNullableTimeSpan(attr.CheckPointAfter);
            if (checkPointAfter.HasValue) { builder.CheckPointAfter(checkPointAfter.Value); }

            var minCheckPointCount = ConfigUtils.ToNullableInt(attr.MinCheckPointCount);
            if (minCheckPointCount.HasValue) { builder.MinimumCheckPointCountOf(minCheckPointCount.Value); }

            var maxCheckPointCount = ConfigUtils.ToNullableInt(attr.MaxCheckPointCount);
            if (maxCheckPointCount.HasValue) { builder.MaximumCheckPointCountOf(maxCheckPointCount.Value); }

            var maxSubscriberCount = ConfigUtils.ToNullableInt(attr.MaxSubscriberCount);
            if (maxSubscriberCount.HasValue) { builder.WithMaxSubscriberCountOf(maxSubscriberCount.Value); }

            if (attr.NamedConsumerStrategy != null) { builder.WithNamedConsumerStrategy(attr.NamedConsumerStrategy); }

            return builder.Build();
        }

        public static SubscriptionSettings ToSettings(this ConnectToVolatileSubscriptionConfigurationAttribute attr)
        {
            var settings = new SubscriptionSettings();
            if (null == attr) { return settings; }

            var maxMessagesPerTask = ConfigUtils.ToNullableInt(attr.MaxMessagesPerTask);
            if (maxMessagesPerTask.HasValue) { settings.MaxMessagesPerTask = maxMessagesPerTask.Value; }

            var boundedCapacityPerBlock = ConfigUtils.ToNullableInt(attr.BoundedCapacityPerBlock);
            if (boundedCapacityPerBlock.HasValue) { settings.BoundedCapacityPerBlock = boundedCapacityPerBlock.Value; }

            var maxDegreeOfParallelismPerBlock = ConfigUtils.ToNullableInt(attr.MaxDegreeOfParallelismPerBlock);
            if (maxDegreeOfParallelismPerBlock.HasValue) { settings.MaxDegreeOfParallelismPerBlock = maxDegreeOfParallelismPerBlock.Value; }

            var numActionBlocks = ConfigUtils.ToNullableInt(attr.NumActionBlocks);
            if (numActionBlocks.HasValue) { settings.NumActionBlocks = numActionBlocks.Value; }

            var resolveLinkTos = ConfigUtils.ToNullableBool(attr.ResolveLinkTos);
            if (resolveLinkTos.HasValue) { settings.ResolveLinkTos = resolveLinkTos.Value; }

            var verboseLogging = ConfigUtils.ToNullableBool(attr.VerboseLogging);
            if (verboseLogging.HasValue) { settings.VerboseLogging = verboseLogging.Value; }

            return settings;
        }

        public static CatchUpSubscriptionSettings ToSettings(this ConnectToCatchUpSubscriptionConfigurationAttribute attr)
        {
            var maxLiveQueueSize = ConfigUtils.ToNullableInt(attr?.MaxLiveQueueSize);
            var readBatchSize = ConfigUtils.ToNullableInt(attr?.ReadBatchSize);
            var resolveLinkTos = ConfigUtils.ToNullableBool(attr?.ResolveLinkTos);
            var verboseLogging = ConfigUtils.ToNullableBool(attr?.VerboseLogging);

            var settings = CatchUpSubscriptionSettings.Create(
                maxLiveQueueSize: maxLiveQueueSize ?? Consts.CatchUpDefaultMaxPushQueueSize,
                readBatchSize: readBatchSize ?? Consts.CatchUpDefaultReadBatchSize,
                resolveLinkTos: resolveLinkTos ?? true,
                subscriptionName: attr?.SubscriptionName ?? string.Empty,
                verboseLogging: verboseLogging ?? false);
            if (null == attr) { return settings; }

            var maxMessagesPerTask = ConfigUtils.ToNullableInt(attr.MaxMessagesPerTask);
            if (maxMessagesPerTask.HasValue) { settings.MaxMessagesPerTask = maxMessagesPerTask.Value; }

            return settings;
        }

        public static ConnectToPersistentSubscriptionSettings ToSettings(this ConnectToPersistentSubscriptionConfigurationAttribute attr)
        {
            var bufferSize = ConfigUtils.ToNullableInt(attr?.BufferSize);
            var autoAck = ConfigUtils.ToNullableBool(attr?.AutoAck);

            var settings = new ConnectToPersistentSubscriptionSettings(
                bufferSize: bufferSize ?? 10,
                autoAck: autoAck ?? true);
            if (null == attr) { return settings; }

            var maxMessagesPerTask = ConfigUtils.ToNullableInt(attr.MaxMessagesPerTask);
            if (maxMessagesPerTask.HasValue) { settings.MaxMessagesPerTask = maxMessagesPerTask.Value; }

            //if (attr.ResolveLinkTos.HasValue) { settings.ResolveLinkTos = attr.ResolveLinkTos.Value; }

            var verboseLogging = ConfigUtils.ToNullableBool(attr.VerboseLogging);
            if (verboseLogging.HasValue) { settings.VerboseLogging = verboseLogging.Value; }

            return settings;
        }

        public static UserCredentials ToCredentials(this AutoSubscriberUserCredentialAttribute attr)
        {
            if (null == attr) { return null; }
            return new UserCredentials(attr.Username, attr.Password);
        }

        public static RetryPolicy ToRetryPolicy(this AutoSubscriberRetryPolicyAttribute attr)
        {
            if (null == attr) { return null; }
            switch (attr.ProviderType)
            {
                case SleepDurationProviderType.FixedDuration:
                    return new RetryPolicy(attr.MaxNoOfRetries, ConfigUtils.ToNullableTimeSpan(attr.FixedSleepDuration).Value);
                case SleepDurationProviderType.ExponentialDuration:
                    return new RetryPolicy(
                        attr.MaxNoOfRetries,
                        ConfigUtils.ToNullableTimeSpan(attr.MinDelay).Value,
                        ConfigUtils.ToNullableTimeSpan(attr.MaxDelay).Value,
                        ConfigUtils.ToNullableTimeSpan(attr.Step).Value,
                        attr.PowerFactor);
                case SleepDurationProviderType.Immediately:
                default:
                    return new RetryPolicy(attr.MaxNoOfRetries);
            }
        }
    }
}
