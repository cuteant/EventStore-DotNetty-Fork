using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Transport.Tcp;
using EventStore.Transport.Tcp.Messages;
using Microsoft.Extensions.Logging;

namespace EventStore.ClientAPI.Transport.Tcp
{
    internal interface IHasTcpPackageConnection
    {
        TcpPackageConnection Connection { get; }
    }

    internal class TcpPackageConnection : DotNettyClientTransport, ITcpPackageListener
    {
        public bool IsClosed { get { return _connection is object ? _connection.IsClosed : true; } }
        public int SendQueueSize { get { return _connection is object ? _connection.SendQueueSize : 0; } }
        public IPEndPoint RemoteEndPoint { get { return _remoteEndPoint; } }
        public IPEndPoint LocalEndPoint { get { return _connection?.LocalEndPoint; } }
        public Guid ConnectionId { get; private set; }

        private static readonly ILogger _log = TraceLogger.GetLogger<TcpPackageConnection>();
        private readonly IConnectionEventHandler _connEventHandler;

        private readonly IPEndPoint _remoteEndPoint;

        private ITcpConnection _connection;
        private int _isClosed;

        public TcpPackageConnection(
            DotNettyTransportSettings settings,
            IPEndPoint remoteEndPoint,
            bool ssl,
            string targetHost,
            bool validateServer,
            IConnectionEventHandler connEventHandler)
            : base(settings, ssl, targetHost, validateServer)
        {
            if (remoteEndPoint is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.remoteEndPoint); }
            if (connEventHandler is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connEventHandler); }

            _remoteEndPoint = remoteEndPoint;
            _connEventHandler = connEventHandler;

            ConnectionId = Guid.NewGuid();
        }

        public async Task ConnectAsync()
        {
            try
            {
                var conn = await ConnectAsync(_remoteEndPoint).ConfigureAwait(false);
                conn.ReadHandlerSource.TrySetResult(this);

                conn.ConnectionClosed += OnConnectionClosed;

                Interlocked.Exchange(ref _connection, conn);

#if DEBUG
                if (_log.IsDebugLevelEnabled()) { _log.ConnectToEstablished(conn); }
#endif
                _connEventHandler.OnConnectionEstablished(this);
            }
            catch (InvalidConnectionException exc)
            {
                if (_log.IsDebugLevelEnabled()) { _log.ConnectToFailed(exc, _remoteEndPoint); }
                Interlocked.Exchange(ref _isClosed, 1);
                _connEventHandler.OnConnectionClosed(this, DisassociateInfo.InvalidConnection);
            }
        }

        private void OnConnectionClosed(ITcpConnection connection, DisassociateInfo error)
        {
            if (Interlocked.Exchange(ref _isClosed, 1) == 1) { return; }
#if DEBUG
            if (_log.IsDebugLevelEnabled()) { _log.ConnectionWasClosed(connection, error); }
#endif
            _connEventHandler.OnConnectionClosed(this, error);
        }

        void ITcpPackageListener.Notify(TcpPackage package)
        {
            _connEventHandler.Handle(this, package);
        }

        void ITcpPackageListener.HandleBadRequest(in Disassociated disassociated)
        {
            _connection.Close(disassociated);
            _connEventHandler.OnError(this, disassociated.Error);

#if DEBUG
            if (_log.IsDebugLevelEnabled()) _log.ConnectionWillBeClosed(disassociated, RemoteEndPoint, LocalEndPoint, ConnectionId);
#endif
        }

        public void EnqueueSend(TcpPackage package)
        {
            if (_connection is null) { CoreThrowHelper.ThrowInvalidOperationException_FailedConnection(); }

            _connection.EnqueueSend(package);
        }

        public void Close(string reason)
        {
            if (_connection is null) { return; }

            _connection.Close(DisassociateInfo.Success, reason);
        }
    }
}