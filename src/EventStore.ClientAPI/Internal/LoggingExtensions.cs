using System;
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
                streamId, reason, error is null ? string.Empty : error.ToString());
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
        internal static void ConnectionWillBeClosed(this ILogger logger, in Disassociated disassociated, IPEndPoint remoteEndPoint, IPEndPoint localEndPoint, Guid connectionId)
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

        private static readonly Action<ILogger, GossipSeed, int, string, Exception> s_respondedWithHttpStatus =
            LoggerMessage.Define<GossipSeed, int, string>(LogLevel.Information, 0,
            "[{endPoint}] responded with {httpStatusCode} ({statusDescription})");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void RespondedWithHttpStatus(this ILogger logger, GossipSeed endPoint, EventStore.ClientAPI.Transport.Http.HttpResponse response)
        {
            s_respondedWithHttpStatus(logger, endPoint, response.HttpStatusCode, response.StatusDescription, null);
        }

        private static readonly Action<ILogger, int, string, NodeEndPoints, Exception> s_discoveringAttemptSuccessful =
            LoggerMessage.Define<int, string, NodeEndPoints>(LogLevel.Information, 0,
            "Discovering attempt {attempt}{maxDiscoverAttemptsStr} successful: best candidate is {endPoints}.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void DiscoveringAttemptSuccessful(this ILogger logger, int attempt, string maxDiscoverAttemptsStr, in NodeEndPoints endPoints)
        {
            s_discoveringAttemptSuccessful(logger, attempt, maxDiscoverAttemptsStr, endPoints, null);
        }

        private static readonly Action<ILogger, int, string, Exception> s_discoveringAttemptFailed =
            LoggerMessage.Define<int, string>(LogLevel.Information, 0,
            "Discovering attempt {attempt}{maxDiscoverAttemptsStr} failed: no candidate found.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void DiscoveringAttemptFailed(this ILogger logger, int attempt, string maxDiscoverAttemptsStr)
        {
            s_discoveringAttemptFailed(logger, attempt, maxDiscoverAttemptsStr, null);
        }

        private static readonly Action<ILogger, int, string, Exception> s_discoveringAttemptFailedWithError =
            LoggerMessage.Define<int, string>(LogLevel.Information, 0,
            "Discovering attempt {attempt}{maxDiscoverAttemptsStr} failed with error: ");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void DiscoveringAttemptFailed(this ILogger logger, int attempt, string maxDiscoverAttemptsStr, Exception exc)
        {
            s_discoveringAttemptFailedWithError(logger, attempt, maxDiscoverAttemptsStr, exc);
        }

        private static readonly Action<ILogger, IPEndPoint, string, ClusterMessages.VNodeState, Exception> s_discoveringFoundBestChoice =
            LoggerMessage.Define<IPEndPoint, string, ClusterMessages.VNodeState>(LogLevel.Information, 0,
            "Discovering: found best choice [{normTcp},{secTcp}] ({state}).");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void DiscoveringFoundBestChoice(this ILogger logger, IPEndPoint normTcp, IPEndPoint secTcp, ClusterMessages.VNodeState state)
        {
            s_discoveringFoundBestChoice(logger, normTcp, secTcp is null ? "n/a" : secTcp.ToString(), state, null);
        }

        private static readonly Action<ILogger, string, Exception> s_noHandlerFoundForEventType =
            LoggerMessage.Define<string>(LogLevel.Warning, 0,
            "No handler found for event type {eventTypeName}, the default hander has been used.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void NoHandlerFoundForEventTypeTheDefaultHanderHasBeenUsed(this ILogger logger, Type eventType)
        {
            s_noHandlerFoundForEventType(logger, eventType.Name, null);
        }

        private static readonly Action<ILogger, string, Guid, long, string, Exception> s_canotDeserializeTheRecordedEvent =
            LoggerMessage.Define<string, Guid, long, string>(LogLevel.Warning, 0,
            "Can't deserialize the recorded event: StreamId - {eventStreamId}, EventId - {eventId}, EventNumber - {eventNumber}, EventType - {eventType}");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void CanotDeserializeTheRecordedEvent(this ILogger logger, TcpClientMessageDto.EventRecord systemRecord, Exception exc)
        {
            s_canotDeserializeTheRecordedEvent(logger, systemRecord.EventStreamId, systemRecord.EventId, systemRecord.EventNumber, systemRecord.EventType, exc);
        }

        private static readonly Action<ILogger, string, Exception> s_noHandlerFo1undForEventTypeError =
            LoggerMessage.Define<string>(LogLevel.Error, 0,
            "No handler found for event type {eventTypeName}.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void NoHandlerFoundForEventType(this ILogger logger, Type eventType)
        {
            s_noHandlerFo1undForEventTypeError(logger, eventType.Name, null);
        }

        private static readonly Action<ILogger, string, Exception> s_failedToCreateInstanceOfType =
            LoggerMessage.Define<string>(LogLevel.Error, 0,
            "Failed to create instance of type: {typeFullName}.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void FailedToCreateInstanceOfType(this ILogger logger, TypeInfo typeInfo, Exception exception)
        {
            s_failedToCreateInstanceOfType(logger, typeInfo.FullName, exception);
        }

        private static readonly Action<ILogger, Exception> s_exceptionDuringExecutingUserCallback =
            LoggerMessage.Define(LogLevel.Error, 0,
            "Exception during executing user callback: ");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ExceptionDuringExecutingUserCallback(this ILogger logger, Exception exception)
        {
            s_exceptionDuringExecutingUserCallback(logger, exception);
        }

        private static readonly Action<ILogger, TcpClientMessageDto.NotHandled.NotHandledReason, Exception> s_unknownNotHandledReason =
            LoggerMessage.Define<TcpClientMessageDto.NotHandled.NotHandledReason>(LogLevel.Error, 0,
            "Unknown NotHandledReason: {reason}.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void UnknownNotHandledReason(this ILogger logger, TcpClientMessageDto.NotHandled.NotHandledReason reason)
        {
            s_unknownNotHandledReason(logger, reason, null);
        }

        private static readonly Action<ILogger, GossipSeed, Exception> s_failed_to_get_cluster_info_deserialization =
            LoggerMessage.Define<GossipSeed>(LogLevel.Error, 0,
            "Failed to get cluster info from [{endPoint}]: deserialization error:");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Failed_to_get_cluster_info_deserialization(this ILogger logger, GossipSeed endPoint, Exception exception)
        {
            s_failed_to_get_cluster_info_deserialization(logger, endPoint, exception);
        }

        private static readonly Action<ILogger, GossipSeed, Exception> s_failed_to_get_cluster_info_request_failed =
            LoggerMessage.Define<GossipSeed>(LogLevel.Error, 0,
            "Failed to get cluster info from [{endPoint}]: request failed, error:");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Failed_to_get_cluster_info_request_failed(this ILogger logger, GossipSeed endPoint, Exception exception)
        {
            s_failed_to_get_cluster_info_request_failed(logger, endPoint, exception);
        }

        private static readonly Action<ILogger, Exception> s_cannot_perform_operation_as_this_node_is_Read_Only =
            LoggerMessage.Define(LogLevel.Error, 0,
            "Cannot perform operation as this node is Read Only");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Cannot_perform_operation_as_this_node_is_Read_Only(this ILogger logger)
        {
            s_cannot_perform_operation_as_this_node_is_Read_Only(logger, null);
        }
    }
}
