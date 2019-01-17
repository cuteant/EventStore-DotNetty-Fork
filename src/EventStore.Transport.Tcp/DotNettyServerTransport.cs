using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using DotNetty.Buffers;
using DotNetty.Common.Utilities;
using DotNetty.Handlers;
using DotNetty.Handlers.Tls;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using DotNetty.Transport.Libuv;

namespace EventStore.Transport.Tcp
{
    public abstract class DotNettyServerTransport : DotNettyTransport, IIConnectionEventListener
    {
        private readonly IEventLoopGroup _serverBossGroup;
        private readonly IEventLoopGroup _serverWorkerGroup;
        private readonly ServerBootstrap _serverBootstrap;
        private volatile IChannel _serverChannel;
        private volatile EndPoint _listenAddress;

        private readonly X509Certificate2 _certificate;

        public DotNettyServerTransport(DotNettyTransportSettings settings, X509Certificate2 certificate)
            : base(settings)
        {
            _certificate = certificate;

            if (settings.EnableLibuv)
            {
                var dispatcher = new DispatcherEventLoopGroup();
                _serverBossGroup = dispatcher;
                _serverWorkerGroup = new WorkerEventLoopGroup(dispatcher, settings.ServerSocketWorkerPoolSize);
            }
            else
            {
                _serverBossGroup = new MultithreadEventLoopGroup(settings.ServerSocketWorkerPoolSize);
                _serverWorkerGroup = new MultithreadEventLoopGroup(settings.ServerSocketWorkerPoolSize);
            }

            _serverBootstrap = ServerFactory();
        }

        public virtual async Task ListenAsync(EndPoint listenAddress)
        {
            try
            {
                var newServerChannel = await NewServer(listenAddress).ConfigureAwait(false);

                ConnectionGroup.TryAdd(newServerChannel);

                newServerChannel.Configuration.AutoRead = true;

                Interlocked.Exchange(ref _serverChannel, newServerChannel);
            }
            catch (Exception ex)
            {
                Logger.FailedToBindToEndPoint(ex, listenAddress);
                try
                {
                    await Shutdown().ConfigureAwait(false);
                }
                catch
                {
                    // ignore errors occurring during shutdown
                }
                throw;
            }
        }

        public abstract void Notify(in InboundConnection connection);

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

                var server = _serverChannel?.CloseAsync() ?? TaskUtil.Completed;
                await server.ConfigureAwait(false);

                return all.IsCompleted && server.IsCompleted;
            }
            finally
            {
                // free all of the connection objects we were holding onto
                ConnectionGroup.Clear();
#pragma warning disable 4014 // shutting down the worker groups can take up to 10 seconds each. Let that happen asnychronously.
                _serverBossGroup?.ShutdownGracefullyAsync();
                _serverWorkerGroup?.ShutdownGracefullyAsync();
#pragma warning restore 4014
            }
        }

        private async Task<IChannel> NewServer(EndPoint listenAddress)
        {
            if (listenAddress is DnsEndPoint dns)
            {
                listenAddress = await DnsToIPEndpoint(dns).ConfigureAwait(false);
            }

            Interlocked.Exchange(ref _listenAddress, listenAddress);

            return await _serverBootstrap.BindAsync(listenAddress).ConfigureAwait(false);
        }

        private async void DoBind()
        {
            if (_listenAddress == null) { return; }

            try
            {
                await _serverChannel.CloseAsync().ConfigureAwait(false);
                Interlocked.Exchange(ref _serverChannel, null);

                var ch = await _serverBootstrap.BindAsync(_listenAddress).ConfigureAwait(false);
                ConnectionGroup.TryAdd(ch);
                ch.Configuration.AutoRead = true;
                Interlocked.Exchange(ref _serverChannel, ch);
            }
            catch
            {
                // TODO
            }
        }

        private ServerBootstrap ServerFactory()
        {
            var addressFamily = Settings.DnsUseIpv6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork;

            var server = new ServerBootstrap();
            server.Group(_serverBossGroup, _serverWorkerGroup);
            if (Settings.EnableLibuv)
            {
                server.Channel<TcpServerChannel>();
            }
            else
            {
                server.ChannelFactory(() => Settings.EnforceIpFamily
                    ? new TcpServerSocketChannel(addressFamily)
                    : new TcpServerSocketChannel());
            }
            if (Settings.EnableLibuv)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    server
                        .Option(ChannelOption.SoReuseport, Settings.TcpReusePort)
                        .ChildOption(ChannelOption.SoReuseaddr, Settings.TcpReuseAddr);
                }
                else
                {
                    server.Option(ChannelOption.SoReuseaddr, Settings.TcpReuseAddr);
                }
            }
            else
            {
                server.Option(ChannelOption.SoReuseaddr, Settings.TcpReuseAddr);
            }

            server.Option(ChannelOption.SoKeepalive, Settings.TcpKeepAlive)
                  .Option(ChannelOption.TcpNodelay, Settings.TcpNoDelay)
                  .Option(ChannelOption.AutoRead, false)
                  .Option(ChannelOption.SoBacklog, Settings.Backlog)
                  //.Option(ChannelOption.SoLinger, Settings.TcpLinger)
                  .Option(ChannelOption.Allocator, Settings.EnableBufferPooling ? (IByteBufferAllocator)PooledByteBufferAllocator.Default : UnpooledByteBufferAllocator.Default)
                  .Handler(new ServerChannelRebindHandler(DoBind))
                  .ChildHandler(new ActionChannelInitializer<ISocketChannel>(SetServerPipeline));

            if (Settings.ReceiveBufferSize.HasValue) { server.Option(ChannelOption.SoRcvbuf, Settings.ReceiveBufferSize.Value); }
            if (Settings.SendBufferSize.HasValue) { server.Option(ChannelOption.SoSndbuf, Settings.SendBufferSize.Value); }

            if (Settings.WriteBufferHighWaterMark.HasValue) server.Option(ChannelOption.WriteBufferHighWaterMark, Settings.WriteBufferHighWaterMark.Value);
            if (Settings.WriteBufferLowWaterMark.HasValue) server.Option(ChannelOption.WriteBufferLowWaterMark, Settings.WriteBufferLowWaterMark.Value);

            return server;
        }

        private void SetServerPipeline(ISocketChannel channel)
        {
            if (_certificate != null)
            {
                channel.Pipeline.AddFirst("TlsHandler", TlsHandler.Server(_certificate));
            }

            SetInitialChannelPipeline(channel);
            var pipeline = channel.Pipeline;

            var handler = new TcpServerHandler(this);
            pipeline.AddLast("ServerHandler", handler);
        }
    }
}
