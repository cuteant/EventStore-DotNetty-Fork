using System.Threading.Tasks;

namespace EventStore.ClientAPI.Consumers
{
  public interface IStreamConsumer
  {
    Task ConnectToSubscriptionAsync();
    Task ConnectToSubscriptionAsync(long? lastCheckpoint);
  }
}
