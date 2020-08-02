using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using EventStore.Core.Bus;
using EventStore.Core.Exceptions;
using EventStore.Core.Index;
using EventStore.Core.Messages;
using EventStore.Core.Messaging;
using EventStore.Core.Services.Monitoring.Stats;
using EventStore.Core.TransactionLog.Chunks.TFChunk;
using Microsoft.Extensions.Logging;

namespace EventStore.Core
{
    internal static partial class CoreLoggingExtensions
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ExceptionWhileTryingToOpenClusterNodeMutex(this ILogger logger, Exception exc, string mutexName)
        {
            logger.LogTrace(exc, "Exception while trying to open Cluster Node mutex '{0}': {1}.", mutexName, exc.Message);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void BindingMiniWeb(this ILogger logger, string pattern)
        {
            logger.LogTrace("Binding MiniWeb to {0}", pattern);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void RequestedFromMiniWeb(this ILogger logger, string contentLocalPath)
        {
            logger.LogTrace("{0} requested from MiniWeb", contentLocalPath);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void AllocatingSpacesInThreadLocalCache(this ILogger logger, int initialSizeOfThreadLocalCache)
        {
            logger.LogTrace("SCAVENGING: Allocating {0} spaces in thread local cache {1}.", initialSizeOfThreadLocalCache,
                        Thread.CurrentThread.ManagedThreadId);
        }

        private static readonly Action<ILogger, string, TimeSpan, string, int, int, string, Exception> s_mergingOfChunks =
            LoggerMessage.Define<string, TimeSpan, string, int, int, string>(LogLevel.Trace, 0,
                "Merging of chunks:"
                + "\n{oldChunksList}"
                + "\ncompleted in {elapsed}."
                + "\nNew chunk: {tmpChunkPath} --> #{chunkStartNumber}-{chunkEndNumber} ({newChunk}).");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void MergingOfChunks(this ILogger logger, string oldChunksList, TimeSpan elapsed, string tmpChunkPath, int chunkStartNumber, int chunkEndNumber, string chunkFileName)
        {
            s_mergingOfChunks(logger, oldChunksList, elapsed, Path.GetFileName(tmpChunkPath), chunkStartNumber, chunkEndNumber, Path.GetFileName(chunkFileName), null);
        }

        private static readonly Action<ILogger, string, TimeSpan, int, int, string, Exception> s_mergingOfChunksButSwitchingWasPreventedForNewChunk =
            LoggerMessage.Define<string, TimeSpan, int, int, string>(LogLevel.Trace, 0,
                "Merging of chunks:"
                + "\n{oldChunksList}"
                + "\ncompleted in {elapsed}."
                + "\nBut switching was prevented for new chunk: #{chunkStartNumber}-{chunkEndNumber} ({tmpChunkPath}).");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void MergingOfChunks(this ILogger logger, string oldChunksList, TimeSpan elapsed, int chunkStartNumber, int chunkEndNumber, string tmpChunkPath)
        {
            s_mergingOfChunksButSwitchingWasPreventedForNewChunk(logger, oldChunksList, elapsed, chunkStartNumber, chunkEndNumber, Path.GetFileName(tmpChunkPath), null);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Forcing_merged_chunk_to_be_kept_even_if_bigger(this ILogger logger)
        {
            logger.LogTrace("Forcing merged chunk to be kept even if bigger.");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Forcing_merged_chunk_to_be_kept_as_old_chunk_is_a_previous_version(this ILogger logger)
        {
            logger.LogTrace("Forcing merged chunk to be kept as old chunk is a previous version.");
        }

        private static readonly Action<ILogger, string, string, int, string, Exception> s_showBusMsg =
            LoggerMessage.Define<string, string, int, string>(LogLevel.Trace, 0,
                "SLOW BUS MSG [{bus}]: {message} - {elapsed}ms. Handler: {handler}.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SlowBusMsg(this ILogger logger, string name, Message message, int elapsed, IMessageHandler handler)
        {
            s_showBusMsg(logger, name, message.GetType().Name, elapsed, handler.HandlerName, null);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void FileBeingDeleted(this ILogger logger)
        {
            logger.LogTrace("File being deleted.");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void PuttingAwaitingFileAsPTableInsteadOfMemTable(this ILogger logger, Guid id)
        {
            logger.LogTrace("Putting awaiting file as PTable instead of MemTable [{0}].", id);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThereAreNowAwaitingTables(this ILogger logger, int count)
        {
            logger.LogTrace("There are now {0} awaiting tables.", count);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void AwaitingTablesQueueSizeIs(this ILogger logger, int count)
        {
            logger.LogTrace("Awaiting tables queue size is: {0}.", count);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SwitchingMemTableCurrentlyAwaitingTables(this ILogger logger, int count)
        {
            logger.LogTrace("Switching MemTable, currently: {awaitingMemTables} awaiting tables.", count);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void PTableScavengeFinishedInEntriesRemoved(this ILogger logger, TimeSpan elapsed, long droppedCount, long keptCount)
        {
            logger.LogTrace("PTable scavenge finished in {elapsed} ({droppedCount} entries removed, {keptCount} remaining).", elapsed, droppedCount, keptCount);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Keeping_scavenged_index_even_though_it_isnot_smaller(this ILogger logger)
        {
            logger.LogTrace("Keeping scavenged index even though it isn't smaller; version upgraded.");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void PTableScavengeFinishedNoEntriesRemovedSoNotKeepingScavengedTable(this ILogger logger, TimeSpan elapsed)
        {
            logger.LogTrace("PTable scavenge finished in {elapsed}. No entries removed so not keeping scavenged table.", elapsed);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void PTablesScavengeStartedWithEntries(this ILogger logger, long numIndexEntries)
        {
            logger.LogTrace("PTables scavenge started with {numIndexEntries} entries.", numIndexEntries);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void PTablesMergeFinished(this ILogger logger, TimeSpan elapsed, IList<PTable> tables, long dumpedEntryCount)
        {
            logger.LogTrace("PTables merge finished in {elapsed} ([{entryCount}] entries merged into {dumpedEntryCount}).",
                elapsed, string.Join(", ", tables.Select(x => x.Count)), dumpedEntryCount);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void PTables_merge_started_specialized_for_less_then_2_tables(this ILogger logger)
        {
            logger.LogTrace("PTables merge started (specialized for <= 2 tables).");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void PTables_merge_started(this ILogger logger)
        {
            logger.LogTrace("PTables merge started.");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void DumpedMemTable(this ILogger logger, Guid id, long count, TimeSpan elapsed)
        {
            logger.LogTrace("Dumped MemTable [{id}, {table} entries] in {elapsed}.", id, count, elapsed);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void LoadingPTable(this ILogger logger, byte version, string filename, long count, int calcdepth, TimeSpan elapsed)
        {
            logger.LogTrace("Loading PTable (Version: {0}) '{1}' ({2} entries, cache depth {3}) done in {4}.",
                version, Path.GetFileName(filename), count, calcdepth, elapsed);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void LoadingPTabl1eStarted(this ILogger logger, bool skipIndexVerify, string filename)
        {
            logger.LogTrace("Loading " + (skipIndexVerify ? "" : "and Verification ") + "of PTable '{0}' started...", Path.GetFileName(filename));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void DumpStatistics(this ILogger logger, QueueStats[] stats)
        {
            logger.LogTrace(Environment.NewLine + string.Join(Environment.NewLine, stats.Select(x => x.ToString())));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ShowQueueMsg(this ILogger logger, QueueStatsCollector queueStats, int totalMilliseconds, int cnt, int queueCount)
        {
            logger.LogTrace("SLOW QUEUE MSG [{queue}]: {message} - {elapsed}ms. Q: {prevQueueCount}/{curQueueCount}.",
                queueStats.Name, queueStats.InProgressMessage.Name, totalMilliseconds, cnt, queueCount);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void MessageHierarchyInitializationTook(this ILogger logger, TimeSpan elapsed)
        {
            logger.LogTrace("MessageHierarchy initialization took {0}.", elapsed);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void LooksLikeNodeIsDeadTCPConnectionLost(this ILogger logger, SystemMessage.VNodeConnectionLost message)
        {
            logger.LogTrace("Looks like node [{0}] is DEAD (TCP connection lost).", message.VNodeEndPoint);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void LooksLikeNodeIsDEADGossipSendFailed(this ILogger logger, GossipMessage.GossipSendFailed message)
        {
            logger.LogTrace("Looks like node [{0}] is DEAD (Gossip send failed).", message.Recipient);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void LooksLikeMasterIsDEADGossipSendFailed(this ILogger logger, GossipMessage.GossipSendFailed message, Guid instanceId)
        {
            logger.LogTrace("Looks like master [{0}, {1:B}] is DEAD (Gossip send failed), though we wait for TCP to decide.", message.Recipient, instanceId);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void NomasterCandidateWhenTryingToSendProposal(this ILogger logger, int lastAttemptedView)
        {
            logger.LogTrace("ELECTIONS: (V={0}) NO MASTER CANDIDATE WHEN TRYING TO SEND PROPOSAL.", lastAttemptedView);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void CompletingDataChunk(this ILogger logger, int chunkStartNumber, int chunkEndNumber)
        {
            logger.LogTrace("Completing data chunk {0}-{1}...", chunkStartNumber, chunkEndNumber);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void CompletingRawChu1nk(this ILogger logger, int chunkStartNumber, int chunkEndNumber)
        {
            logger.LogTrace("Completing raw chunk {0}-{1}...", chunkStartNumber, chunkEndNumber);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void IgnoringBecauseSubscriptionIdIsWrong(this ILogger logger, ReplicationMessage.IReplicationMessage message, Guid subscriptionId)
        {
            logger.LogTrace("Ignoring {0} because SubscriptionId {1:B} is wrong. Current SubscriptionId is {2:B}.",
                message.GetType().Name, message.SubscriptionId, subscriptionId);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void BlockingMessageInStorageWriterService(this ILogger logger, Message message)
        {
            logger.LogTrace("Blocking message {0} in StorageWriterService. Message:", message.GetType().Name);
            logger.LogTrace("{0}", message);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void IdempotentWriteToStreamClientcorrelationid(this ILogger logger, Guid clientCorrId, StorageMessage.AlreadyCommitted message)
        {
            logger.LogTrace("IDEMPOTENT WRITE TO STREAM ClientCorrelationID {0}, {1}.", clientCorrId, message);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void CreatedStatsStream(this ILogger logger, string nodeStatsStream, OperationResult result)
        {
            logger.LogTrace("Created stats stream '{0}', code = {1}", nodeStatsStream, result);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void UnableToGetPerformanceCounterCategoryInstances(this ILogger logger, string categoryName)
        {
            logger.LogTrace("Unable to get performance counter category '{0}' instances.", categoryName);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void CouldNotCreatePerformanceCounter(this ILogger logger, string category, string counter, string instance, Exception ex)
        {
            logger.LogTrace("Could not create performance counter: category='{0}', counter='{1}', instance='{2}'. Error: {3}",
                category, counter, instance ?? string.Empty, ex.Message);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void UncachedTFChunk(this ILogger logger, TFChunk chunk)
        {
            logger.LogTrace("UNCACHED TFChunk {0}.", chunk);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void CachedTFChunk(this ILogger logger, TFChunk chunk, TimeSpan elapsed)
        {
            logger.LogTrace("CACHED TFChunk {0} in {1}.", chunk, elapsed);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void CachingAbortedForTFChunk(this ILogger logger, TFChunk chunk)
        {
            logger.LogTrace("CACHING ABORTED for TFChunk {0} as TFChunk was probably marked for deletion.", chunk);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void VerifyingHashForTFChunk(this ILogger logger, string filename)
        {
            logger.LogTrace("Verifying hash for TFChunk '{0}'...", filename);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void UpgradingOngoingFileToVersion3(this ILogger logger, string filename)
        {
            logger.LogTrace("Upgrading ongoing file " + filename + " to version 3");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void UsingUnbufferedAccessForTFChunk(this ILogger logger, string filename)
        {
            logger.LogTrace("Using unbuffered access for TFChunk '{0}'...", filename);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void OpenedOngoing(this ILogger logger, string filename, byte version)
        {
            logger.LogTrace("Opened ongoing " + filename + " as version " + version);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ExceptionWasThrownWhileDoingBackgroundValidationOfChunk(this ILogger logger, FileBeingDeletedException exc, TFChunk chunk)
        {
            logger.LogTrace("{exceptionType} exception was thrown while doing background validation of chunk {chunk}.",
                exc.GetType().Name, chunk);
            logger.LogTrace("That's probably OK, especially if truncation was request at the same time: {e}.",
                exc.Message);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void StartedToMergeChunks(this ILogger logger, string oldChunksList, string tmpChunkPath)
        {
            logger.LogTrace("SCAVENGING: started to merge chunks: {oldChunksList}"
                + "\nResulting temp chunk file: {tmpChunkPath}.",
                oldChunksList, Path.GetFileName(tmpChunkPath));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void TriedToMergeLessThan2chunksAborting(this ILogger logger, string oldChunksList)
        {
            logger.LogTrace("SCAVENGING: Tried to merge less than 2 chunks, aborting: {oldChunksList}", oldChunksList);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ScavengingOfChunks(this ILogger logger, string oldChunkName, TimeSpan elapsed, string tmpChunkPath, int chunkStartNumber, int chunkEndNumber, string chunkFileName, long oldSize, long newSize)
        {
            logger.LogTrace("Scavenging of chunks:"
                + "\n{oldChunkName}"
                + "\ncompleted in {elapsed}."
                + "\nNew chunk: {tmpChunkPath} --> #{chunkStartNumber}-{chunkEndNumber} ({newChunk})."
                + "\nOld chunk total size: {oldSize}, scavenged chunk size: {newSize}.",
                oldChunkName, elapsed, Path.GetFileName(tmpChunkPath), chunkStartNumber, chunkEndNumber, Path.GetFileName(chunkFileName), oldSize, newSize);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ScavengingOfChunks(this ILogger logger, string oldChunkName, TimeSpan elapsed, int chunkStartNumber, int chunkEndNumber, string tmpChunkPath, long oldSize, long newSize)
        {
            logger.LogTrace("Scavenging of chunks:"
                + "\n{oldChunkName}"
                + "\ncompleted in {elapsed}."
                + "\nBut switching was prevented for new chunk: #{chunkStartNumber}-{chunkEndNumber} ({tmpChunkPath})."
                + "\nOld chunks total size: {oldSize}, scavenged chunk size: {newSize}.",
                oldChunkName, elapsed, chunkStartNumber, chunkEndNumber, Path.GetFileName(tmpChunkPath), oldSize, newSize);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ScavengingOfChunks(this ILogger logger, string oldChunkName, TimeSpan elapsed, long oldSize, long newSize)
        {
            logger.LogTrace(
                "Scavenging of chunks:"
                + "\n{oldChunkName}"
                + "\ncompleted in {elapsed}."
                + "\nOld chunks' versions are kept as they are smaller."
                + "\nOld chunk total size: {oldSize}, scavenged chunk size: {newSize}."
                + "\nScavenged chunk removed.", oldChunkName, elapsed, oldSize, newSize);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Forcing_scavenge_chunk_to_be_kept_even_if_bigger(this ILogger logger)
        {
            logger.LogTrace("Forcing scavenge chunk to be kept even if bigger.");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ForcingScavengedChunkToBeKeptAsOldChunkIsAPreviousVersion(this ILogger logger)
        {
            logger.LogTrace("Forcing scavenged chunk to be kept as old chunk is a previous version.");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ScavengingChunkTraversedIncluding(this ILogger logger, string oldChunkName, int recordsCount, int filteredCount)
        {
            logger.LogTrace("Scavenging {oldChunkName} traversed {recordsCount} including {filteredCount}.", oldChunkName, recordsCount, filteredCount);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void StartedToScavengeChunks(this ILogger logger, string oldChunkName, int chunkStartNumber, int chunkEndNumber, long chunkStartPos, long chunkEndPos, string tmpChunkPath)
        {
            logger.LogTrace("SCAVENGING: started to scavenge chunks: {oldChunkName} {chunkStartNumber} => {chunkEndNumber} ({chunkStartPosition} => {chunkEndPosition})", oldChunkName, chunkStartNumber, chunkEndNumber,
                chunkStartPos, chunkEndPos);
            logger.LogTrace("Resulting temp chunk file: {tmpChunkPath}.", Path.GetFileName(tmpChunkPath));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ScavengingTotaltimetakenTotalSpaceSaved(this ILogger logger, TimeSpan elapsed, long spaceSaved)
        {
            logger.LogTrace("SCAVENGING: total time taken: {elapsed}, total space saved: {spaceSaved}.", elapsed, spaceSaved);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ScavengingMergePassCompleted(this ILogger logger, int passNum, TimeSpan elapsed, bool mergedSomething)
        {
            logger.LogTrace("SCAVENGING: merge pass #{pass} completed in {elapsed}. {merged} merged.", passNum, elapsed, mergedSomething ? "Some chunks" : "Nothing");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ScavengingInitialPassCompleted(this ILogger logger, TimeSpan elapsed)
        {
            logger.LogTrace("SCAVENGING: initial pass completed in {elapsed}.", elapsed);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Removing_excess_chunk_version(this ILogger logger, string chunkFileName)
        {
            logger.LogTrace("Removing excess chunk version: {chunk}...", chunkFileName);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Deleting_temporary_file(this ILogger logger, string chunkFileName)
        {
            logger.LogTrace("Deleting temporary file {file}...", chunkFileName);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void StartedScavengingOfDB(this ILogger logger, int chunksCount, bool alwaysKeepScavenged, bool mergeChunks)
        {
            logger.LogTrace("SCAVENGING: started scavenging of DB. Chunks count at start: {chunksCount}. Options: alwaysKeepScavenged = {alwaysKeepScavenged}, mergeChunks = {mergeChunks}",
                chunksCount, alwaysKeepScavenged, mergeChunks);
        }
    }
}
