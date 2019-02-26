using System.Threading.Tasks;

namespace EventStore.ClientAPI.AutoSubscribing
{
    public interface IAutoSubscriberConsumeAsync<in T>
    {
        Task ConsumeAsync(T message);
    }
}