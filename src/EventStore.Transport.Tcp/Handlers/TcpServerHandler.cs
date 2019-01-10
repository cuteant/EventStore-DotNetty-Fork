using DotNetty.Transport.Channels;

namespace EventStore.Transport.Tcp
{
    internal sealed class TcpServerHandler : TcpHandlers
    {
        private readonly IIConnectionEventListener _eventListener;

        public TcpServerHandler(DotNettyTransport transport)
            : base(transport)
        {
            _eventListener = (IIConnectionEventListener)transport;
        }

        public override void ChannelActive(IChannelHandlerContext context)
        {
            InitInbound(context.Channel);
            base.ChannelActive(context);
        }

        private void InitInbound(IChannel channel)
        {
            // disable automatic reads
            channel.Configuration.AutoRead = false;

            Init(channel, out var connection);
            TrySetStatus(connection);
            _eventListener.Notify(new InboundConnection(connection));
        }
    }
}
