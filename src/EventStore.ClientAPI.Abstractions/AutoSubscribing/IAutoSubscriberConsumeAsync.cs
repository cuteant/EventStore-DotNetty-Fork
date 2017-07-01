using System.Threading.Tasks;

namespace EventStore.ClientAPI.AutoSubscribing
{
  public interface IAutoSubscriberConsumeAsync<in T> where T : class
  {
    Task Consume(T message);
  }
}