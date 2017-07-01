namespace EventStore.ClientAPI.AutoSubscribing
{
  public interface IAutoSubscriberConsumerRegistration
  {
    void RegisterConsumers(IConsumerRegistration registration);
  }
}
