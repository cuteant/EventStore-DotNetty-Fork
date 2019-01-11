using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Libuv.Native;
using Microsoft.Extensions.Logging;

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

        private static readonly Action<ILogger, string, DateTime, IPEndPoint, IPEndPoint, Guid, long, long, Exception> s_analyzeConnectionSendAndReceivedBytes =
            LoggerMessageFactory.Define<string, DateTime, IPEndPoint, IPEndPoint, Guid, long, long>(LogLevel.Trace,
            "ES {connectionType} closed [{dateTime:HH:mm:ss.fff}: N{remoteEndPoint}, L{localEndPoint}, {connectionId:B}]:Received bytes: {totalBytesReceived}, Sent bytes: {totalBytesSent}");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void AnalyzeConnectionSendAndReceivedBytes(this ILogger logger, Type connectionType, DateTime dt, IPEndPoint remoteEndPoint, IPEndPoint localEndPoint, Guid connectionId, long totalBytesReceived, long totalBytesSent)
        {
            s_analyzeConnectionSendAndReceivedBytes(logger, connectionType.Name, dt, remoteEndPoint, localEndPoint, connectionId, totalBytesReceived, totalBytesSent, null);
        }

        private static readonly Action<ILogger, string, DateTime, IPEndPoint, IPEndPoint, Guid, int, int, Exception> s_analyzeConnectionSendCalls =
            LoggerMessageFactory.Define<string, DateTime, IPEndPoint, IPEndPoint, Guid, int, int>(LogLevel.Trace,
            "ES {connectionType} closed [{dateTime:HH:mm:ss.fff}: N{remoteEndPoint}, L{localEndPoint}, {connectionId:B}]:Send calls: {sendCalls}, callbacks: {sendCallbacks}");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void AnalyzeConnectionSendCalls(this ILogger logger, Type connectionType, DateTime dt, IPEndPoint remoteEndPoint, IPEndPoint localEndPoint, Guid connectionId, int sendCalls, int sendCallbacks)
        {
            s_analyzeConnectionSendCalls(logger, connectionType.Name, dt, remoteEndPoint, localEndPoint, connectionId, sendCalls, sendCallbacks, null);
        }

        private static readonly Action<ILogger, string, DateTime, IPEndPoint, IPEndPoint, Guid, int, int, Exception> s_analyzeConnectionReceiveCalls =
            LoggerMessageFactory.Define<string, DateTime, IPEndPoint, IPEndPoint, Guid, int, int>(LogLevel.Trace,
            "ES {connectionType} closed [{dateTime:HH:mm:ss.fff}: N{remoteEndPoint}, L{localEndPoint}, {connectionId:B}]:Receive calls: {receiveCalls}, callbacks: {receiveCallbacks}");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void AnalyzeConnectionReceiveCalls(this ILogger logger, Type connectionType, DateTime dt, IPEndPoint remoteEndPoint, IPEndPoint localEndPoint, Guid connectionId, int receiveCalls, int receiveCallbacks)
        {
            s_analyzeConnectionReceiveCalls(logger, connectionType.Name, dt, remoteEndPoint, localEndPoint, connectionId, receiveCalls, receiveCallbacks, null);
        }

        private static readonly Action<ILogger, string, DateTime, IPEndPoint, IPEndPoint, Guid, DisassociateInfo, string, Exception> s_analyzeConnectionCloseReason =
            LoggerMessageFactory.Define<string, DateTime, IPEndPoint, IPEndPoint, Guid, DisassociateInfo, string>(LogLevel.Trace,
            "ES {connectionType} closed [{dateTime:HH:mm:ss.fff}: N{remoteEndPoint}, L{localEndPoint}, {connectionId:B}]:Close reason: [{disassociateInfo}] {reason}");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void AnalyzeConnectionCloseReason(this ILogger logger, Type connectionType, DateTime dt, IPEndPoint remoteEndPoint, IPEndPoint localEndPoint, Guid connectionId, DisassociateInfo disassociateInfo, string reason)
        {
            s_analyzeConnectionCloseReason(logger, connectionType.Name, dt, remoteEndPoint, localEndPoint, connectionId, disassociateInfo, reason, null);
        }

        private static readonly Action<ILogger, IMonitoredTcpConnection, Exception> s_connectionIsFaulted =
            LoggerMessageFactory.Define<IMonitoredTcpConnection>(LogLevel.Information,
            "# {connection} is faulted");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ConnectionIsFaulted(this ILogger logger, IMonitoredTcpConnection connection)
        {
            s_connectionIsFaulted(logger, connection, null);
        }

        private static readonly Action<ILogger, IMonitoredTcpConnection, int, Exception> s_connectionPendingSend =
            LoggerMessageFactory.Define<IMonitoredTcpConnection, int>(LogLevel.Information,
            "# {connection} {pendingSendBytes}kb pending send");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ConnectionPendingSend(this ILogger logger, IMonitoredTcpConnection connection, int pendingSendBytes)
        {
            s_connectionPendingSend(logger, connection, pendingSendBytes / 1024, null);
        }

        private static readonly Action<ILogger, IMonitoredTcpConnection, int, Exception> s_connectionPendingReceived =
            LoggerMessageFactory.Define<IMonitoredTcpConnection, int>(LogLevel.Information,
            "# {connection} {pendingReceivedBytes}kb are not dispatched");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ConnectionPendingReceived(this ILogger logger, IMonitoredTcpConnection connection, int pendingReceivedBytes)
        {
            s_connectionPendingReceived(logger, connection, pendingReceivedBytes / 1024, null);
        }

        private static readonly Action<ILogger, IMonitoredTcpConnection, int, Exception> s_connectionMissingReceiveCallback =
            LoggerMessageFactory.Define<IMonitoredTcpConnection, int>(LogLevel.Error,
            "# {connection} {sinceLastReceive}ms since last Receive started. No completion callback received, but socket status is READY_FOR_RECEIVE");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ConnectionMissingReceiveCallback(this ILogger logger, IMonitoredTcpConnection connection, int sinceLastReceive)
        {
            s_connectionMissingReceiveCallback(logger, connection, sinceLastReceive, null);
        }

        private static readonly Action<ILogger, IMonitoredTcpConnection, int, int, Exception> s_connectionMissingSendCallback =
            LoggerMessageFactory.Define<IMonitoredTcpConnection, int, int>(LogLevel.Error,
            "# {connection} {sinceLastSend}ms since last send started. No completion callback received, but socket status is READY_FOR_SEND. In send: {inSendBytes}");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ConnectionMissingSendCallback(this ILogger logger, IMonitoredTcpConnection connection, int sinceLastSend, int inSendBytes)
        {
            s_connectionMissingSendCallback(logger, connection, sinceLastSend, inSendBytes, null);
        }
    }
}
