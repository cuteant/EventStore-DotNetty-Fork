using System;
using System.Net;
using System.Threading.Tasks;
using EventStore.Transport.Tcp.Messages;

namespace EventStore.Transport.Tcp
{
    public interface ITcpConnection
    {
        event Action<ITcpConnection, DisassociateInfo> ConnectionClosed;

        Guid ConnectionId { get; }
        string ClientConnectionName { get; }
        IPEndPoint RemoteEndPoint { get; }
        IPEndPoint LocalEndPoint { get; }
        int SendQueueSize { get; }
        int PendingSendBytes { get; }
        bool IsClosed { get; }

        TaskCompletionSource<ITcpPackageListener> ReadHandlerSource { get; }

        //void ReceiveAsync(Action<ITcpConnection, IEnumerable<ArraySegment<byte>>> callback);
        void EnqueueSend(TcpPackage package);
        void Close(DisassociateInfo info, string reason = null);
        void Close(in Disassociated disassociated);
        void SetClientConnectionName(string clientConnectionName);
    }
}