using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
    }
}
