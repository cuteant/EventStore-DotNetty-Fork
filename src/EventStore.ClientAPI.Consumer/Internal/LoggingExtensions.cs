using System;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace EventStore.ClientAPI
{
    internal static class ConsumerLoggingExtensions
    {
        private static readonly Action<ILogger, string, DateTime, Exception> s_caughtUpOnStreamAt =
            LoggerMessage.Define<string, DateTime>(LogLevel.Information, 0,
            "Caught up on {streamId} at {dateTime}.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void CaughtUpOnStreamAt(this ILogger logger, string streamId)
        {
            if (logger.IsInformationLevelEnabled()) s_caughtUpOnStreamAt(logger, streamId, DateTime.Now, null);
        }

        private static readonly Action<ILogger, string, string, Exception> s_subscriptionWasClosedByTheClient =
            LoggerMessage.Define<string, string>(LogLevel.Information, 0,
            "Subscription to {streamId} was closed by the client. {message}.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SubscriptionWasClosedByTheClient(this ILogger logger, string streamId, string message)
        {
            s_subscriptionWasClosedByTheClient(logger, streamId, Environment.NewLine + message, null);
        }
    }
}
