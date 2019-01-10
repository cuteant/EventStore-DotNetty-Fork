using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using DotNetty.Buffers;
using DotNetty.Handlers.Tls;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using DotNetty.Transport.Libuv;

namespace EventStore.Transport.Tcp
{
    public abstract class DotNettyClientTransport : DotNettyTransport
    {
        private readonly IEventLoopGroup _clientWorkerGroup;
        private Bootstrap _clientBootstrap;
        private readonly bool _useSsl;
        private readonly string _sslTargetHost;
        private readonly bool _sslValidateServer;

        public DotNettyClientTransport(DotNettyTransportSettings settings, bool useSsl, string sslTargetHost, bool sslValidateServer)
            : base(settings)
        {
            if (useSsl && string.IsNullOrWhiteSpace(sslTargetHost)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.sslTargetHost); }

            _useSsl = useSsl;
            _sslTargetHost = sslTargetHost;
            _sslValidateServer = sslValidateServer;
            if (settings.EnableLibuv)
            {
                _clientWorkerGroup = new EventLoopGroup(settings.ClientSocketWorkerPoolSize);
            }
            else
            {
                _clientWorkerGroup = new MultithreadEventLoopGroup(settings.ClientSocketWorkerPoolSize);
            }
            _clientBootstrap = ClientFactory();
        }

        public virtual async Task<DotNettyConnection> ConnectAsync(EndPoint remoteAddress)
        {
            try
            {
                var socketAddress = await MapEndpointAsync(remoteAddress).ConfigureAwait(false);
                var associate = await _clientBootstrap.ConnectAsync(socketAddress).ConfigureAwait(false);
                var handler = (ITcpClientHandler)associate.Pipeline.Last();
                return await handler.StatusFuture.ConfigureAwait(false);
            }
            catch (AggregateException e) when (e.InnerException is ConnectException cause)
            {
                throw HandleConnectException(remoteAddress, cause, e);
            }
            catch (AggregateException e) when (e.InnerException is ConnectTimeoutException cause)
            {
                throw new InvalidConnectionException(cause.Message);
            }
            catch (AggregateException e) when (e.InnerException is ChannelException cause)
            {
                throw new InvalidConnectionException(cause.InnerException?.Message ?? cause.Message);
            }
            catch (ConnectException exc)
            {
                throw HandleConnectException(remoteAddress, exc, null);
            }
            catch (ConnectTimeoutException exc)
            {
                throw new InvalidConnectionException(exc.Message);
            }
            catch (ChannelException exc)
            {
                throw new InvalidConnectionException(exc.InnerException?.Message ?? exc.Message);
            }
        }

        private async Task<IPEndPoint> MapEndpointAsync(EndPoint socketAddress)
        {
            IPEndPoint ipEndPoint;

            if (socketAddress is DnsEndPoint dns)
            {
                ipEndPoint = await DnsToIPEndpoint(dns).ConfigureAwait(false);
            }
            else
            {
                ipEndPoint = (IPEndPoint)socketAddress;
            }

            if (ipEndPoint.Address.Equals(IPAddress.Any) || ipEndPoint.Address.Equals(IPAddress.IPv6Any))
            {
                // client hack
                return ipEndPoint.AddressFamily == AddressFamily.InterNetworkV6
                    ? new IPEndPoint(IPAddress.IPv6Loopback, ipEndPoint.Port)
                    : new IPEndPoint(IPAddress.Loopback, ipEndPoint.Port);
            }
            return ipEndPoint;
        }

        private static Exception HandleConnectException(EndPoint remoteAddress, ConnectException cause, AggregateException e)
        {
            var socketException = cause?.InnerException as SocketException;

            if (socketException?.SocketErrorCode == SocketError.ConnectionRefused)
            {
                return new InvalidConnectionException(socketException.Message + " " + remoteAddress);
            }

            return new InvalidConnectionException("Failed to associate with " + remoteAddress, e ?? (Exception)cause);
        }

        public override async Task<bool> Shutdown()
        {
            try
            {
                var tasks = new List<Task>();
                foreach (var channel in ConnectionGroup)
                {
                    tasks.Add(channel.CloseAsync());
                }
                var all = Task.WhenAll(tasks);
                await all.ConfigureAwait(false);

                return all.IsCompleted;
            }
            finally
            {
                // free all of the connection objects we were holding onto
                ConnectionGroup.Clear();
#pragma warning disable 4014 // shutting down the worker groups can take up to 10 seconds each. Let that happen asnychronously.
                _clientWorkerGroup?.ShutdownGracefullyAsync();
#pragma warning restore 4014
            }
        }

        private Bootstrap ClientFactory()
        {
            var addressFamily = Settings.DnsUseIpv6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork;

            var client = new Bootstrap()
                .Group(_clientWorkerGroup)
                .Option(ChannelOption.SoReuseaddr, Settings.TcpReuseAddr)
                //.Option(ChannelOption.SoReuseport, Settings.TcpReusePort)
                .Option(ChannelOption.SoKeepalive, Settings.TcpKeepAlive)
                .Option(ChannelOption.TcpNodelay, Settings.TcpNoDelay)
                //.Option(ChannelOption.SoLinger, Settings.TcpLinger)
                .Option(ChannelOption.ConnectTimeout, Settings.ConnectTimeout)
                .Option(ChannelOption.AutoRead, false)
                .Option(ChannelOption.Allocator, Settings.EnableBufferPooling ? (IByteBufferAllocator)PooledByteBufferAllocator.Default : UnpooledByteBufferAllocator.Default);
            if (Settings.EnableLibuv)
            {
                client.Channel<TcpChannel>();
            }
            else
            {
                client.ChannelFactory(() => Settings.EnforceIpFamily
                    ? new TcpSocketChannel(addressFamily)
                    : new TcpSocketChannel());
            }
            client.Handler(new ActionChannelInitializer<ISocketChannel>(channel => SetClientPipeline(channel)));

            if (Settings.ReceiveBufferSize.HasValue) { client.Option(ChannelOption.SoRcvbuf, Settings.ReceiveBufferSize.Value); }
            if (Settings.SendBufferSize.HasValue) { client.Option(ChannelOption.SoSndbuf, Settings.SendBufferSize.Value); }
            if (Settings.WriteBufferHighWaterMark.HasValue) { client.Option(ChannelOption.WriteBufferHighWaterMark, Settings.WriteBufferHighWaterMark.Value); }
            if (Settings.WriteBufferLowWaterMark.HasValue) { client.Option(ChannelOption.WriteBufferLowWaterMark, Settings.WriteBufferLowWaterMark.Value); }

            return client;
        }

        private void SetClientPipeline(ISocketChannel channel)
        {
            if (_useSsl)
            {
                var tlsSettings = new ClientTlsSettings(_sslTargetHost);
                if (!_sslValidateServer)
                {
                    tlsSettings.ServerCertificateValidation = (cert, chain, errors) => true ;
            }
                var tlsHandler = new TlsHandler(tlsSettings);

                channel.Pipeline.AddFirst("TlsHandler", tlsHandler);
            }

            SetInitialChannelPipeline(channel);
            var pipeline = channel.Pipeline;

            var handler = new TcpClientHandler(this);
            pipeline.AddLast("ClientHandler", handler);
        }
    }
}
