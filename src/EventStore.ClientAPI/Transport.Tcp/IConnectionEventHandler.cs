using System;
using EventStore.Transport.Tcp;
using EventStore.Transport.Tcp.Messages;

namespace EventStore.ClientAPI.Transport.Tcp
{
    internal interface IConnectionEventHandler
    {
        void Handle(TcpPackageConnection connection, TcpPackage package);

        void OnConnectionEstablished(TcpPackageConnection connection);

        void OnError(TcpPackageConnection connection, Exception exc);

        void OnConnectionClosed(TcpPackageConnection connection, DisassociateInfo disassociateInfo);
    }
}
