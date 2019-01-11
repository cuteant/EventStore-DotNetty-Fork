using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Transport.Tcp;
using EventStore.Transport.Tcp.Messages;
using Microsoft.Extensions.Logging;

namespace EventStore.ClientAPI.Transport.Tcp
{
    internal class TcpPackageConnection : DotNettyClientTransport, ITcpPackageListener
    {
        public bool IsClosed { get { return _connection != null ? _connection.IsClosed : true; } }
        public int SendQueueSize { get { return _connection != null ? _connection.SendQueueSize : 0; } }
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
            if (null == remoteEndPoint) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.remoteEndPoint); }
            if (null == connEventHandler) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connEventHandler); }

            _remoteEndPoint = remoteEndPoint;
            _connEventHandler = connEventHandler;

            ConnectionId = Guid.Empty;
        }

        public async Task ConnectAsync()
        {
            try
            {
                var conn = await ConnectAsync(_remoteEndPoint).ConfigureAwait(false);
                conn.ReadHandlerSource.TrySetResult(this);

                ConnectionId = conn.ConnectionId;

                conn.ConnectionClosed += OnConnectionClosed;

                Interlocked.Exchange(ref _connection, conn);

                if (_log.IsDebugLevelEnabled())
                {
                    _log.LogDebug("TcpPackageConnection: connected to [{0}, L{1}, {2:B}].", conn.RemoteEndPoint, conn.LocalEndPoint, ConnectionId);
                }
                _connEventHandler.OnConnectionEstablished(this);
            }
            catch (InvalidConnectionException exc)
            {
                if (_log.IsDebugLevelEnabled())
                {
                    _log.LogDebug(exc, "TcpPackageConnection: connection to [{0} failed. Error: ", _remoteEndPoint);
                }
                Interlocked.Exchange(ref _isClosed, 1);
                _connEventHandler.OnConnectionClosed(this, DisassociateInfo.InvalidConnection);
            }
        }

        private void OnConnectionClosed(ITcpConnection connection, DisassociateInfo error)
        {
            if (Interlocked.Exchange(ref _isClosed, 1) == 1) { return; }
            if (_log.IsDebugLevelEnabled())
            {
                _log.LogDebug("TcpPackageConnection: connection [{0}, L{1}, {2:B}] was closed {3}", _connection.RemoteEndPoint, _connection.LocalEndPoint,
                        ConnectionId, error == DisassociateInfo.Success ? "cleanly." : "with error: " + error + ".");
            }
            _connEventHandler.OnConnectionClosed(this, error);
        }

        void ITcpPackageListener.Notify(TcpPackage package)
        {
            _connEventHandler.Handle(this, package);
        }

        void ITcpPackageListener.HandleBadRequest(in Disassociated disassociated)
        {
            _connection.Close(disassociated);

            var message = string.Format("TcpPackageConnection: [{0}, L{1}, {2}] ERROR for {3}. Connection will be closed.",
                                        RemoteEndPoint, LocalEndPoint, ConnectionId,
                                        "<invalid package>");
            _connEventHandler.OnError(this, disassociated.Error);
            if (_log.IsDebugLevelEnabled()) _log.LogDebug(disassociated.Error, message);
        }

        public void EnqueueSend(TcpPackage package)
        {
            if (_connection == null) { CoreThrowHelper.ThrowInvalidOperationException_FailedConnection(); }

            _connection.EnqueueSend(package);
        }

        public void Close(string reason)
        {
            if (null == _connection) { return; }

            _connection.Close(DisassociateInfo.Success, reason);
        }
    }
}