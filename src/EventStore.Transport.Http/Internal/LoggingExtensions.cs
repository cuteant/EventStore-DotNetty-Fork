using System;
using System.Net;
using System.Text;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace EventStore.Transport.Http
{
    internal static class TransportHttpLoggingExtensions
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void EndGetContextException(this ILogger logger, Exception e, bool isListening)
        {
            logger.LogDebug(e, "EndGetContext exception. Status : {0}.", isListening ? "listening" : "stopped");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void CloseConnectionErrorAfterCrashInReadRequest(this ILogger logger, Exception exc)
        {
            logger.LogDebug("Close connection error (after crash in read request): {0}", exc.Message);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void CloseConnectionError(this ILogger logger, string message, Exception exc)
        {
            logger.LogDebug(message + "\nException: " + exc.Message);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void FailedToSetAdditionalResponseHeaders(this ILogger logger, Exception e)
        {
            logger.LogDebug("Failed to set additional response headers: {0}.", e.Message);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void FailedToSetContentEncodingHeader(this ILogger logger, Exception e)
        {
            logger.LogDebug("Failed to set Content-Encoding header: {0}.", e.Message);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void FailedToSetRequiredResponseHeaders(this ILogger logger, Exception e)
        {
            logger.LogDebug("Failed to set required response headers: {0}.", e.Message);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ErrorDuringSettingContentLengthOnHttpResponse(this ILogger logger, Exception e)
        {
            logger.LogDebug("Error during setting content length on HTTP response: {0}.", e.Message);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ErrorDuringSettingContentTypeOnHttpResponse(this ILogger logger, Exception e)
        {
            logger.LogDebug("Error during setting content type on HTTP response: {0}.", e.Message);
        }

        private static readonly Action<ILogger, string, Exception> s_errorWhileClosingStream =
            LoggerMessageFactory.Define<string>(LogLevel.Information,
            "Error while closing stream : {errMsg}");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ErrorWhileClosingStream(this ILogger logger, Exception exception)
        {
            s_errorWhileClosingStream(logger, exception.Message, null);
        }

        private static readonly Action<ILogger, string, string, Exception> s_attemptingToAddPermissionsUsingNetsh =
            LoggerMessageFactory.Define<string, string>(LogLevel.Information,
            "Attempting to add permissions for {address} using netsh {args}");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void AttemptingToAddPermissionsUsingNetsh(this ILogger logger, string address, string args)
        {
            s_attemptingToAddPermissionsUsingNetsh(logger, address, args, null);
        }

        private static readonly Action<ILogger, string, Exception> s_httpServerIsUpAndListeningOn =
            LoggerMessageFactory.Define<string>(LogLevel.Information,
            "HTTP server is up and listening on [{prefixes}]");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void HttpServerIsUpAndListeningOn(this ILogger logger, HttpListener listener)
        {
            s_httpServerIsUpAndListeningOn(logger, string.Join(",", listener.Prefixes), null);
        }

        private static readonly Action<ILogger, string, Exception> s_retryingHttpServerOn =
            LoggerMessageFactory.Define<string>(LogLevel.Information,
            "Retrying HTTP server on [{prefixes}]...");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void RetryingHttpServerOn(this ILogger logger, HttpListener listener)
        {
            s_retryingHttpServerOn(logger, string.Join(",", listener.Prefixes), null);
        }

        private static readonly Action<ILogger, string, Exception> s_startingHttpServerOn =
            LoggerMessageFactory.Define<string>(LogLevel.Information,
            "Starting HTTP server on [{prefixes}]...");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void StartingHttpServerOn(this ILogger logger, HttpListener listener)
        {
            s_startingHttpServerOn(logger, string.Join(",", listener.Prefixes), null);
        }

        private static readonly Action<ILogger, string, Exception> s_beginGetContextError =
            LoggerMessageFactory.Define<string>(LogLevel.Error,
            "BeginGetContext error. Status : {status}.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void BeginGetContextError(this ILogger logger, Exception e, bool isListening)
        {
            s_beginGetContextError(logger, isListening ? "listening" : "stopped", e);
        }

        private static readonly Action<ILogger, Exception> s_processRequestError =
            LoggerMessageFactory.Define(LogLevel.Error,
            "ProcessRequest error");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ProcessRequestError(this ILogger logger, Exception e)
        {
            s_processRequestError(logger, e);
        }

        private static readonly Action<ILogger, Exception> s_errorWhileShuttingDownHttpServer =
            LoggerMessageFactory.Define(LogLevel.Error,
            "Error while shutting down http server");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ErrorWhileShuttingDownHttpServer(this ILogger logger, Exception e)
        {
            s_errorWhileShuttingDownHttpServer(logger, e);
        }

        private static readonly Action<ILogger, Uri, Exception> s_failedToSetupForwardedResponseParameters =
            LoggerMessageFactory.Define<Uri>(LogLevel.Error,
            "Failed to set up forwarded response parameters for '{requestedUrl}'.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void FailedToSetupForwardedResponseParameters(this ILogger logger, Uri requestedUrl, Exception e)
        {
            s_failedToSetupForwardedResponseParameters(logger, requestedUrl, e);
        }

        private static readonly Action<ILogger, long, Exception> s_attemptToSetInvalidContentLength =
            LoggerMessageFactory.Define<long>(LogLevel.Error,
            "Attempt to set invalid value '{length}' as content length.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void AttemptToSetInvalidContentLength(this ILogger logger, long length, ArgumentOutOfRangeException e)
        {
            s_attemptToSetInvalidContentLength(logger, length, e);
        }

        private static readonly Action<ILogger, Exception> s_invalidResponseType =
            LoggerMessageFactory.Define(LogLevel.Error,
            "Invalid response type.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void InvalidResponseType(this ILogger logger, ArgumentOutOfRangeException e)
        {
            s_invalidResponseType(logger, e);
        }

        private static readonly Action<ILogger, string, Exception> s_descriptionStringDidnotPassValidation =
            LoggerMessageFactory.Define<string>(LogLevel.Error,
            "Description string '{desc}' did not pass validation. Status description was not set.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void DescriptionStringDidnotPassValidation(this ILogger logger, string desc, ArgumentException e)
        {
            s_descriptionStringDidnotPassValidation(logger, desc, e);
        }

        private static readonly Action<ILogger, Exception> s_attemptToSetInvalidHttpStatusCodeOccurred =
            LoggerMessageFactory.Define(LogLevel.Error,
            "Attempt to set invalid HTTP status code occurred.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void AttemptToSetInvalidHttpStatusCodeOccurred(this ILogger logger, ProtocolViolationException e)
        {
            s_attemptToSetInvalidHttpStatusCodeOccurred(logger, e);
        }

        private static readonly Action<ILogger, string, Exception> s_errorSerializingObjectOfType =
            LoggerMessageFactory.Define<string>(LogLevel.Error,
            "Error serializing object of type {typeName}.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ErrorSerializingObjectOfType<T>(this ILogger logger, Exception e)
        {
            s_errorSerializingObjectOfType(logger, typeof(T).FullName, e);
        }

        private static readonly Action<ILogger, string, string, Exception> s_isNotAValidSerialized =
            LoggerMessageFactory.Define<string, string>(LogLevel.Error,
            "'{text}' is not a valid serialized {typeName}");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void IsNotAValidSerialized<T>(this ILogger logger, string text, Exception e)
        {
            s_isNotAValidSerialized(logger, text, typeof(T).FullName, e);
        }

        private static readonly Action<ILogger, string, Exception> s_isNotAValidSerialized0 =
            LoggerMessageFactory.Define<string>(LogLevel.Error,
            "'{text}' is not a valid serialized");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void IsNotAValidSerialized(this ILogger logger, byte[] json, Exception e)
        {
            s_isNotAValidSerialized0(logger, Encoding.UTF8.GetString(json), e);
        }
    }
}
