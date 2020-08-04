using System;
using System.Net;
using System.Runtime.CompilerServices;
using EventStore.ClientAPI.ClientOperations;
using EventStore.ClientAPI.SystemData;
using EventStore.ClientAPI.Transport.Tcp;
using EventStore.Transport.Tcp.Messages;
using Microsoft.Extensions.Logging;

namespace EventStore.ClientAPI.Internal
{
    partial class EventStoreConnectionLogicHandler
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void LogEnqueueingMessage(Message message)
        {
            s_logger.LogDebug("EventStoreConnection '{connectionName}': enqueueing message {msg}.", _esConnection.ConnectionName, message);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void LogStartConnection()
        {
            s_logger.LogDebug("EventStoreConnection '{connectionName}': StartConnection.", _esConnection.ConnectionName);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void LogDiscoverEndPoint()
        {
            s_logger.LogDebug("EventStoreConnection '{connectionName}': DiscoverEndPoint.", _esConnection.ConnectionName);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void LogEstablishTcpConnectionTo(IPEndPoint endPoint)
        {
            s_logger.LogDebug("EventStoreConnection '{connectionName}': EstablishTcpConnection to [{endPoint}].", _esConnection.ConnectionName, endPoint);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void LogTcpConnectionError(Guid connectionId, Exception exception)
        {
            s_logger.LogDebug(exception, "EventStoreConnection '{connectionName}': TcpConnectionError connId {connectionId:B}.",
                _esConnection.ConnectionName, connectionId);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void LogCloseConnectionIgnoredBecauseIsESConnectionIsClosed(string reason, Exception exception)
        {
            s_logger.LogDebug(exception, "EventStoreConnection '{connectionName}': CloseConnection IGNORED because is ESConnection is CLOSED, reason {reason}.",
                _esConnection.ConnectionName, reason);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void LogCloseConnectionReason(string reason, Exception exception)
        {
            s_logger.LogDebug(exception, "EventStoreConnection '{connectionName}': CloseConnection, reason {reason}.",
                _esConnection.ConnectionName, reason);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void LogCloseTcpConnectionIgnoredBecauseConnectionIsNull()
        {
            s_logger.LogDebug("EventStoreConnection '{connectionName}': CloseTcpConnection IGNORED because _connection is null.", _esConnection.ConnectionName);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void LogCloseTcpConnection()
        {
            s_logger.LogDebug("EventStoreConnection '{connectionName}': CloseTcpConnection.", _esConnection.ConnectionName);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void LogTcpConnectionClosed(TcpPackageConnection connection)
        {
            s_logger.LogDebug("EventStoreConnection '{connectionName}': TCP connection to [{remoteEndPoint}, L{localEndPoint}, {connectionId:B}] closed.",
                _esConnection.ConnectionName, connection.RemoteEndPoint, connection.LocalEndPoint, connection.ConnectionId);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void LogTcpConnectionEstablished(TcpPackageConnection connection)
        {
            s_logger.LogDebug("EventStoreConnection '{connectionName}': TCP connection to [{remoteEndPoint}, L{localEndPoint}, {connectionId:B}] established.",
                _esConnection.ConnectionName, connection.RemoteEndPoint, connection.LocalEndPoint, connection.ConnectionId);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void LogTimerTickCheckingReconnection()
        {
            s_logger.LogDebug("EventStoreConnection '{connectionName}': TimerTick checking reconnection...", _esConnection.ConnectionName);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void LogTimedoutWaitingForClientToBeIdentified()
        {
            s_logger.LogDebug("EventStoreConnection '{connectionName}': Timed out waiting for client to be identified.", _esConnection.ConnectionName);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void LogIgnoredBecauseTcpConnectionEstablished(TcpPackageConnection connection)
        {
            var innerConn = InternalConnection;
            s_logger.LogDebug("EventStoreConnection '{connectionName}': " +
                "IGNORED (_state {state}, _conn.Id {connId:B}, conn.Id {connectionId:B}, conn.closed {isClosed}): TCP connection to [{remoteEndPoint}, L{localEndPoint}] established.",
                _esConnection.ConnectionName,
                _state, innerConn is null ? Guid.Empty : innerConn.ConnectionId,
                connection.ConnectionId, connection.IsClosed, connection.RemoteEndPoint, connection.LocalEndPoint);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void LogIgnoredBecauseTcpConnectionClosed(TcpPackageConnection connection)
        {
            var innerConn = InternalConnection;
            s_logger.LogDebug("EventStoreConnection '{connectionName}': " +
                "IGNORED (_state: {state}, _conn.ID: {connId:B}, conn.ID: {connectionId:B}): TCP connection to [{remoteEndPoint}, L{localEndPoint}] closed.",
                _esConnection.ConnectionName,
                _state, innerConn is null ? Guid.Empty : innerConn.ConnectionId,
                connection.ConnectionId, connection.RemoteEndPoint, connection.LocalEndPoint);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void LogStartOperationEnqueue(IClientOperation operation, int maxRetries, TimeSpan timeout)
        {
            s_logger.LogDebug("EventStoreConnection '{connectionName}': StartOperation enqueue {typeName}, {op}, {maxRetries}, {timeout}.",
                _esConnection.ConnectionName, operation.GetType().Name, operation, maxRetries, timeout);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void LogStartOperationSchedule(IClientOperation operation, int maxRetries, TimeSpan timeout)
        {
            s_logger.LogDebug("EventStoreConnection '{connectionName}': StartOperation schedule {typeName}, {op}, {maxRetries}, {timeout}.",
                _esConnection.ConnectionName, operation.GetType().Name, operation, maxRetries, timeout);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void LogStartSubscription(ISubscriptionOperation operation, int maxRetries, TimeSpan timeout)
        {
            s_logger.LogDebug("EventStoreConnection '{connectionName}': StartSubscription {action} {typeName}, {op}, {maxRetries}, {timeout}.",
                _esConnection.ConnectionName, operation.GetType().Name, operation, maxRetries, timeout, _state == InternalConnectionState.Connected ? "fire" : "enqueue");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void LogIgnoredTcpPackage(Guid connectionId, TcpCommand command, Guid correlationId)
        {
            s_logger.LogDebug("EventStoreConnection '{connectionName}': IGNORED: HandleTcpPackage connId {connId}, package {command}, {correlationId}.",
                _esConnection.ConnectionName, connectionId, command, correlationId);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void LogHandleTcpPackage(Guid connectionId, TcpCommand command, Guid correlationId)
        {
            s_logger.LogDebug("EventStoreConnection '{connectionName}': HandleTcpPackage connId {connectionId}, package {command}, {correlationId}.",
                _esConnection.ConnectionName, connectionId, command, correlationId);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void LogHandleTcpPackageOPERATIONDECISION(InspectionResult result, OperationItem operation)
        {
            s_logger.LogDebug("EventStoreConnection '{connectionName}': HandleTcpPackage OPERATION DECISION {decision} ({description}), {operation}.",
                _esConnection.ConnectionName, result.Decision, result.Description, operation);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void LogHandleTcpPackageSUBSCRIPTIONDECISION(InspectionResult result, SubscriptionItem subscription)
        {
            s_logger.LogDebug("EventStoreConnection '{connectionName}': HandleTcpPackage SUBSCRIPTION DECISION {decision} ({description}), {subscription}.",
                _esConnection.ConnectionName, result.Decision, result.Description, subscription);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void LogHandleTcpPackageUNMAPPEDPACKAGE(Guid correlationId, TcpCommand command)
        {
            s_logger.LogDebug("EventStoreConnection '{connectionName}': HandleTcpPackage UNMAPPED PACKAGE with CorrelationId {correlationId:B}, Command: {command}.",
                _esConnection.ConnectionName, correlationId, command);
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        private void LogClosedReason(string reason)
        {
            s_logger.LogInformation("EventStoreConnection '{connectionName}': Closed. Reason: {reason}.", _esConnection.ConnectionName, reason);
        }
    }
}
