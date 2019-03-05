using System;
using System.Threading.Tasks;

namespace EventStore.ClientAPI.Consumers
{
    public interface IStreamConsumer : IDisposable
    {
        Task ConnectToSubscriptionAsync();
    }
}
