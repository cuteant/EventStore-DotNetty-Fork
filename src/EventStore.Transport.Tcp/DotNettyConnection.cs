using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using DotNetty.Buffers;
using DotNetty.Common.Utilities;
using DotNetty.Handlers.Tls;
using DotNetty.Transport.Channels;
using EventStore.Transport.Tcp.Messages;
using MessagePack;
using Microsoft.Extensions.Logging;

namespace EventStore.Transport.Tcp
{
    public sealed partial class DotNettyConnection : ITcpConnection, IEquatable<DotNettyConnection>
    {
        private const int MaxSendPacketSize = 72 * 1024;
        private static readonly ILogger s_logger = TraceLogger.GetLogger<DotNettyConnection>();
        private static readonly IFormatterResolver s_defaultResolver = MessagePackStandardResolver.Default;

        private readonly DotNettyTransport _transport;
        private readonly IChannel _channel;


        public event Action<ITcpConnection, DisassociateInfo> ConnectionClosed;
        public Guid ConnectionId => _connectionId;
        public int SendQueueSize => _sendQueue.Count;
        public string ClientConnectionName => _clientConnectionName;

        private readonly Guid _connectionId;
        private string _clientConnectionName;

        private readonly ConcurrentQueue<TcpPackage> _sendQueue = new ConcurrentQueue<TcpPackage>();

        private int _status = TransportStatus.Idle;

        public DotNettyConnection(DotNettyTransport transport, IChannel channel)
        {
            _connectionId = Guid.NewGuid();

            LocalEndPoint = Helper.EatException(() => (IPEndPoint)channel.LocalAddress);
            RemoteEndPoint = Helper.EatException(() => (IPEndPoint)channel.RemoteAddress);

            _transport = transport;
            _channel = channel;

            ReadHandlerSource = new TaskCompletionSource<ITcpPackageListener>();

            TcpConnectionMonitor.Default.Register(this);
        }

        /// <summary>
        /// The TaskCompletionSource returned by this call must be completed with an <see
        /// cref="ITcpPackageListener"/> to register a listener responsible for handling the
        /// incoming payload. Until the listener is not registered the transport SHOULD buffer
        /// incoming messages.
        /// </summary>
        public TaskCompletionSource<ITcpPackageListener> ReadHandlerSource { get; }

        private bool? _isSsl;

        public bool IsSsl
        {
            get
            {
                if (_isSsl.HasValue) { return _isSsl.Value; }
                _isSsl = null != _channel.Pipeline.Get<TlsHandler>();
                return _isSsl.Value;
            }
        }

        public void SetClientConnectionName(string clientConnectionName)
        {
            _clientConnectionName = clientConnectionName;
        }

        public void EnqueueSend(TcpPackage package)
        {
            _sendQueue.Enqueue(package);
            NotifySendScheduled(package.Length);

            if (TransportStatus.Idle == Interlocked.CompareExchange(ref _status, TransportStatus.Busy, TransportStatus.Idle))
            {
                Task.Run(SendPackagesAsync);
            }
        }

        private async Task<bool> SendPackagesAsync()
        {
            var batch = new List<TcpPackage>(256);

            while (true)
            {
                batch.Clear();
                var sendPacketSize = 0;

                while (_sendQueue.TryDequeue(out var msg))
                {
                    batch.Add(msg);
                    sendPacketSize += msg.Length;
                    if (sendPacketSize >= MaxSendPacketSize) { break; }
                }

                try
                {
                    var payload = MessagePackSerializer.Serialize(batch, s_defaultResolver);
                    NotifySendStarting(payload.Length);
                    await _channel.WriteAndFlushAsync(Unpooled.WrappedBuffer(payload));
                    NotifySendCompleted(payload.Length);
                }
                catch (ClosedChannelException cce) { return NotifySendCompleted(cce); }
                catch (Exception exc) { return NotifySendCompleted(exc); }

                if (_sendQueue.IsEmpty)
                {
                    Interlocked.Exchange(ref _status, TransportStatus.Idle);
                    return true;
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private bool NotifySendCompleted(ClosedChannelException cce)
        {
            NotifySendCompleted(0);
            Interlocked.Exchange(ref _status, TransportStatus.Fault);
            Close(new Disassociated(DisassociateInfo.Shutdown, new ClosedConnectionException(cce.Message, cce)));
            return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private bool NotifySendCompleted(Exception exc)
        {
            NotifySendCompleted(0);
            Interlocked.Exchange(ref _status, TransportStatus.Fault);
            Close(new Disassociated(DisassociateInfo.CodecError, new ClosedConnectionException(exc.Message, exc)));
            return false;
        }

        public void Close(DisassociateInfo info, string reason = null)
        {
            Close(new Disassociated(DisassociateInfo.Success, reason ?? "Normal socket close."));
        }

        public void Close(in Disassociated disassociated)
        {
            if (IsClosed) { return; }

            NotifyClosed();

            if (s_logger.IsTraceLevelEnabled()) { AnalyzeConnection(disassociated); }

            _channel.CloseAsync().Ignore();

            ConnectionClosed?.Invoke(this, disassociated.Info);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void AnalyzeConnection(in Disassociated disassociated)
        {
            s_logger.AnalyzeConnectionSendAndReceivedBytes(
                    GetType(), DateTime.UtcNow, RemoteEndPoint, LocalEndPoint, _connectionId,
                    TotalBytesReceived, TotalBytesSent);
            s_logger.AnalyzeConnectionSendCalls(
                    GetType(), DateTime.UtcNow, RemoteEndPoint, LocalEndPoint, _connectionId,
                    SendCalls, SendCallbacks);
            s_logger.AnalyzeConnectionReceiveCalls(
                    GetType(), DateTime.UtcNow, RemoteEndPoint, LocalEndPoint, _connectionId,
                    ReceiveCalls, ReceiveCallbacks);
            s_logger.AnalyzeConnectionCloseReason(
                    GetType(), DateTime.UtcNow, RemoteEndPoint, LocalEndPoint, _connectionId,
                    disassociated.Info, disassociated.Reason);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is DotNettyConnection associationHandle && Equals(associationHandle);
        }

        /// <inheritdoc/>
        public bool Equals(DotNettyConnection other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return _channel.Equals(other._channel);
        }

        /// <inheritdoc/>
        public override int GetHashCode() => _channel.GetHashCode();

        static class TransportStatus
        {
            public const int Idle = 0;
            public const int Busy = 1;
            public const int Fault = 2;
        }
    }
}

