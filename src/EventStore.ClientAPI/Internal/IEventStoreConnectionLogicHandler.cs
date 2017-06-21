using System;
using System.Threading.Tasks;

namespace EventStore.ClientAPI.Internal
{
  internal interface IEventStoreConnectionLogicHandler
  {
    int TotalOperationCount { get; }
    void EnqueueMessage(Message message);
    Task EnqueueMessageAsync(Message message);
    event EventHandler<ClientConnectionEventArgs> Connected;
    event EventHandler<ClientConnectionEventArgs> Disconnected;
    event EventHandler<ClientReconnectingEventArgs> Reconnecting;
    event EventHandler<ClientClosedEventArgs> Closed;
    event EventHandler<ClientErrorEventArgs> ErrorOccurred;
    event EventHandler<ClientAuthenticationFailedEventArgs> AuthenticationFailed;
  }
}