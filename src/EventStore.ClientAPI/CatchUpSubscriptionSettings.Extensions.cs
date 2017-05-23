using System;

namespace EventStore.ClientAPI
{
  public partial class CatchUpSubscriptionSettings
  {
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