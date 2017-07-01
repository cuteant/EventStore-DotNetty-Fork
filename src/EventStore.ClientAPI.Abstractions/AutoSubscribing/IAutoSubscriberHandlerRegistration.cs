namespace EventStore.ClientAPI.AutoSubscribing
{
  public interface IAutoSubscriberHandlerRegistration
  {
    void RegisterHandlers(IHandlerRegistration registration);
  }
}
