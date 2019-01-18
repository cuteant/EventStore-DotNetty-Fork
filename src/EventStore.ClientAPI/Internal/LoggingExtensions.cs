﻿using System;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using EventStore.ClientAPI.ClientOperations;
using EventStore.ClientAPI.Messages;
using EventStore.Transport.Tcp;
using EventStore.Transport.Tcp.Messages;
using Microsoft.Extensions.Logging;

namespace EventStore.ClientAPI
{
    internal static class ClientCoreLoggingExtensions
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void PersistentSubscriptionRequestingStop(this ILogger logger, string streamId)
        {
            logger.LogDebug("Persistent Subscription to {0}: requesting stop...", streamId);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void PersistentDroppingSubscriptionReason(this ILogger logger, string streamId, SubscriptionDropReason reason, Exception error)
        {
            logger.LogDebug("Persistent Subscription to {0}: dropping subscription, reason: {1} {2}.",
                streamId, reason, error == null ? string.Empty : error.ToString());
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void PersistentSubscriptionProcessedEvent(this ILogger logger, string streamId, IPersistentSubscriptionResolvedEvent resolvedEvent)
        {
            logger.LogDebug("Persistent Subscription to {0}: processed event ({1}, {2}, {3} @ {4}).",
                      streamId,
                      resolvedEvent.OriginalStreamId, resolvedEvent.OriginalEventNumber, resolvedEvent.OriginalEventType, resolvedEvent.OriginalEventNumber);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void CatchupSubscriptionSubscribing(this ILogger logger, string subscriptionName, bool isSubscribedToAll, string streamId)
        {
            logger.LogDebug("Catch-up Subscription {0} to {1}: subscribing...", subscriptionName, isSubscribedToAll ? "<all>" : streamId);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void IdempotentWriteSucceededFor(this ILogger logger, IClientOperation operation)
        {
            logger.LogDebug("IDEMPOTENT WRITE SUCCEEDED FOR {0}.", operation);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SubscriptionDroppedByServerReason(this ILogger logger, TcpClientMessageDto.SubscriptionDropped.SubscriptionDropReason reason)
        {
            logger.LogDebug("Subscription dropped by server. Reason: {0}.", reason);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ClosingSubscription(this ILogger logger, Guid correlationId, string streamId, SubscriptionDropReason reason, Exception exc)
        {
            logger.LogDebug("Subscription {0:B} to {1}: closing subscription, reason: {2}, exception: {3}...",
                correlationId, streamId == string.Empty ? "<all>" : streamId, reason, exc);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SubscribedAtCommitPosition(this ILogger logger, Guid correlationId, string streamId, long lastCommitPosition, long? lastEventNumber)
        {
            logger.LogDebug("Subscription {0:B} to {1}: subscribed at CommitPosition: {2}, EventNumber: {3}.",
                correlationId, streamId == string.Empty ? "<all>" : streamId, lastCommitPosition, lastEventNumber);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SubscribedEventAppeared<TResolvedEvent>(this ILogger logger, Guid correlationId, string streamId, in TResolvedEvent e)
            where TResolvedEvent : IResolvedEvent
        {
            logger.LogDebug("Subscription {0:B} to {1}: event appeared ({2}, {3}, {4} @ {5}).",
                            correlationId, streamId == string.Empty ? "<all>" : streamId,
                            e.OriginalStreamId, e.OriginalEventNumber, e.OriginalEventType, e.OriginalPosition);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ConnectionWillBeClosed(this ILogger logger, in Disassociated disassociated, IPEndPoint remoteEndPoint, IPEndPoint localEndPoint, in Guid connectionId)
        {
            var message = string.Format("TcpPackageConnection: [{0}, L{1}, {2}] ERROR for {3}. Connection will be closed.",
                                        remoteEndPoint, localEndPoint, connectionId,
                                        "<invalid package>");
            logger.LogDebug(disassociated.Error, message);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ConnectionWasClosed(this ILogger logger, ITcpConnection connection, DisassociateInfo error)
        {
            logger.LogDebug("TcpPackageConnection: connection [{0}, L{1}, {2:B}] was closed {3}", connection.RemoteEndPoint, connection.LocalEndPoint,
                    connection.ConnectionId, error == DisassociateInfo.Success ? "cleanly." : "with error: " + error + ".");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ConnectToEstablished(this ILogger logger, ITcpConnection conn)
        {
            logger.LogDebug("TcpPackageConnection: connected to [{0}, L{1}, {2:B}].", conn.RemoteEndPoint, conn.LocalEndPoint, conn.ConnectionId);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ConnectToFailed(this ILogger logger, InvalidConnectionException exc, IPEndPoint remoteEndPoint)
        {
            logger.LogDebug(exc, "TcpPackageConnection: connection to [{0} failed. Error: ", remoteEndPoint);
        }

        private static readonly Action<ILogger, int, string, NodeEndPoints, Exception> s_discoveringAttemptSuccessful =
            LoggerMessageFactory.Define<int, string, NodeEndPoints>(LogLevel.Information,
            "Discovering attempt {attempt}{maxDiscoverAttemptsStr} successful: best candidate is {endPoints}.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void DiscoveringAttemptSuccessful(this ILogger logger, int attempt, string maxDiscoverAttemptsStr, in NodeEndPoints endPoints)
        {
            s_discoveringAttemptSuccessful(logger, attempt, maxDiscoverAttemptsStr, endPoints, null);
        }

        private static readonly Action<ILogger, int, string, Exception> s_discoveringAttemptFailed =
            LoggerMessageFactory.Define<int, string>(LogLevel.Information,
            "Discovering attempt {attempt}{maxDiscoverAttemptsStr} failed: no candidate found.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void DiscoveringAttemptFailed(this ILogger logger, int attempt, string maxDiscoverAttemptsStr)
        {
            s_discoveringAttemptFailed(logger, attempt, maxDiscoverAttemptsStr, null);
        }

        private static readonly Action<ILogger, int, string, Exception> s_discoveringAttemptFailedWithError =
            LoggerMessageFactory.Define<int, string>(LogLevel.Information,
            "Discovering attempt {attempt}{maxDiscoverAttemptsStr} failed with error: ");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void DiscoveringAttemptFailed(this ILogger logger, int attempt, string maxDiscoverAttemptsStr, Exception exc)
        {
            s_discoveringAttemptFailedWithError(logger, attempt, maxDiscoverAttemptsStr, exc);
        }

        private static readonly Action<ILogger, IPEndPoint, string, ClusterMessages.VNodeState, Exception> s_discoveringFoundBestChoice =
            LoggerMessageFactory.Define<IPEndPoint, string, ClusterMessages.VNodeState>(LogLevel.Information,
            "Discovering: found best choice [{normTcp},{secTcp}] ({state}).");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void DiscoveringFoundBestChoice(this ILogger logger, IPEndPoint normTcp, IPEndPoint secTcp, ClusterMessages.VNodeState state)
        {
            s_discoveringFoundBestChoice(logger, normTcp, secTcp == null ? "n/a" : secTcp.ToString(), state, null);
        }

        private static readonly Action<ILogger, string, Exception> s_noHandlerFoundForEventType =
            LoggerMessageFactory.Define<string>(LogLevel.Warning,
            "No handler found for event type {eventTypeName}, the default hander has been used.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void NoHandlerFoundForEventTypeTheDefaultHanderHasBeenUsed(this ILogger logger, Type eventType)
        {
            s_noHandlerFoundForEventType(logger, eventType.Name, null);
        }

        private static readonly Action<ILogger, string, Guid, long, string, Exception> s_canotDeserializeTheRecordedEvent =
            LoggerMessageFactory.Define<string, Guid, long, string>(LogLevel.Warning,
            "Can't deserialize the recorded event: StreamId - {eventStreamId}, EventId - {eventId}, EventNumber - {eventNumber}, EventType - {eventType}");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void CanotDeserializeTheRecordedEvent(this ILogger logger, TcpClientMessageDto.EventRecord systemRecord, Exception exc)
        {
            s_canotDeserializeTheRecordedEvent(logger, systemRecord.EventStreamId, new Guid(systemRecord.EventId), systemRecord.EventNumber, systemRecord.EventType, exc);
        }

        private static readonly Action<ILogger, string, Exception> s_noHandlerFo1undForEventTypeError =
            LoggerMessageFactory.Define<string>(LogLevel.Error,
            "No handler found for event type {eventTypeName}.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void NoHandlerFoundForEventType(this ILogger logger, Type eventType)
        {
            s_noHandlerFo1undForEventTypeError(logger, eventType.Name, null);
        }

        private static readonly Action<ILogger, string, Exception> s_failedToCreateInstanceOfType =
            LoggerMessageFactory.Define<string>(LogLevel.Error,
            "Failed to create instance of type: {typeFullName}.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void FailedToCreateInstanceOfType(this ILogger logger, TypeInfo typeInfo, Exception exception)
        {
            s_failedToCreateInstanceOfType(logger, typeInfo.FullName, exception);
        }

        private static readonly Action<ILogger, Exception> s_exceptionDuringExecutingUserCallback =
            LoggerMessageFactory.Define(LogLevel.Error,
            "Exception during executing user callback: ");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ExceptionDuringExecutingUserCallback(this ILogger logger, Exception exception)
        {
            s_exceptionDuringExecutingUserCallback(logger, exception);
        }

        private static readonly Action<ILogger, TcpClientMessageDto.NotHandled.NotHandledReason, Exception> s_unknownNotHandledReason =
            LoggerMessageFactory.Define<TcpClientMessageDto.NotHandled.NotHandledReason>(LogLevel.Error,
            "Unknown NotHandledReason: {reason}.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void UnknownNotHandledReason(this ILogger logger, TcpClientMessageDto.NotHandled.NotHandledReason reason)
        {
            s_unknownNotHandledReason(logger, reason, null);
        }
    }
}