using System;

namespace EventStore.ClientAPI.AutoSubscribing
{
  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = false)]
  public class AutoSubscriberRetryPolicyAttribute : Attribute
  {
  }
}
