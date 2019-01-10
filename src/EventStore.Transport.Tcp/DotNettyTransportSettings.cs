using System;

namespace EventStore.Transport.Tcp
{
    /// <summary>INTERNAL API.
    ///
    /// Defines the settings for the <see cref="DotNettyTransport"/>.
    /// </summary>
    public sealed class DotNettyTransportSettings
    {
        public readonly bool EnableLibuv;

        /// <summary>Sets a connection timeout for all outbound connections i.e. how long a connect may take
        /// until it is timed out.</summary>
        public readonly TimeSpan ConnectTimeout;

        public readonly int ServerSocketWorkerPoolSize;
        public readonly int ClientSocketWorkerPoolSize;
        public readonly int MaxFrameSize;

        /// <summary>If set to true, we will use IPv6 addresses upon DNS resolution for host names. Otherwise
        /// IPv4 will be used.</summary>
        public readonly bool DnsUseIpv6;

        /// <summary>Enables SO_REUSEADDR, which determines when an ActorSystem can open the specified listen
        /// port (the meaning differs between *nix and Windows).</summary>
        public readonly bool TcpReuseAddr;
        public readonly bool TcpReusePort;

        /// <summary>Enables TCP Keepalive, subject to the O/S kernel's configuration.</summary>
        public readonly bool TcpKeepAlive;

        /// <summary>Enables the TCP_NODELAY flag, i.e. disables Nagle's algorithm</summary>
        public readonly bool TcpNoDelay;

        public readonly int TcpLinger;

        /// <summary>If set to true, we will enforce usage of IPv4 or IPv6 addresses upon DNS resolution for
        /// host names. If true, we will use IPv6 enforcement. Otherwise, we will use IPv4.</summary>
        public readonly bool EnforceIpFamily;

        /// <summary>Sets the size of the connection backlog.</summary>
        public readonly int Backlog;

        /// <summary>Sets the default receive buffer size of the Sockets.</summary>
        public readonly int? ReceiveBufferSize;

        /// <summary>Sets the default send buffer size of the Sockets.</summary>
        public readonly int? SendBufferSize;

        public readonly int? WriteBufferHighWaterMark;
        public readonly int? WriteBufferLowWaterMark;

        public readonly bool EnableBufferPooling;

        public DotNettyTransportSettings(bool enableLibuv, TimeSpan connectTimeout, int serverSocketWorkerPoolSize, int clientSocketWorkerPoolSize,
            int maxFrameSize, bool dnsUseIpv6, bool tcpReuseAddr, bool tcpReusePort, bool tcpKeepAlive, bool tcpNoDelay, int tcpLinger, int backlog,
            bool enforceIpFamily, int? receiveBufferSize, int? sendBufferSize, int? writeBufferHighWaterMark, int? writeBufferLowWaterMark, bool enableBufferPooling)
        {
            if (maxFrameSize < 32000) throw new ArgumentException("maximum-frame-size must be at least 32000 bytes", nameof(maxFrameSize));

            EnableLibuv = enableLibuv;
            ConnectTimeout = connectTimeout;
            ServerSocketWorkerPoolSize = serverSocketWorkerPoolSize;
            ClientSocketWorkerPoolSize = clientSocketWorkerPoolSize;
            MaxFrameSize = maxFrameSize;
            DnsUseIpv6 = dnsUseIpv6;
            TcpReuseAddr = tcpReuseAddr;
            TcpReusePort = tcpReusePort;
            TcpKeepAlive = tcpKeepAlive;
            TcpNoDelay = tcpNoDelay;
            TcpLinger = tcpLinger;
            Backlog = backlog;
            EnforceIpFamily = enforceIpFamily;
            ReceiveBufferSize = receiveBufferSize;
            SendBufferSize = sendBufferSize;
            WriteBufferHighWaterMark = writeBufferHighWaterMark;
            WriteBufferLowWaterMark = writeBufferLowWaterMark;
            EnableBufferPooling = enableBufferPooling;
        }
    }
}
