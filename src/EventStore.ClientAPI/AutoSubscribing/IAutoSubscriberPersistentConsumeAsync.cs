using System.Threading.Tasks;

namespace EventStore.ClientAPI.AutoSubscribing
{
  public interface IAutoSubscriberPersistentConsumeAsync
  {
    Task Consume(EventStorePersistentSubscription subscription, ResolvedEvent<object> resolvedEvent);
  }

  public interface IAutoSubscriberPersistentConsumeAsync<T> where T : class
  {
    Task Consume(EventStorePersistentSubscription<T> subscription, ResolvedEvent<T> resolvedEvent);
  }
}
