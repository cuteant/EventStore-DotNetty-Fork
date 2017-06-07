using System;

namespace EventStore.ClientAPI.AutoSubscribe
{
  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
  public class AutoSubscriberConsumerAttribute : Attribute
  {
    public string SubscriptionId { get; set; }
  }
}