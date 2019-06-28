using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using CuteAnt.Pool;
using EventStore.Core.Data;
using EventStore.Core.Exceptions;
using EventStore.Core.Index;
using EventStore.Core.Messages;
using EventStore.Core.Messaging;
using EventStore.Core.Services;
using EventStore.Core.Services.PersistentSubscription;
using EventStore.Core.TransactionLog;
using EventStore.Core.TransactionLog.Chunks;
using EventStore.Core.TransactionLog.Chunks.TFChunk;
using EventStore.Core.TransactionLog.LogRecords;
using EventStore.Transport.Tcp;
using EsIndexReadStreamResult = EventStore.Core.Services.Storage.ReaderIndex.IndexReadStreamResult;

namespace EventStore.Core
{
    #region -- ExceptionArgument --

    /// <summary>The convention for this enum is using the argument name as the enum name</summary>
    internal enum ExceptionArgument
    {
        n,
        id,
        action,
        handle,
        md5,
        result,
        prefix,
        streamToUse,
        offset,
        completed,
        unwrapper,
        replyMessage,
        replyAction,
        ipEndPoints,
        creator,
        compare,
        comparer,
        oldChunk,
        toScavenge,
        scavengerLog,
        streamAccessType,
        maxDataSize,
        lastEventNumber,
        startVersion,
        endVersion,
        number,
        maxCachedEntries,
        uriRouter,
        prefixes,
        formatter,
        expectedVersion,
        transactionOffset,
        recordType,
        configurator,
        callback,
        uriTemplate,
        httpMethod,
        requestCodecs,
        responseCodecs,
        link,
        msg,
        httpAuthenticationProviders,
        inputBus,
        valueSize,
        metastreamMetadata,
        indexReader,
        dict,
        chunkHeader,
        consumerStrategy,
        ioDispatcher,
        subscribeToBus,
        tcpSendPublisher,
        checkpoint,
        readerFactory,
        indexCommitterService,
        logManager,
        indexCommitter,
        log,
        statGroup,
        monitoringQueue,
        statsCollectionBus,
        tcpEndpoint,
        statsContainer,
        groupName,
        messageParker,
        checkpointWriter,
        checkpointReader,
        eventLoader,
        dispatcherFactory,
        fileNamingStrategy,
        nextEventNumber,
        forwardingProxy,
        httpPipe,
        getLastCommitPosition,
        timeoutMessage,
        mainQueue,
        vnodeSettings,
        serverEndPoint,
        isEndOfStream,
        dataBytes,
        chunkHeaderBytes,
        masterEndPoint,
        connection,
        statsSelector,
        queuedHandler,
        epochCheckpoint,
        truncateCheckpoint,
        replicationCheckpoint,
        md5Hash,
        httpPrefixes,
        handler,
        queueCount,
        config,
        events,
        authenticationProvider,
        workerThreads,
        maxReaders,
        vNodeSettings,
        gossipSeedSource,
        entity,
        dbPath,
        queueFactory,
        queues,
        gossipAdvertiseInfo,
        clusterDns,
        gossipSeeds,
        intHttpPrefixes,
        indexCacheDepth,
        maxTablesPerLevel,
        extHttpPrefixes,
        correlationId_should_be_equal,
        certificate,
        sslTargetHost,
        entries,
        memTableFactory,
        lowHasher,
        highHasher,
        tfReaderFactory,
        factory,
        packageHandler,
        path,
        writerCheckpoint,
        timer,
        epoch,
        indexBackend,
        node,
        outputBus,
        indexWriter,
        epochManager,
        masterBus,
        chaser,
        mainBus,
        stats,
        readers,
        manager,
        timeProvider,
        nodeEndpoint,
        controller,
        backend,
        master,
        records,
        readerPool,
        dispatcher,
        writer,
        subscriber,
        openedConnection,
        tableIndex,
        lastEpochs,
        vNodeEndPoint,
        filename,
        subSystemName,
        replicaEndPoint,
        service,
        input,
        externalTcpEndPoint,
        internalTcpEndPoint,
        internalHttpEndPoint,
        externalHttpEndPoint,
        serviceName,
        table,
        tables,
        processedEventIds,
        directory,
        readIndex,
        outputFile,
        objectPoolName,
        buffer,
        poolName,
        instanceId,
        sender,
        entries_Count,
        maxPackageSize,
        midpointsDepth,
        rawBytes_Length,
        queues_Length,
        initializationThreads,
        epochId,
        streamId,
        stateCorrelationId,
        eventId,
        commitAckCount,
        connectionId,
        prepareAckCount,
        clusterNodeCount,
        correlationId,
        tickInterval,
        internalCorrId,
        commitPos,
        clusterSize,
        position,
        commitCount,
        metastreamMaxCount,
        threadCount,
        states_Length,
        fileSize,
        chunkSize,
        maxReaderCount,
        chaserCheckpoint,
        depth,
        commitCheckpoint,
        prepareCheckpoint,
        entry_Version,
        maxLimit,
        entry_Position,
        initialCount,
        streamInfoCacheCapacity,
        fromEventNumber,
        epochPosition,
        threads,
        epochNumber,
        installedView,
        AllowedInFlightMessages,
        prepareCount,
        queueId,
        FirstEventNumber,
        dataBytes_Length,
        cachedEpochCount,
        initialReaderCount,
        message,
        endPoint,
        name,
        maxCount,
        firstEventNumber,
        index,
        startFromChunk,
        initialPosition,
        maxChunksCacheSize,
        chunkEndNumber,
        chunkStartNumber,
        mapSize,
        logicalDataSize,
        physicalDataSize,
        writePosition,
        count,
        consumer,
        eventStreamId,
        internalTcp,
        externalTcp,
        internalHttp,
        subscriptionPosition,
        externalHttp,
        envelope,
        data,
        chunk,
        masterId,
        subscriptionId,
        bus,
        rawBytes,
        db,
        publisher,
        networkSendQueue,
        authProvider,
        logPosition,
        nodeInfo,
        version,
        transactionId,
        cacheDepth,
        startNumber,
        endNumber,
        transactionPosition,
        eventNumber,
    }

    #endregion

    #region -- ExceptionResource --

    /// <summary>The convention for this enum is using the resource name as the enum name</summary>
    internal enum ExceptionResource
    {
        Unable_to_acquire_work_item,
        Incorrect_Message_Type_IDs_setup,
        Could_not_begin_restart_session,
        Could_not_register_resource,
        Could_not_list_processes_locking_resource,
        Could_not_list_processes_locking_resource_F,
        Too_many_retrials_to_acquire_reader,
        BulkReader_is_null_for_subscription,
        StateIsNotReplica,
        ConnectionIsNull,
        TransactionId_was_not_set,
        Concurrency_Error_In_ReadIndex_Commit,
        Could_not_read_latest_stream_prepare,
        No_transaction_ID_specified,
        Record_too_large,
        SCAVENGING_no_chunks_to_merge,
        No_chunks_in_DB,
        Count_of_file_streams_reduced_below_zero,
        Unable_to_acquire_reader_work_item,
        Not_enough_memory_streams_during_inMem_TFChunk_mode,
        Count_of_memory_streams_reduced_below_zero,
        MemStream_readers_are_in_use_when_writing_scavenged_chunk,
        YouCanOnlyRunUnbufferedModeOnV3OrHigherChunkFiles,
        There_is_some_data_left_in_framer_when_completing_chunk,
        Data_chunk_bulk_received_but,
        Physical_chunk_bulk_received_but,
        First_write_failed_when_writing_replicated_record,
        Could_not_append_raw_bytes_to_chunk,
        Master_subscribed_which_is_greater,
        Received_request_to_create_chunk,
        IReplicationMessage_with_empty_SubscriptionId_provided,
        We_should_not_BecomeMaster_twice_in_a_row,
        NewPositionIsLessThanOldPosition,
        offset_count_must_be_less_than_size_of_array,
        Provided_list_of_chunks_to_merge_is_empty,
        Scavenged_TFChunk_passed_into_unscavenged_chunk_read_side,
        Duplicate_route,
        Neither_eventStreamId_nor_transactionId,
        Invalid_constructor_used_for_successful_write,
        Unable_to_find_table_in_map,
        Record_is_too_big,
        Empty_eventType_provided,
        Empty_eventId_provided,
        Could_not_finish_background_thread_in_reasonable_time,
        Waiting_for_background_tasks_took_too_long,
    }

