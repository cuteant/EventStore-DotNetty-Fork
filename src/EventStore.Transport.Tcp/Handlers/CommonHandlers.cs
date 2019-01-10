using System;
using System.Threading.Tasks;
using DotNetty.Transport.Channels;
using Microsoft.Extensions.Logging;

namespace EventStore.Transport.Tcp
{
    internal abstract class CommonHandlers : ChannelHandlerAdapter
    {
        protected readonly ILogger Logger;
        protected readonly DotNettyTransport Transport;

        public CommonHandlers(DotNettyTransport transport)
        {
            Logger = TraceLogger.GetLogger(this.GetType());
            Transport = transport;
        }

        public override void ChannelActive(IChannelHandlerContext context)
        {
            base.ChannelActive(context);

            var channel = context.Channel;
            if (!Transport.ConnectionGroup.TryAdd(channel))
            {
                if (Logger.IsWarningLevelEnabled()) Logger.UnableToAddChannelToConnectionGroup(channel);
            }
        }

        public override void ChannelInactive(IChannelHandlerContext context)
        {
            base.ChannelInactive(context);

            var channel = context.Channel;
            if (!Transport.ConnectionGroup.TryRemove(channel))
            {
                if (Logger.IsWarningLevelEnabled()) Logger.UnableToRemoveChannelFromConnectionGroup(channel);
            }
        }

        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            base.ExceptionCaught(context, exception);

            Logger.ErrorCaughtChannel(context, exception);
        }

        protected abstract DotNettyConnection CreateConnection(IChannel channel);

        protected abstract void RegisterListener(IChannel channel, ITcpPackageListener listener);

        protected void Init(IChannel channel, out DotNettyConnection connection)
        {
            var conn = CreateConnection(channel);
            conn.ReadHandlerSource.Task.ContinueWith(s =>
            {
                var listener = s.Result;
                RegisterListener(channel, listener);
                channel.Configuration.AutoRead = true; // turn reads back on
            }, TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.NotOnCanceled | TaskContinuationOptions.NotOnFaulted);
            connection = conn;
        }
    }
}
