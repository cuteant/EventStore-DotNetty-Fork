using System;
using System.Runtime.CompilerServices;
using EventStore.Transport.Tcp.Messages;

namespace EventStore.ClientAPI.Internal
{
    partial class OperationsManager
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void LogOperationNeverGotResponseFromServer(OperationItem operation)
        {
            var err = $"operation never got response from server.\n UTC now: {DateTime.UtcNow:HH:mm:ss.fff}, operation: {operation}.";
            LogDebug(err);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void LogRetrying(Guid oldCorrId, OperationItem operation)
        {
            LogDebug("retrying, old corrId {0}, operation {1}.", oldCorrId, operation);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void LogScheduleOperationRetryFor(OperationItem operation)
        {
            LogDebug("ScheduleOperationRetry for {0}", operation);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void LogRemoveOperationFailedFor(OperationItem operation)
        {
            LogDebug("RemoveOperation FAILED for {0}", operation);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void LogRemoveOperationSucceededFor(OperationItem operation)
        {
            LogDebug("RemoveOperation SUCCEEDED for {0}", operation);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void LogRequestExpired(OperationItem operation)
        {
            var err = $"request expired.\nUTC now: {DateTime.UtcNow:HH:mm:ss.fff}, operation: {operation}.";
            LogDebug(err);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void LogExecuteOperationPackage(TcpPackage package, OperationItem operation)
        {
            LogDebug("ExecuteOperation package {0}, {1}, {2}.", package.Command, package.CorrelationId, operation);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void LogEnqueueOperationWaiting(OperationItem operation)
        {
            LogDebug("EnqueueOperation WAITING for {0}.", operation);
        }
    }
}
