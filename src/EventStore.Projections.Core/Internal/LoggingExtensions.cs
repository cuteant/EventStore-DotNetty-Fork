using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using EventStore.Common.Utils;
using EventStore.Core.Data;
using EventStore.Core.Messages;
using EventStore.Projections.Core.Messages;
using EventStore.Projections.Core.Services;
using EventStore.Projections.Core.Services.Processing;

namespace EventStore.Projections.Core
{
    internal static class ProjectionsCoreLoggingExtensions
    {
        #region -- LogTrace --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void WritingCheckpointForAtWithExpectedVersionNumber(this ILogger logger, string name, CheckpointTag requestedCheckpointPosition, long lastWrittenCheckpointEventNumber)
        {
            logger.LogTrace(
                    "Writing checkpoint for {name} at {requestedCheckpointPosition} with expected version number {lastWrittenCheckpointEventNumber}", name, requestedCheckpointPosition, lastWrittenCheckpointEventNumber);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void CheckpointHasBeenWrittenForProjection(this ILogger logger, string name, long firstWrittenEventNumber)
        {
            logger.LogTrace("Checkpoint has been written for projection {name} at sequence number {firstWrittenEventNumber} (current)", name, firstWrittenEventNumber);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ProjectionManagerDidNotFindAnyProjectionConfigurationRecordsInTheStream(this ILogger logger, string eventStreamId)
        {
            logger.LogTrace(
                "Projection manager did not find any projection configuration records in the {eventStreamId} stream.  Projection stays in CREATING state",
                eventStreamId);
        }

        #endregion

        #region -- LogDebug --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void UnknownCommandHandlerRegistered(this ILogger logger, string commandName)
        {
            logger.LogDebug("Unknown command handler registered. Command name: {commandName}", commandName);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void IgnoringUnknownReverseCommand(this ILogger logger, string commandName)
        {
            logger.LogDebug("Ignoring unknown reverse command: '{commandName}'", commandName);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ProjectionsCommandReceived(this ILogger logger, long originalEventNumber, string command)
        {
            logger.LogDebug("PROJECTIONS: Command received: {eventNum}@{command}", originalEventNumber, command);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ProjectionsFinishedStartingProjectionCoreReader(this ILogger logger, string coreServiceId)
        {
            logger.LogDebug("PROJECTIONS: Finished Starting Projection Core Reader (reads from $projections-${coreServiceId})", coreServiceId);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ProjectionsStartingRead(this ILogger logger, Guid epochId)
        {
            logger.LogDebug("PROJECTIONS: Starting read {stream}", ProjectionNamesBuilder.BuildControlStreamName(epochId));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ProjectionsStoppingProjectionCoreReader(this ILogger logger, string coreServiceId)
        {
            logger.LogDebug("PROJECTIONS: Stopping Projection Core Reader ({coreServiceId})", coreServiceId);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ProjectionsStartingProjectionCoreReader(this ILogger logger, string coreServiceId)
        {
            logger.LogDebug("PROJECTIONS: Starting Projection Core Reader (reads from $projections-${coreServiceId})", coreServiceId);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void StartingACheckpointInNonRunnableState(this ILogger logger)
        {
            logger.LogDebug("Starting a checkpoint in non-runnable state");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void EmittedStreamDeletionCheckpointFailedToBeWrittenAt(this ILogger logger, long eventNumber)
        {
            logger.LogDebug("PROJECTIONS: Emitted Stream Deletion Checkpoint Failed to be written at {eventNumber}", eventNumber);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void EmittedStreamDeletionCheckpointWrittenAt(this ILogger logger, long eventNumber)
        {
            logger.LogDebug("PROJECTIONS: Emitted Stream Deletion Checkpoint written at {eventNumber}", eventNumber);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ProjectionsFinishedWritingEventsTo(this ILogger logger, string eventType)
        {
            logger.LogDebug("PROJECTIONS: Finished writing events to {stream}: {evtType}", ProjectionNamesBuilder._projectionsMasterStream, eventType);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ProjectionsFinishedWritingEventsTo(this ILogger logger, string streamId, string eventType)
        {
            logger.LogDebug("PROJECTIONS: Finished writing events to {stream}: {evtType}", streamId, eventType);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ProjectionsFailedWritingEventsTo(this ILogger logger, OperationResult result, Event[] events)
        {
            logger.LogDebug("PROJECTIONS: Failed writing events to {stream} because of {result}: {evts}",
                ProjectionNamesBuilder._projectionsMasterStream,
                result, String.Join(",", events.Select(x => $"{x.EventType}-{Helper.UTF8NoBom.GetString(x.Data)}")));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ProjectionsFailedWritingEventsTo(this ILogger logger, string streamId, OperationResult result, Event[] events)
        {
            logger.LogDebug("PROJECTIONS: Failed writing events to {stream} because of {result}: {evts}",
                streamId, result, String.Join(",", events.Select(x => String.Format("{0}", x.EventType)))); //Can't do anything about it, log and move on
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ProjectionsSchedulingTheWritingOf(this ILogger logger, string command, bool busy)
        {
            logger.LogDebug("PROJECTIONS: Scheduling the writing of {cmd} to {stream}. Current status of Writer: Busy: {busy}", command, ProjectionNamesBuilder._projectionsMasterStream, busy);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ProjectionsSchedulingTheWritingOf(this ILogger logger, string command, Guid workerId, bool busy)
        {
            logger.LogDebug("PROJECTIONS: Scheduling the writing of {cmd} to {stream}. Current status of Writer: Busy: {busy}", command, "$projections-$" + workerId, busy);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ProjectionsResettingMasterWriter(this ILogger logger)
        {
            logger.LogDebug("PROJECTIONS: Resetting Master Writer");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ProjectionsResponseReceived(this ILogger logger, long originalEventNumber, string command)
        {
            logger.LogDebug("PROJECTIONS: Response received: {originalEventNumber}@{command}", originalEventNumber, command);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ReadForwardOfProjectionsMasterStreamTimedOut(this ILogger logger)
        {
            logger.LogDebug("Read forward of stream {stream} timed out. Retrying", ProjectionNamesBuilder._projectionsMasterStream);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void FinishedStartingProjectionManagerResponseReader(this ILogger logger)
        {
            logger.LogDebug("PROJECTIONS: Finished Starting Projection Manager Response Reader (reads from $projections-$master)");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void StartingProjectionManagerResponseReader(this ILogger logger)
        {
            logger.LogDebug("PROJECTIONS: Starting Projection Manager Response Reader (reads from $projections-$master)");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThereWasAnActiveCancellationScopeCancellingNow(this ILogger logger)
        {
            logger.LogDebug("PROJECTIONS: There was an active cancellation scope, cancelling now");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void AddingProjectionToList(this ILogger logger, Guid projectionCorrelationId, string name)
        {
            logger.LogDebug("Adding projection {projectionCorrelationId}@{name} to list", projectionCorrelationId, name);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void NoProjectionsWereFoundInProjectionsRegistrationStream(this ILogger logger)
        {
            logger.LogDebug("PROJECTIONS: No projections were found in {stream}, starting from empty stream", ProjectionNamesBuilder.ProjectionsRegistrationStream);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void FoundTheFollowingProjectionsInProjectionsRegistrationStream(this ILogger logger, IDictionary<string, long> registeredProjections)
        {
            logger.LogDebug("PROJECTIONS: Found the following projections in {stream}: {projections}", ProjectionNamesBuilder.ProjectionsRegistrationStream,
                    String.Join(", ", registeredProjections
                        .Where(x => x.Key != ProjectionEventTypes.ProjectionsInitialized)
                        .Select(x => x.Key)));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ReadingExistingProjectionsFrom(this ILogger logger, string projectionsRegistrationStreamId)
        {
            logger.LogDebug("PROJECTIONS: Reading Existing Projections from {stream}", projectionsRegistrationStreamId);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void StoppingProjectionsManager(this ILogger logger, VNodeState state)
        {
            logger.LogDebug("PROJECTIONS: Stopping Projections Manager. (Node State : {state})", state);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void StartingProjectionsManager(this ILogger logger, VNodeState state)
        {
            logger.LogDebug("PROJECTIONS: Starting Projections Manager. (Node State : {state})", state);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SubComponentStopped(this ILogger logger, string subComponent)
        {
            logger.LogDebug("PROJECTIONS: SubComponent Stopped: {subComponent}", subComponent);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SubComponentStarted(this ILogger logger, string subComponent)
        {
            logger.LogDebug("PROJECTIONS: SubComponent Started: {subComponent}", subComponent);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void StartingProjectionsCoreCoordinator(this ILogger logger, VNodeState state)
        {
            logger.LogDebug("PROJECTIONS: Starting Projections Core Coordinator. (Node State : {state})", state);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void StoppingProjectionsCoreCoordinator(this ILogger logger, VNodeState state)
        {
            logger.LogDebug("PROJECTIONS: Stopping Projections Core Coordinator. (Node State : {state})", state);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ResettingWorkerWriter(this ILogger logger)
        {
            logger.LogDebug("PROJECTIONS: Resetting Worker Writer");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ResponseReceived(this ILogger logger, string command)
        {
            logger.LogDebug("Response received: {command}", command);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ReplyTextBodyFailed(this ILogger logger, Exception ex)
        {
            logger.LogDebug(ex, "Reply Text Body Failed.");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ReadRequestBodyFailed(this ILogger logger, Exception ex)
        {
            logger.LogDebug(ex, "Read Request Body Failed.");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void FailedToUpdateProjectionConfiguration(this ILogger logger, Exception ex)
        {
            logger.LogDebug(ex, "Failed to update projection configuration. Error: ");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ReplyTextContentFailed(this ILogger logger, Exception ex)
        {
            logger.LogDebug(ex, "Reply Text Content Failed.");
        }

        #endregion

        #region -- LogInformation --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void TheProjectionSubsystemFailedToLoadJs1Dll(this ILogger logger, DllNotFoundException ex)
        {
            logger.LogInformation(ex, "The projection subsystem failed to load a libjs1.so/js1.dll/... or one of its dependencies.");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ProjectionsIsNotEmptyAfterAllTheProjectionsHaveBeenKilled(this ILogger logger)
        {
            logger.LogInformation("_projections is not empty after all the projections have been killed");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void EventReaderSubscriptionsIsNotEmptyAfterAllTheProjectionsHaveBeenKilled(this ILogger logger)
        {
            logger.LogInformation("_eventReaderSubscriptions is not empty after all the projections have been killed");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SubscriptionEventReadersIsNotEmptyAfterAllTheProjectionsHaveBeenKilled(this ILogger logger)
        {
            logger.LogInformation("_subscriptionEventReaders is not empty after all the projections have been killed");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void EventReadersIsNotEmptyAfterAllTheProjectionsHaveBeenKilled(this ILogger logger)
        {
            logger.LogInformation("_eventReaders is not empty after all the projections have been killed");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SubscriptionsIsNotEmptyAfterAllTheProjectionsHaveBeenKilled(this ILogger logger)
        {
            logger.LogInformation("_subscriptions is not empty after all the projections have been killed");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void FailedToWriteEventsToStream(this ILogger logger, string streamsId, OperationResult result)
        {
            logger.LogInformation("Failed to write events to stream {streamsId}. Error: {result}", streamsId, result);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void FailedToWriteProjectionCheckpointToStream(this ILogger logger, string streamsId, OperationResult result)
        {
            logger.LogInformation("Failed to write projection checkpoint to stream {streamsId}. Error: {result}", streamsId, result);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ProjectionRestartHasBeenRequestedDueTo(this ILogger logger, string name, Guid projectionCorrelationId, string reason)
        {
            logger.LogInformation("Projection '{name}'({projectionCorrelationId}) restart has been requested due to: '{reason}'", name, projectionCorrelationId, reason);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void CannotFindAWorkerWithId(this ILogger logger, Guid workerId)
        {
            logger.LogInformation("Cannot find a worker with ID: {workerId}", workerId);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void RetryingWriteProjectionRegistrationFor(this ILogger logger, string name)
        {
            logger.LogInformation("Retrying write projection registration for {name}", name);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ProjectionRegistrationHasNotBeenWrittenToStreams(this ILogger logger, string name, string streamsId, OperationResult result)
        {
            logger.LogInformation("Projection '{name}' registration has not been written to {streamsId}. Error: {result}", name, streamsId, result);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ResettingProjection(this ILogger logger, string name)
        {
            logger.LogInformation("Resetting '{name}' projection", name);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SettingRunAs1AccountForProjection(this ILogger logger, string name)
        {
            logger.LogInformation("Setting RunAs1 account for '{name}' projection", name);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void AbortingProjection(this ILogger logger, string name)
        {
            logger.LogInformation("Aborting '{name}' projection", name);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void EnablingProjection(this ILogger logger, string name)
        {
            logger.LogInformation("Enabling '{Name}' projection", name);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void DisablingProjection(this ILogger logger, string name)
        {
            logger.LogInformation("Disabling '{Name}' projection", name);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void UpdatingProjectionSourceTo(this ILogger logger, ProjectionManagementMessage.Command.UpdateQuery message)
        {
            logger.LogInformation("Updating '{name}' projection source to '{query}' (Requested type is: '{handlerType}')", message.Name, message.Query, message.HandlerType);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ProjectionsProjectionStreamDeleted(this ILogger logger, string streamId)
        {
            logger.LogInformation("PROJECTIONS: Projection Stream '{streamId}' deleted", streamId);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ProjectionStreamCouldNotBeDeleted(this ILogger logger, string streamId, OperationResult result)
        {
            logger.LogInformation("PROJECTIONS: Projection stream '{streamId}' could not be deleted. Error: {result}", streamId, result);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void RetryingWriteProjectionSourceFor(this ILogger logger, string name)
        {
            logger.LogInformation("Retrying write projection source for {name}", name);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ProjectionSourceHasNotBeenWrittenTo(this ILogger logger, string name, string streamId, OperationResult result)
        {
            logger.LogInformation("Projection '{name}' source has not been written to {streamId}. Error: {result}", name, streamId, result);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ProjectionSourceHasBeenWritten(this ILogger logger, string name)
        {
            logger.LogInformation("'{name}' projection source has been written", name);
        }

        #endregion

        #region -- LogWarning --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ReadForwardOfStreamTimedOut(this ILogger logger, string streamID)
        {
            logger.LogWarning("Read forward of stream {streamID} timed out. Retrying", streamID);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ReadBackwardOfStreamTimedOut(this ILogger logger, string streamID)
        {
            logger.LogWarning("Read backward of stream {streamID} timed out. Retrying", streamID);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void TheFollowingProjectionHasADuplicateRegistrationEvent(this ILogger logger, string projectionName)
        {
            logger.LogWarning("PROJECTIONS: The following projection: {projectionName} has a duplicate registration event.", projectionName);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void TheFollowingProjectionHasADuplicateCreatedEvent(this ILogger logger, string projectionName, long projectionId)
        {
            logger.LogWarning("PROJECTIONS: The following projection: {projectionName} has a duplicate created event. Using projection Id {projectionId}", projectionName, projectionId);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void TransientProjectionHasExpiredAndWillBeDeleted(this ILogger logger, string name, DateTime lastAccessed)
        {
            logger.LogWarning("Transient projection {name} has expired and will be deleted. Last accessed at {lastAccessed}", name, lastAccessed);
        }

        #endregion

        #region -- LogError --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void CannotLoadModule(this ILogger logger, Exception ex, string moduleName)
        {
            logger.LogError(ex, "Cannot load module '{moduleName}'", moduleName);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ShouldNotReceiveEventsInStoppedStateAnymore(this ILogger logger)
        {
            logger.LogError("Should not receive events in stopped state anymore");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void IgnoringCommittedEventInStoppedState(this ILogger logger)
        {
            logger.LogError("Ignoring committed event in stopped state");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void IgnoringCommittedCatalogEventInStoppedState(this ILogger logger)
        {
            logger.LogError("Ignoring committed catalog event in stopped state");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void FailedToWriteATrackedStreamIdOfToTheStream(this ILogger logger, string streamId, string streamName, int maxRetryCount, OperationResult result)
        {
            logger.LogError("PROJECTIONS: Failed to write a tracked stream id of {streamId} to the {streamName} stream. Retry limit of {maxRetryCount} reached. Reason: {result}", streamId, streamName, maxRetryCount, result);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void FailedToDeleteEmittedStream(this ILogger logger, string streamId, int retries, int retryLimit, OperationResult result)
        {
            logger.LogError("PROJECTIONS: Failed to delete emitted stream {streamId}, Retrying ({retries}/{retryLimit}). Reason: {result}", streamId, retries, retryLimit, result);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void RetryLimitReachedCouldNotDeleteStream(this ILogger logger, string streamId)
        {
            logger.LogError("PROJECTIONS: Retry limit reached, could not delete stream: {streamId}. Manual intervention is required and you may need to delete this stream manually", streamId);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void FailedToDeleteProjectionStream(this ILogger logger, string streamId, OperationResult result)
        {
            logger.LogError("PROJECTIONS: Failed to delete projection stream '{streamId}'. Reason: {result}", streamId, result);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void FailedReadingStream(this ILogger logger, ClientMessage.ReadStreamEventsForwardCompleted completed)
        {
            logger.LogError("Failed reading stream {stream}. Read result: {result}, Error: '{error}'", ProjectionNamesBuilder._projectionsMasterStream, completed.Result, completed.Error);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void DbgProjectionNotFound(this ILogger logger, string name)
        {
            logger.LogError("DBG: PROJECTION *{name}* NOT FOUND.", name);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void TheProjectionFaultedDueTo(this ILogger logger, string name, string reason)
        {
            logger.LogError("The '{name}' projection faulted due to '{reason}'", name, reason);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ProjectionDefinitionWriteCompletedInNonWritingState(this ILogger logger, string name)
        {
            logger.LogError("Projection definition write completed in non writing state. ({name})", name);
        }

        #endregion

        #region -- LogCritical --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThereWasAnErrorReadingTheProjectionsListDueTo(this ILogger logger, ReadStreamResult result)
        {
            logger.LogCritical("There was an error reading the projections list due to {result}. Projections could not be loaded.", result);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void CannotInitializeProjectionsSubsystemCannotWriteAFakeProjection(this ILogger logger)
        {
            logger.LogCritical("Cannot initialize projections subsystem. Cannot write a fake projection");
        }

        #endregion
    }
}
