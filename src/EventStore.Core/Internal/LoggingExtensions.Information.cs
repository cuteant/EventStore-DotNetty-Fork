using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using EventStore.Common.Utils;
using EventStore.Core.Data;
using EventStore.Core.Exceptions;
using EventStore.Core.Messages;
using EventStore.Core.Services;
using EventStore.Core.Services.Monitoring.Utils;
using EventStore.Core.Services.PersistentSubscription;
using EventStore.Core.Services.Replication;
using EventStore.Core.Services.Transport.Tcp;
using EventStore.Core.TransactionLog.Chunks;
using EventStore.Core.TransactionLog.Chunks.TFChunk;
using EventStore.Core.TransactionLog.LogRecords;
using EventStore.Transport.Tcp;
using Microsoft.Extensions.Logging;

namespace EventStore.Core
{
    partial class CoreLoggingExtensions
    {
        private static readonly Action<ILogger, Exception> s_starting_scavenge_of_TableIndex =
            LoggerMessageFactory.Define(LogLevel.Information,
                "Starting scavenge of TableIndex.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Starting_scavenge_of_TableIndex(this ILogger logger)
        {
            s_starting_scavenge_of_TableIndex(logger, null);
        }

        private static readonly Action<ILogger, TimeSpan, Exception> s_completed_scavenge_of_TableIndex =
            LoggerMessageFactory.Define<TimeSpan>(LogLevel.Information,
                "Completed scavenge of TableIndex.  Elapsed: {elapsed}");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Completed_scavenge_of_TableIndex(this ILogger logger, TimeSpan elapsed)
        {
            s_completed_scavenge_of_TableIndex(logger, elapsed, null);
        }

        private static readonly Action<ILogger, string, Exception> s_defaulting_DB_Path_to =
            LoggerMessageFactory.Define<string>(LogLevel.Information,
                "Defaulting DB Path to {dbPath}");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Defaulting_DB_Path_to(this ILogger logger, string dbPath)
        {
            s_defaulting_DB_Path_to(logger, dbPath, null);
        }

        private static readonly Action<ILogger, string, string, Exception> s_access_to_path_denied =
            LoggerMessageFactory.Define<string, string>(LogLevel.Information,
                "Access to path {dbPath} denied. The Event Store database will be created in {fallbackDefaultDataDirectory}");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Access_to_path_denied(this ILogger logger, string dbPath)
        {
            s_access_to_path_denied(logger, dbPath, Locations.FallbackDefaultDataDirectory, null);
        }

        private static readonly Action<ILogger, string, Exception> s_db_mutex_is_said_to_be_abandoned =
            LoggerMessageFactory.Define<string>(LogLevel.Information,
                "DB mutex '{mutexName}' is said to be abandoned. Probably previous instance of server was terminated abruptly.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void DB_mutex_is_said_to_be_abandoned(this ILogger logger, string mutexName, AbandonedMutexException exc)
        {
            s_db_mutex_is_said_to_be_abandoned(logger, mutexName, exc);
        }

        private static readonly Action<ILogger, long, long, long, long, long, long, long, long, Exception> s_truncate_checkpoint_is_present =
            LoggerMessageFactory.Define<long, long, long, long, long, long, long, long>(LogLevel.Information,
                "Truncate checkpoint is present. Truncate: {truncatePosition} (0x{truncatePosition:X}), Writer: {writerCheckpoint} (0x{writerCheckpoint:X}), Chaser: {chaserCheckpoint} (0x{chaserCheckpoint:X}), Epoch: {epochCheckpoint} (0x{epochCheckpoint:X})");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Truncate_checkpoint_is_present(this ILogger logger, long truncPos, TFChunkDb db)
        {
            var writerCheckpoint = db.Config.WriterCheckpoint.Read();
            var chaserCheckpoint = db.Config.ChaserCheckpoint.Read();
            var epochCheckpoint = db.Config.EpochCheckpoint.Read();
            s_truncate_checkpoint_is_present(logger,
                     truncPos, truncPos, writerCheckpoint, writerCheckpoint, chaserCheckpoint, chaserCheckpoint, epochCheckpoint, epochCheckpoint, null);
        }

        private static readonly Action<ILogger, string, Exception> s_cluster_node_mutex_is_said_to_be_ab1andoned =
            LoggerMessageFactory.Define<string>(LogLevel.Information,
                "Cluster Node mutex '{mutexName}' is said to be abandoned. Probably previous instance of server was terminated abruptly.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Cluster_Node_mutex_is_said_to_be_abando1ned(this ILogger logger, string mutexName, AbandonedMutexException exc)
        {
            s_cluster_node_mutex_is_said_to_be_ab1andoned(logger, mutexName, exc);
        }

        private static readonly Action<ILogger, Exception> s_error_while_replying_from_MiniWeb =
            LoggerMessageFactory.Define(LogLevel.Information,
                "Error while replying from MiniWeb");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Error_while_replying_from_MiniWeb(this ILogger logger, Exception exc)
        {
            s_error_while_replying_from_MiniWeb(logger, exc);
        }

        private static readonly Action<ILogger, string, string, Exception> s_replying_404_for =
            LoggerMessageFactory.Define<string, string>(LogLevel.Information,
                "Replying 404 for {contentLocalPath} ==> {fullPath}");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Replying_404_for(this ILogger logger, string contentLocalPath, string fullPath)
        {
            s_replying_404_for(logger, contentLocalPath, fullPath, null);
        }

        private static readonly Action<ILogger, string, string, Exception> s_starting_MiniWeb_for =
            LoggerMessageFactory.Define<string, string>(LogLevel.Information,
                "Starting MiniWeb for {localWebRootPath} ==> {fileSystemRoot}");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Starting_MiniWeb_for(this ILogger logger, string localWebRootPath, string fileSystemRoot)
        {
            s_starting_MiniWeb_for(logger, localWebRootPath, fileSystemRoot, null);
        }

        private static readonly Action<ILogger, int, string, string, string, string, Exception> s_found_incomplete_scavenges_on_node =
            LoggerMessageFactory.Define<int, string, string, string, string>(LogLevel.Information,
                "Found {incomplete} incomplete scavenge{s} on node {nodeEndPoint}. Marking as failed:{newLine}{incompleteScavenges}");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Found_incomplete_scavenges_on_node(this ILogger logger, IList<string> incompletedScavenges, string nodeEndpoint)
        {
            s_found_incomplete_scavenges_on_node(logger,
                incompletedScavenges.Count,
                incompletedScavenges.Count == 1 ? "" : "s",
                nodeEndpoint,
                Environment.NewLine,
                string.Join(Environment.NewLine, incompletedScavenges),
                null);
        }

        private static readonly Action<ILogger, long, long, Exception> s_movingWritercheckpointAsItPointsToTheScavengedChunk =
            LoggerMessageFactory.Define<long, long>(LogLevel.Information,
                "Moving WriterCheckpoint from {checkpoint} to {chunkEndPosition}, as it points to the scavenged chunk. "
                + "If that was not caused by replication of scavenged chunks, that could be a bug.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void MovingWritercheckpointAsItPointsToTheScavengedChunk(this ILogger logger, long checkpoint, long chunkEndPosition)
        {
            s_movingWritercheckpointAsItPointsToTheScavengedChunk(logger, checkpoint, chunkEndPosition, null);
        }

        private static readonly Action<ILogger, string, Exception> s_fileHasBeenMarkedForDeleteAndWillBeDeletedInTrydestructfilestreams =
            LoggerMessageFactory.Define<string>(LogLevel.Information,
                "File {fileName} has been marked for delete and will be deleted in TryDestructFileStreams.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void FileHasBeenMarkedForDeleteAndWillBeDeletedInTrydestructfilestreams(this ILogger logger, string filename)
        {
            s_fileHasBeenMarkedForDeleteAndWillBeDeletedInTrydestructfilestreams(logger, Path.GetFileName(filename), null);
        }

        private static readonly Action<ILogger, int, string, string, Exception> s_elections_done_elected_master =
            LoggerMessageFactory.Define<int, string, string>(LogLevel.Information,
                "ELECTIONS: (V={view}) DONE. ELECTED MASTER = {masterInfo}. ME={ownInfo}.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Elections_done_elected_master(this ILogger logger, int view, ElectionsService.MasterCandidate masterProposal, ElectionsService service)
        {
            s_elections_done_elected_master(logger, view, ElectionsService.FormatNodeInfo(masterProposal), ElectionsService.FormatNodeInfo(service.GetOwnInfo()), null);
        }

        private static readonly Action<ILogger, Exception> s_waitingForTableindexBackgroundTaskToCompleteBeforeStartingScavenge =
            LoggerMessageFactory.Define(LogLevel.Information,
                "Waiting for TableIndex background task to complete before starting scavenge.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void WaitingForTableindexBackgroundTaskToCompleteBeforeStartingScavenge(this ILogger logger)
        {
            s_waitingForTableindexBackgroundTaskToCompleteBeforeStartingScavenge(logger, null);
        }

        private static readonly Action<ILogger, string, Exception> s_theExceptionsOccuredWhenScanningForMessageTypes =
            LoggerMessageFactory.Define<string>(LogLevel.Information,
                "The exception(s) occured when scanning for message types: {e}");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void TheExceptionsOccuredWhenScanningForMessageTypes(this ILogger logger, ReflectionTypeLoadException ex)
        {
            s_theExceptionsOccuredWhenScanningForMessageTypes(logger, string.Join(",", ex.LoaderExceptions.Select(x => x.Message)), null);
        }

        private static readonly Action<ILogger, Exception> s_exception_while_scanning_for_message_types =
            LoggerMessageFactory.Define(LogLevel.Information,
                "Exception while scanning for message types");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Exception_while_scanning_for_message_types(this ILogger logger, ReflectionTypeLoadException ex)
        {
            s_exception_while_scanning_for_message_types(logger, ex);
        }

        private static readonly Action<ILogger, string, Exception> s_message_doesnot_have_TypeId_field =
            LoggerMessageFactory.Define<string>(LogLevel.Information,
                "Message {typeName} doesn't have TypeId field!");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Message_doesnot_have_TypeId_field(this ILogger logger, Type type)
        {
            s_message_doesnot_have_TypeId_field(logger, type.Name, null);
        }

        private static readonly Action<ILogger, Exception> s_could_not_parse_Linux_stats =
            LoggerMessageFactory.Define(LogLevel.Information,
                "Could not parse Linux stats.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Could_not_parse_Linux_stats(this ILogger logger, Exception ex)
        {
            s_could_not_parse_Linux_stats(logger, ex);
        }

        private static readonly Action<ILogger, Exception> s_error_while_reading_disk_IO_on_Windows =
            LoggerMessageFactory.Define(LogLevel.Information,
                "Error while reading disk IO on Windows.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Error_while_reading_disk_IO_on_Windows(this ILogger logger, Exception ex)
        {
            s_error_while_reading_disk_IO_on_Windows(logger, ex);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void LogCsvStatsHeader(this ILogger logger, string header)
        {
            logger.LogInformation(Environment.NewLine);
            logger.LogInformation(header);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void LogCsvStats(this ILogger logger, Dictionary<string, object> rawStats)
        {
            logger.LogInformation(StatsCsvEncoder.GetLine(rawStats));
        }

        private static readonly Action<ILogger, Exception> s_received_error_reading_counters =
            LoggerMessageFactory.Define(LogLevel.Information,
                "Received error reading counters. Attempting to rebuild.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Received_error_reading_counters(this ILogger logger)
        {
            s_received_error_reading_counters(logger, null);
        }

        private static readonly Action<ILogger, Guid, NakAction, string, Exception> s_message_NAKed_id_action_to_take =
            LoggerMessageFactory.Define<Guid, NakAction, string>(LogLevel.Information,
                "Message NAK'ed id {id} action to take {action} reason '{reason}'");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Message_NAKed_id_action_to_take(this ILogger logger, Guid id, NakAction action, string reason)
        {
            s_message_NAKed_id_action_to_take(logger, id, action, reason ?? "", null);
        }

        private static readonly Action<ILogger, string, long, OperationResult, Exception> s_unable_to_park_message_operation_failed =
            LoggerMessageFactory.Define<string, long, OperationResult>(LogLevel.Information,
                "Unable to park message {originalStreamId}/{originalEventNumber} operation failed {result} retrying");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Unable_to_park_message_operation_failed(this ILogger logger, in ResolvedEvent resolvedEvent, OperationResult result)
        {
            s_unable_to_park_message_operation_failed(logger, resolvedEvent.OriginalStreamId, resolvedEvent.OriginalEventNumber, result, null);
        }

        private static readonly Action<ILogger, Exception> s_timeoutWhileTryingToSavePersistentSubscriptionConfiguration =
            LoggerMessageFactory.Define(LogLevel.Information,
                "Timeout while trying to save persistent subscription configuration. Retrying");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void TimeoutWhileTryingToSavePersistentSubscriptionConfiguration(this ILogger logger)
        {
            s_timeoutWhileTryingToSavePersistentSubscriptionConfiguration(logger, null);
        }

        private static readonly Action<ILogger, Exception> s_failedToLoadXmlInvalidFormat =
            LoggerMessageFactory.Define(LogLevel.Information,
                "Failed to load xml. Invalid format");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void FailedToLoadXmlInvalidFormat(this ILogger logger, Exception e)
        {
            s_failedToLoadXmlInvalidFormat(logger, e);
        }

        private static readonly Action<ILogger, IPEndPoint, Guid, Guid, long, long, string, Exception> s_subscribeRequestFrom =
            LoggerMessageFactory.Define<IPEndPoint, Guid, Guid, long, long, string>(LogLevel.Information,
                "SUBSCRIBE REQUEST from [{replicaEndPoint},C:{connectionId:B},S:{subscriptionId:B},{logPosition}(0x{logPosition:X}),{epochs}]...");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SubscribeRequestFrom(this ILogger logger, MasterReplicationService.ReplicaSubscription replica, long logPosition, Epoch[] epochs)
        {
            s_subscribeRequestFrom(logger, replica.ReplicaEndPoint, replica.ConnectionId, replica.SubscriptionId, logPosition, logPosition,
                         string.Join(", ", epochs.Select(x => EpochRecordExtensions.AsString((Epoch)x))), null);
        }

        private static readonly Action<ILogger, IPEndPoint, Guid, long, long, long, long, Exception> s_subscribedReplicaForRawSendAt =
            LoggerMessageFactory.Define<IPEndPoint, Guid, long, long, long, long>(LogLevel.Information,
                "Subscribed replica [{replicaEndPoint}, S:{subscriptionId}] for raw send at {chunkStartPosition} (0x{chunkStartPosition:X}) (requested {logPosition} (0x{logPosition:X})).");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SubscribedReplicaForRawSendAt(this ILogger logger, MasterReplicationService.ReplicaSubscription sub, long chunkStartPos, long logPosition)
        {
            s_subscribedReplicaForRawSendAt(logger, sub.ReplicaEndPoint, sub.SubscriptionId, chunkStartPos, chunkStartPos, logPosition, logPosition, null);
        }

        private static readonly Action<ILogger, IPEndPoint, Guid, long, long, Exception> s_forcingReplicaToRecreateChunkFromPosition =
            LoggerMessageFactory.Define<IPEndPoint, Guid, long, long>(LogLevel.Information,
                "Forcing replica [{replicaEndPoint}, S:{subscriptionId}] to recreate chunk from position {chunkStartPosition} (0x{chunkStartPosition:X})...");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ForcingReplicaToRecreateChunkFromPosition(this ILogger logger, MasterReplicationService.ReplicaSubscription sub, long chunkStartPos)
        {
            s_forcingReplicaToRecreateChunkFromPosition(logger, sub.ReplicaEndPoint, sub.SubscriptionId, chunkStartPos, chunkStartPos, null);
        }

        private static readonly Action<ILogger, IPEndPoint, Guid, long, long, Exception> s_subscribedReplicaForDataSendAt =
            LoggerMessageFactory.Define<IPEndPoint, Guid, long, long>(LogLevel.Information,
                "Subscribed replica [{replicaEndPoint},S:{subscriptionId}] for data send at {logPosition} (0x{logPosition:X}).");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SubscribedReplicaForDataSendAt(this ILogger logger, MasterReplicationService.ReplicaSubscription sub, long logPosition)
        {
            s_subscribedReplicaForDataSendAt(logger, sub.ReplicaEndPoint, sub.SubscriptionId, logPosition, logPosition, null);
        }

        private static readonly Action<ILogger, Exception> s_errorDuringMasterReplicationIteration =
            LoggerMessageFactory.Define(LogLevel.Information,
                "Error during master replication iteration.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ErrorDuringMasterReplicationIteration(this ILogger logger, Exception e)
        {
            s_errorDuringMasterReplicationIteration(logger, e);
        }

        private static readonly Action<ILogger, MasterReplicationService.ReplicaSubscription, Exception> s_errorDuringReplicationSendToReplica =
            LoggerMessageFactory.Define<MasterReplicationService.ReplicaSubscription>(LogLevel.Information,
                "Error during replication send to replica: {subscription}.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ErrorDuringReplicationSendToReplica(this ILogger logger, MasterReplicationService.ReplicaSubscription sub, Exception e)
        {
            s_errorDuringReplicationSendToReplica(logger, sub, e);
        }

        private static readonly Action<ILogger, string, IPEndPoint, Exception> s_connectionToMasterFailed =
            LoggerMessageFactory.Define<string, IPEndPoint>(LogLevel.Information,
                "Connection '{master}' to [{remoteEndPoint}] failed: ");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ConnectionToMasterFailed(this ILogger logger, bool useSsl, IPEndPoint remoteEndPoint, InvalidConnectionException exc)
        {
            s_connectionToMasterFailed(logger, useSsl ? "master-secure" : "master-normal", remoteEndPoint, exc);
        }

        private static readonly Action<ILogger, long, long, IPEndPoint, Guid, Guid, Guid, IPEndPoint, string, Exception> s_subscribingAtLogpositionToMasterAsReplicaWithSubscriptionid =
            LoggerMessageFactory.Define<long, long, IPEndPoint, Guid, Guid, Guid, IPEndPoint, string>(LogLevel.Information,
                "Subscribing at LogPosition: {logPosition} (0x{logPosition:X}) to MASTER [{remoteEndPoint}, {masterId:B}] as replica with SubscriptionId: {subscriptionId:B}, "
                + "ConnectionId: {connectionId:B}, LocalEndPoint: [{localEndPoint}], Epochs:\n{epochs}...\n.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SubscribingAtLogpositionToMasterAsReplicaWithSubscriptionid(this ILogger logger, long logPosition, TcpConnectionManager connection, ReplicationMessage.SubscribeToMaster message, EpochRecord[] epochs)
        {
            s_subscribingAtLogpositionToMasterAsReplicaWithSubscriptionid(logger, logPosition, logPosition, connection.RemoteEndPoint, message.MasterId, message.SubscriptionId,
                      connection.ConnectionId, connection.LocalEndPoint, string.Join("\n", epochs.Select(x => x.AsString())), null);
        }

        private static readonly Action<ILogger, Exception> s_tableIndexInitialization =
            LoggerMessageFactory.Define(LogLevel.Information,
                "TableIndex initialization...");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void TableIndexInitialization(this ILogger logger)
        {
            s_tableIndexInitialization(logger, null);
        }

        private static readonly Action<ILogger, Exception> s_readIndexBuilding =
            LoggerMessageFactory.Define(LogLevel.Information,
                "ReadIndex building...");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ReadIndexBuilding(this ILogger logger)
        {
            s_readIndexBuilding(logger, null);
        }

        private static readonly Action<ILogger, Exception> s_requestShutDownOfNodeBecauseShutdownCommandHasBeenReceived =
            LoggerMessageFactory.Define(LogLevel.Information,
                "Request shut down of node because shutdown command has been received.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void RequestShutDownOfNodeBecauseShutdownCommandHasBeenReceived(this ILogger logger)
        {
            s_requestShutDownOfNodeBecauseShutdownCommandHasBeenReceived(logger, null);
        }

        private static readonly Action<ILogger, int, int, Exception> s_requestScavengingBecauseRequestHasBeenReceived =
            LoggerMessageFactory.Define<int, int>(LogLevel.Information,
                "Request scavenging because /admin/scavenge?startFromChunk={startFromChunk}&threads={numThreads} request has been received.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void RequestScavengingBecauseRequestHasBeenReceived(this ILogger logger, int startFromChunk, int threads)
        {
            s_requestScavengingBecauseRequestHasBeenReceived(logger, startFromChunk, threads, null);
        }

        private static readonly Action<ILogger, string, Exception> s_stoppingScavengeBecauseDeleteRequestHasBeenReceived =
            LoggerMessageFactory.Define<string>(LogLevel.Information,
                "Stopping scavenge because /admin/scavenge/{scavengeId} DELETE request has been received.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void StoppingScavengeBecauseDeleteRequestHasBeenReceived(this ILogger logger, string scavengeId)
        {
            s_stoppingScavengeBecauseDeleteRequestHasBeenReceived(logger, scavengeId, null);
        }

        private static readonly Action<ILogger, Exception> s_failedToPrepareMainMenu =
            LoggerMessageFactory.Define(LogLevel.Information,
                "Failed to prepare main menu");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void FailedToPrepareMainMenu(this ILogger logger, Exception ex)
        {
            s_failedToPrepareMainMenu(logger, ex);
        }

        private static readonly Action<ILogger, string, Guid, string, ClientVersion, Exception> s_connectionIdentifiedByClient =
            LoggerMessageFactory.Define<string, Guid, string, ClientVersion>(LogLevel.Information,
                "Connection '{connectionName}' ({connectionId:B}) identified by client. Client connection name: '{clientConnectionName}', Client version: {clientVersion}.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ConnectionIdentifiedByClient(this ILogger logger, TcpConnectionManager conn, ClientMessage.IdentifyClient message)
        {
            s_connectionIdentifiedByClient(logger, conn.ConnectionName, conn.ConnectionId, message.ConnectionName, (ClientVersion)message.Version, null);
        }

        private static readonly Action<ILogger, string, Guid, IPEndPoint, Exception> s_onConnectionEstablished =
            LoggerMessageFactory.Define<string, Guid, IPEndPoint>(LogLevel.Information,
                "Connection '{connectionName}' ({connectionId:B}) to [{remoteEndPoint}] established.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void OnConnectionEstablished(this ILogger logger, string connectionName, Guid connectionId, IPEndPoint remoteEndPoint)
        {
            s_onConnectionEstablished(logger, connectionName, connectionId, remoteEndPoint, null);
        }

        private static readonly Action<ILogger, string, string, IPEndPoint, Guid, DisassociateInfo, Exception> s_onConnectionClosed =
            LoggerMessageFactory.Define<string, string, IPEndPoint, Guid, DisassociateInfo>(LogLevel.Information,
                "Connection '{connectionName}{clientConnectionName}' [{remoteEndPoint}, {connectionId:B}] closed: {e}.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void OnConnectionClosed(this ILogger logger, string connectionName, string clientConnectionName, IPEndPoint remoteEndPoint, Guid connectionId, DisassociateInfo disassociateInfo)
        {
            s_onConnectionClosed(logger, connectionName, clientConnectionName.IsEmptyString() ? string.Empty : ":" + clientConnectionName, remoteEndPoint, connectionId, disassociateInfo, null);
        }

        private static readonly Action<ILogger, TcpServiceType, TcpSecurityType, IPEndPoint, IPEndPoint, Guid, Exception> s_onTcpConnectionAccepted =
            LoggerMessageFactory.Define<TcpServiceType, TcpSecurityType, IPEndPoint, IPEndPoint, Guid>(LogLevel.Information,
                "{serviceType} TCP connection accepted: [{securityType}, {remoteEndPoint}, L{localEndPoint}, {connectionId:B}].");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void OnTcpConnectionAccepted(this ILogger logger, TcpServiceType serviceType, TcpSecurityType securityType, ITcpConnection conn)
        {
            s_onTcpConnectionAccepted(logger, serviceType, securityType, conn.RemoteEndPoint, conn.LocalEndPoint, conn.ConnectionId, null);
        }

        private static readonly Action<ILogger, TcpSecurityType, IPEndPoint, Exception> s_startingTcpListeningOnTcpEndpoint =
            LoggerMessageFactory.Define<TcpSecurityType, IPEndPoint>(LogLevel.Information,
                "Starting {securityType} TCP listening on TCP endpoint: {serverEndPoint}.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void StartingTcpListeningOnTcpEndpoint(this ILogger logger, TcpSecurityType securityType, IPEndPoint serverEndPoint)
        {
            s_startingTcpListeningOnTcpEndpoint(logger, securityType, serverEndPoint, null);
        }

        private static readonly Action<ILogger, Exception> s_opsUserAddedToUsers =
            LoggerMessageFactory.Define(LogLevel.Information,
                "'ops' user added to $users.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void OpsUserAddedToUsers(this ILogger logger)
        {
            s_opsUserAddedToUsers(logger, null);
        }

        private static readonly Action<ILogger, Exception> s_opsUserAccountHasBeenCreated =
            LoggerMessageFactory.Define(LogLevel.Information,
                "'ops' user account has been created.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void OpsUserAccountHasBeenCreated(this ILogger logger)
        {
            s_opsUserAccountHasBeenCreated(logger, null);
        }

        private static readonly Action<ILogger, Exception> s_adminUserAddedToUsers =
            LoggerMessageFactory.Define(LogLevel.Information,
                "'admin' user added to $users.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void AdminUserAddedToUsers(this ILogger logger)
        {
            s_adminUserAddedToUsers(logger, null);
        }

        private static readonly Action<ILogger, Exception> s_adminUserAccountHasBeenCreated =
            LoggerMessageFactory.Define(LogLevel.Information,
                "'admin' user account has been created.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void AdminUserAccountHasBeenCreated(this ILogger logger)
        {
            s_adminUserAccountHasBeenCreated(logger, null);
        }

        private static readonly Action<ILogger, IPEndPoint, Exception> s_allServicesShutdown =
            LoggerMessageFactory.Define<IPEndPoint>(LogLevel.Information,
                "========== [{internalHttp}] All Services Shutdown.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void AllServicesShutdown(this ILogger logger, VNodeInfo nodeInfo)
        {
            s_allServicesShutdown(logger, nodeInfo.InternalHttp, null);
        }

        private static readonly Action<ILogger, IPEndPoint, string, Exception> s_serviceHasShutDown =
            LoggerMessageFactory.Define<IPEndPoint, string>(LogLevel.Information,
                "========== [{internalHttp}] Service '{serviceName}' has shut down.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ServiceHasShutDown(this ILogger logger, VNodeInfo nodeInfo, SystemMessage.ServiceShutdown message)
        {
            s_serviceHasShutDown(logger, nodeInfo.InternalHttp, message.ServiceName, null);
        }

        private static readonly Action<ILogger, IPEndPoint, IPEndPoint, string, Guid, Exception> s_cloneAssignmentReceivedFrom =
            LoggerMessageFactory.Define<IPEndPoint, IPEndPoint, string, Guid>(LogLevel.Information,
                "========== [{internalHttp}] CLONE ASSIGNMENT RECEIVED FROM [{internalTcp},{internalSecureTcp},{masterId:B}].");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void CloneAssignmentReceivedFrom(this ILogger logger, VNodeInfo nodeInfo, VNodeInfo master, in Guid masterId)
        {
            s_cloneAssignmentReceivedFrom(logger,
                nodeInfo.InternalHttp, master.InternalTcp, master.InternalSecureTcp == null ? "n/a" : master.InternalSecureTcp.ToString(), masterId, null);
        }

        private static readonly Action<ILogger, IPEndPoint, IPEndPoint, string, Guid, Exception> s_slaveAssignmentReceivedFrom =
            LoggerMessageFactory.Define<IPEndPoint, IPEndPoint, string, Guid>(LogLevel.Information,
                "========== [{internalHttp}] SLAVE ASSIGNMENT RECEIVED FROM [{internalTcp},{internalSecureTcp},{masterId:B}].");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SlaveAssignmentReceivedFrom(this ILogger logger, VNodeInfo nodeInfo, VNodeInfo master, in Guid masterId)
        {
            s_slaveAssignmentReceivedFrom(logger,
                nodeInfo.InternalHttp, master.InternalTcp, master.InternalSecureTcp == null ? "n/a" : master.InternalSecureTcp.ToString(), masterId, null);
        }

        private static readonly Action<ILogger, Exception> s_noQuorumEmergedWithinTimeoutRetiring =
            LoggerMessageFactory.Define(LogLevel.Information,
                "=== NO QUORUM EMERGED WITHIN TIMEOUT... RETIRING...");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void NoQuorumEmergedWithinTimeoutRetiring(this ILogger logger)
        {
            s_noQuorumEmergedWithinTimeoutRetiring(logger, null);
        }

        private static readonly Action<ILogger, IPEndPoint, string, Exception> s_subSystemInitialized =
            LoggerMessageFactory.Define<IPEndPoint, string>(LogLevel.Information,
                "========== [{internalHttp}] Sub System '{subSystemName}' initialized.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SubSystemInitialized(this ILogger logger, VNodeInfo nodeInfo, SystemMessage.SubSystemInitialized message)
        {
            s_subSystemInitialized(logger, nodeInfo.InternalHttp, message.SubSystemName, null);
        }

        private static readonly Action<ILogger, IPEndPoint, string, Exception> s_serviceInitializ1ed =
            LoggerMessageFactory.Define<IPEndPoint, string>(LogLevel.Information,
                "========== [{internalHttp}] Service '{serviceName}' initialized.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ServiceInitializ1ed(this ILogger logger, VNodeInfo nodeInfo, SystemMessage.ServiceInitialized message)
        {
            s_serviceInitializ1ed(logger, nodeInfo.InternalHttp, message.ServiceName, null);
        }

        private static readonly Action<ILogger, IPEndPoint, Exception> s_vNodeIsShutDown =
            LoggerMessageFactory.Define<IPEndPoint>(LogLevel.Information,
                "========== [{internalHttp}] IS SHUT DOWN.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void VNodeIsShutDown(this ILogger logger, VNodeInfo nodeInfo)
        {
            s_vNodeIsShutDown(logger, nodeInfo.InternalHttp, null);
        }

        private static readonly Action<ILogger, IPEndPoint, Exception> s_vNodeIsShuttingDown =
            LoggerMessageFactory.Define<IPEndPoint>(LogLevel.Information,
                "========== [{internalHttp}] IS SHUTTING DOWN...");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void VNodeIsShuttingDown(this ILogger logger, VNodeInfo nodeInfo)
        {
            s_vNodeIsShuttingDown(logger, nodeInfo.InternalHttp, null);
        }

        private static readonly Action<ILogger, IPEndPoint, Exception> s_vNodeIsMasterSparta =
            LoggerMessageFactory.Define<IPEndPoint>(LogLevel.Information,
                "========== [{internalHttp}] IS MASTER... SPARTA!");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void VNodeIsMasterSparta(this ILogger logger, VNodeInfo nodeInfo)
        {
            s_vNodeIsMasterSparta(logger, nodeInfo.InternalHttp, null);
        }

        private static readonly Action<ILogger, IPEndPoint, Exception> s_preMasterStateWaitingForChaserToCatchUp =
            LoggerMessageFactory.Define<IPEndPoint>(LogLevel.Information,
                "========== [{internalHttp}] PRE-MASTER STATE, WAITING FOR CHASER TO CATCH UP...");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void PreMasterStateWaitingForChaserToCatchUp(this ILogger logger, VNodeInfo nodeInfo)
        {
            s_preMasterStateWaitingForChaserToCatchUp(logger, nodeInfo.InternalHttp, null);
        }

        private static readonly Action<ILogger, IPEndPoint, IPEndPoint, Guid, Exception> s_vnodeIsSlaveMasterIs =
            LoggerMessageFactory.Define<IPEndPoint, IPEndPoint, Guid>(LogLevel.Information,
                "========== [{internalHttp}] IS SLAVE... MASTER IS [{masterInternalHttp},{instanceId:B}]");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void VnodeIsSlaveMasterIs(this ILogger logger, VNodeInfo nodeInfo, VNodeInfo master)
        {
            s_vnodeIsSlaveMasterIs(logger, nodeInfo.InternalHttp, master.InternalHttp, master.InstanceId, null);
        }

        private static readonly Action<ILogger, IPEndPoint, IPEndPoint, Guid, Exception> s_vnodeIsCloneMasterIs =
            LoggerMessageFactory.Define<IPEndPoint, IPEndPoint, Guid>(LogLevel.Information,
                "========== [{internalHttp}] IS CLONE... MASTER IS [{masterInternalHttp},{instanceId:B}]");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void VnodeIsCloneMasterIs(this ILogger logger, VNodeInfo nodeInfo, VNodeInfo master)
        {
            s_vnodeIsCloneMasterIs(logger, nodeInfo.InternalHttp, master.InternalHttp, master.InstanceId, null);
        }

        private static readonly Action<ILogger, IPEndPoint, IPEndPoint, Guid, Exception> s_vnodeIsCatchingUpMasterIs =
            LoggerMessageFactory.Define<IPEndPoint, IPEndPoint, Guid>(LogLevel.Information,
                "========== [{internalHttp}] IS CATCHING UP... MASTER IS [{masterInternalHttp},{instanceId:B}]");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void VnodeIsCatchingUpMasterIs(this ILogger logger, VNodeInfo nodeInfo, VNodeInfo master)
        {
            s_vnodeIsCatchingUpMasterIs(logger, nodeInfo.InternalHttp, master.InternalHttp, master.InstanceId, null);
        }

        private static readonly Action<ILogger, IPEndPoint, IPEndPoint, Guid, Exception> s_preReplicaStateWaitingForChaserToCatchUp =
            LoggerMessageFactory.Define<IPEndPoint, IPEndPoint, Guid>(LogLevel.Information,
                "========== [{internalHttp}] PRE-REPLICA STATE, WAITING FOR CHASER TO CATCH UP... MASTER IS [{masterInternalHttp},{instanceId:B}]");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void PreReplicaStateWaitingForChaserToCatchUp(this ILogger logger, VNodeInfo nodeInfo, VNodeInfo master)
        {
            s_preReplicaStateWaitingForChaserToCatchUp(logger, nodeInfo.InternalHttp, master.InternalHttp, master.InstanceId, null);
        }

        private static readonly Action<ILogger, IPEndPoint, Exception> s_vNodeIsUnknown =
            LoggerMessageFactory.Define<IPEndPoint>(LogLevel.Information,
                "========== [{internalHttp}] IS UNKNOWN...");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void VNodeIsUnknown(this ILogger logger, VNodeInfo nodeInfo)
        {
            s_vNodeIsUnknown(logger, nodeInfo.InternalHttp, null);
        }

        private static readonly Action<ILogger, IPEndPoint, Exception> s_vNodeSystemStart =
            LoggerMessageFactory.Define<IPEndPoint>(LogLevel.Information,
                "========== [{internalHttp}] SYSTEM START...");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void VNodeSystemStart(this ILogger logger, VNodeInfo nodeInfo)
        {
            s_vNodeSystemStart(logger, nodeInfo.InternalHttp, null);
        }

        private static readonly Action<ILogger, IPEndPoint, Exception> s_vNodeSystemInit =
            LoggerMessageFactory.Define<IPEndPoint>(LogLevel.Information,
                "========== [{internalHttp}] SYSTEM INIT...");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void VNodeSystemInit(this ILogger logger, VNodeInfo nodeInfo)
        {
            s_vNodeSystemInit(logger, nodeInfo.InternalHttp, null);
        }

        private static readonly Action<ILogger, long, long, long, long, Exception> s_offlineTruncationIsNeededShuttingDownNode =
            LoggerMessageFactory.Define<long, long, long, long>(LogLevel.Information,
                "OFFLINE TRUNCATION IS NEEDED (SubscribedAt {subscriptionPosition} (0x{subscriptionPosition:X}) <= LastCommitPosition {lastCommitPosition} (0x{lastCommitPosition:X})). SHUTTING DOWN NODE.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void OfflineTruncationIsNeededShuttingDownNode(this ILogger logger, long subscriptionPosition, long lastCommitPosition)
        {
            s_offlineTruncationIsNeededShuttingDownNode(logger, subscriptionPosition, subscriptionPosition, lastCommitPosition, lastCommitPosition, null);
        }

        private static readonly Action<ILogger, Exception> s_onlineTruncationIsNeededNotImplementedOfflineTruncationWillBePerformed =
            LoggerMessageFactory.Define(LogLevel.Information,
                "ONLINE TRUNCATION IS NEEDED. NOT IMPLEMENTED. OFFLINE TRUNCATION WILL BE PERFORMED. SHUTTING DOWN NODE.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void OnlineTruncationIsNeededNotImplementedOfflineTruncationWillBePerformed(this ILogger logger)
        {
            s_onlineTruncationIsNeededNotImplementedOfflineTruncationWillBePerformed(logger, null);
        }

        private static readonly Action<ILogger, IPEndPoint, Guid, long, long, long, long, Exception> s_masterSubscribedUsAtWhichIsLessThanOurWriterCheckpoint =
            LoggerMessageFactory.Define<IPEndPoint, Guid, long, long, long, long>(LogLevel.Information,
                "Master [{masterEndPoint},{masterId:B}] subscribed us at {subscriptionPosition} (0x{subscriptionPosition:X}), which is less than our writer checkpoint {writerCheckpoint} (0x{writerCheckpoint:X}). TRUNCATION IS NEEDED.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void MasterSubscribedUsAtWhichIsLessThanOurWriterCheckpoint(this ILogger logger, ReplicationMessage.ReplicaSubscribed message, long writerCheck)
        {
            s_masterSubscribedUsAtWhichIsLessThanOurWriterCheckpoint(logger, message.MasterEndPoint, message.MasterId, message.SubscriptionPosition, message.SubscriptionPosition, writerCheck, writerCheck, null);
        }

        private static readonly Action<ILogger, IPEndPoint, Guid, long, long, Guid, Exception> s_subscribedToMasterAt =
            LoggerMessageFactory.Define<IPEndPoint, Guid, long, long, Guid>(LogLevel.Information,
                "=== SUBSCRIBED to [{masterEndPoint},{masterId:B}] at {subscriptionPosition} (0x{subscriptionPosition:X}). SubscriptionId: {subscriptionId:B}.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SubscribedToMasterAt(this ILogger logger, ReplicationMessage.ReplicaSubscribed message)
        {
            s_subscribedToMasterAt(logger, message.MasterEndPoint, message.MasterId, message.SubscriptionPosition, message.SubscriptionPosition, message.SubscriptionId, null);
        }

        private static readonly Action<ILogger, string, long, Exception> s_removingHardDeletedStreamTombstoneForStreamAtPosition =
            LoggerMessageFactory.Define<string, long>(LogLevel.Information,
                "Removing hard deleted stream tombstone for stream {stream} at position {transactionPosition}");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void RemovingHardDeletedStreamTombstoneForStreamAtPosition(this ILogger logger, PrepareLogRecord prepare)
        {
            s_removingHardDeletedStreamTombstoneForStreamAtPosition(logger, prepare.EventStreamId, prepare.TransactionPosition, null);
        }

        private static readonly Action<ILogger, IList<TFChunk>, Exception> s_gotExceptionWhileMergingChunk =
            LoggerMessageFactory.Define<IList<TFChunk>>(LogLevel.Information,
                "Got exception while merging chunk:\n{oldChunks}");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void GotExceptionWhileMergingChunk(this ILogger logger, IList<TFChunk> oldChunks, Exception ex)
        {
            s_gotExceptionWhileMergingChunk(logger, oldChunks, ex);
        }

        private static readonly Action<ILogger, string, Exception> s_scavengingCancelledAt =
            LoggerMessageFactory.Define<string>(LogLevel.Information,
                "Scavenging cancelled at:\n{oldChunksList}");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ScavengingCancelledAt(this ILogger logger, string oldChunksList)
        {
            s_scavengingCancelledAt(logger, oldChunksList, null);
        }

        private static readonly Action<ILogger, string, string, Exception> s_gotFilebeingdeletedexceptionExceptionDuringScavengeMerging =
            LoggerMessageFactory.Define<string, string>(LogLevel.Information,
                "Got FileBeingDeletedException exception during scavenge merging, that probably means some chunks were re-replicated."
                        + "\nMerging of following chunks will be skipped:"
                        + "\n{oldChunksList}"
                        + "\nStopping merging and removing temp chunk '{tmpChunkPath}'...");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void GotFilebeingdeletedexceptionExceptionDuringScavengeMerging(this ILogger logger, string oldChunksList, string tmpChunkPath, FileBeingDeletedException exc)
        {
            s_gotFilebeingdeletedexceptionExceptionDuringScavengeMerging(logger, oldChunksList, tmpChunkPath, exc);
        }

        private static readonly Action<ILogger, int, int, Exception> s_gotExceptionWhileScavengingChunk =
            LoggerMessageFactory.Define<int, int>(LogLevel.Information,
                "Got exception while scavenging chunk: #{chunkStartNumber}-{chunkEndNumber}. This chunk will be skipped");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void GotExceptionWhileScavengingChunk(this ILogger logger, int chunkStartNumber, int chunkEndNumber, Exception ex)
        {
            s_gotExceptionWhileScavengingChunk(logger, chunkStartNumber, chunkEndNumber, ex);
        }

        private static readonly Action<ILogger, string, Exception> s_scavengingCancelledAtOldChunk =
            LoggerMessageFactory.Define<string>(LogLevel.Information,
                "Scavenging cancelled at: {oldChunkName}");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ScavengingCancelledAtOldChunk(this ILogger logger, string oldChunkName)
        {
            s_scavengingCancelledAtOldChunk(logger, oldChunkName, null);
        }

        private static readonly Action<ILogger, string, string, Exception> s_gotFilebeingdeletedexceptionExceptionDuringScavenging =
            LoggerMessageFactory.Define<string, string>(LogLevel.Information,
                "Got FileBeingDeletedException exception during scavenging, that probably means some chunks were re-replicated."
                + "\nScavenging of following chunks will be skipped: {oldChunkName}"
                + "\nStopping scavenging and removing temp chunk '{tmpChunkPath}'...");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void GotFilebeingdeletedexceptionExceptionDuringScavenging(this ILogger logger, string oldChunkName, string tmpChunkPath, FileBeingDeletedException exc)
        {
            s_gotFilebeingdeletedexceptionExceptionDuringScavenging(logger, oldChunkName, tmpChunkPath, exc);
        }

        private static readonly Action<ILogger, Exception> s_scavengeCancelledk =
            LoggerMessageFactory.Define(LogLevel.Information,
                "SCAVENGING: Scavenge cancelled.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ScavengeCancelled(this ILogger logger)
        {
            s_scavengeCancelledk(logger, null);
        }

        private static readonly Action<ILogger, string, TFChunk, Exception> s_chunkIsMarkedForDeletion =
            LoggerMessageFactory.Define<string, TFChunk>(LogLevel.Information,
                "{chunkExplanation} chunk #{oldChunk} is marked for deletion.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ChunkIsMarkedForDeletion(this ILogger logger, string chunkExplanation, TFChunk oldChunk)
        {
            s_chunkIsMarkedForDeletion(logger, chunkExplanation, oldChunk, null);
        }

        private static readonly Action<ILogger, TFChunk, Exception> s_chunkWillBeNotSwitchedMarkingForRemove =
            LoggerMessageFactory.Define<TFChunk>(LogLevel.Information,
                "Chunk {newChunk} will be not switched, marking for remove...");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ChunkWillBeNotSwitchedMarkingForRemove(this ILogger logger, TFChunk newChunk)
        {
            s_chunkWillBeNotSwitchedMarkingForRemove(logger, newChunk, null);
        }

        private static readonly Action<ILogger, string, string, Exception> s_chunkFileWillBeMovedToFile =
            LoggerMessageFactory.Define<string, string>(LogLevel.Information,
                "File {oldFileName} will be moved to file {newFileName}");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ChunkFileWillBeMovedToFile(this ILogger logger, string oldFileName, string newFileName)
        {
            s_chunkFileWillBeMovedToFile(logger, Path.GetFileName(oldFileName), Path.GetFileName(newFileName), null);
        }

        private static readonly Action<ILogger, int, int, string, Exception> s_switchingChunk =
            LoggerMessageFactory.Define<int, int, string>(LogLevel.Information,
                "Switching chunk #{chunkStartNumber}-{chunkEndNumber} ({oldFileName})...");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SwitchingChunk(this ILogger logger, int chunkStartNumber, int chunkEndNumber, string oldFileName)
        {
            s_switchingChunk(logger, chunkStartNumber, chunkEndNumber, Path.GetFileName(oldFileName), null);
        }

        private static readonly Action<ILogger, int, int, Exception> s_resettingTruncatecheckpointTo =
            LoggerMessageFactory.Define<int, int>(LogLevel.Information,
                "Resetting TruncateCheckpoint to {epoch} (0x{epoch:X}).");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ResettingTruncatecheckpointTo(this ILogger logger)
        {
            s_resettingTruncatecheckpointTo(logger, -1, -1, null);
        }

        private static readonly Action<ILogger, long, long, long, long, Exception> s_truncatingWriterFrom =
            LoggerMessageFactory.Define<long, long, long, long>(LogLevel.Information,
                "Truncating writer from {writerCheckpoint} (0x{writerCheckpoint:X}) to {truncateCheckpoint} (0x{truncateCheckpoint:X}).");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void TruncatingWriterFrom(this ILogger logger, long writerCheckpoint, long truncateChk)
        {
            s_truncatingWriterFrom(logger, writerCheckpoint, writerCheckpoint, truncateChk, truncateChk, null);
        }

        private static readonly Action<ILogger, long, long, long, long, Exception> s_truncatingChaserFrom =
            LoggerMessageFactory.Define<long, long, long, long>(LogLevel.Information,
                "Truncating chaser from {chaserCheckpoint} (0x{chaserCheckpoint:X}) to {truncateCheckpoint} (0x{truncateCheckpoint:X}).");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void TruncatingChaserFrom(this ILogger logger, long chaserCheckpoint, long truncateChk)
        {
            s_truncatingChaserFrom(logger, chaserCheckpoint, chaserCheckpoint, truncateChk, truncateChk, null);
        }

        private static readonly Action<ILogger, long, long, long, long, Exception> s_truncatingEpochFrom =
            LoggerMessageFactory.Define<long, long, long, long>(LogLevel.Information,
                "Truncating epoch from {epochFrom} (0x{epochFrom:X}) to {epochTo} (0x{epochTo:X}).");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void TruncatingEpochFrom(this ILogger logger, long epochFrom)
        {
            s_truncatingEpochFrom(logger, epochFrom, epochFrom, -1L, -1L, null);
        }

        private static readonly Action<ILogger, long, int, Exception> s_settingTruncatecheckpointAndDeletingAllChunksFromInclusively =
            LoggerMessageFactory.Define<long, int>(LogLevel.Information,
                "Setting TruncateCheckpoint to {truncateCheckpoint} and deleting ALL chunks from #{chunkStartNumber} inclusively "
                + "as truncation position is in the middle of scavenged chunk.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SettingTruncatecheckpointAndDeletingAllChunksFromInclusively(this ILogger logger, long truncateChk, int chunkStartNumber)
        {
            s_settingTruncatecheckpointAndDeletingAllChunksFromInclusively(logger, truncateChk, chunkStartNumber, null);
        }

        private static readonly Action<ILogger, string, Exception> s_fileWillBeDeletedDuringTruncatedbProcedure =
            LoggerMessageFactory.Define<string>(LogLevel.Information,
                "File {chunk} will be deleted during TruncateDb procedure.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void FileWillBeDeletedDuringTruncatedbProcedure(this ILogger logger, string chunkFile)
        {
            s_fileWillBeDeletedDuringTruncatedbProcedure(logger, chunkFile, null);
        }
    }
}
