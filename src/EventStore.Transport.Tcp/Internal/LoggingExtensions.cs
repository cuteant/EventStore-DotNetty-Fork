using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using DotNetty.Transport.Channels;
using System.Net.Sockets;
using DotNetty.Transport.Libuv.Native;

namespace EventStore.Transport.Tcp
{
    internal static class TransportTcpLoggingExtensions
    {
        private static readonly Action<ILogger, int, double, double, long, long, long, TimeSpan, Exception> s_analyzeConnections =
            LoggerMessageFactory.Define<int, double, double, long, long, long, TimeSpan>(LogLevel.Trace,
            "\n# Total connections: {connections,3}. Out: {sendingSpeed:0.00}b/s  In: {receivingSpeed:0.00}b/s  Pending Send: {pendingSend}  " +
            "In Send: {inSend}  Pending Received: {pendingReceived} Measure Time: {measureTime}");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void AnalyzeConnections(this ILogger logger, TcpStats stats)
        {
            s_analyzeConnections(logger, stats.Connections, stats.SendingSpeed, stats.ReceivingSpeed,
                stats.PendingSend, stats.InSend, stats.PendingSend, stats.MeasureTime, null);
        }

        private static readonly Action<ILogger, EndPoint, EndPoint, IChannelId, Exception> s_unableToAddChannelToConnectionGroup =
            LoggerMessageFactory.Define<EndPoint, EndPoint, IChannelId>(LogLevel.Warning,
            "Unable to ADD channel [{localAddress}->{remoteAddress}](Id={channelId}) to connection group. May not shut down cleanly.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void UnableToAddChannelToConnectionGroup(this ILogger logger, IChannel channel)
        {
            s_unableToAddChannelToConnectionGroup(logger, channel.LocalAddress, channel.RemoteAddress, channel.Id, null);
        }

        private static readonly Action<ILogger, EndPoint, EndPoint, IChannelId, Exception> s_unableToRemoveChannelFromConnectionGroup =
            LoggerMessageFactory.Define<EndPoint, EndPoint, IChannelId>(LogLevel.Warning,
            "Unable to REMOVE channel [{localAddress}->{remoteAddress}](Id={channelId}) from connection group. May not shut down cleanly.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void UnableToRemoveChannelFromConnectionGroup(this ILogger logger, IChannel channel)
        {
            s_unableToRemoveChannelFromConnectionGroup(logger, channel.LocalAddress, channel.RemoteAddress, channel.Id, null);
        }

        private static readonly Action<ILogger, EndPoint, EndPoint, IChannelId, Exception> s_errorCaughtChannel =
            LoggerMessageFactory.Define<EndPoint, EndPoint, IChannelId>(LogLevel.Error,
            "Error caught channel [{localAddress}->{remoteAddress}](Id={channelId})");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ErrorCaughtChannel(this ILogger logger, IChannelHandlerContext context, Exception exception)
        {
            var channel = context.Channel;
            s_errorCaughtChannel(logger, channel.LocalAddress, channel.RemoteAddress, channel.Id, exception);
        }

        private static readonly Action<ILogger, string, EndPoint, EndPoint, IChannelId, Exception> s_dotNettyExceptionCaught =
            LoggerMessageFactory.Define<string, EndPoint, EndPoint, IChannelId>(LogLevel.Information,
            "{socketExcMsg} Channel [{localAddress}->{remoteAddress}](Id={channelId})");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void DotNettyExceptionCaught(this ILogger logger, SocketException se, IChannelHandlerContext context)
        {
            var channel = context.Channel;
            s_dotNettyExceptionCaught(logger, se.Message, channel.LocalAddress, channel.RemoteAddress, channel.Id, null);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void DotNettyExceptionCaught(this ILogger logger, OperationException exc, IChannelHandlerContext context)
        {
            var channel = context.Channel;
            s_dotNettyExceptionCaught(logger, exc.Description, channel.LocalAddress, channel.RemoteAddress, channel.Id, null);
        }

        private static readonly Action<ILogger, EndPoint, Exception> s_failedToBindToEndPoint =
            LoggerMessageFactory.Define<EndPoint>(LogLevel.Error,
            "Failed to bind to {listenAddress}; shutting down DotNetty transport.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void FailedToBindToEndPoint(this ILogger logger, Exception ex, EndPoint listenAddress)
        {
            s_failedToBindToEndPoint(logger, listenAddress, ex);
        }
    }
}
