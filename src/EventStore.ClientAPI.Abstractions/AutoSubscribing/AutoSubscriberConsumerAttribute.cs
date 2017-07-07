using System;

namespace EventStore.ClientAPI.AutoSubscribing
{
  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = false)]
  public class AutoSubscriberConsumerAttribute : Attribute
  {
    //public string StreamId { get; set; }

    public string SubscriptionId { get; set; }

    public SubscriptionType Subscription { get; set; } = SubscriptionType.Persistent;
  }
}