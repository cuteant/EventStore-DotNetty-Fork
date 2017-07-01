using System.Threading.Tasks;

namespace EventStore.ClientAPI.AutoSubscribing
{
  public interface IAutoSubscriberVolatileConsumeAsync
  {
    Task Consume(EventStoreSubscription subscription, ResolvedEvent<object> resolvedEvent);
  }

  public interface IAutoSubscriberVolatileConsumeAsync<T> where T : class
  {
    Task Consume(EventStoreSubscription subscription, ResolvedEvent<T> resolvedEvent);
  }
}
