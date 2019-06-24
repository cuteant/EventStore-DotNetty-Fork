using System;
using System.Net;
using System.Runtime.CompilerServices;
using EventStore.Core.Cluster;
using EventStore.Core.Data;
using EventStore.Core.Messages;
using EventStore.Core.Services;
using EventStore.Core.Services.PersistentSubscription;
using EventStore.Core.TransactionLog.Chunks.TFChunk;
using EventStore.Core.TransactionLog.LogRecords;
using Microsoft.Extensions.Logging;

namespace EventStore.Core
{
    internal static partial class CoreLoggingExtensions
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void No_incomplete_scavenges_found_on_node(this ILogger logger, string nodeEndpoint)
        {
            logger.LogDebug("No incomplete scavenges found on node {nodeEndPoint}.", nodeEndpoint);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Forcing_index_verification_on_next_startup(this ILogger logger)
        {
            logger.LogDebug("Forcing index verification on next startup");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Forcing_verification_of_index_files(this ILogger logger)
        {
            logger.LogDebug("Forcing verification of index files...");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void NotCachingIndexMidpointsToPtableDueToCountMismatch(this ILogger logger, long numIndexEntries, long dumpedEntryCount, long requiredMidpointCount, int midpointsCount)
        {
            logger.LogDebug("Not caching index midpoints to PTable due to count mismatch. Table entries: {numIndexEntries} / Dumped entries: {dumpedEntryCount}, Required midpoint count: {requiredMidpointCount} /  Actual midpoint count: {midpoints}", numIndexEntries, dumpedEntryCount, requiredMidpointCount, midpointsCount);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Cached_index_midpoints_to_PTable(this ILogger logger, int midpointsWritten)
        {
            logger.LogDebug("Cached {midpointsWritten} index midpoints to PTable", midpointsWritten);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SkippingLoadingOfCachedmidpointsFromPTable(this ILogger logger, uint midpointsCached, int midpointsCount)
        {
            logger.LogDebug("Skipping loading of cached midpoints from PTable due to count mismatch, cached midpoints: {0} / required midpoints: {1}", midpointsCached, midpointsCount);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void LoadingCachedMidpointsFromPTable(this ILogger logger, uint midpointsCached)
        {
            logger.LogDebug("Loading {0} cached midpoints from PTable", midpointsCached);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Disabling_Verification_of_PTable(this ILogger logger)
        {
            logger.LogDebug("Disabling Verification of PTable");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void GettingDiskIOError(this ILogger logger, Exception exc)
        {
            logger.LogDebug("Getting disk IO error: {0}.", exc.Message);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Error_while_reading_drive_info_for_path(this ILogger logger, string path, Exception ex)
        {
            logger.LogDebug("Error while reading drive info for path {0}. Message: {1}.", path, ex.Message);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Could_not_get_drive_name_for_directory(this ILogger logger, Exception ex, string directory)
        {
            logger.LogDebug(ex, "Could not get drive name for directory '{0}' on Unix.", directory);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Failed_to_create_stats_stream(this ILogger logger, string nodeStatsStream, ClientMessage.WriteEventsCompleted message)
        {
            logger.LogDebug("Failed to create stats stream '{0}'. Reason : {1}({2}). Retrying...", nodeStatsStream, message.Result, message.Message);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Could_not_get_free_memory_on_OSX(this ILogger logger, Exception ex)
        {
            logger.LogDebug(ex, "Could not get free memory on OSX.");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Could_not_get_free_memory_on_BSD(this ILogger logger, Exception ex)
        {
            logger.LogDebug(ex, "Could not get free memory on BSD.");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Could_not_get_free_mem_on_linux(this ILogger logger, Exception ex, string meminfo)
        {
            logger.LogDebug(ex, "Could not get free mem on linux, received memory info raw string: [{0}]", meminfo);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Error_writing_checkpoint_for(this ILogger logger, string subscriptionStateStream, OperationResult result)
        {
            logger.LogDebug("Error writing checkpoint for {0}: {1}", subscriptionStateStream, result);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void RetryingMessage(this ILogger logger, string subscriptionId, in ResolvedEvent @event)
        {
            logger.LogDebug("Retrying message {0} {1}/{2}", subscriptionId, @event.OriginalStreamId, @event.OriginalEventNumber);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void RetryingEventOnSubscription(this ILogger logger, in ResolvedEvent ev, string subscriptionId)
        {
            logger.LogDebug("Retrying event {0} {1}/{2} on subscription {3}", ev.OriginalEvent.EventId, ev.OriginalStreamId, ev.OriginalEventNumber, subscriptionId);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SubscriptionReadCheckpoint(this ILogger logger, string subscriptionId, long checkpoint)
        {
            logger.LogDebug($"Subscription {subscriptionId}: read checkpoint {checkpoint}");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SubscriptionNoCheckpointFound(this ILogger logger, PersistentSubscriptionParams settings)
        {
            logger.LogDebug($"Subscription {settings.SubscriptionId}: no checkpoint found.");
            logger.LogDebug($"Start from = {settings.StartFrom}");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Saving_persistent_subscription_configuration(this ILogger logger)
        {
            logger.LogDebug("Saving persistent subscription configuration");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Replaying_parked_messages_for_persistent_subscription(this ILogger logger, string key)
        {
            logger.LogDebug("Replaying parked messages for persistent subscription {0}", key);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void New_connection_to_persistent_subscription(this ILogger logger, string key, Guid connectionId)
        {
            logger.LogDebug("New connection to persistent subscription {0} by {1}", key, connectionId);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Persistent_subscription_lost_connection_from(this ILogger logger, TcpMessage.ConnectionClosed message)
        {
            logger.LogDebug($"Persistent subscription lost connection from {message.Connection.RemoteEndPoint}");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Deleting_persistent_subscription(this ILogger logger, string key)
        {
            logger.LogDebug($"Deleting persistent subscription {key}");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Updating_persistent_subscription(this ILogger logger, string key)
        {
            logger.LogDebug($"Updating persistent subscription {key}");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void New_persistent_subscription(this ILogger logger, string key)
        {
            logger.LogDebug("New persistent subscription {0}.", key);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Creating_persistent_subscription(this ILogger logger, string key)
        {
            logger.LogDebug($"Creating persistent subscription {key}");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Persistent_subscriptions_Became_Master_so_now_handling_subscriptions(this ILogger logger)
        {
            logger.LogDebug("Persistent subscriptions Became Master so now handling subscriptions");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Persistent_subscriptions_received_state_change_to(this ILogger logger, VNodeState state)
        {
            logger.LogDebug($"Persistent subscriptions received state change to {state}. Stopping listening.");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Update_Last_Epoch(this ILogger logger, EpochRecord epoch)
        {
            logger.LogDebug("=== Update Last Epoch E{0}@{1}:{2:B} (previous epoch at {3}).",
                epoch.EpochNumber, epoch.EpochPosition, epoch.EpochId, epoch.PrevEpochPosition);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Writing_Epoch_previous_epoch_at(this ILogger logger, int epochNumber, long epochPosition, Guid epochId, long lastEpochPosition)
        {
            logger.LogDebug("=== Writing E{0}@{1}:{2:B} (previous epoch at {3}).", epochNumber, epochPosition, epochId, lastEpochPosition);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ReadIndex_rebuilding_done(this ILogger logger, long processed, DateTime startTime)
        {
            logger.LogDebug("ReadIndex rebuilding done: total processed {0} records, time elapsed: {1}.", processed, DateTime.UtcNow - startTime);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ReadIndexRebuilding(this ILogger logger, long processed, long recordPostPosition, long startPosition, long buildToPosition)
        {
            logger.LogDebug("ReadIndex Rebuilding: processed {0} records ({1:0.0}%).",
                processed, (recordPostPosition - startPosition) * 100.0 / (buildToPosition - startPosition));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Still_waiting_for_chaser_to_catch_up_already_for(this ILogger logger, TimeSpan totalTime)
        {
            logger.LogDebug("Still waiting for chaser to catch up already for {0}...", totalTime);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Check_Stream_Access_operation_has_expired_for_Stream(this ILogger logger, StorageMessage.CheckStreamAccess msg)
        {
            logger.LogDebug("Check Stream Access operation has expired for Stream: {0}. Operation Expired at {1}", msg.EventStreamId, msg.Expires);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Read_All_Stream_Events_Backward_operation_has_expired_for_C(this ILogger logger, ClientMessage.ReadAllEventsBackward msg)
        {
            logger.LogDebug("Read All Stream Events Backward operation has expired for C:{0}/P:{1}. Operation Expired at {2}", msg.CommitPosition, msg.PreparePosition, msg.Expires);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Read_All_Stream_Events_Forward_operation_has_expired_for_C(this ILogger logger, ClientMessage.ReadAllEventsForward msg)
        {
            logger.LogDebug("Read All Stream Events Forward operation has expired for C:{0}/P:{1}. Operation Expired at {2}", msg.CommitPosition, msg.PreparePosition, msg.Expires);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Read_Stream_Events_Backward_operation_has_expired_for_Stream(this ILogger logger, ClientMessage.ReadStreamEventsBackward msg)
        {
            logger.LogDebug("Read Stream Events Backward operation has expired for Stream: {0}, From Event Number: {1}, Max Count: {2}. Operation Expired at {3}", msg.EventStreamId, msg.FromEventNumber, msg.MaxCount, msg.Expires);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ReadStreamEventsForwardOperationHasExpiredForStream(this ILogger logger, ClientMessage.ReadStreamEventsForward msg)
        {
            logger.LogDebug("Read Stream Events Forward operation has expired for Stream: {0}, From Event Number: {1}, Max Count: {2}. Operation Expired at {3}", msg.EventStreamId, msg.FromEventNumber, msg.MaxCount, msg.Expires);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Read_Event_operation_has_expired_for_Stream(this ILogger logger, ClientMessage.ReadEvent msg)
        {
            logger.LogDebug("Read Event operation has expired for Stream: {0}, Event Number: {1}. Operation Expired at {2}", msg.EventStreamId, msg.EventNumber, msg.Expires);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThereIsNoMasterOrMasterIsDeadAccordingToGossip(this ILogger logger, VNodeInfo master)
        {
            logger.LogDebug("There is NO MASTER or MASTER is DEAD according to GOSSIP. Starting new elections. MASTER: [{0}].", master);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThereAreFewMastersAccordingToGossipNeedToStartElections(this ILogger logger, VNodeInfo master, GossipMessage.GossipUpdated message)
        {
            logger.LogDebug("There are FEW MASTERS according to gossip, need to start elections. MASTER: [{0}]", master);
            logger.LogDebug("GOSSIP:");
            logger.LogDebug("{0}", message.ClusterInfo);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Error_while_closing_HTTP_connection(this ILogger logger, Exception e)
        {
            logger.LogDebug("Error while closing HTTP connection (HTTP service core): {0}.", e.Message);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Error_occurred_while_closing_timed_out_connection(this ILogger logger, Exception e)
        {
            logger.LogDebug("Error occurred while closing timed out connection (HTTP service core): {0}.", e.Message);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Reply_Text_Content_Failed(this ILogger logger, Exception e)
        {
            logger.LogDebug(e, "Reply Text Content Failed.");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Error_while_replying_info_controller(this ILogger logger, Exception e)
        {
            logger.LogDebug("Error while replying (info controller): {0}.", e.Message);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Error_while_reading_request_gossip(this ILogger logger, Exception e)
        {
            logger.LogDebug("Error while reading request (gossip): {0}", e.Message);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Error_while_reading_request(this ILogger logger, Exception e)
        {
            logger.LogDebug("Error while reading request: {0}.", e.Message);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Error_while_closing_HTTP_connection_ok(this ILogger logger, Exception e)
        {
            logger.LogDebug("Error while closing HTTP connection (ok): {0}.", e.Message);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Too_large_events_received_over_HTTP(this ILogger logger)
        {
            logger.LogDebug("Too large events received over HTTP");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Error_while_closing_HTTP_connection_bad_request(this ILogger logger, Exception e)
        {
            logger.LogDebug("Error while closing HTTP connection (bad request): {0}.", e.Message);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Error_while_reading_request_POST_entry(this ILogger logger, Exception e)
        {
            logger.LogDebug("Error while reading request (POST entry): {0}.", e.Message);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Error_while_closing_HTTP_connection_admin_controller(this ILogger logger, Exception e)
        {
            logger.LogDebug("Error while closing HTTP connection (admin controller): {0}.", e.Message);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Error_forwarding_response_for(this ILogger logger, Uri requestedUrl, Exception e)
        {
            logger.LogDebug("Error forwarding response for '{0}': {1}.", requestedUrl, e.Message);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Error_in_SendAsync_for_forwarded_request_for(this ILogger logger, Uri requestedUrl, Exception e)
        {
            logger.LogDebug("Error in SendAsync for forwarded request for '{0}': {1}.",
                requestedUrl, e.InnerException.Message);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Error_occurred_while_replying_to_HTTP_with_message(this ILogger logger, HttpMessage.HttpSendPart message, Exception e)
        {
            logger.LogDebug("Error occurred while replying to HTTP with message {0}: {1}.", message, e.Message);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Error_occurred_while_replying_to_HTTP_with_message(this ILogger logger, HttpMessage.HttpSend message, Exception exc)
        {
            logger.LogDebug("Error occurred while replying to HTTP with message {0}: {1}.", message.Message, exc.Message);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Dropping_HTTP_send_message_due_to_TTL_being_over(this ILogger logger, HttpMessage.SendOverHttp message)
        {
            logger.LogDebug("Dropping HTTP send message due to TTL being over. {1} To : {0}", message.EndPoint, message.Message.GetType().Name.ToString());
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ElectionsAcceptFrom(this ILogger logger, ElectionMessage.Accept message)
        {
            logger.LogDebug("ELECTIONS: (V={0}) ACCEPT FROM [{1},{2:B}] M=[{3},{4:B}]).", message.View,
                message.ServerInternalHttp, message.ServerId, message.MasterInternalHttp, message.MasterId);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ElectionsProposalFrom(this ILogger logger, int lastAttemptedView, ElectionMessage.Proposal message, ElectionsService.MasterCandidate candidate, ElectionsService service)
        {
            logger.LogDebug("ELECTIONS: (V={0}) PROPOSAL FROM [{1},{2:B}] M={3}. ME={4}.", lastAttemptedView,
                        message.ServerInternalHttp, message.ServerId, ElectionsService.FormatNodeInfo(candidate), ElectionsService.FormatNodeInfo(service.GetOwnInfo()));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ElectionsNotLegitimateMasterProposalFrom(this ILogger logger, int view, IPEndPoint proposingServerEndPoint, Guid proposingServerId, ElectionsService.MasterCandidate candidate, ElectionsService.MasterCandidate ownInfo)
        {
            logger.LogDebug("ELECTIONS: (V={0}) NOT LEGITIMATE MASTER PROPOSAL FROM [{1},{2:B}] M={3}. ME={4}.",
                            view, proposingServerEndPoint, proposingServerId, ElectionsService.FormatNodeInfo(candidate), ElectionsService.FormatNodeInfo(ownInfo));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ElectionsNotLegitimateMasterProposalFrom(this ILogger logger, int view, IPEndPoint proposingServerEndPoint, Guid proposingServerId, ElectionsService.MasterCandidate candidate, MemberInfo master)
        {
            logger.LogDebug("ELECTIONS: (V={0}) NOT LEGITIMATE MASTER PROPOSAL FROM [{1},{2:B}] M={3}. "
                    + "PREVIOUS MASTER IS ALIVE: [{4},{5:B}].",
                    view, proposingServerEndPoint, proposingServerId, ElectionsService.FormatNodeInfo(candidate),
                    master.InternalHttpEndPoint, master.InstanceId);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ElectionsSendingProposalCandidate(this ILogger logger, int lastAttemptedView, ElectionsService.MasterCandidate master, ElectionsService service)
        {
            logger.LogDebug("ELECTIONS: (V={0}) SENDING PROPOSAL CANDIDATE: {1}, ME: {2}.",
                    lastAttemptedView, ElectionsService.FormatNodeInfo(master), ElectionsService.FormatNodeInfo(service.GetOwnInfo()));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ElectionsShiftToRegLeader(this ILogger logger, int lastAttemptedView)
        {
            logger.LogDebug("ELECTIONS: (V={0}) SHIFT TO REG_LEADER.", lastAttemptedView);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ElectionsPrepare_okFrom(this ILogger logger, ElectionMessage.PrepareOk msg)
        {
            logger.LogDebug("ELECTIONS: (V={0}) PREPARE_OK FROM {1}.", msg.View,
                    ElectionsService.FormatNodeInfo(msg.ServerInternalHttp, msg.ServerId,
                                   msg.LastCommitPosition, msg.WriterCheckpoint, msg.ChaserCheckpoint,
                                   msg.EpochNumber, msg.EpochPosition, msg.EpochId));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ElectionsShiftToReg_Nonleader(this ILogger logger, int lastAttemptedView)
        {
            logger.LogDebug("ELECTIONS: (V={0}) SHIFT TO REG_NONLEADER.", lastAttemptedView);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ElectionsPrepareFrom(this ILogger logger, int lastAttemptedView, ElectionMessage.Prepare message)
        {
            logger.LogDebug("ELECTIONS: (V={0}) PREPARE FROM [{1}, {2:B}].", lastAttemptedView, message.ServerInternalHttp, message.ServerId);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ElectionsShiftToPreparePhase(this ILogger logger, int lastAttemptedView)
        {
            logger.LogDebug("ELECTIONS: (V={0}) SHIFT TO PREPARE PHASE.", lastAttemptedView);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ElectionsViewchangeproof_From_jumping_to_non_leader_state(this ILogger logger, ElectionMessage.ViewChangeProof message)
        {
            logger.LogDebug("ELECTIONS: (IV={0}) VIEWCHANGEPROOF FROM [{1}, {2:B}]. JUMPING TO NON-LEADER STATE.",
                    message.InstalledView, message.ServerInternalHttp, message.ServerId);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ElectionsViewchangeproof_From_jumping_to_lead1er_state(this ILogger logger, ElectionMessage.ViewChangeProof message)
        {
            logger.LogDebug("ELECTIONS: (IV={0}) VIEWCHANGEPROOF FROM [{1}, {2:B}]. JUMPING TO LEADER STATE.",
                    message.InstalledView, message.ServerInternalHttp, message.ServerId);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ElectionsMajority_of_viewchange(this ILogger logger, int attemptedView)
        {
            logger.LogDebug("ELECTIONS: (V={0}) MAJORITY OF VIEWCHANGE.", attemptedView);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Elections_viewchange_from(this ILogger logger, ElectionMessage.ViewChange message)
        {
            logger.LogDebug("ELECTIONS: (V={0}) VIEWCHANGE FROM [{1}, {2:B}].", message.AttemptedView, message.ServerInternalHttp, message.ServerId);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Elections_starting_elections(this ILogger logger)
        {
            logger.LogDebug("ELECTIONS: STARTING ELECTIONS.");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Elections_timed_out(this ILogger logger, int view, ElectionsState state, Guid? master)
        {
            logger.LogDebug("ELECTIONS: (V={0}) TIMED OUT! (S={1}, M={2}).", view, state, master);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Elections_shift_to_leader_election(this ILogger logger, int view)
        {
            logger.LogDebug("ELECTIONS: (V={0}) SHIFT TO LEADER ELECTION.", view);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Unable_to_read_for_scavenge_log_clean_up(this ILogger logger, ReadStreamResult result)
        {
            logger.LogDebug("Unable to read {stream} for scavenge log clean up. Result: {result}", SystemStreams.ScavengesStream, result);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Setting_max_age_for_the_stream_to(this ILogger logger, TimeSpan scavengeHistoryMaxAge)
        {
            logger.LogDebug("Setting max age for the {stream} stream to {maxAge}.", SystemStreams.ScavengesStream, scavengeHistoryMaxAge);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Max_age_already_set_for_the_stream(this ILogger logger)
        {
            logger.LogDebug("Max age already set for the {stream} stream.", SystemStreams.ScavengesStream);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Searching_for_incomplete_scavenges_on_node(this ILogger logger, string nodeEndpoint)
        {
            logger.LogDebug("Searching for incomplete scavenges on node {nodeEndPoint}.", nodeEndpoint);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Clearing_fast_merge_optimizations_from_chunk(this ILogger logger, string filename)
        {
            logger.LogDebug("Clearing fast merge optimizations from chunk " + filename + "...");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Optimizing_chunk_for_fast_merge(this ILogger logger, string filename)
        {
            logger.LogDebug("Optimizing chunk " + filename + " for fast merge...");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void BufferSizeIs(this ILogger logger, long bufferSize)
        {
            logger.LogDebug("Buffer size is " + bufferSize);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void CACHING_FAILED_due_to_FileBeingDeleted_exception(this ILogger logger, TFChunk chunk)
        {
            logger.LogDebug("CACHING FAILED due to FileBeingDeleted exception (TFChunk is being disposed) in TFChunk {0}.", chunk);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Opened_completed_as_version(this ILogger logger, string filename, byte version)
        {
            logger.LogDebug("Opened completed " + filename + " as version " + version);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void DroppingIdempotentWriteToStream(this ILogger logger, in Core.Services.Storage.ReaderIndex.CommitCheckResult result)
        {
            logger.LogDebug("Dropping idempotent write to stream {@stream}, startEventNumber: {@startEventNumber}, endEventNumber: {@endEventNumber} since the original write has not yet been replicated.", result.EventStreamId, result.StartEventNumber, result.EndEventNumber);
        }
    }
}
