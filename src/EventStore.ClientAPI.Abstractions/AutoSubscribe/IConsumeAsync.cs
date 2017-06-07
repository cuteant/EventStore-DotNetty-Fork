using System.Threading.Tasks;

namespace EventStore.ClientAPI.AutoSubscribe
{
  public interface IConsumeAsync<in T> where T : class
  {
    Task Consume(T message);
  }
}