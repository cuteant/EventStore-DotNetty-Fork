using System.Threading.Tasks;

namespace EventStore.ClientAPI.AutoSubscribing
{
  public interface IAutoSubscriberPersistentConsumeAsync
  {
    Task ConsumeAsync(EventStorePersistentSubscription subscription, ResolvedEvent<object> resolvedEvent, int? retryCount);
  }

  public interface IAutoSubscriberPersistentConsumeAsync<T> where T : class
  {
    Task ConsumeAsync(EventStorePersistentSubscription<T> subscription, ResolvedEvent<T> resolvedEvent, int? retryCount);
  }
}
