using System.Threading.Tasks;
using DotNetty.Transport.Channels;

namespace EventStore.Transport.Tcp
{
    internal sealed class TcpClientHandler : TcpHandlers, ITcpClientHandler
    {
        private readonly TaskCompletionSource<DotNettyConnection> _statusPromise = new TaskCompletionSource<DotNettyConnection>();
        public Task<DotNettyConnection> StatusFuture => _statusPromise.Task;

        public TcpClientHandler(DotNettyTransport transport) : base(transport) { }

        public override void ChannelActive(IChannelHandlerContext context)
        {
            InitOutbound(context.Channel);
            base.ChannelActive(context);
        }

        private void InitOutbound(IChannel channel)
        {
            Init(channel, out var connection);
            TrySetStatus(connection);
        }

        protected override void TrySetStatus(DotNettyConnection connection)
        {
            base.TrySetStatus(connection);
            _statusPromise.TrySetResult(connection);
        }
    }

    internal interface ITcpClientHandler
    {
        Task<DotNettyConnection> StatusFuture { get; }
    }
}
