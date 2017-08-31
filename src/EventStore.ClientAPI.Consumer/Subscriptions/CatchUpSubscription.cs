﻿namespace EventStore.ClientAPI.Subscriptions
{
  /// <summary>Represents a catch-up subscription to EventStore.</summary>
  public class CatchUpSubscription : Subscription<CatchUpSubscription, CatchUpSubscriptionSettings>
  {
    public CatchUpSubscription(string streamId) : base(streamId, default(string)) { }
  }
}