    #endregion

    partial class ThrowHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsInvalidEventNumber(long value)
        {
            const long End = -1L;

            return (ulong)(value - End) > 9223372036854775808ul/*unchecked((ulong)(long.MaxValue - End))*/;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsInvalidCheckpoint(long value)
        {
            const long End = -1L;

            return (ulong)(value - End) > 9223372036854775808ul/*unchecked((ulong)(long.MaxValue - End))*/;
        }

        #region -- Exception --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static Exception GetException_CommitInfo_for_stream_is_not_present(string x)
        {
            return new Exception($"CommitInfo for stream '{x}' is not present!");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException()
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException(ExceptionResource resource)
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception(GetResourceString(resource));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_UnknownNodeState(VNodeState state)
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception($"Unknown node state: {state}.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static Message ThrowException_NewEpochRequestNotInMasterState(VNodeState state)
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception($"New Epoch request not in master state. State: {state}.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static Message ThrowException_WaitForChaserToCatchUpAppearedIn(SystemMessage.WaitForChaserToCatchUp message, VNodeState state)
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception(string.Format("{0} appeared in {1} state.", message.GetType().Name, state));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static Message ThrowException_CommitInvariantViolation(long newEventNumber, long lastEventNumber, string streamId, CommitLogRecord commit)
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception(
                            string.Format("Commit invariant violation: new event number {0} does not correspond to current stream version {1}.\n"
                                          + "Stream ID: {2}.\nCommit: {3}.", newEventNumber, lastEventNumber, streamId, commit));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static Message ThrowException_SecondWriteTryFailedWhenFirstWritingPrepare(long logPosition, long writtenPos)
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception($"Second write try failed when first writing prepare at {logPosition}, then at {writtenPos}.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static Message ThrowException_SecondWriteTryFailedWhenFirstWritingCommit(long logPosition, long writtenPos)
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception($"Second write try failed when first writing commit at {logPosition}, then at {writtenPos}.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static Message ThrowException_UnexpectedReadRequest(Message originalRequest)
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception($"Unexpected read request of type {originalRequest.GetType()} for long polling: {originalRequest}.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static Message ThrowException_UnhandledMessage(Message message, VNodeState state)
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception(string.Format("Unhandled message: {0} occurred in state: {1}.", message, state));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static ChunkFooter ThrowException_ErrorInChunkFile(string filename, Exception ex)
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception("error in chunk file " + filename, ex);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_DataSizesViolation(string fileName, bool isScavenged, long logicalDataSize, int physicalDataSize)
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception(string.Format("Data sizes violation. Chunk: {0}, IsScavenged: {1}, LogicalDataSize: {2}, PhysicalDataSize: {3}.",
                                                  fileName, isScavenged, logicalDataSize, physicalDataSize));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_SuffixLengthInconsistency(int length, int suffixLength, long actualPosition, TFChunk chunk)
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception(
                    string.Format("Prefix/suffix length inconsistency: prefix length({0}) != suffix length ({1}).\n"
                                  + "Actual pre-position: {2}. Something is seriously wrong in chunk {3}.",
                                  length, suffixLength, actualPosition, chunk));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_PrefixLengthInconsistency(int prefixLength, int length, long actualPosition, TFChunk chunk)
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception(
                            string.Format("Prefix/suffix length inconsistency: prefix length({0}) != suffix length ({1})"
                                          + "Actual post-position: {2}. Something is seriously wrong in chunk {3}.",
                                          prefixLength, length, actualPosition, chunk));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_GlobalLogicalPositionIsOutOfChunkLogicalPositions(long globalLogicalPosition, long chunkStartPosition, long chunkEndPosition)
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception(string.Format("globalLogicalPosition {0} is out of chunk logical positions [{1}, {2}].",
                                                  globalLogicalPosition, chunkStartPosition, chunkEndPosition));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_DuringTruncationOfDBExcessiveTFChunksWereFound(string[] excessiveChunks)
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception($"During truncation of DB excessive TFChunks were found:\n{string.Join("\n", excessiveChunks)}.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_CouldnotFindAnyChunk(int chunkNum)
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception($"Could not find any chunk #{chunkNum}.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_ChunkIsNotCorrectUnscavengedChunk(ChunkHeader chunkHeader, string chunkFilename, long truncateChk)
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception(
                    string.Format("Chunk #{0}-{1} ({2}) is not correct unscavenged chunk. TruncatePosition: {3}, ChunkHeader: {4}.",
                                  chunkHeader.ChunkStartNumber, chunkHeader.ChunkEndNumber, chunkFilename, truncateChk, chunkHeader));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_ReceivedRequestToCreateAnewOngoingChunk(int chunkStartNumber, int chunkEndNumber, int chunksCount)
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception(string.Format("Received request to create a new ongoing chunk #{0}-{1}, but current chunks count is {2}.",
                                                      chunkStartNumber, chunkEndNumber, chunksCount));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_TheChunkThatIsBeingSwitched(TFChunk chunk, TimeoutException exc)
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception($"The chunk that is being switched {chunk} is used by someone else.", exc);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_ExcessiveChunkFoundAfterRawReplicationSwitch(int chunksCount)
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception($"Excessive chunk #{chunksCount} found after raw replication switch.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_RequestedPositionIsGreaterThanWriterCheckpoint(long pos, long writerChk)
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception($"Requested position {pos} is greater than writer checkpoint {writerChk} when requesting to read previous record from TF.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_GotFileThatWasBeingDeletedTimesFromTFChunkDb(int maxRetries)
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception($"Got a file that was being deleted {maxRetries} times from TFChunkDb, likely a bug there.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_UnableToAppendRecordDuringScavenging(long oldPosition, LogRecord record)
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception(string.Format(
                    "Unable to append record during scavenging. Scavenge position: {0}, Record: {1}.",
                    oldPosition, record));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_CouldnotDetermineVersionFromFilename(string firstVersion)
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception(string.Format("Could not determine version from filename '{0}'.", firstVersion));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_WritebuffersizeMustbealignedtoBlockSize(uint blockSize)
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception("write buffer size must be aligned to block size of " + blockSize + " bytes");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_ReadbuffersizeMustbealignedtoBlockSize(uint blockSize)
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception("read buffer size must be aligned to block size of " + blockSize + " bytes");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_InvalidOrderOfEventsHasBeenDetectedInReadIndex(ClientMessage.ReadStreamEventsForward msg, in EsIndexReadStreamResult result, int index)
        {
            throw new Exception(
                    string.Format("Invalid order of events has been detected in read index for the event stream '{0}'. "
                                  + "The event {1} at position {2} goes after the event {3} at position {4}",
                                  msg.EventStreamId,
                                  result.Records[index].EventNumber,
                                  result.Records[index].LogPosition,
                                  result.Records[index - 1].EventNumber,
                                  result.Records[index - 1].LogPosition));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_InvalidOrderOfEventsHasBeenDetectedInReadIndex(ClientMessage.ReadStreamEventsBackward msg, in EsIndexReadStreamResult result, int index)
        {
            throw new Exception(string.Format("Invalid order of events has been detected in read index for the event stream '{0}'. "
                                              + "The event {1} at position {2} goes after the event {3} at position {4}",
                                              msg.EventStreamId,
                                              result.Records[index].EventNumber,
                                              result.Records[index].LogPosition,
                                              result.Records[index - 1].EventNumber,
                                              result.Records[index - 1].LogPosition));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_NoTransactionWithID(StorageMessage.CheckStreamAccess msg)
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception($"No transaction with ID {msg.TransactionId} found.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_EventNumberIsIncorrect(long eventNumber)
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception($"EventNumber {eventNumber} is incorrect.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_ReadPrepareInternalCouldNotFindMetaevent(long metaEventNumber, string metastreamId)
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception($"ReadPrepareInternal could not find metaevent #{metaEventNumber} on metastream '{metastreamId}'. That should never happen.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_TryingToAddDuplicateEventToStream(long version, PrepareLogRecord prepare, CommitLogRecord commit, PrepareLogRecord indexedPrepare)
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception(string.Format("Trying to add duplicate event #{0} to stream {1} \nCommit: {2}\n"
                                                  + "Prepare: {3}\nIndexed prepare: {4}.",
                                                  version, prepare.EventStreamId, commit, prepare, indexedPrepare));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_ExpectedStream(string streamId, PrepareLogRecord prepare)
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception($"Expected stream: {streamId}, actual: {prepare.EventStreamId}.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_ExpectedStream(string streamId, PrepareLogRecord prepare, IList<PrepareLogRecord> commitedPrepares)
        {
            throw GetException();
            Exception GetException()
            {
                var sb = StringBuilderManager.Allocate();
                sb.Append($"ERROR: Expected stream: {streamId}, actual: {prepare.EventStreamId}.");
                sb.Append(Environment.NewLine);
                sb.Append(Environment.NewLine);
                sb.Append("Prepares: (" + commitedPrepares.Count + ")");
                sb.Append(Environment.NewLine);
                for (int i = 0; i < commitedPrepares.Count; i++)
                {
                    var p = commitedPrepares[i];
                    sb.Append("Stream ID: " + p.EventStreamId);
                    sb.Append(Environment.NewLine);
                    sb.Append("LogPosition: " + p.LogPosition);
                    sb.Append(Environment.NewLine);
                    sb.Append("Flags: " + p.Flags);
                    sb.Append(Environment.NewLine);
                    sb.Append("Type: " + p.EventType);
                    sb.Append(Environment.NewLine);
                    sb.Append("MetaData: " + Encoding.UTF8.GetString(p.Metadata));
                    sb.Append(Environment.NewLine);
                    sb.Append("Data: " + Encoding.UTF8.GetString(p.Data));
                    sb.Append(Environment.NewLine);
                }
                return new Exception(StringBuilderManager.ReturnAndFree(sb));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_ExpectedStream(string streamId, PrepareLogRecord prepare, long logPosition)
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception($"Expected stream: {streamId}, actual: {prepare.EventStreamId}. LogPosition: {logPosition}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_LastCommitPositionIsGreaterThanOrEqualBuildToPosition(long lastCommitPosition, long buildToPosition)
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception($"_lastCommitPosition {lastCommitPosition} >= buildToPosition {buildToPosition}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_IncorrectTypeOfLogRecord(LogRecordType recordType)
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception($"Incorrect type of log record {recordType}, expected Prepare record.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_UnknownRecordType(LogRecordType recordType)
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception(string.Format("Unknown RecordType: {0}", recordType));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_UnexpectedLogRecordType(LogRecordType recordType)
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception(string.Format("Unexpected log record type: {0}.", recordType));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_NotFoundEpoch(long epochPosition, int epochNumber, Guid epochId)
        {
            var msg = string.Format("Not found epoch at {0} with epoch number: {1} and epoch ID: {2}. "
                                    + "SetLastEpoch FAILED! Data corruption risk!",
                                    epochPosition,
                                    epochNumber,
                                    epochId);
            throw GetException();
            Exception GetException()
            {
                return new Exception(msg);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_SecondWriteTryFailed(long epochPosition)
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception($"Second write try failed at {epochPosition}.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_ConcurrencyFailureEpochIsNull(int epochNumber)
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception(string.Format("Concurrency failure, epoch #{0} should not be null.", epochNumber));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_SystemLogRecordIsNotOfEpochSubType(in RecordReadResult result)
        {
            var msg = $"SystemLogRecord is not of Epoch sub-type: {result.LogRecord}.";
            throw GetException();
            Exception GetException()
            {
                return new Exception(msg);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_LogRecordIsNotSystemLogRecord(in RecordReadResult result)
        {
            var msg = $"LogRecord is not SystemLogRecord: {result.LogRecord}.";
            throw GetException();
            Exception GetException()
            {
                return new Exception(msg);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_CouldNotFindEpochRecordAtLogPosition(long epochPos)
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception($"Could not find Epoch record at LogPosition {epochPos}.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_UnexpectedState(VNodeState state)
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception($"Unexpected state: {state}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_StateIsExpectedToBeVNodeStatePreReplica(VNodeState state)
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception($"_state is {state}, but is expected to be {VNodeState.PreReplica}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_ChunkWasNullDuringSubscribing(long logPosition)
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception($"Chunk was null during subscribing at {logPosition} (0x{logPosition:X}).");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_ReplicationInvariantFailure(long logPosition, int oldPosition)
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception(string.Format("Replication invariant failure. SubscriptionPosition {0}, bulkResult.OldPosition {1}", logPosition, oldPosition));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_UnexpectedOperationResultWritingPersistentSubscriptionConfiguration(OperationResult result)
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception(result + " is an unexpected result writing persistent subscription configuration.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_UnexpectedReadStreamResultWritingSubscriptionConfiguration(ReadStreamResult result)
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception(result + " is an unexpected result writing subscription configuration.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_MessageDoesntHaveTypeIdField(Type type)
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception($"Message {type.Name} doesn't have TypeId field!");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_InvalidJson()
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception("Invalid JSON");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_MasterIsNull()
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception("_master == null");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_SomehowWeManagedToDecreaseCountOfPoolItemsBelowZero()
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception("Somehow we managed to decrease count of pool items below zero.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_NotAllIndexEntriesInABulkHaveTheSameStreamHash()
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception("Not all index entries in a bulk have the same stream hash.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_ShouldNotDoNegativeReads()
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception("should not do negative reads.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_PrefixLengthIsNotEqualToSuffixLength(int packageLength, int prefixLength, int suffixLength)
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception($"Prefix length: {packageLength - prefixLength} is not equal to suffix length: {suffixLength}.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_NoDescendantsForMessage<T>() where T : Message
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception(string.Format("No descendants for message of type '{0}'.", typeof(T).Name));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_RequestedChunkWhichIsNotPresentInTFChunkManager(int chunkNum)
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception($"Requested chunk #{chunkNum}, which is not present in TFChunkManager.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_RequestedChunkForLogPositionWhichIsNotPresentInTFChunkManager(long logPosition)
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception($"Requested chunk for LogPosition {logPosition}, which is not present in TFChunkManager.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_WrongMapSize(int mapSize, int posMapSize)
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception($"Wrong MapSize {mapSize} -- not divisible by PosMap.Size {posMapSize}.");
            }
        }

        #endregion

        #region -- ArgumentException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_NotEmptyGuid(ExceptionArgument argument)
        {
            throw GetException();
            ArgumentException GetException()
            {
                var argumentName = GetArgumentName(argument);
                return new ArgumentException($"{argumentName} should be non-empty GUID.", argumentName);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_Equal(int expected, int actual, ExceptionArgument argument)
        {
            throw GetException();
            ArgumentException GetException()
            {
                var argumentName = GetArgumentName(argument);
                return new ArgumentException($"{argumentName} expected value: {expected}, actual value: {actual}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_Equal(long expected, long actual, ExceptionArgument argument)
        {
            throw GetException();
            ArgumentException GetException()
            {
                var argumentName = GetArgumentName(argument);
                return new ArgumentException($"{argumentName} expected value: {expected}, actual value: {actual}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_Equal(bool expected, bool actual, ExceptionArgument argument)
        {
            throw GetException();
            ArgumentException GetException()
            {
                var argumentName = GetArgumentName(argument);
                return new ArgumentException($"{argumentName} expected value: {expected}, actual value: {actual}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_MD5HashIsOfWrongLength()
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException("MD5Hash is of wrong length.", "md5Hash");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_UseOnlySizesEqualPowerOf2()
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException("Use only sizes equal power of 2");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_MPSCTheSizeShouldBeAtLeast()
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException("The size should be at least " + Bus.MPSCMessageQueue.MinimalSize);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_SPSCTheSizeShouldBeAtLeast()
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException("The size should be at least " + Bus.SPSCMessageQueue.MinimalSize);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_WrongStateForVNode(VNodeState state)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException(string.Format("Wrong State for VNode: {0}", state), nameof(state));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_TheNamedConsumerStrategyIsUnknown(string namedConsumerStrategy)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException(string.Format("The named consumer strategy '{0}' is unknown.", namedConsumerStrategy), nameof(namedConsumerStrategy));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_WrongReadEventResultProvided(ReadEventResult result)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException(string.Format("Wrong ReadEventResult provided for failure constructor: {0}.", result), nameof(result));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_WrongReadStreamResultProvided(EventStore.Core.Services.Storage.ReaderIndex.ReadStreamResult result)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException(String.Format("Wrong ReadStreamResult provided for failure constructor: {0}.", result), nameof(result));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_ReadSizeTooBig(int maxPageSize)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException($"Read size too big, should be less than {maxPageSize} items");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_LogRecordThatEndsAtActualPosHasTooLargeLength(long actualPosition, int length, TFChunk chunk)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException(
                        string.Format("Log record that ends at actual pos {0} has too large length: {1} bytes, "
                                      + "while limit is {2} bytes. In chunk {3}.",
                                      actualPosition, length, TFConsts.MaxLogRecordSize, chunk));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_ChunkProvidedIsNotScavenged(TFChunk chunk)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException($"Chunk provided is not scavenged: {chunk}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_PassedTFChunkIsNotCompleted(TFChunk chunk)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException($"Passed TFChunk is not completed: {chunk.FileName}.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_CommitRecordVersionIsIncorrect(byte version)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException(
                    string.Format("CommitRecord version {0} is incorrect. Supported version: {1}.", version, CommitLogRecord.CommitRecordVersion));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_PrepareRecordVersionIsIncorrect(byte version)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException(string.Format(
                    "PrepareRecord version {0} is incorrect. Supported version: {1}.", version, PrepareLogRecord.PrepareRecordVersion));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_SystemRecordVersionIsIncorrect(byte version)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException(string.Format(
                    "SystemRecord version {0} is incorrect. Supported version: {1}.", version, SystemLogRecord.SystemRecordVersion));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_InvalidSystemRecordType(SystemRecordType systemRecordType, long logPosition)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException(string.Format("Invalid SystemRecordType {0} at LogPosition {1}.", systemRecordType, logPosition));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_InvalidSystemRecordSerialization(SystemRecordSerialization systemRecordSerialization, long logPosition)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException(string.Format("Invalid SystemRecordSerialization {0} at LogPosition {1}.", systemRecordSerialization, logPosition));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_UnexpectedTypeOfSystemRecord(SystemRecordType systemRecordType)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException(
                    string.Format("Unexpected type of system record. Requested: {0}, actual: {1}.", SystemRecordType.Epoch, systemRecordType));
            }
        }

        #endregion

        #region -- ArgumentOutOfRangeException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentOutOfRangeException_Positive(ExceptionArgument argument)
        {
            throw GetException();
            ArgumentOutOfRangeException GetException()
            {
                var argumentName = GetArgumentName(argument);
                return new ArgumentOutOfRangeException(argumentName, $"{argumentName} should be positive.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentOutOfRangeException_Nonnegative(ExceptionArgument argument)
        {
            throw GetException();
            ArgumentOutOfRangeException GetException()
            {
                var argumentName = GetArgumentName(argument);
                return new ArgumentOutOfRangeException(argumentName, $"{argumentName} should be non negative.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentOutOfRangeException_UnexpectedSystemRecordSerializationType(SystemRecordSerialization systemRecordSerialization)
        {
            throw GetException();
            ArgumentOutOfRangeException GetException()
            {
                return new ArgumentOutOfRangeException(
                            string.Format("Unexpected SystemRecordSerialization type: {0}", systemRecordSerialization),
                            "SystemRecordSerialization");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentOutOfRangeException_ChunkIsNotPresentInDB(int chunkNum)
        {
            throw GetException();
            ArgumentOutOfRangeException GetException()
            {
                return new ArgumentOutOfRangeException("chunkNum", $"Chunk #{chunkNum} is not present in DB.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentOutOfRangeException_LogPositionDoesNotHaveCorrespondingChunkInDB(long logPosition)
        {
            throw GetException();
            ArgumentOutOfRangeException GetException()
            {
                return new ArgumentOutOfRangeException(nameof(logPosition), $"LogPosition {logPosition} does not have corresponding chunk in DB.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentOutOfRangeException_DataPositionIsOutOfBounds(long dataPosition)
        {
            throw GetException();
            ArgumentOutOfRangeException GetException()
            {
                return new ArgumentOutOfRangeException(nameof(dataPosition), string.Format("Data position {0} is out of bounds.", dataPosition));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentOutOfRangeException_RawPositionIsOutOfBounds(int rawPosition)
        {
            throw GetException();
            ArgumentOutOfRangeException GetException()
            {
                return new ArgumentOutOfRangeException(nameof(rawPosition), string.Format("Raw position {0} is out of bounds.", rawPosition));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentOutOfRangeException_ChunkStartNumberIsGreaterThanChunkEndNumber()
        {
            throw GetException();
            ArgumentOutOfRangeException GetException()
            {
                return new ArgumentOutOfRangeException("chunkStartNumber", "chunkStartNumber is greater than ChunkEndNumber.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentOutOfRangeException_LogicalDataSizeIsLessThanPhysicalDataSize(long logicalDataSize, int physicalDataSize)
        {
            throw GetException();
            ArgumentOutOfRangeException GetException()
            {
                return new ArgumentOutOfRangeException(nameof(logicalDataSize), $"LogicalDataSize {logicalDataSize} is less than PhysicalDataSize {physicalDataSize}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static PosMap ThrowArgumentOutOfRangeException_CouldNotReadPosMapAtIndex(long index)
        {
            throw GetException();
            ArgumentOutOfRangeException GetException()
            {
                return new ArgumentOutOfRangeException("Could not read PosMap at index: " + index);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentOutOfRangeException_DepthTooForMidpoints()
        {
            throw GetException();
            ArgumentOutOfRangeException GetException()
            {
                return new ArgumentOutOfRangeException("depth", "Depth too for midpoints.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentOutOfRangeException_InitialReaderCountIsGreaterThanMaxReaderCount()
        {
            throw GetException();
            ArgumentOutOfRangeException GetException()
            {
                return new ArgumentOutOfRangeException("initialReaderCount", "initialReaderCount is greater than maxReaderCount.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentOutOfRangeException_InitialCountIsGreaterThanMaxCount()
        {
            throw GetException();
            ArgumentOutOfRangeException GetException()
            {
                return new ArgumentOutOfRangeException("initialCount", "initialCount is greater than maxCount.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentOutOfRangeException_UnknownReadAllResult(ReadAllResult result)
        {
            throw GetException();
            ArgumentOutOfRangeException GetException()
            {
                return new ArgumentOutOfRangeException($"Unknown ReadAllResult: {result}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentOutOfRangeException_UnknownReadStreamResult(ReadStreamResult result)
        {
            throw GetException();
            ArgumentOutOfRangeException GetException()
            {
                return new ArgumentOutOfRangeException(string.Format("Unknown ReadStreamResult: {0}", result));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentOutOfRangeException_EpochNumberRequestedShouldNotBeCached(int epochNumber, int minCachedEpochNumber)
        {
            throw GetException();
            ArgumentOutOfRangeException GetException()
            {
                return new ArgumentOutOfRangeException(nameof(epochNumber),
                    $"EpochNumber requested should not be cached. Requested: {epochNumber}, min cached: {minCachedEpochNumber}.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentOutOfRangeException_CalculatedNumberOfBitsIsTooLarge(long m)
        {
            throw GetException();
            ArgumentOutOfRangeException GetException()
            {
                return new ArgumentOutOfRangeException("p", "calculated number of bits, m, is too large: " + m + ". please choose a larger value of p.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentOutOfRangeException_ShouldBeBetween0And05Exclusive()
        {
            throw GetException();
            ArgumentOutOfRangeException GetException()
            {
                return new ArgumentOutOfRangeException("p", "p should be between 0 and 0.5 exclusive");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentOutOfRangeException_StreamMetadata_MaxCount()
        {
            throw GetException();
            ArgumentOutOfRangeException GetException()
            {
                return new ArgumentOutOfRangeException("maxCount", $"{SystemMetadata.MaxCount} should be positive value.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentOutOfRangeException_StreamMetadata_MaxAge()
        {
            throw GetException();
            ArgumentOutOfRangeException GetException()
            {
                return new ArgumentOutOfRangeException("maxAge", $"{SystemMetadata.MaxAge} should be positive time span.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentOutOfRangeException_StreamMetadata_TruncateBefore()
        {
            throw GetException();
            ArgumentOutOfRangeException GetException()
            {
                return new ArgumentOutOfRangeException("truncateBefore", $"{SystemMetadata.TruncateBefore} should be non-negative value.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentOutOfRangeException_StreamMetadata_CacheControl()
        {
            throw GetException();
            ArgumentOutOfRangeException GetException()
            {
                return new ArgumentOutOfRangeException("cacheControl", $"{SystemMetadata.CacheControl} should be positive time span.");
            }
        }

        #endregion

        #region -- InvalidOperationException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidOperationException()
        {
            throw GetException();
            InvalidOperationException GetException()
            {
                return new InvalidOperationException();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidOperationException_FilesAreLocked()
        {
            throw GetException();
            InvalidOperationException GetException()
            {
                return new InvalidOperationException("Files are locked.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidOperationException_CannotCompleteReadonlyTFChunk()
        {
            throw GetException();
            InvalidOperationException GetException()
            {
                return new InvalidOperationException("Cannot complete a read-only TFChunk.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidOperationException_AlreadyAThreadRunning()
        {
            throw GetException();
            InvalidOperationException GetException()
            {
                return new InvalidOperationException("Already a thread running.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidOperationException_NoChunkGivenForExistingPosition()
        {
            throw GetException();
            InvalidOperationException GetException()
            {
                return new InvalidOperationException("No chunk given for existing position.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidOperationException_TheRawChunkIsNotCompletelyWritten()
        {
            throw GetException();
            InvalidOperationException GetException()
            {
                return new InvalidOperationException("The raw chunk is not completely written.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidOperationException_CannotWriteToReadonlyBlock()
        {
            throw GetException();
            InvalidOperationException GetException()
            {
                return new InvalidOperationException("Cannot write to a read-only block.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidOperationException_CannotWriteToReadonlyTFChunk()
        {
            throw GetException();
            InvalidOperationException GetException()
            {
                return new InvalidOperationException("Cannot write to a read-only TFChunk.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidOperationException_CompleteScavengedShouldBeUsedForScavengedChunks()
        {
            throw GetException();
            InvalidOperationException GetException()
            {
                return new InvalidOperationException("CompleteScavenged should be used for scavenged chunks.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidOperationException_CompleteScavengedShouldNotBeUsedForNonScavengedChunks()
        {
            throw GetException();
            InvalidOperationException GetException()
            {
                return new InvalidOperationException("CompleteScavenged should not be used for non-scavenged chunks.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidOperationException_WhenTryingToBuildCache()
        {
            throw GetException();
            InvalidOperationException GetException()
            {
                return new InvalidOperationException("When trying to build cache, reader worker is already in-memory reader.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidOperationException_AllUsersReaderCannotBeReused()
        {
            throw GetException();
            InvalidOperationException GetException()
            {
                return new InvalidOperationException("AllUsersReader cannot be re-used");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidOperationException_CommitAckNotPresentInNodeList()
        {
            throw GetException();
            InvalidOperationException GetException()
            {
                return new InvalidOperationException("Commit ack not present in node list");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidOperationException_FailedToAddPendingCommit()
        {
            throw GetException();
            InvalidOperationException GetException()
            {
                return new InvalidOperationException("Failed to add pending commit");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidOperationException_FailedToUpdatePendingCommit()
        {
            throw GetException();
            InvalidOperationException GetException()
            {
                return new InvalidOperationException("Failed to update pending commit");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidOperationException_FailedToAddPendingPrepare()
        {
            throw GetException();
            InvalidOperationException GetException()
            {
                return new InvalidOperationException("Failed to add pending prepare");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidOperationException_FailedToUpdatePendingPrepare()
        {
            throw GetException();
            InvalidOperationException GetException()
            {
                return new InvalidOperationException("Failed to update pending prepare");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidOperationException_OnlyAddedClientsCanBeRemoved()
        {
            throw GetException();
            InvalidOperationException GetException()
            {
                return new InvalidOperationException("Only added clients can be removed.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidOperationException_TheMessageCanOnlyBeAcceptedOnce()
        {
            throw GetException();
            InvalidOperationException GetException()
            {
                return new InvalidOperationException("The message can only be accepted once.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidOperationException_ShouldNeverCompleteRequestTwice()
        {
            throw GetException();
            InvalidOperationException GetException()
            {
                return new InvalidOperationException("Should never complete request twice.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidOperationException_YoucanotVerifyHashOfNotCompletedTFChunk()
        {
            throw GetException();
            InvalidOperationException GetException()
            {
                return new InvalidOperationException("You can't verify hash of not-completed TFChunk.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidOperationException_TryingToWriteMappingWhileChunkIsCached()
        {
            throw GetException();
            InvalidOperationException GetException()
            {
                return new InvalidOperationException("Trying to write mapping while chunk is cached. "
                                                      + "You probably are writing scavenged chunk as cached. "
                                                      + "Do not do this.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidOperationException_UnknownPTableVersion(byte version)
        {
            throw GetException();
            InvalidOperationException GetException()
            {
                return new InvalidOperationException("Unknown PTable version: " + version);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidOperationException_TFChunkIsNotInWriteMode(string filename)
        {
            throw GetException();
            InvalidOperationException GetException()
            {
                return new InvalidOperationException(string.Format("TFChunk {0} is not in write mode.", filename));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidOperationException_DBMutexWasNotAcquired(string mutexName)
        {
            throw GetException();
            InvalidOperationException GetException()
            {
                return new InvalidOperationException($"DB mutex '{mutexName}' was not acquired.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidOperationException_DBMutexIsAlreadyAcquired(string mutexName)
        {
            throw GetException();
            InvalidOperationException GetException()
            {
                return new InvalidOperationException($"DB mutex '{mutexName}' is already acquired.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidOperationException_ClusterNodeMutexWasNotAcquired(string mutexName)
        {
            throw GetException();
            InvalidOperationException GetException()
            {
                return new InvalidOperationException($"DB mutex '{mutexName}' was not acquired.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidOperationException_ClusterNodeMutexIsAlreadyAcquired(string mutexName)
        {
            throw GetException();
            InvalidOperationException GetException()
            {
                return new InvalidOperationException($"DB mutex '{mutexName}' is already acquired.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidOperationException_DefaultHandlerAlreadyDefinedForState(VNodeState state)
        {
            throw GetException();
            InvalidOperationException GetException()
            {
                return new InvalidOperationException(string.Format("Default handler already defined for state {0}", state));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidOperationException_HandlerAlreadyDefinedForStateAndMessage<TActualMessage>(VNodeState state) where TActualMessage : Message
        {
            throw GetException();
            InvalidOperationException GetException()
            {
                return new InvalidOperationException(
                    string.Format("Handler already defined for state {0} and message {1}", state, typeof(TActualMessage).FullName));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidOperationException_UnhandledMessage(Message message, VNodeState state)
        {
            throw GetException();
            InvalidOperationException GetException()
            {
                return new InvalidOperationException(string.Format("Unhandled message: {0} occured in state: {1}.", message, state));
            }
        }

        #endregion

        #region -- TimeoutException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowTimeoutException()
        {
            throw GetException();
            TimeoutException GetException()
            {
                return new TimeoutException();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowTimeoutException(ExceptionResource resource)
        {
            throw GetException();
            TimeoutException GetException()
            {
                return new TimeoutException(GetResourceString(resource));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowTimeoutException_UnableToStopThread(string name)
        {
            throw GetException();
            TimeoutException GetException()
            {
                return new TimeoutException($"Unable to stop thread '{name}'.");
            }
        }

        #endregion

        #region -- NotSupportedException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowNotSupportedException()
        {
            throw GetException();
            NotSupportedException GetException()
            {
                return new NotSupportedException();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowNotSupportedException_QueueIsSupportedOnlyOnX64()
        {
            throw GetException();
            NotSupportedException GetException()
            {
                return new NotSupportedException("This queue is supported only on architectures having IntPtr.Size equal to 8");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowNotSupportedException_UnknownEventType(string eventType)
        {
            throw GetException();
            NotSupportedException GetException()
            {
                return new NotSupportedException("Unknown event type: " + eventType);
            }
        }

        #endregion

        #region -- NotImplementedException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowNotImplementedException_OnlySupportsSeekOriginBeginEnd()
        {
            throw GetException();
            NotImplementedException GetException()
            {
                return new NotImplementedException("only supports seek origin begin/end");
            }
        }

        #endregion

        #region -- IOException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowIOException_TableIndexIsAlreadyInitialized()
        {
            throw GetException();
            IOException GetException()
            {
                return new IOException("TableIndex is already initialized.");
            }
        }

        #endregion

        #region -- PackageFramingException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowPackageFramingException(int packageLength, int maxPackageSize)
        {
            throw GetException();
            PackageFramingException GetException()
            {
                return new PackageFramingException(string.Format("Package size is out of bounds: {0} (max: {1}).", packageLength, maxPackageSize));
            }
        }

        #endregion

        #region -- ApplicationException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowApplicationException_InfiniteWaitStop()
        {
            throw GetException();
            ApplicationException GetException()
            {
                return new ApplicationException("Infinite WaitStop() loop?");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowApplicationException_InfiniteWaitIdle()
        {
            throw GetException();
            ApplicationException GetException()
            {
                return new ApplicationException("Infinite WaitIdle() loop?");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowApplicationException_ClientRemovedWasCalledForClientTheConsumerStrategy()
        {
            throw GetException();
            ApplicationException GetException()
            {
                return new ApplicationException("ClientRemoved was called for a client the consumer strategy didn't have.");
            }
        }

        #endregion

        #region -- BadConfigDataException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowBadConfigDataException_DeserializedButNoVersionPresent()
        {
            throw GetException();
            BadConfigDataException GetException()
            {
                return new BadConfigDataException("Deserialized but no version present, invalid configuration data.", null);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowBadConfigDataException_TheConfigDataAppearsToBeInvalid(Exception ex)
        {
            throw GetException();
            BadConfigDataException GetException()
            {
                return new BadConfigDataException("The config data appears to be invalid", ex);
            }
        }

        #endregion

        #region -- FileBeingDeletedException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowFileBeingDeletedException()
        {
            throw GetException();
            FileBeingDeletedException GetException()
            {
                return new FileBeingDeletedException();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowFileBeingDeletedException_BeenToldTheFileWasDeletedGreaterThanMaxRetriesTimes()
        {
            throw GetException();
            FileBeingDeletedException GetException()
            {
                return new FileBeingDeletedException("Been told the file was deleted > MaxRetries times. Probably a problem in db.");
            }
        }

        #endregion

        #region -- HashValidationException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowHashValidationException()
        {
            throw GetException();
            HashValidationException GetException()
            {
                return new HashValidationException();
            }
        }

        #endregion

        #region -- InvalidReadException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidReadException_LogRecordAtActualPosHasNonPositiveLength(long actualPosition, long length, TFChunk chunk)
        {
            throw GetException();
            InvalidReadException GetException()
            {
                return new InvalidReadException(
                        string.Format("Log record at actual pos {0} has non-positive length: {1}. "
                                      + " in chunk {2}.", actualPosition, length, chunk));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidReadException_LogRecordAtActualPosHasTooLargeLength(long actualPosition, long length, TFChunk chunk)
        {
            throw GetException();
            InvalidReadException GetException()
            {
                return new InvalidReadException(
                        string.Format("Log record at actual pos {0} has too large length: {1} bytes, "
                                      + "while limit is {2} bytes. In chunk {3}.",
                                      actualPosition, length, TFConsts.MaxLogRecordSize, chunk));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidReadException_LogRecordThatEndsAtActualPosHasNonPositiveLength(long actualPosition, long length, TFChunk chunk)
        {
            throw GetException();
            InvalidReadException GetException()
            {
                return new InvalidReadException(
                        string.Format("Log record that ends at actual pos {0} has non-positive length: {1}. "
                                      + "In chunk {2}.",
                                      actualPosition, length, chunk));
            }
        }

        #endregion

        #region -- PossibleToHandleOutOfMemoryException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowPossibleToHandleOutOfMemoryException_FailedToAllocateMemoryForMidpointCache(OutOfMemoryException exc)
        {
            throw GetException();
            PossibleToHandleOutOfMemoryException GetException()
            {
                return new PossibleToHandleOutOfMemoryException("Failed to allocate memory for Midpoint cache.", exc);
            }
        }

        #endregion

        #region -- UnableToReadPastEndOfStreamException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowUnableToReadPastEndOfStreamException_ThereIsNotEnoughSpaceToReadFullRecordPrefix(long length, long actualPosition, TFChunk chunk)
        {
            throw GetException();
            UnableToReadPastEndOfStreamException GetException()
            {
                return new UnableToReadPastEndOfStreamException(
                        string.Format("There is not enough space to read full record (length prefix: {0}). "
                                      + "Actual pre-position: {1}. Something is seriously wrong in chunk {2}.",
                                      length, actualPosition, chunk));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowUnableToReadPastEndOfStreamException_ThereIsNotEnoughSpaceToReadFullRecordSuffix(long length, long actualPosition, TFChunk chunk)
        {
            throw GetException();
            UnableToReadPastEndOfStreamException GetException()
            {
                return new UnableToReadPastEndOfStreamException(
                        string.Format("There is not enough space to read full record (length suffix: {0}). "
                                      + "Actual post-position: {1}. Something is seriously wrong in chunk {2}.",
                                      length, actualPosition, chunk));
            }
        }

        #endregion

        #region -- UnableToAcquireLockInReasonableTimeException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowUnableToAcquireLockInReasonableTimeException()
        {
            throw GetException();
            UnableToAcquireLockInReasonableTimeException GetException()
            {
                return new UnableToAcquireLockInReasonableTimeException();
            }
        }

        #endregion

        #region -- MaybeCorruptIndexException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowMaybeCorruptIndexException_LowBounds(ulong stream, long version, ulong lowStream, long lowVersion)
        {
            throw GetException();
            MaybeCorruptIndexException GetException()
            {
                return new MaybeCorruptIndexException(String.Format("Midpoint key (stream: {0}, version: {1}) > low bounds check key (stream: {2}, version: {3})", stream, version, lowStream, lowVersion));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowMaybeCorruptIndexException_HighBounds(ulong stream, long version, ulong lowStream, long lowVersion)
        {
            throw GetException();
            MaybeCorruptIndexException GetException()
            {
                return new MaybeCorruptIndexException(String.Format("Midpoint key (stream: {0}, version: {1}) < high bounds check key (stream: {2}, version: {3})", stream, version, lowStream, lowVersion));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowMaybeCorruptIndexException_CandEntryGreater(ulong entryStream, long entryVersion, PTable.IndexEntryKey startKey, ulong stream, long startNumber, long endNumber, string filename)
        {
            throw GetException();
            MaybeCorruptIndexException GetException()
            {
                return new MaybeCorruptIndexException(string.Format("candEntry ({0}@{1}) > startKey {2}, stream {3}, startNum {4}, endNum {5}, PTable: {6}.", entryStream, entryVersion, startKey, stream, startNumber, endNumber, filename));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowMaybeCorruptIndexException_CandEntryLess(ulong entryStream, long entryVersion, PTable.IndexEntryKey startKey, ulong stream, long startNumber, long endNumber, string filename)
        {
            throw GetException();
            MaybeCorruptIndexException GetException()
            {
                return new MaybeCorruptIndexException(string.Format("candEntry ({0}@{1}) < startKey {2}, stream {3}, startNum {4}, endNum {5}, PTable: {6}.", entryStream, entryVersion, startKey, stream, startNumber, endNumber, filename));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowMaybeCorruptIndexException_Entry(ulong entryStream, long entryVersion, PTable.IndexEntryKey startKey, ulong stream, long startNumber, long endNumber, string filename)
        {
            throw GetException();
            MaybeCorruptIndexException GetException()
            {
                return new MaybeCorruptIndexException(string.Format("entry ({0}@{1}) > endKey {2}, stream {3}, startNum {4}, endNum {5}, PTable: {6}.", entryStream, entryVersion, startKey, stream, startNumber, endNumber, filename));
            }
        }

        #endregion

        #region -- CorruptDatabaseException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowCorruptDatabaseException_ChunkNotFound(string filename)
        {
            throw GetException();
            CorruptDatabaseException GetException()
            {
                return new CorruptDatabaseException(new ChunkNotFoundException(filename));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowCorruptDatabaseException_ChunkFileShouldBeCompleted(string filename)
        {
            throw GetException();
            CorruptDatabaseException GetException()
            {
                return new CorruptDatabaseException(new BadChunkInDatabaseException(
                        string.Format("Chunk file '{0}' should be completed, but is not.", filename)));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowCorruptDatabaseException_WrongFileVersion(string filename, byte version, byte currentChunkVersion)
        {
            throw GetException();
            CorruptDatabaseException GetException()
            {
                return new CorruptDatabaseException(new WrongFileVersionException(filename, version, currentChunkVersion));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowCorruptDatabaseException_ChunkFileShouldHaveFileSize1(string filename, int expectedFileSize, long len)
        {
            throw GetException();
            CorruptDatabaseException GetException()
            {
                return new CorruptDatabaseException(new BadChunkInDatabaseException(
                        string.Format("Chunk file '{0}' should have file size {1} bytes, but instead has {2} bytes length.", filename, expectedFileSize, len)));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowCorruptDatabaseException_ChunkFileShouldHaveFileSize(string filename, int expectedFileSize, long len)
        {
            throw GetException();
            CorruptDatabaseException GetException()
            {
                return new CorruptDatabaseException(new BadChunkInDatabaseException(
                        string.Format("Chunk file '{0}' should have file size {1} bytes, but instead has {2} bytes length.", filename, expectedFileSize, len)));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowCorruptDatabaseException_ChunkFileIsTooShortToEvenReadChunkHeader(string filename, long len)
        {
            throw GetException();
            CorruptDatabaseException GetException()
            {
                return new CorruptDatabaseException(new BadChunkInDatabaseException(
                    string.Format("Chunk file '{0}' is too short to even read ChunkHeader, its size is {1} bytes.", filename, len)));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowCorruptDatabaseException_ChunkFileIsTooShortToEvenReadChunkFooter(string filename, long len)
        {
            throw GetException();
            CorruptDatabaseException GetException()
            {
                return new CorruptDatabaseException(new BadChunkInDatabaseException(
                    string.Format("Chunk file '{0}' is too short to even read ChunkFooter, its size is {1} bytes.", filename, len)));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowCorruptDatabaseException_InvalidFile()
        {
            throw GetException();
            CorruptDatabaseException GetException()
            {
                return new CorruptDatabaseException(new InvalidFileException());
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowCorruptDatabaseException_ChunkNotFound(TFChunkDbConfig config, int lastChunkNum)
        {
            throw GetException();
            CorruptDatabaseException GetException()
            {
                return new CorruptDatabaseException(new ChunkNotFoundException(config.FileNamingStrategy.GetFilenameFor(lastChunkNum, 0)));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowCorruptDatabaseException_ChunkIsCorrupted(string chunkFileName, long chunkLocalPos, TFChunk lastChunk, long checkpoint)
        {
            throw GetException();
            CorruptDatabaseException GetException()
            {
                return new CorruptDatabaseException(new BadChunkInDatabaseException(
                            string.Format("Chunk {0} is corrupted. Expected local chunk position: {1}, "
                                          + "but Chunk.LogicalDataSize is {2} (Chunk.PhysicalDataSize is {3}). Writer checkpoint: {4}.",
                                chunkFileName, chunkLocalPos, lastChunk.LogicalDataSize, lastChunk.PhysicalDataSize, checkpoint)));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowCorruptDatabaseException_ValidateReaderChecksumsMustBeLess(string name)
        {
            throw GetException();
            CorruptDatabaseException GetException()
            {
                return new CorruptDatabaseException(new ReaderCheckpointHigherThanWriterException(name));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowCorruptDatabaseException_ChunkFileIsBad(string chunkFileName, long len)
        {
            throw GetException();
            CorruptDatabaseException GetException()
            {
                return new CorruptDatabaseException(new BadChunkInDatabaseException(
                        string.Format("Chunk file '{0}' is bad. It does not have enough size for header and footer. File size is {1} bytes.",
                            chunkFileName, len)));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowCorruptDatabaseException_UnexpectedFiles(string[] allFiles, List<string> allowedFiles)
        {
            throw GetException();
            CorruptDatabaseException GetException()
            {
                return new CorruptDatabaseException(new ExtraneousFileFoundException(
                    string.Format("Unexpected files: {0}.", string.Join(", ", allFiles.Except(allowedFiles)))));
            }
        }

        #endregion

        #region -- CorruptIndexException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowCorruptIndexException_CorruptedPTable()
        {
            throw GetException();
            CorruptIndexException GetException()
            {
                return new CorruptIndexException("Corrupted PTable.", new InvalidFileException("Wrong type of PTable."));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowCorruptIndexException_InternalIndexmapStructureCorruption()
        {
            throw GetException();
            CorruptIndexException GetException()
            {
                return new CorruptIndexException("Internal indexmap structure corruption.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowCorruptIndexException_CalculatedMD5HashIsNull()
        {
            throw GetException();
            CorruptIndexException GetException()
            {
                return new CorruptIndexException(new HashValidationException("Calculated MD5 hash is null!"));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowCorruptIndexException_CorruptedVersion()
        {
            throw GetException();
            CorruptIndexException GetException()
            {
                return new CorruptIndexException("Corrupted version.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowCorruptIndexException_CorruptedCommitCheckpoint()
        {
            throw GetException();
            CorruptIndexException GetException()
            {
                return new CorruptIndexException("Corrupted commit checkpoint.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowCorruptIndexException_ReadFromFileMD5HashIsNull()
        {
            throw GetException();
            CorruptIndexException GetException()
            {
                return new CorruptIndexException(new HashValidationException("Read from file MD5 hash is null!"));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowCorruptIndexException_IndexMapFileIsEmpty()
        {
            throw GetException();
            CorruptIndexException GetException()
            {
                return new CorruptIndexException("IndexMap file is empty.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowCorruptIndexException_CorruptedIndexMapMD5Hash(string text)
        {
            throw GetException();
            CorruptIndexException GetException()
            {
                return new CorruptIndexException(string.Format("Corrupted IndexMap MD5 hash. Hash ({0}): {1}.", text.Length, text));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowCorruptIndexException_HashValidationErrorDifferentHasheSizes(byte[] expectedHash, byte[] realHash)
        {
            throw GetException();
            CorruptIndexException GetException()
            {
                return new CorruptIndexException(
                        string.Format("Hash validation error (different hash sizes).\n"
                                      + "Expected hash ({0}): {1}, real hash ({2}): {3}.",
                                      expectedHash.Length, BitConverter.ToString(expectedHash),
                                      realHash.Length, BitConverter.ToString(realHash)));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowCorruptIndexException_HashValidationErrorDifferentHashes(byte[] expectedHash, byte[] realHash)
        {
            throw GetException();
            CorruptIndexException GetException()
            {
                return new CorruptIndexException(
                            string.Format("Hash validation error (different hashes).\n"
                                          + "Expected hash ({0}): {1}, real hash ({2}): {3}.",
                                          expectedHash.Length, BitConverter.ToString(expectedHash),
                                          realHash.Length, BitConverter.ToString(realHash)));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowCorruptIndexException_InvalidPrepareCheckpoint(string checkpoint)
        {
            throw GetException();
            CorruptIndexException GetException()
            {
                return new CorruptIndexException(string.Format("Invalid prepare checkpoint: {0}.", checkpoint));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowCorruptIndexException_InvalidCommitCheckpoint(string checkpoint)
        {
            throw GetException();
            CorruptIndexException GetException()
            {
                return new CorruptIndexException(string.Format("Invalid commit checkpoint: {0}.", checkpoint));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowCorruptIndexException_NegativePrepareCommitCheckpointInNonEmptyIndexMap(in TFPos checkpoints)
        {
            var msg = string.Format("Negative prepare/commit checkpoint in non-empty IndexMap: {0}.", checkpoints);
            throw GetException();
            CorruptIndexException GetException()
            {
                return new CorruptIndexException(msg);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowCorruptIndexException_IndexMapHasLowerMaximumAutoMergeLevelThanIsCurrentlyConfigured(int tmpMaxAutoMergeLevel, int maxAutoMergeLevel)
        {
            throw GetException();
            CorruptIndexException GetException()
            {
                return new CorruptIndexException($"Index map has lower maximum auto merge level ({tmpMaxAutoMergeLevel}) than is currently configured ({maxAutoMergeLevel}) and the index will need to be rebuilt");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowCorruptIndexException_IndexmapIsMissingContiguousLevelPosition(int i, int j)
        {
            throw GetException();
            CorruptIndexException GetException()
            {
                return new CorruptIndexException($"indexmap is missing contiguous level,position {i},{j}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowCorruptIndexException_CorruptedPrepareCommitCheckpointsPair(Exception exc)
        {
            throw GetException();
            CorruptIndexException GetException()
            {
                return new CorruptIndexException("Corrupted prepare/commit checkpoints pair.", exc);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowCorruptIndexException_ErrorWhileLoadingIndexMap(Exception exc)
        {
            throw GetException();
            CorruptIndexException GetException()
            {
                return new CorruptIndexException("Error while loading IndexMap.", exc);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowCorruptIndexException_PTableNotFound(string filename)
        {
            throw GetException();
            CorruptIndexException GetException()
            {
                return new CorruptIndexException(new PTableNotFoundException(filename));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowCorruptIndexException_WrongFileVersion(string fileName, byte headerVersion, byte version)
        {
            throw GetException();
            CorruptIndexException GetException()
            {
                return new CorruptIndexException(new WrongFileVersionException(fileName, headerVersion, version));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowCorruptIndexException_PTableHeaderAndFooterVersionMismatch(byte headerVersion, byte footerVersion)
        {
            throw GetException();
            CorruptIndexException GetException()
            {
                return new CorruptIndexException(String.Format("PTable header/footer version mismatch: {0}/{1}", headerVersion, footerVersion), new InvalidFileException("Invalid PTable file."));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowCorruptIndexException_TotalSizeOfIndexEntries(long indexEntriesTotalSize, long size, long midpointsCacheSize, byte version)
        {
            throw GetException();
            CorruptIndexException GetException()
            {
                return new CorruptIndexException(String.Format("Total size of index entries < 0: {0}. _size: {1}, header size: {2}, _midpointsCacheSize: {3}, footer size: {4}, md5 size: {5}", indexEntriesTotalSize, size, PTableHeader.Size, midpointsCacheSize, PTableFooter.GetSize(version), PTable.MD5Size));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowCorruptIndexException_TotalSizeOfIndexEntries(long indexEntriesTotalSize, int indexEntrySize)
        {
            throw GetException();
            CorruptIndexException GetException()
            {
                return new CorruptIndexException(String.Format("Total size of index entries: {0} is not divisible by index entry size: {1}", indexEntriesTotalSize, indexEntrySize));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowCorruptIndexException_LessThan2MidpointsCachedInPTable(long count, uint midpointsCached)
        {
            throw GetException();
            CorruptIndexException GetException()
            {
                return new CorruptIndexException(String.Format("Less than 2 midpoints cached in PTable. Index entries: {0}, Midpoints cached: {1}", count, midpointsCached));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowCorruptIndexException_MoreMidpointsCachedInPTableThanIndexEntries(uint midpointsCached, long count)
        {
            throw GetException();
            CorruptIndexException GetException()
            {
                return new CorruptIndexException(String.Format("More midpoints cached in PTable than index entries. Midpoints: {0} , Index entries: {1}", midpointsCached, count));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowCorruptIndexException_ItemIndexForMidpointLessThan(long k, EventStore.Core.Index.PTable.Midpoint[] midpoints)
        {
            throw GetException();
            CorruptIndexException GetException()
            {
                return new CorruptIndexException(String.Format("Index entry key for midpoint {0} (stream: {1}, version: {2}) < index entry key for midpoint {3} (stream: {4}, version: {5})", k - 1, midpoints[k - 1].Key.Stream, midpoints[k - 1].Key.Version, k, midpoints[k].Key.Stream, midpoints[k].Key.Version));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowCorruptIndexException_ItemIndexForMidpointGreaterThan(long k, EventStore.Core.Index.PTable.Midpoint[] midpoints)
        {
            throw GetException();
            CorruptIndexException GetException()
            {
                return new CorruptIndexException(String.Format("Item index for midpoint {0} ({1}) > Item index for midpoint {2} ({3})", k - 1, midpoints[k - 1].ItemIndex, k, midpoints[k].ItemIndex));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowCorruptIndexException_HashSizesDiffer(byte[] fromFile, byte[] computed)
        {
            throw GetException();
            CorruptIndexException GetException()
            {
                return new CorruptIndexException(
                    new HashValidationException(
                        string.Format(
                           "Hash sizes differ! FileHash({0}): {1}, hash({2}): {3}.",
                           computed.Length,
                           BitConverter.ToString(computed),
                           fromFile.Length,
                           BitConverter.ToString(fromFile))));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowCorruptIndexException_HashesAreDifferent(byte[] fromFile, byte[] computed)
        {
            throw GetException();
            CorruptIndexException GetException()
            {
                return new CorruptIndexException(
                        new HashValidationException(
                            string.Format(
                                "Hashes are different! computed: {0}, hash: {1}.",
                                BitConverter.ToString(computed),
                                BitConverter.ToString(fromFile))));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowCorruptIndexException_PTableFooterWithVersionLess4Found()
        {
            throw GetException();
            CorruptIndexException GetException()
            {
                return new CorruptIndexException("PTable footer with version < 4 found. PTable footers are supported as from version 4.", new InvalidFileException("Invalid PTable file."));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowCorruptIndexException_CouldntReadVersionOfPTableFromFooter()
        {
            throw GetException();
            CorruptIndexException GetException()
            {
                return new CorruptIndexException("Couldn't read version of PTable from footer.", new InvalidFileException("Invalid PTable file."));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowCorruptIndexException_CouldntReadVersionOfPTableFromHeader()
        {
            throw GetException();
            CorruptIndexException GetException()
            {
                return new CorruptIndexException("Couldn't read version of PTable from header.", new InvalidFileException("Invalid PTable file."));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowCorruptIndexException_CommitCheckpointIsGreaterThanChaserCheckpoint(long commitCheckpoint, long chaserCheckpoint)
        {
            throw GetException();
            CorruptIndexException GetException()
            {
                return new CorruptIndexException(String.Format("IndexMap's CommitCheckpoint ({0}) is greater than ChaserCheckpoint ({1}).", commitCheckpoint, chaserCheckpoint));
            }
        }

        #endregion

        #region -- DirectoryNotFoundException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowDirectoryNotFoundException(string sourceDirName)
        {
            throw GetException();
            DirectoryNotFoundException GetException()
            {
                return new DirectoryNotFoundException("Source directory does not exist or could not be found: " + sourceDirName);
            }
        }

        #endregion
    }
}
