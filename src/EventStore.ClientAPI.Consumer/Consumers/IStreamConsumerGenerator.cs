using System;
using System.Reflection;
using EventStore.ClientAPI.AutoSubscribing;

namespace EventStore.ClientAPI.Consumers
{
  public interface IStreamConsumerGenerator
  {
    IEventStoreConnectionBase2 Connection { get; set; }
    Func<AutoSubscriberConsumerInfo, string> GenerateSubscriptionId { get; set; }
    Func<string, string> CombineSubscriptionId { get; set; }

    IStreamConsumer CreateConsumer(SubscriptionType subscription, AutoSubscriberConsumerInfo consumerInfo, MethodInfo consumeMethod, object concreteConsumer, string topic = null);

    IStreamConsumer CreateAsyncConsumer(SubscriptionType subscription, AutoSubscriberConsumerInfo consumerInfo, MethodInfo consumeMethod, object concreteConsumer, string topic = null);

    IStreamConsumer CreateResolvedEventConsumer(SubscriptionType subscription, AutoSubscriberConsumerInfo consumerInfo, MethodInfo consumeMethod, object concreteConsumer, string topic = null);

    IStreamConsumer CreateAsyncResolvedEventConsumer(SubscriptionType subscription, AutoSubscriberConsumerInfo consumerInfo, MethodInfo consumeMethod, object concreteConsumer, string topic = null);
  }
}
