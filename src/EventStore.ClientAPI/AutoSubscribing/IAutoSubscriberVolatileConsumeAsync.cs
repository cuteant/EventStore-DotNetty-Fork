using System.Threading.Tasks;

namespace EventStore.ClientAPI.AutoSubscribing
{
  public interface IAutoSubscriberVolatileConsumeAsync
  {
    Task ConsumeAsync(EventStoreSubscription subscription, ResolvedEvent<object> resolvedEvent);
  }

  public interface IAutoSubscriberVolatileConsumeAsync<T> where T : class
  {
    Task ConsumeAsync(EventStoreSubscription subscription, ResolvedEvent<T> resolvedEvent);
  }
}
