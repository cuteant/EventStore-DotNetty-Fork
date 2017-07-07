using System.Threading.Tasks;

namespace EventStore.ClientAPI.AutoSubscribing
{
  public interface IAutoSubscriberCatchUpConsumeAsync
  {
    Task ConsumeAsync(EventStoreCatchUpSubscription subscription, ResolvedEvent<object> resolvedEvent);
  }

  public interface IAutoSubscriberCatchUpConsumeAsync<T> where T : class
  {
    Task ConsumeAsync(EventStoreCatchUpSubscription<T> subscription, ResolvedEvent<T> resolvedEvent);
  }
}
