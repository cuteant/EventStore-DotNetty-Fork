using System.Threading.Tasks;

namespace EventStore.ClientAPI.AutoSubscribing
{
  public interface IAutoSubscriberConsumeAsync<in T> where T : class
  {
    Task ConsumeAsync(T message);
  }
}