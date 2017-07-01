using System.Threading.Tasks;

namespace EventStore.ClientAPI.AutoSubscribing
{
  public interface IAutoSubscriberCatchUpConsumeAsync
  {
    Task Consume(EventStoreCatchUpSubscription subscription, ResolvedEvent<object> resolvedEvent);
  }

  public interface IAutoSubscriberCatchUpConsumeAsync<T> where T : class
  {
    Task Consume(EventStoreCatchUpSubscription<T> subscription, ResolvedEvent<T> resolvedEvent);
  }
}
