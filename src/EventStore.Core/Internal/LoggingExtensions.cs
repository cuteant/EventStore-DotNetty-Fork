using System;
using System.Runtime.CompilerServices;
using EventStore.Core.Services.PersistentSubscription;
using Microsoft.Extensions.Logging;
using EventStore.Core.Authentication;

namespace EventStore.Core
{
    partial class CoreLoggingExtensions
    {
        private static readonly Action<ILogger, Exception> s_an_entry_in_the_scavenge_log_has_no_scavengeId =
            LoggerMessage.Define(LogLevel.Warning, 0,
                "An entry in the scavenge log has no scavengeId");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void An_entry_in_the_scavenge_log_has_no_scavengeId(this ILogger logger)
        {
            s_an_entry_in_the_scavenge_log_has_no_scavengeId(logger, null);
        }

        private static readonly Action<ILogger, int, int, Exception> s_scavenging_threads_not_allowed =
            LoggerMessage.Define<int, int>(LogLevel.Warning, 0,
                "{numThreads} scavenging threads not allowed.  Max threads allowed for scavenging is {maxThreadCount}. Capping.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Scavenging_threads_not_allowed(this ILogger logger, int threads, int maxThreadCount)
        {
            s_scavenging_threads_not_allowed(logger, threads, maxThreadCount, null);
        }

        private static readonly Action<ILogger, string, int, Exception> s_could_not_create_bloom_filter_for_chunk =
            LoggerMessage.Define<string, int>(LogLevel.Warning, 0,
                "Could not create bloom filter for chunk: {fileName}, map count: {mapCount}");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Could_not_create_bloom_filter_for_chunk(this ILogger logger, string filename, int mapCount)
        {
            s_could_not_create_bloom_filter_for_chunk(logger, filename, mapCount, null);
        }

        private static readonly Action<ILogger, string, long, Guid, Exception> s_skipping_message_with_duplicate_eventId =
            LoggerMessage.Define<string, long, Guid>(LogLevel.Warning, 0,
                "Skipping message {originalStreamId}/{originalEventNumber} with duplicate eventId {eventId}");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Skipping_message_with_duplicate_eventId(this ILogger logger, in OutstandingMessage message)
        {
            s_skipping_message_with_duplicate_eventId(logger, message.ResolvedEvent.OriginalStreamId, message.ResolvedEvent.OriginalEventNumber, message.EventId, null);
        }

        private static readonly Action<ILogger, int, Exception> s_batch_logging_enabled_high_rate_of_expired_read_messages_detected =
            LoggerMessage.Define<int>(LogLevel.Warning, 0,
                "StorageReaderWorker #{queueId}: Batch logging enabled, high rate of expired read messages detected");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Batch_logging_enabled_high_rate_of_expired_read_messages_detected(this ILogger logger, int queueId)
        {
            s_batch_logging_enabled_high_rate_of_expired_read_messages_detected(logger, queueId, null);
        }

        private static readonly Action<ILogger, int, Exception> s_batch_logging_disabled_read_load_is_back_to_normal =
            LoggerMessage.Define<int>(LogLevel.Warning, 0,
                "StorageReaderWorker #{queueId}: Batch logging disabled, read load is back to normal");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Batch_logging_disabled_read_load_is_back_to_normal(this ILogger logger, int queueId)
        {
            s_batch_logging_disabled_read_load_is_back_to_normal(logger, queueId, null);
        }

        private static readonly Action<ILogger, int, long, Exception> s_read_operations_have_expired =
            LoggerMessage.Define<int, long>(LogLevel.Warning, 0,
                "StorageReaderWorker #{queueId}: {expiredBatchCount} read operations have expired");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Read_operations_have_expired(this ILogger logger, int queueId, long expiredBatchCount)
        {
            s_read_operations_have_expired(logger, queueId, expiredBatchCount, null);
        }

        private static readonly Action<ILogger, string, Exception> s_timeout_reading_stream =
            LoggerMessage.Define<string>(LogLevel.Warning, 0,
                "Timeout reading stream: {stream}. Trying again in 10 seconds.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Timeout_reading_stream(this ILogger logger)
        {
            s_timeout_reading_stream(logger, Core.Services.UserManagement.UserManagementService.UserPasswordNotificationsStreamId, null);
        }

        private static readonly Action<ILogger, string, string, Exception> s_authenticationFailed =
            LoggerMessage.Define<string, string>(LogLevel.Warning, 0,
                "Authentication Failed for {id}: {reason}");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void AuthenticationFailed_Invalid_user(this ILogger logger, AuthenticationRequest authenticationRequest)
        {
            s_authenticationFailed(logger, authenticationRequest.Id, "Invalid user.", null);
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void AuthenticationFailed_The_system_is_not_ready(this ILogger logger, AuthenticationRequest authenticationRequest)
        {
            s_authenticationFailed(logger, authenticationRequest.Id, "The system is not ready.", null);
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void AuthenticationFailed_The_account_is_disabled(this ILogger logger, AuthenticationRequest authenticationRequest)
        {
            s_authenticationFailed(logger, authenticationRequest.Id, "The account is disabled.", null);
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void AuthenticationFailed_Invalid_credentials_supplied(this ILogger logger, AuthenticationRequest authenticationRequest)
        {
            s_authenticationFailed(logger, authenticationRequest.Id, "Invalid credentials supplied.", null);
        }

        private static readonly Action<ILogger, Exception> s_unexpected_error_in_StorageWriterService =
            LoggerMessage.Define(LogLevel.Critical, 0,
                "Unexpected error in StorageWriterService. Terminating the process...");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Unexpected_error_in_StorageWriterService(this ILogger logger, Exception exc)
        {
            s_unexpected_error_in_StorageWriterService(logger, exc);
        }

        private static readonly Action<ILogger, Exception> s_error_in_StorageChaser =
            LoggerMessage.Define(LogLevel.Critical, 0,
                "Error in StorageChaser. Terminating...");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Error_in_StorageChaser(this ILogger logger, Exception exc)
        {
            s_error_in_StorageChaser(logger, exc);
        }

        private static readonly Action<ILogger, Exception> s_error_in_IndexCommitterService =
            LoggerMessage.Define(LogLevel.Critical, 0,
                "Error in IndexCommitterService. Terminating...");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Error_in_IndexCommitterService(this ILogger logger, Exception exc)
        {
            s_error_in_IndexCommitterService(logger, exc);
        }
    }
}
