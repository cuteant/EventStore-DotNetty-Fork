using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using EventStore.Common.Utils;
using EventStore.Core.Bus;
using EventStore.Core.Data;
using EventStore.Core.Exceptions;
using EventStore.Core.Messages;
using EventStore.Core.Messaging;
using EventStore.Core.Services;
using EventStore.Core.Services.Replication;
using EventStore.Core.Services.Storage.EpochManager;
using EventStore.Core.Services.Transport.Tcp;
using EventStore.Core.Services.UserManagement;
using EventStore.Core.TransactionLog.Chunks.TFChunk;
using EventStore.Core.TransactionLog.LogRecords;
using EventStore.Transport.Http;
using EventStore.Transport.Http.EntityManagement;
using EventStore.Transport.Tcp;
using EventStore.Transport.Tcp.Messages;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace EventStore.Core
{
    partial class CoreLoggingExtensions
    {
        private static readonly Action<ILogger, string, string, int, string, Exception> s_very_slow_bus_msg =
            LoggerMessageFactory.Define<string, string, int, string>(LogLevel.Error,
                "---!!! VERY SLOW BUS MSG [{bus}]: {message} - {elapsed}ms. Handler: {handler}.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Very_slow_bus_msg(this ILogger logger, string name, Message message, int elapsed, IMessageHandler handler)
        {
            s_very_slow_bus_msg(logger, name, message.GetType().Name, elapsed, handler.HandlerName, null);
        }

        private static readonly Action<ILogger, VNodeInfo, Exception> s_internalSecureConnectionsAreRequired =
            LoggerMessageFactory.Define<VNodeInfo>(LogLevel.Error,
                "Internal secure connections are required, but no internal secure TCP end point is specified for master [{master}]!");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void InternalSecureConnectionsAreRequired(this ILogger logger, VNodeInfo master)
        {
            s_internalSecureConnectionsAreRequired(logger, master, null);
        }

        private static readonly Action<ILogger, Exception> s_exceptionInStorageWriter =
            LoggerMessageFactory.Define(LogLevel.Error,
                "Exception in writer.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ExceptionInStorageWriter(this ILogger logger, Exception ex)
        {
            s_exceptionInStorageWriter(logger, ex);
        }

        private static readonly Action<ILogger, double, string, OperationResult, Exception> s_failedToWriteTheMaxageOfDaysMetadataForTheStream =
            LoggerMessageFactory.Define<double, string, OperationResult>(LogLevel.Error,
                "Failed to write the $maxAge of {days} days metadata for the {stream} stream. Reason: {reason}");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void FailedToWriteTheMaxageOfDaysMetadataForTheStream(this ILogger logger, double totalDays, OperationResult result)
        {
            s_failedToWriteTheMaxageOfDaysMetadataForTheStream(logger, totalDays, SystemStreams.ScavengesStream, result, null);
        }
        internal static void FailedToWriteTheMaxageOfDaysMetadataForTheStream(this ILogger logger, double totalDays, string stream, OperationResult result)
        {
            s_failedToWriteTheMaxageOfDaysMetadataForTheStream(logger, totalDays, stream, result, null);
        }

        private static readonly Action<ILogger, string, int, int, OperationResult, Exception> s_failedToWriteAnEventToTheStreamRetrying =
            LoggerMessageFactory.Define<string, int, int, OperationResult>(LogLevel.Error,
                "Failed to write an event to the {stream} stream. Retrying {retry}/{retryCount}. Reason: {reason}");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void FailedToWriteAnEventToTheStreamRetrying(this ILogger logger, string stream, int retry, int retryCount, OperationResult result)
        {
            s_failedToWriteAnEventToTheStreamRetrying(logger, stream, retry, retryCount, result, null);
        }

        private static readonly Action<ILogger, string, int, OperationResult, Exception> s_failedToWriteAnEventToTheStreamRetryLimitOfReached =
            LoggerMessageFactory.Define<string, int, OperationResult>(LogLevel.Error,
                "Failed to write an event to the {stream} stream. Retry limit of {retryCount} reached. Reason: {reason}");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void FailedToWriteAnEventToTheStreamRetryLimitOfReached(this ILogger logger, string stream, int retryCount, OperationResult result)
        {
            s_failedToWriteAnEventToTheStreamRetryLimitOfReached(logger, stream, retryCount, result, null);
        }

        private static readonly Action<ILogger, int, Exception> s_failedToDeleteTheTempChunkRetryLimitOfReached =
            LoggerMessageFactory.Define<int>(LogLevel.Error,
                "Failed to delete the temp chunk. Retry limit of {maxRetryCount} reached. Reason: ");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void FailedToDeleteTheTempChunkRetryLimitOfReached(this ILogger logger, int maxRetryCount, Exception ex)
        {
            s_failedToDeleteTheTempChunkRetryLimitOfReached(logger, maxRetryCount, ex);
        }

        private static readonly Action<ILogger, int, int, Exception> s_failedToDeleteTheTempChunkRetrying =
            LoggerMessageFactory.Define<int, int>(LogLevel.Error,
                "Failed to delete the temp chunk. Retrying {retry}/{maxRetryCount}. Reason: ");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void FailedToDeleteTheTempChunkRetrying(this ILogger logger, int retries, int maxRetryCount, Exception ex)
        {
            s_failedToDeleteTheTempChunkRetrying(logger, maxRetryCount - retries, maxRetryCount, ex);
        }

        private static readonly Action<ILogger, Exception> s_ioexceptionDuringCreatingNewChunkForScavengingMergePurposes =
            LoggerMessageFactory.Define(LogLevel.Error,
                "IOException during creating new chunk for scavenging merge purposes. Stopping scavenging merge process...");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void IoExceptionDuringCreatingNewChunkForScavengingMergePurposes(this ILogger logger, Exception ex)
        {
            s_ioexceptionDuringCreatingNewChunkForScavengingMergePurposes(logger, ex);
        }

        private static readonly Action<ILogger, Exception> s_ioExceptionDuringCreatingNewChunkForScavengingPurposes =
            LoggerMessageFactory.Define(LogLevel.Error,
                "IOException during creating new chunk for scavenging purposes. Stopping scavenging process...");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void IOExceptionDuringCreatingNewChunkForScavengingPurposes(this ILogger logger, Exception ex)
        {
            s_ioExceptionDuringCreatingNewChunkForScavengingPurposes(logger, ex);
        }

        private static readonly Action<ILogger, TransactionLog.Chunks.ScavengeResult, TimeSpan, string, Exception> s_errorWhilstRecordingScavengeCompleted =
            LoggerMessageFactory.Define<TransactionLog.Chunks.ScavengeResult, TimeSpan, string>(LogLevel.Error,
                "Error whilst recording scavenge completed. Scavenge result: {result}, Elapsed: {elapsed}, Original error: {e}");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ErrorWhilstRecordingScavengeCompleted(this ILogger logger,
            EventStore.Core.TransactionLog.Chunks.ScavengeResult result, TimeSpan elapsed, string error, Exception ex)
        {
            s_errorWhilstRecordingScavengeCompleted(logger, result, elapsed, error, ex);
        }

        private static readonly Action<ILogger, Exception> s_scavengingErrorWhileScavengingDb =
            LoggerMessageFactory.Define(LogLevel.Error,
                "SCAVENGING: error while scavenging DB.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ScavengingErrorWhileScavengingDb(this ILogger logger, Exception ex)
        {
            s_scavengingErrorWhileScavengingDb(logger, ex);
        }

        private static readonly Action<ILogger, string, Exception> s_errorWhileTryingToDeleteRemainingTempFile =
            LoggerMessageFactory.Define<string>(LogLevel.Error,
                "Error while trying to delete remaining temp file: '{tempFile}'.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ErrorWhileTryingToDeleteRemainingTempFile(this ILogger logger, string tempFile, Exception ex)
        {
            s_errorWhileTryingToDeleteRemainingTempFile(logger, tempFile, ex);
        }

        private static readonly Action<ILogger, TFChunk, Exception> s_cachingFailedDueToOutofmemoryExceptionInTFChunk =
            LoggerMessageFactory.Define<TFChunk>(LogLevel.Error,
                "CACHING FAILED due to OutOfMemory exception in TFChunk {chunk}.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void CachingFailedDueToOutofmemoryExceptionInTFChunk(this ILogger logger, TFChunk chunk)
        {
            s_cachingFailedDueToOutofmemoryExceptionInTFChunk(logger, chunk, null);
        }

        private static readonly Action<ILogger, EventRecord, Exception> s_errorWhileResolvingLinkForEventRecord =
            LoggerMessageFactory.Define<EventRecord>(LogLevel.Error,
                "Error while resolving link for event record: {eventRecord}");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ErrorWhileResolvingLinkForEventRecord(this ILogger logger, EventRecord eventRecord, Exception ex)
        {
            s_errorWhileResolvingLinkForEventRecord(logger, eventRecord, ex);
        }

        private static readonly Action<ILogger, long, long, long, long, Exception> s_receivedDatachunkbulkAtSubscriptionpositionWhileCurrentSubscriptionpositionIs =
            LoggerMessageFactory.Define<long, long, long, long>(LogLevel.Error,
                "Received DataChunkBulk at SubscriptionPosition {subscriptionPosition} (0x{subscriptionPosition:X}) while current SubscriptionPosition is {subscriptionPos} (0x{subscriptionPos:X}).");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ReceivedDatachunkbulkAtSubscriptionpositionWhileCurrentSubscriptionpositionIs(this ILogger logger, long subscriptionPosition, long subscriptionPos)
        {
            s_receivedDatachunkbulkAtSubscriptionpositionWhileCurrentSubscriptionpositionIs(logger, subscriptionPosition, subscriptionPosition, subscriptionPos, subscriptionPos, null);
        }

        private static readonly Action<ILogger, int, int, int, int, Exception> s_receivedDataChunkBulkForTFChunkButActiveChunkIs =
            LoggerMessageFactory.Define<int, int, int, int>(LogLevel.Error,
                "Received DataChunkBulk for TFChunk {chunkStartNumber}-{chunkEndNumber}, but active chunk is {activeChunkStartNumber}-{activeChunkEndNumber}.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ReceivedDataChunkBulkForTFChunkButActiveChunkIs(this ILogger logger, int chunkStartNumber, int chunkEndNumber, int activeChunkStartNumber, int activeChunkEndNumber)
        {
            s_receivedDataChunkBulkForTFChunkButActiveChunkIs(logger, chunkStartNumber, chunkEndNumber, activeChunkStartNumber, activeChunkEndNumber, null);
        }

        private static readonly Action<ILogger, int, int, int, int, Exception> s_receivedRawchunkbulkAtRawPosWhileCurrentWriterRawPosIs =
            LoggerMessageFactory.Define<int, int, int, int>(LogLevel.Error,
                "Received RawChunkBulk at raw pos {rawPosition} (0x{rawPosition:X}) while current writer raw pos is {rawWriterPosition} (0x{rawWriterPosition:X}).");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ReceivedRawchunkbulkAtRawPosWhileCurrentWriterRawPosIs(this ILogger logger, int rawPosition, int rawWriterPosition)
        {
            s_receivedRawchunkbulkAtRawPosWhileCurrentWriterRawPosIs(logger, rawPosition, rawPosition, rawWriterPosition, rawWriterPosition, null);
        }

        private static readonly Action<ILogger, int, int, TFChunk, Exception> s_receivedRawChunkBulkForTFChunkButActiveChunkIs =
            LoggerMessageFactory.Define<int, int, TFChunk>(LogLevel.Error,
                "Received RawChunkBulk for TFChunk {chunkStartNumber}-{chunkEndNumber}, but active chunk is {activeChunk}.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ReceivedRawChunkBulkForTFChunkButActiveChunkIs(this ILogger logger, int chunkStartNumber, int chunkEndNumber, TFChunk activeChunk)
        {
            s_receivedRawChunkBulkForTFChunkButActiveChunkIs(logger, chunkStartNumber, chunkEndNumber, activeChunk, null);
        }

        private static readonly Action<ILogger, Exception> s_attemptToTruncateEpochWithCommittedRecords =
            LoggerMessageFactory.Define(LogLevel.Error,
                "ATTEMPT TO TRUNCATE EPOCH WITH COMMITTED RECORDS. THIS MAY BE BAD, BUT IT IS OK IF JUST-ELECTED MASTER FAILS IMMEDIATELY AFTER ITS ELECTION.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void AttemptToTruncateEpochWithCommittedRecords(this ILogger logger)
        {
            s_attemptToTruncateEpochWithCommittedRecords(logger, null);
        }

        private static readonly Action<ILogger, IPEndPoint, Guid, long, long, long, long, long, long, Exception> s_masterSubscribedUsAtWhichIsLessThanOurLastEpochAndLastcommitposition =
            LoggerMessageFactory.Define<IPEndPoint, Guid, long, long, long, long, long, long>(LogLevel.Error,
                "Master [{masterEndPoint},{masterId:B}] subscribed us at {subscriptionPosition} (0x{subscriptionPosition:X}), which is less than our last epoch and LastCommitPosition {lastCommitPosition} (0x{lastCommitPosition:X}) >= lastEpoch.EpochPosition {lastEpochPosition} (0x{lastEpochPosition:X}). That might be bad, especially if the LastCommitPosition is way beyond EpochPosition.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void MasterSubscribedUsAtWhichIsLessThanOurLastEpochAndLastcommitposition(this ILogger logger, IPEndPoint masterEndPoint, Guid masterId, long subscriptionPosition, long lastCommitPosition, long lastEpochPosition)
        {
            s_masterSubscribedUsAtWhichIsLessThanOurLastEpochAndLastcommitposition(logger, masterEndPoint, masterId, subscriptionPosition, subscriptionPosition, lastCommitPosition, lastCommitPosition, lastEpochPosition, lastEpochPosition, null);
        }

        private static readonly Action<ILogger, IPEndPoint, Exception> s_vNodeShutdownTimeout =
            LoggerMessageFactory.Define<IPEndPoint>(LogLevel.Error,
                "========== [{internalHttp}] Shutdown Timeout.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void VNodeShutdownTimeout(this ILogger logger, VNodeInfo nodeInfo)
        {
            s_vNodeShutdownTimeout(logger, nodeInfo.InternalHttp, null);
        }

        private static readonly Action<ILogger, Exception> s_errorWhenStoppingWorkersOrMainQueue =
            LoggerMessageFactory.Define(LogLevel.Error,
                "Error when stopping workers/main queue.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ErrorWhenStoppingWorkersOrMainQueue(this ILogger logger, Exception exc)
        {
            s_errorWhenStoppingWorkersOrMainQueue(logger, exc);
        }

        private static readonly Action<ILogger, SystemMessage.BecomeShutdown, Exception> s_errorWhenPublishing =
            LoggerMessageFactory.Define<SystemMessage.BecomeShutdown>(LogLevel.Error,
                "Error when publishing {msg}.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ErrorWhenPublishing(this ILogger logger, SystemMessage.BecomeShutdown message, Exception exc)
        {
            s_errorWhenPublishing(logger, message, exc);
        }

        private static readonly Action<ILogger, Exception> s_opsUserAccountCouldNotBeCreated =
            LoggerMessageFactory.Define(LogLevel.Error,
                "'ops' user account could not be created.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void OpsUserAccountCouldNotBeCreated(this ILogger logger)
        {
            s_opsUserAccountCouldNotBeCreated(logger, null);
        }

        private static readonly Action<ILogger, Exception> s_opsUserAccountCreationTimedOutRetrying =
            LoggerMessageFactory.Define(LogLevel.Error,
                "'ops' user account creation timed out retrying.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void OpsUserAccountCreationTimedOutRetrying(this ILogger logger)
        {
            s_opsUserAccountCreationTimedOutRetrying(logger, null);
        }

        private static readonly Action<ILogger, OperationResult, Exception> s_unableToAddOpsToUsers =
            LoggerMessageFactory.Define<OperationResult>(LogLevel.Error,
                "unable to add 'ops' to $users. {result}");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void UnableToAddOpsToUsers(this ILogger logger, OperationResult result)
        {
            s_unableToAddOpsToUsers(logger, result, null);
        }

        private static readonly Action<ILogger, Exception> s_adminUserAccountCouldNotBeCreated =
            LoggerMessageFactory.Define(LogLevel.Error,
                "'admin' user account could not be created.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void AdminUserAccountCouldNotBeCreated(this ILogger logger)
        {
            s_adminUserAccountCouldNotBeCreated(logger, null);
        }

        private static readonly Action<ILogger, Exception> s_adminUserAccountCreationTimedOutRetrying =
            LoggerMessageFactory.Define(LogLevel.Error,
                "'admin' user account creation timed out retrying.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void AdminUserAccountCreationTimedOutRetrying(this ILogger logger)
        {
            s_adminUserAccountCreationTimedOutRetrying(logger, null);
        }

        private static readonly Action<ILogger, OperationResult, Exception> s_unableToAddAdminToUsers =
            LoggerMessageFactory.Define<OperationResult>(LogLevel.Error,
                "unable to add 'admin' to $users. {result}");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void UnableToAddAdminToUsers(this ILogger logger, OperationResult result)
        {
            s_unableToAddAdminToUsers(logger, result, null);
        }

        private static readonly Action<ILogger, TcpCommand, Exception> s_errorWhileUnwrappingTcppackageWithCommand =
            LoggerMessageFactory.Define<TcpCommand>(LogLevel.Error,
                "Error while unwrapping TcpPackage with command {command}.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ErrorWhileUnwrappingTcppackageWithCommand(this ILogger logger, TcpCommand command, Exception exc)
        {
            s_errorWhileUnwrappingTcppackageWithCommand(logger, command, exc);
        }

        private static readonly Action<ILogger, Message, Exception> s_errorWhileWrappingMessage =
            LoggerMessageFactory.Define<Message>(LogLevel.Error,
                "Error while wrapping message {msg}.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ErrorWhileWrappingMessage(this ILogger logger, Message message, Exception exc)
        {
            s_errorWhileWrappingMessage(logger, message, exc);
        }

        private static readonly Action<ILogger, string, string, IPEndPoint, IPEndPoint, Guid, string, Exception> s_closingConnectionDueToError =
            LoggerMessageFactory.Define<string, string, IPEndPoint, IPEndPoint, Guid, string>(LogLevel.Error,
                "Closing connection '{connectionName}{clientConnectionName}' [{remoteEndPoint}, L{localEndPoint}, {connectionId:B}] due to error. Reason: {e}");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ClosingConnectionDueToError(this ILogger logger, TcpConnectionManager tcpConnection, string message)
        {
            s_closingConnectionDueToError(logger,
                tcpConnection.ConnectionName,
                tcpConnection.ClientConnectionName.IsEmptyString() ? string.Empty : ":" + tcpConnection.ClientConnectionName,
                tcpConnection.RemoteEndPoint,
                tcpConnection.LocalEndPoint,
                tcpConnection.ConnectionId,
                message, null);
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ClosingConnectionDueToError(this ILogger logger, TcpConnectionManager tcpConnection, in Disassociated disassociated)
        {
            ClosingConnectionDueToError(logger, tcpConnection, disassociated.ToString());
        }

        private static readonly Action<ILogger, string, string, IPEndPoint, IPEndPoint, Guid, Guid, string, Exception> s_badRequestReceivedFromWillStopServer =
            LoggerMessageFactory.Define<string, string, IPEndPoint, IPEndPoint, Guid, Guid, string>(LogLevel.Error,
                "Bad request received from '{connectionName}{clientConnectionName}' [{remoteEndPoint}, L{localEndPoint}, {connectionId:B}], will stop server. CorrelationId: {correlationId:B}, Error: {e}.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void BadRequestReceivedFromWillStopServer(this ILogger logger, TcpConnectionManager tcpConnection, Guid correlationId, string reason)
        {
            s_badRequestReceivedFromWillStopServer(logger,
                tcpConnection.ConnectionName,
                tcpConnection.ClientConnectionName.IsEmptyString() ? string.Empty : ":" + tcpConnection.ClientConnectionName,
                tcpConnection.RemoteEndPoint,
                tcpConnection.LocalEndPoint,
                tcpConnection.ConnectionId,
                correlationId,
                reason.IsEmptyString() ? "<reason missing>" : reason, null);
        }

        private static readonly Action<ILogger, Exception> s_errorIdentifyingClient =
            LoggerMessageFactory.Define(LogLevel.Error,
                "Error identifying client: ");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ErrorIdentifyingClient(this ILogger logger, Exception exc)
        {
            s_errorIdentifyingClient(logger, exc);
        }

        private static readonly Action<ILogger, Exception> s_errorPurgingTimedOutRequestsInHttpRequestProcessor =
            LoggerMessageFactory.Define(LogLevel.Error,
                "Error purging timed out requests in HTTP request processor.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ErrorPurgingTimedOutRequestsInHttpRequestProcessor(this ILogger logger, Exception exc)
        {
            s_errorPurgingTimedOutRequestsInHttpRequestProcessor(logger, exc);
        }

        private static readonly Action<ILogger, Uri, Exception> s_errorWhileHandlingHttpRequest =
            LoggerMessageFactory.Define<Uri>(LogLevel.Error,
                "Error while handling HTTP request '{requestUrl}'.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ErrorWhileHandlingHttpRequest(this ILogger logger, Uri requestUrl, Exception exc)
        {
            s_errorWhileHandlingHttpRequest(logger, requestUrl, exc);
        }

        private static readonly Action<ILogger, string, Exception> s_unhandledExceptionWhileProcessingHttpRequestAt =
            LoggerMessageFactory.Define<string>(LogLevel.Error,
                "Unhandled exception while processing HTTP request at [{listenPrefixes}].");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void UnhandledExceptionWhileProcessingHttpRequestAt(this ILogger logger, IEnumerable<string> listenPrefixes, Exception exc)
        {
            s_unhandledExceptionWhileProcessingHttpRequestAt(logger, string.Join(", ", listenPrefixes), exc);
        }

        private static readonly Action<ILogger, Exception> s_errorWhileWritingHttpResponsePing =
            LoggerMessageFactory.Define(LogLevel.Error,
                "Error while writing HTTP response (ping)");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ErrorWhileWritingHttpResponsePing(this ILogger logger, Exception exc)
        {
            s_errorWhileWritingHttpResponsePing(logger, exc);
        }

        private static readonly Action<ILogger, Exception> s_errorWhileWritingHttpResponseOptions =
            LoggerMessageFactory.Define(LogLevel.Error,
                "error while writing HTTP response (options)");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ErrorWhileWritingHttpResponseOptions(this ILogger logger, Exception exc)
        {
            s_errorWhileWritingHttpResponseOptions(logger, exc);
        }

        private static readonly Action<ILogger, Exception> s_errorWhileWritingHttpResponseInfo =
            LoggerMessageFactory.Define(LogLevel.Error,
                "Error while writing HTTP response (info)");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ErrorWhileWritingHttpResponseInfo(this ILogger logger, Exception exc)
        {
            s_errorWhileWritingHttpResponseInfo(logger, exc);
        }

        private static readonly Action<ILogger, Uri, string, Exception> s_receivedAsPostInvalidClusterinfoContentType =
            LoggerMessageFactory.Define<Uri, string>(LogLevel.Error,
                "Received as POST invalid ClusterInfo from [{requestedUrl}]. Content-Type: {contentType}.");
        private static readonly Action<ILogger, Uri, string, Exception> s_receivedAsPostInvalidClusterinfoBody =
            LoggerMessageFactory.Define<Uri, string>(LogLevel.Error,
                "Received as POST invalid ClusterInfo from [{requestedUrl}]. Body: {body}.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ReceivedAsPostInvalidClusterinfo(this ILogger logger, HttpEntityManager manager, string body)
        {
            s_receivedAsPostInvalidClusterinfoContentType(logger, manager.RequestedUrl, manager.RequestCodec.ContentType, null);
            s_receivedAsPostInvalidClusterinfoBody(logger, manager.RequestedUrl, body, null);
        }

        private static readonly Action<ILogger, string, string, Exception> s_receivedAsResponseInvalidClusterinfoContentType =
            LoggerMessageFactory.Define<string, string>(LogLevel.Error,
                "Received as RESPONSE invalid ClusterInfo from [{url}]. Content-Type: {contentType}.");
        private static readonly Action<ILogger, string, string, Exception> s_receivedAsResponseInvalidClusterinfoBody =
            LoggerMessageFactory.Define<string, string>(LogLevel.Error,
                "Received as RESPONSE invalid ClusterInfo from [{url}]. Body: {body}.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ReceivedAsResponseInvalidClusterinfo(this ILogger logger, string url, HttpResponse response)
        {
            s_receivedAsResponseInvalidClusterinfoContentType(logger, url, response.ContentType, null);
            s_receivedAsResponseInvalidClusterinfoBody(logger, url, response.Body, null);
        }

        private static readonly Action<ILogger, Exception> s_errorWhileWritingHttpResponse =
            LoggerMessageFactory.Define(LogLevel.Error,
                "Error while writing HTTP response");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ErrorWhileWritingHttpResponse(this ILogger logger, Exception exc)
        {
            s_errorWhileWritingHttpResponse(logger, exc);
        }

        private static readonly Action<ILogger, long, Exception> s_failedToDeserializeEvent =
            LoggerMessageFactory.Define<long>(LogLevel.Error,
                "Failed to de-serialize event #{originalEventNumber}.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void FailedToDeserializeEvent(this ILogger logger, long originalEventNumber, JsonException exc)
        {
            s_failedToDeserializeEvent(logger, originalEventNumber, exc);
        }

        private static readonly Action<ILogger, string, ReadStreamResult, Exception> s_failedToReadUserPasswordNotificationsStream =
            LoggerMessageFactory.Define<string, ReadStreamResult>(LogLevel.Error,
                "Failed to read: {stream} completed.Result={e}");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void FailedToReadUserPasswordNotificationsStream(this ILogger logger, ReadStreamResult result)
        {
            s_failedToReadUserPasswordNotificationsStream(logger, UserManagementService.UserPasswordNotificationsStreamId, result, null);
        }

        private static readonly Action<ILogger, long, int, string, Exception> s_invalidTransactionInfoFoundForTransactionId =
            LoggerMessageFactory.Define<long, int, string>(LogLevel.Error,
                "Invalid transaction info found for transaction ID {transactionId}. Possibly wrong transactionId provided. TransactionOffset: {transactionOffset}, EventStreamId: {streamId}");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void InvalidTransactionInfoFoundForTransactionId(this ILogger logger, long transactionId, in Core.Services.Storage.ReaderIndex.TransactionInfo transactionInfo)
        {
            s_invalidTransactionInfoFoundForTransactionId(logger,
                    transactionId,
                    transactionInfo.TransactionOffset,
                    transactionInfo.EventStreamId.IsEmptyString() ? "<null>" : transactionInfo.EventStreamId, null);
        }

        private static readonly Action<ILogger, string, long?, Exception> s_errorDuringProcessingCheckstreamaccessRequest =
            LoggerMessageFactory.Define<string, long?>(LogLevel.Error,
                "Error during processing CheckStreamAccess({eventStreamId}, {transactionId}) request.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ErrorDuringProcessingCheckstreamaccessRequest(this ILogger logger, StorageMessage.CheckStreamAccess msg, Exception exc)
        {
            s_errorDuringProcessingCheckstreamaccessRequest(logger, msg.EventStreamId, msg.TransactionId, exc);
        }

        private static readonly Action<ILogger, Exception> s_errorDuringProcessingReadAllEventsBackwardRequest =
            LoggerMessageFactory.Define(LogLevel.Error,
                "Error during processing ReadAllEventsBackward request.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ErrorDuringProcessingReadAllEventsBackwardRequest(this ILogger logger, Exception exc)
        {
            s_errorDuringProcessingReadAllEventsBackwardRequest(logger, exc);
        }

        private static readonly Action<ILogger, Exception> s_errorDuringProcessingReadAllEventsForwardRequest =
            LoggerMessageFactory.Define(LogLevel.Error,
                "Error during processing ReadAllEventsForward request.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ErrorDuringProcessingReadAllEventsForwardRequest(this ILogger logger, Exception exc)
        {
            s_errorDuringProcessingReadAllEventsForwardRequest(logger, exc);
        }

        private static readonly Action<ILogger, Exception> s_errorDuringProcessingReadStreamEventsBackwardRequest =
            LoggerMessageFactory.Define(LogLevel.Error,
                "Error during processing ReadStreamEventsBackward request.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ErrorDuringProcessingReadStreamEventsBackwardRequest(this ILogger logger, Exception exc)
        {
            s_errorDuringProcessingReadStreamEventsBackwardRequest(logger, exc);
        }

        private static readonly Action<ILogger, Exception> s_errorDuringProcessingReadStreamEventsForwardRequest =
            LoggerMessageFactory.Define(LogLevel.Error,
                "Error during processing ReadStreamEventsForward request.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ErrorDuringProcessingReadStreamEventsForwardRequest(this ILogger logger, Exception exc)
        {
            s_errorDuringProcessingReadStreamEventsForwardRequest(logger, exc);
        }

        private static readonly Action<ILogger, Exception> s_errorDuringProcessingReadEventRequest =
            LoggerMessageFactory.Define(LogLevel.Error,
                "Error during processing ReadEvent request.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ErrorDuringProcessingReadEventRequest(this ILogger logger, Exception exc)
        {
            s_errorDuringProcessingReadEventRequest(logger, exc);
        }

        private static readonly Action<ILogger, Exception> s_errorWhileStoppingReadersMultiHandler =
            LoggerMessageFactory.Define(LogLevel.Error,
                "Error while stopping readers multi handler.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ErrorWhileStoppingReadersMultiHandler(this ILogger logger, Exception exc)
        {
            s_errorWhileStoppingReadersMultiHandler(logger, exc);
        }

        private static readonly Action<ILogger, string, Exception> s_aHashCollisionResultedInNotFindingTheLastEventNumberForTheStream =
            LoggerMessageFactory.Define<string>(LogLevel.Error,
                "A hash collision resulted in not finding the last event number for the stream {streamId}.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void AHashCollisionResultedInNotFindingTheLastEventNumberForTheStream(this ILogger logger, string streamId)
        {
            s_aHashCollisionResultedInNotFindingTheLastEventNumberForTheStream(logger, streamId, null);
        }

        private static readonly Action<ILogger, Exception> s_errorDeserializingSystemsettingsRecord =
            LoggerMessageFactory.Define(LogLevel.Error,
                "Error deserializing SystemSettings record.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ErrorDeserializingSystemsettingsRecord(this ILogger logger, Exception exc)
        {
            s_errorDeserializingSystemsettingsRecord(logger, exc);
        }

        private static readonly Action<ILogger, Exception> s_timeoutExceptionWhenTryingToCloseTableindex =
            LoggerMessageFactory.Define(LogLevel.Error,
                "Timeout exception when trying to close TableIndex.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void TimeoutExceptionWhenTryingToCloseTableindex(this ILogger logger, Exception exc)
        {
            s_timeoutExceptionWhenTryingToCloseTableindex(logger, exc);
        }

        private static readonly Action<ILogger, IPEndPoint, Guid, long, long, string, long, long, string, Exception> s_noCommonEpochFoundForReplica =
            LoggerMessageFactory.Define<IPEndPoint, Guid, long, long, string, long, long, string>(LogLevel.Error,
                "No common epoch found for replica [{replicaEndPoint},S{subscriptionId},{logPosition}(0x{logPosition:X}),{epochs}]. "
                + "Subscribing at 0. Master LogPosition: {masterCheckpoint} (0x{masterCheckpoint:X}), known epochs: {knownEpochs}.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void NoCommonEpochFoundForReplica(this ILogger logger,
            IPEndPoint replicaEndPoint, Guid subscriptionId, long logPosition, Epoch[] epochs, long masterCheckpoint, IEpochManager epochManager)
        {
            s_noCommonEpochFoundForReplica(logger,
                replicaEndPoint, subscriptionId,
                logPosition, logPosition,
                string.Join(", ", epochs.Select(x => x.AsString())),
                masterCheckpoint, masterCheckpoint,
                string.Join(", ", epochManager.GetLastEpochs(int.MaxValue).Select(x => x.AsString())),
                null);
        }

        private static readonly Action<ILogger, Exception> s_exceptionWhileSubscribingReplicaConnectionWillBeDropped =
            LoggerMessageFactory.Define(LogLevel.Error,
                "Exception while subscribing replica. Connection will be dropped.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ExceptionWhileSubscribingReplicaConnectionWillBeDropped(this ILogger logger, Exception exc)
        {
            s_exceptionWhileSubscribingReplicaConnectionWillBeDropped(logger, exc);
        }

        private static readonly Action<ILogger, Guid, MasterReplicationService.ReplicaSubscription, Exception> s_thereIsAlreadyASubscriptionWithSubscriptionid =
            LoggerMessageFactory.Define<Guid, MasterReplicationService.ReplicaSubscription>(LogLevel.Error,
                "There is already a subscription with SubscriptionID {subscriptionId:B}: {existingSubscription}.");
        private static readonly Action<ILogger, MasterReplicationService.ReplicaSubscription, Exception> s_subscriptionWeTriedToAdd =
            LoggerMessageFactory.Define<MasterReplicationService.ReplicaSubscription>(LogLevel.Error,
                "Subscription we tried to add: {existingSubscription}.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThereIsAlreadyASubscriptionWithSubscriptionid(this ILogger logger, Guid subscriptionId, MasterReplicationService.ReplicaSubscription existingSubscr)
        {
            s_thereIsAlreadyASubscriptionWithSubscriptionid(logger, subscriptionId, existingSubscr, null);
            s_subscriptionWeTriedToAdd(logger, existingSubscr, null);
        }

        private static readonly Action<ILogger, Exception> s_thereWasAnErrorLoadingConfigurationFromStorage =
            LoggerMessageFactory.Define(LogLevel.Error,
                "There was an error loading configuration from storage.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThereWasAnErrorLoadingConfigurationFromStorage(this ILogger logger, Exception exc)
        {
            s_thereWasAnErrorLoadingConfigurationFromStorage(logger, exc);
        }

        private static readonly Action<ILogger, string, Exception> s_aPersistentSubscriptionExistsWithAnInvalidConsumerStrategy =
            LoggerMessageFactory.Define<string>(LogLevel.Error,
                "A persistent subscription exists with an invalid consumer strategy '{namedConsumerStrategy}'. Ignoring it.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void APersistentSubscriptionExistsWithAnInvalidConsumerStrategy(this ILogger logger, string namedConsumerStrategy)
        {
            s_aPersistentSubscriptionExistsWithAnInvalidConsumerStrategy(logger, namedConsumerStrategy, null);
        }

        private static readonly Action<ILogger, Exception> s_messagesWereNotRemovedOnRetry =
            LoggerMessageFactory.Define(LogLevel.Error,
                "Messages were not removed on retry");

        private static readonly Action<ILogger, string, OperationResult, Exception> s_anErrorOccuredTruncatingTheParkedMessageStream =
            LoggerMessageFactory.Define<string, OperationResult>(LogLevel.Error,
                "An error occured truncating the parked message stream {streamId} due to {result}.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void AnErrorOccuredTruncatingTheParkedMessageStream(this ILogger logger, string parkedStreamId, OperationResult result)
        {
            s_anErrorOccuredTruncatingTheParkedMessageStream(logger, parkedStreamId, result, null);
            s_messagesWereNotRemovedOnRetry(logger, null);
        }

        private static readonly Action<ILogger, string, ReadStreamResult, Exception> s_anErrorOccuredReadingTheLastEventInTheParkedMessageStream =
            LoggerMessageFactory.Define<string, ReadStreamResult>(LogLevel.Error,
                "An error occured reading the last event in the parked message stream {streamId} due to {result}.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void AnErrorOccuredReadingTheLastEventInTheParkedMessageStream(this ILogger logger, string parkedStreamId, ReadStreamResult result)
        {
            s_anErrorOccuredReadingTheLastEventInTheParkedMessageStream(logger, parkedStreamId, result, null);
            s_messagesWereNotRemovedOnRetry(logger, null);
        }

        private static readonly Action<ILogger, string, long, OperationResult, Exception> s_unableToParkMessageOperationFailedAfterRetriesPossibleMessageLoss =
            LoggerMessageFactory.Define<string, long, OperationResult>(LogLevel.Error,
                "Unable to park message {originalStreamId}/{originalEventNumber} operation failed {result} after retries. Possible message loss");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void UnableToParkMessageOperationFailedAfterRetriesPossibleMessageLoss(this ILogger logger, in ResolvedEvent e, OperationResult result)
        {
            s_unableToParkMessageOperationFailedAfterRetriesPossibleMessageLoss(logger, e.OriginalStreamId, e.OriginalEventNumber, result, null);
        }

        private static readonly Action<ILogger, Exception> s_maximumRebuildAttemptsReachedGivingUpOnRebuilds =
            LoggerMessageFactory.Define(LogLevel.Error,
                "Maximum rebuild attempts reached. Giving up on rebuilds.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void MaximumRebuildAttemptsReachedGivingUpOnRebuilds(this ILogger logger)
        {
            s_maximumRebuildAttemptsReachedGivingUpOnRebuilds(logger, null);
        }

        private static readonly Action<ILogger, Exception> s_errorOnGettingFreshTcpConnectionStats =
            LoggerMessageFactory.Define(LogLevel.Error,
                "Error on getting fresh tcp connection stats");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ErrorOnGettingFreshTcpConnectionStats(this ILogger logger, Exception exc)
        {
            s_errorOnGettingFreshTcpConnectionStats(logger, exc);
        }

        private static readonly Action<ILogger, Exception> s_errorOnGettingFreshStats =
            LoggerMessageFactory.Define(LogLevel.Error,
                "Error on getting fresh stats");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ErrorOnGettingFreshStats(this ILogger logger, Exception exc)
        {
            s_errorOnGettingFreshStats(logger, exc);
        }

        private static readonly Action<ILogger, OperationResult, Exception> s_monitoringServiceGotUnexpectedResponseCodeWhenTryingToCreateStatsStream =
            LoggerMessageFactory.Define<OperationResult>(LogLevel.Error,
                "Monitoring service got unexpected response code when trying to create stats stream ({result}).");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void MonitoringServiceGotUnexpectedResponseCodeWhenTryingToCreateStatsStream(this ILogger logger, OperationResult result)
        {
            s_monitoringServiceGotUnexpectedResponseCodeWhenTryingToCreateStatsStream(logger, result, null);
        }

        private static readonly Action<ILogger, Exception> s_errorWhileCollectingStats =
            LoggerMessageFactory.Define(LogLevel.Error,
                "Error while collecting stats");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ErrorWhileCollectingStats(this ILogger logger, Exception exc)
        {
            s_errorWhileCollectingStats(logger, exc);
        }

        private static readonly Action<ILogger, Exception> s_errorOnRegularStatsCollection =
            LoggerMessageFactory.Define(LogLevel.Error,
                "Error on regular stats collection.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ErrorOnRegularStatsCollection(this ILogger logger, Exception exc)
        {
            s_errorOnRegularStatsCollection(logger, exc);
        }

        private static readonly Action<ILogger, string, Exception> s_couldNotRetrieveListOfProcessesUsingFileHandle =
            LoggerMessageFactory.Define<string>(LogLevel.Error,
                "Could not retrieve list of processes using file handle {path}");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void CouldNotRetrieveListOfProcessesUsingFileHandle(this ILogger logger, string path, Exception exc)
        {
            s_couldNotRetrieveListOfProcessesUsingFileHandle(logger, path, exc);
        }

        private static readonly Action<ILogger, string, Exception> s_tryingToRetrieveListOfProcessesHavingAFileHandleOpen =
            LoggerMessageFactory.Define<string>(LogLevel.Error,
                "Trying to retrieve list of processes having a file handle open on {path} (requires admin privileges)");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void TryingToRetrieveListOfProcessesHavingAFileHandleOpen(this ILogger logger, string path)
        {
            s_tryingToRetrieveListOfProcessesHavingAFileHandleOpen(logger, path, null);
        }

        private static readonly Action<ILogger, string, string, Exception> s_processesLocking =
            LoggerMessageFactory.Define<string, string>(LogLevel.Error,
                "Processes locking {path}:" + Environment.NewLine + "{processList}");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ProcessesLocking(this ILogger logger, string path, string processList)
        {
            s_processesLocking(logger, path, processList, null);
        }

        private static readonly Action<ILogger, IPEndPoint, DateTime, DateTime, Exception> s_timeDifferenceBetweenUsAndPeerendpointIsTooGreat =
            LoggerMessageFactory.Define<IPEndPoint, DateTime, DateTime>(LogLevel.Error,
                "Time difference between us and [{peerEndPoint}] is too great! "
                + "UTC now: {dateTime:yyyy-MM-dd HH:mm:ss.fff}, peer's time stamp: {peerTimestamp:yyyy-MM-dd HH:mm:ss.fff}.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void TimeDifferenceBetweenUsAndPeerendpointIsTooGreat(this ILogger logger, IPEndPoint peerEndPoint, DateTime peerTimestamp)
        {
            s_timeDifferenceBetweenUsAndPeerendpointIsTooGreat(logger, peerEndPoint, DateTime.UtcNow, peerTimestamp, null);
        }

        private static readonly Action<ILogger, Exception> s_errorWhileRetrievingClusterMembersThroughDNS =
            LoggerMessageFactory.Define(LogLevel.Error,
                "Error while retrieving cluster members through DNS.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ErrorWhileRetrievingClusterMembersThroughDNS(this ILogger logger, Exception exc)
        {
            s_errorWhileRetrievingClusterMembersThroughDNS(logger, exc);
        }

        private static readonly Action<ILogger, string, Exception> s_framingError =
            LoggerMessageFactory.Define<string>(LogLevel.Error,
                "FRAMING ERROR! Data:\n{data}");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void FramingError(this ILogger logger, ArraySegment<byte> bytes)
        {
            s_framingError(logger, Helper.FormatBinaryDump(bytes), null);
        }

        private static readonly Action<ILogger, string, string, Exception> s_failedTrialToReplaceIndexmap =
            LoggerMessageFactory.Define<string, string>(LogLevel.Error,
                "Failed trial to replace indexmap {filename} with {tmpIndexMap}.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void FailedTrialToReplaceIndexmap(this ILogger logger, string filename, string tmpIndexMap, Exception ex)
        {
            s_failedTrialToReplaceIndexmap(logger, filename, tmpIndexMap, ex);
        }

        private static readonly Action<ILogger, string, long, int, Exception> s_unableToCreateMidpointsForPtable =
            LoggerMessageFactory.Define<string, long, int>(LogLevel.Error,
                "Unable to create midpoints for PTable '{filename}' ({count} entries, depth {depth} requested). "
                          + "Performance hit will occur. OOM Exception.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void UnableToCreateMidpointsForPtable(this ILogger logger, string filename, long count, int depth)
        {
            s_unableToCreateMidpointsForPtable(logger, Path.GetFileName(filename), count, depth, null);
        }

        private static readonly Action<ILogger, string, Exception> s_unableToDeleteUnwantedScavengedPtable =
            LoggerMessageFactory.Define<string>(LogLevel.Error,
                "Unable to delete unwanted scavenged PTable: {outputFile}");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void UnableToDeleteUnwantedScavengedPtable(this ILogger logger, string outputFile, Exception ex)
        {
            s_unableToDeleteUnwantedScavengedPtable(logger, outputFile, ex);
        }

        private static readonly Action<ILogger, string, Exception> s_couldNotDeleteForceIndexVerificationFileAt =
            LoggerMessageFactory.Define<string>(LogLevel.Error,
                "Could not delete force index verification file at: {path}");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void CouldNotDeleteForceIndexVerificationFileAt(this ILogger logger, string path)
        {
            s_couldNotDeleteForceIndexVerificationFileAt(logger, path, null);
        }

        private static readonly Action<ILogger, string, Exception> s_couldNotCreateForceIndexVerificationFileAt =
            LoggerMessageFactory.Define<string>(LogLevel.Error,
                "Could not create force index verification file at: {path}");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void CouldNotCreateForceIndexVerificationFileAt(this ILogger logger, string path)
        {
            s_couldNotCreateForceIndexVerificationFileAt(logger, path, null);
        }

        private static readonly Action<ILogger, Exception> s_errorInTableIndexReadOffQueue =
            LoggerMessageFactory.Define(LogLevel.Error,
                "Error in TableIndex.ReadOffQueue");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ErrorInTableIndexReadOffQueue(this ILogger logger, Exception exc)
        {
            s_errorInTableIndexReadOffQueue(logger, exc);
        }

        private static readonly Action<ILogger, Exception> s_couldNotAcquireChunkInTableIndexReadOffQueue =
            LoggerMessageFactory.Define(LogLevel.Error,
                "Could not acquire chunk in TableIndex.ReadOffQueue. It is OK if node is shutting down.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void CouldNotAcquireChunkInTableIndexReadOffQueue(this ILogger logger, FileBeingDeletedException exc)
        {
            s_couldNotAcquireChunkInTableIndexReadOffQueue(logger, exc);
        }

        private static readonly Action<ILogger, string, Exception> s_unexpectedErrorWhileCopyingIndexToBackupDir =
            LoggerMessageFactory.Define<string>(LogLevel.Error,
                "Unexpected error while copying index to backup dir '{dumpPath}'");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void UnexpectedErrorWhileCopyingIndexToBackupDir(this ILogger logger, string dumpPath, Exception exc)
        {
            s_unexpectedErrorWhileCopyingIndexToBackupDir(logger, dumpPath, exc);
        }

        private static readonly Action<ILogger, string, Exception> s_makingBackupOfIndexFolderForInspectionTo =
            LoggerMessageFactory.Define<string>(LogLevel.Error,
                "Making backup of index folder for inspection to {dumpPath}...");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void MakingBackupOfIndexFolderForInspectionTo(this ILogger logger, string dumpPath)
        {
            s_makingBackupOfIndexFolderForInspectionTo(logger, dumpPath, null);
        }

        private static readonly Action<ILogger, string, Exception> s_unexpectedErrorWhileDumpingIndexmap =
            LoggerMessageFactory.Define<string>(LogLevel.Error,
                "Unexpected error while dumping IndexMap '{indexmapFile}'.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void UnexpectedErrorWhileDumpingIndexmap(this ILogger logger, string indexmapFile, Exception exc)
        {
            s_unexpectedErrorWhileDumpingIndexmap(logger, indexmapFile, exc);
        }

        private static readonly Action<ILogger, string, string, Exception> s_indexMapAndContent =
            LoggerMessageFactory.Define<string, string>(LogLevel.Error,
                "IndexMap '{indexmapFile}' content:\n {data}");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void IndexMapAndContent(this ILogger logger, string indexmapFile)
        {
            s_indexMapAndContent(logger, indexmapFile, Helper.FormatBinaryDump(File.ReadAllBytes(indexmapFile)), null);
        }

        private static readonly Action<ILogger, Exception> s_readindexIsCorrupted =
            LoggerMessageFactory.Define(LogLevel.Error,
                "ReadIndex is corrupted...");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ReadindexIsCorrupted(this ILogger logger, CorruptIndexException exc)
        {
            s_readindexIsCorrupted(logger, exc);
        }

        private static readonly Action<ILogger, Message, string, Exception> s_errorWhileProcessingMessageInQueuedHandler =
            LoggerMessageFactory.Define<Message, string>(LogLevel.Error,
                "Error while processing message {msg} in queued handler '{queue}'.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ErrorWhileProcessingMessageInQueuedHandler(this ILogger logger, Message msg, string queue, Exception ex)
        {
            s_errorWhileProcessingMessageInQueuedHandler(logger, msg, queue, ex);
        }

        private static readonly Action<ILogger, string, string, int, int, int, Exception> s_verySlowQueueMsg =
            LoggerMessageFactory.Define<string, string, int, int, int>(LogLevel.Error,
                "---!!! VERY SLOW QUEUE MSG [{name}]: {msgName} - {totalMilliseconds}ms. Q: {queueCnt}/{currentQueueCnt}.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void VerySlowQueueMsg(this ILogger logger, QueueStatsCollector queueStats, int totalMilliseconds, int queueCnt, int currentQueueCnt)
        {
            s_verySlowQueueMsg(logger, queueStats.Name, queueStats.InProgressMessage.Name, totalMilliseconds, queueCnt, currentQueueCnt, null);
        }
    }
}
