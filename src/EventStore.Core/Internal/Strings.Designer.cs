﻿//------------------------------------------------------------------------------
// <auto-generated>
//     此代码由工具生成。
//     运行时版本:4.0.30319.42000
//
//     对此文件的更改可能会导致不正确的行为，并且如果
//     重新生成代码，这些更改将会丢失。
// </auto-generated>
//------------------------------------------------------------------------------

namespace EventStore.Core.Internal {
    using System;
    
    
    /// <summary>
    ///   一个强类型的资源类，用于查找本地化的字符串等。
    /// </summary>
    // 此类是由 StronglyTypedResourceBuilder
    // 类通过类似于 ResGen 或 Visual Studio 的工具自动生成的。
    // 若要添加或移除成员，请编辑 .ResX 文件，然后重新运行 ResGen
    // (以 /str 作为命令选项)，或重新生成 VS 项目。
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "16.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Strings {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Strings() {
        }
        
        /// <summary>
        ///   返回此类使用的缓存的 ResourceManager 实例。
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("EventStore.Core.Internal.Strings", typeof(Strings).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   重写当前线程的 CurrentUICulture 属性
        ///   重写当前线程的 CurrentUICulture 属性。
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   查找类似 BulkReader is null for subscription. 的本地化字符串。
        /// </summary>
        internal static string BulkReader_is_null_for_subscription {
            get {
                return ResourceManager.GetString("BulkReader_is_null_for_subscription", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 Concurrency error in ReadIndex.Commit: _lastCommitPosition was modified during Commit execution! 的本地化字符串。
        /// </summary>
        internal static string Concurrency_Error_In_ReadIndex_Commit {
            get {
                return ResourceManager.GetString("Concurrency_Error_In_ReadIndex_Commit", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 _connection == null 的本地化字符串。
        /// </summary>
        internal static string ConnectionIsNull {
            get {
                return ResourceManager.GetString("ConnectionIsNull", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 Could not append raw bytes to chunk {0}-{1}, raw pos: {2} (0x{2:X}), bytes length: {3} (0x{3:X}). Chunk file size: {4} (0x{4:X}). 的本地化字符串。
        /// </summary>
        internal static string Could_not_append_raw_bytes_to_chunk {
            get {
                return ResourceManager.GetString("Could_not_append_raw_bytes_to_chunk", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 Could not begin restart session.  Unable to determine file locker. 的本地化字符串。
        /// </summary>
        internal static string Could_not_begin_restart_session {
            get {
                return ResourceManager.GetString("Could_not_begin_restart_session", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 Could not finish background thread in reasonable time. 的本地化字符串。
        /// </summary>
        internal static string Could_not_finish_background_thread_in_reasonable_time {
            get {
                return ResourceManager.GetString("Could_not_finish_background_thread_in_reasonable_time", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 Could not list processes locking resource. 的本地化字符串。
        /// </summary>
        internal static string Could_not_list_processes_locking_resource {
            get {
                return ResourceManager.GetString("Could_not_list_processes_locking_resource", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 Could not list processes locking resource. Failed to get size of result. 的本地化字符串。
        /// </summary>
        internal static string Could_not_list_processes_locking_resource_F {
            get {
                return ResourceManager.GetString("Could_not_list_processes_locking_resource_F", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 Could not read latest stream&apos;s prepare. That should never happen. 的本地化字符串。
        /// </summary>
        internal static string Could_not_read_latest_stream_prepare {
            get {
                return ResourceManager.GetString("Could_not_read_latest_stream_prepare", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 Could not register resource. 的本地化字符串。
        /// </summary>
        internal static string Could_not_register_resource {
            get {
                return ResourceManager.GetString("Could_not_register_resource", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 Count of file streams reduced below zero. 的本地化字符串。
        /// </summary>
        internal static string Count_of_file_streams_reduced_below_zero {
            get {
                return ResourceManager.GetString("Count_of_file_streams_reduced_below_zero", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 Count of memory streams reduced below zero. 的本地化字符串。
        /// </summary>
        internal static string Count_of_memory_streams_reduced_below_zero {
            get {
                return ResourceManager.GetString("Count_of_memory_streams_reduced_below_zero", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 Data chunk bulk received, but we have active chunk for receiving raw chunk bulks. 的本地化字符串。
        /// </summary>
        internal static string Data_chunk_bulk_received_but {
            get {
                return ResourceManager.GetString("Data_chunk_bulk_received_but", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 Duplicate route. 的本地化字符串。
        /// </summary>
        internal static string Duplicate_route {
            get {
                return ResourceManager.GetString("Duplicate_route", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 Empty eventId provided. 的本地化字符串。
        /// </summary>
        internal static string Empty_eventId_provided {
            get {
                return ResourceManager.GetString("Empty_eventId_provided", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 Empty eventType provided. 的本地化字符串。
        /// </summary>
        internal static string Empty_eventType_provided {
            get {
                return ResourceManager.GetString("Empty_eventType_provided", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 First write failed when writing replicated record: {0}. 的本地化字符串。
        /// </summary>
        internal static string First_write_failed_when_writing_replicated_record {
            get {
                return ResourceManager.GetString("First_write_failed_when_writing_replicated_record", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 Incorrect Message Type IDs setup. 的本地化字符串。
        /// </summary>
        internal static string Incorrect_Message_Type_IDs_setup {
            get {
                return ResourceManager.GetString("Incorrect_Message_Type_IDs_setup", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 Invalid constructor used for successful write. 的本地化字符串。
        /// </summary>
        internal static string Invalid_constructor_used_for_successful_write {
            get {
                return ResourceManager.GetString("Invalid_constructor_used_for_successful_write", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 IReplicationMessage with empty SubscriptionId provided. 的本地化字符串。
        /// </summary>
        internal static string IReplicationMessage_with_empty_SubscriptionId_provided {
            get {
                return ResourceManager.GetString("IReplicationMessage_with_empty_SubscriptionId_provided", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 Master [{0},{1:B}] subscribed us at {2} (0x{2:X}), which is greater than our writer checkpoint {3} (0x{3:X}). REPLICATION BUG. 的本地化字符串。
        /// </summary>
        internal static string Master_subscribed_which_is_greater {
            get {
                return ResourceManager.GetString("Master_subscribed_which_is_greater", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 MemStream readers are in use when writing scavenged chunk. 的本地化字符串。
        /// </summary>
        internal static string MemStream_readers_are_in_use_when_writing_scavenged_chunk {
            get {
                return ResourceManager.GetString("MemStream_readers_are_in_use_when_writing_scavenged_chunk", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 Neither eventStreamId nor transactionId is specified. 的本地化字符串。
        /// </summary>
        internal static string Neither_eventStreamId_nor_transactionId {
            get {
                return ResourceManager.GetString("Neither_eventStreamId_nor_transactionId", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 New position is less than old position. 的本地化字符串。
        /// </summary>
        internal static string NewPositionIsLessThanOldPosition {
            get {
                return ResourceManager.GetString("NewPositionIsLessThanOldPosition", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 No chunks in DB. 的本地化字符串。
        /// </summary>
        internal static string No_chunks_in_DB {
            get {
                return ResourceManager.GetString("No_chunks_in_DB", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 No transaction ID specified. 的本地化字符串。
        /// </summary>
        internal static string No_transaction_ID_specified {
            get {
                return ResourceManager.GetString("No_transaction_ID_specified", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 Not enough memory streams during in-mem TFChunk mode. 的本地化字符串。
        /// </summary>
        internal static string Not_enough_memory_streams_during_inMem_TFChunk_mode {
            get {
                return ResourceManager.GetString("Not_enough_memory_streams_during_inMem_TFChunk_mode", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 offset + count must be less than size of array 的本地化字符串。
        /// </summary>
        internal static string offset_count_must_be_less_than_size_of_array {
            get {
                return ResourceManager.GetString("offset_count_must_be_less_than_size_of_array", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 Physical chunk bulk received, but we do not have active chunk. 的本地化字符串。
        /// </summary>
        internal static string Physical_chunk_bulk_received_but {
            get {
                return ResourceManager.GetString("Physical_chunk_bulk_received_but", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 Provided list of chunks to merge is empty. 的本地化字符串。
        /// </summary>
        internal static string Provided_list_of_chunks_to_merge_is_empty {
            get {
                return ResourceManager.GetString("Provided_list_of_chunks_to_merge_is_empty", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 Received request to create a new ongoing chunk #{0}-{1}, but current chunks count is {2}. 的本地化字符串。
        /// </summary>
        internal static string Received_request_to_create_chunk {
            get {
                return ResourceManager.GetString("Received_request_to_create_chunk", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 Record is too big 的本地化字符串。
        /// </summary>
        internal static string Record_is_too_big {
            get {
                return ResourceManager.GetString("Record_is_too_big", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 Record too large. 的本地化字符串。
        /// </summary>
        internal static string Record_too_large {
            get {
                return ResourceManager.GetString("Record_too_large", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 Scavenged TFChunk passed into unscavenged chunk read side. 的本地化字符串。
        /// </summary>
        internal static string Scavenged_TFChunk_passed_into_unscavenged_chunk_read_side {
            get {
                return ResourceManager.GetString("Scavenged_TFChunk_passed_into_unscavenged_chunk_read_side", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 SCAVENGING: no chunks to merge, unexpectedly... 的本地化字符串。
        /// </summary>
        internal static string SCAVENGING_no_chunks_to_merge {
            get {
                return ResourceManager.GetString("SCAVENGING_no_chunks_to_merge", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 !_state.IsReplica() 的本地化字符串。
        /// </summary>
        internal static string StateIsNotReplica {
            get {
                return ResourceManager.GetString("StateIsNotReplica", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 There is some data left in framer when completing chunk. 的本地化字符串。
        /// </summary>
        internal static string There_is_some_data_left_in_framer_when_completing_chunk {
            get {
                return ResourceManager.GetString("There_is_some_data_left_in_framer_when_completing_chunk", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 Too many retrials to acquire reader for subscriber. 的本地化字符串。
        /// </summary>
        internal static string Too_many_retrials_to_acquire_reader {
            get {
                return ResourceManager.GetString("Too_many_retrials_to_acquire_reader", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 TransactionId was not set, transactionId = -1. 的本地化字符串。
        /// </summary>
        internal static string TransactionId_was_not_set {
            get {
                return ResourceManager.GetString("TransactionId_was_not_set", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 Unable to acquire reader work item. Max internal streams limit reached. 的本地化字符串。
        /// </summary>
        internal static string Unable_to_acquire_reader_work_item {
            get {
                return ResourceManager.GetString("Unable_to_acquire_reader_work_item", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 Unable to acquire work item. 的本地化字符串。
        /// </summary>
        internal static string Unable_to_acquire_work_item {
            get {
                return ResourceManager.GetString("Unable_to_acquire_work_item", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 Unable to find table in map. 的本地化字符串。
        /// </summary>
        internal static string Unable_to_find_table_in_map {
            get {
                return ResourceManager.GetString("Unable_to_find_table_in_map", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 Waiting for background tasks took too long. 的本地化字符串。
        /// </summary>
        internal static string Waiting_for_background_tasks_took_too_long {
            get {
                return ResourceManager.GetString("Waiting_for_background_tasks_took_too_long", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 We should not BecomeMaster twice in a row. 的本地化字符串。
        /// </summary>
        internal static string We_should_not_BecomeMaster_twice_in_a_row {
            get {
                return ResourceManager.GetString("We_should_not_BecomeMaster_twice_in_a_row", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 You can only run unbuffered mode on v3 or higher chunk files. Please run scavenge on your database to upgrade your transaction file to v3. 的本地化字符串。
        /// </summary>
        internal static string YouCanOnlyRunUnbufferedModeOnV3OrHigherChunkFiles {
            get {
                return ResourceManager.GetString("YouCanOnlyRunUnbufferedModeOnV3OrHigherChunkFiles", resourceCulture);
            }
        }
    }
}
