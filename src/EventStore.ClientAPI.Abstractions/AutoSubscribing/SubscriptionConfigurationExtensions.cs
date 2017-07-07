using System;
using EventStore.ClientAPI.SystemData;

namespace EventStore.ClientAPI.AutoSubscribing
{
  internal static class SubscriptionConfigurationExtensions
  {
    public static PersistentSubscriptionSettings ToSettings(this PersistentSubscriptionConfigurationAttribute attr)
    {
      var builder = PersistentSubscriptionSettings.Create();
      if (null == attr) { return builder.Build(); }

      if (attr.ResolveLinkTos.HasValue)
      {
        if (attr.ResolveLinkTos.Value) { builder.ResolveLinkTos(); } else { builder.DoNotResolveLinkTos(); }
      }

      if (attr.StartFrom.HasValue) { builder.StartFrom(attr.StartFrom.Value); }

      if (attr.ExtraStatistics.HasValue && attr.ExtraStatistics.Value) { builder.WithExtraStatistics(); }

      if (attr.MessageTimeout.HasValue) { builder.WithMessageTimeoutOf(attr.MessageTimeout.Value); }

      if (attr.MaxRetryCount.HasValue) { builder.WithMaxRetriesOf(attr.MaxRetryCount.Value); }

      if (attr.LiveBufferSize.HasValue) { builder.WithLiveBufferSizeOf(attr.LiveBufferSize.Value); }

      if (attr.ReadBatchSize.HasValue) { builder.WithReadBatchOf(attr.ReadBatchSize.Value); }

      if (attr.HistoryBufferSize.HasValue) { builder.WithBufferSizeOf(attr.HistoryBufferSize.Value); }

      if (attr.CheckPointAfter.HasValue) { builder.CheckPointAfter(attr.CheckPointAfter.Value); }

      if (attr.MinCheckPointCount.HasValue) { builder.MinimumCheckPointCountOf(attr.MinCheckPointCount.Value); }

      if (attr.MaxCheckPointCount.HasValue) { builder.MaximumCheckPointCountOf(attr.MaxCheckPointCount.Value); }

      if (attr.MaxSubscriberCount.HasValue) { builder.WithMaxSubscriberCountOf(attr.MaxSubscriberCount.Value); }

      if (attr.NamedConsumerStrategy != null) { builder.WithNamedConsumerStrategy(attr.NamedConsumerStrategy); }

      return builder.Build();
    }

    public static SubscriptionSettings ToSettings(this ConnectToVolatileSubscriptionConfigurationAttribute attr)
    {
      var settings = new SubscriptionSettings();
      if (null == attr) { return settings; }

      if (attr.MaxMessagesPerTask.HasValue) { settings.MaxMessagesPerTask = attr.MaxMessagesPerTask.Value; }

      if (attr.BoundedCapacityPerBlock.HasValue) { settings.BoundedCapacityPerBlock = attr.BoundedCapacityPerBlock.Value; }

      if (attr.MaxDegreeOfParallelismPerBlock.HasValue) { settings.MaxDegreeOfParallelismPerBlock = attr.MaxDegreeOfParallelismPerBlock.Value; }

      if (attr.NumActionBlocks.HasValue) { settings.NumActionBlocks = attr.NumActionBlocks.Value; }

      if (attr.ResolveLinkTos.HasValue) { settings.ResolveLinkTos = attr.ResolveLinkTos.Value; }

      if (attr.VerboseLogging.HasValue) { settings.VerboseLogging = attr.VerboseLogging.Value; }

      return settings;
    }

    public static CatchUpSubscriptionSettings ToSettings(this ConnectToCatchUpSubscriptionConfigurationAttribute attr)
    {
      var settings = CatchUpSubscriptionSettings.Create(
          maxLiveQueueSize: attr?.MaxLiveQueueSize ?? Consts.CatchUpDefaultMaxPushQueueSize,
          readBatchSize: attr?.ReadBatchSize ?? Consts.CatchUpDefaultReadBatchSize,
          resolveLinkTos: attr?.ResolveLinkTos ?? true,
          subscriptionName: attr?.SubscriptionName ?? string.Empty,
          verboseLogging: attr?.VerboseLogging ?? false);
      if (null == attr) { return settings; }

      if (attr.MaxMessagesPerTask.HasValue) { settings.MaxMessagesPerTask = attr.MaxMessagesPerTask.Value; }

      //if (attr.ResolveLinkTos.HasValue) { settings.ResolveLinkTos = attr.ResolveLinkTos.Value; }

      //if (attr.VerboseLogging.HasValue) { settings.VerboseLogging = attr.VerboseLogging.Value; }

      return settings;
    }

    public static ConnectToPersistentSubscriptionSettings ToSettings(this ConnectToPersistentSubscriptionConfigurationAttribute attr)
    {
      var settings = new ConnectToPersistentSubscriptionSettings(
          bufferSize: attr?.BufferSize ?? 10,
          autoAck: attr?.AutoAck ?? true);
      if (null == attr) { return settings; }

      if (attr.MaxMessagesPerTask.HasValue) { settings.MaxMessagesPerTask = attr.MaxMessagesPerTask.Value; }

      //if (attr.ResolveLinkTos.HasValue) { settings.ResolveLinkTos = attr.ResolveLinkTos.Value; }

      if (attr.VerboseLogging.HasValue) { settings.VerboseLogging = attr.VerboseLogging.Value; }

      return settings;
    }

    public static UserCredentials ToCredentials(this AutoSubscriberUserCredentialAttribute attr)
    {
      if (null == attr) { return null; }
      return new UserCredentials(attr.Username, attr.Password);
    }
  }
}
