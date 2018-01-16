using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;

namespace EventStore.Projections.Core.Tests
{
    internal class LoggingUtils
    {
        private static ILogger s_logger = TraceLogger.GetLogger<LoggingUtils>();

        public static void WriteLine(string message) => s_logger.LogInformation(message);

        public static void WriteLine(string format, params object[] args) => s_logger.LogInformation(format, args);
    }
}
