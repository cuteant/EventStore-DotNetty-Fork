using System;
using System.Threading.Tasks;
using CuteAnt.AsyncEx;
using EventStore.ClientAPI.SystemData;

namespace EventStore.ClientAPI
{
    partial class IEventStoreConnectionExtensions
    {
        #region -- SetStreamMetadata --

        /// <summary>Sets the metadata for a stream.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="stream">The name of the stream for which to set metadata.</param>
        /// <param name="expectedMetastreamVersion">The expected version for the write to the metadata stream.</param>
        /// <param name="metadata">A <see cref="StreamMetadata"/> representing the new metadata.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <returns>A <see cref="WriteResult"/> containing the results of the write operation.</returns>
        public static WriteResult SetStreamMetadata(this IEventStoreConnectionBase connection,
            string stream, long expectedMetastreamVersion, StreamMetadata metadata, UserCredentials userCredentials = null)
        {
            if (connection is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }

            return AsyncContext.Run(
                async (conn, streamId, expectedVersion, meta, credentials)
                    => await conn.SetStreamMetadataAsync(streamId, expectedVersion, meta, credentials).ConfigureAwait(false),
                connection, stream, expectedMetastreamVersion, metadata, userCredentials);
        }

        /// <summary>Sets the metadata for a stream.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="stream">The name of the stream for which to set metadata.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="expectedMetastreamVersion">The expected version for the write to the metadata stream.</param>
        /// <param name="metadata">A <see cref="StreamMetadata"/> representing the new metadata.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <returns>A <see cref="WriteResult"/> containing the results of the write operation.</returns>
        public static WriteResult SetStreamMetadata(this IEventStoreConnectionBase connection,
            string stream, string topic, long expectedMetastreamVersion, StreamMetadata metadata, UserCredentials userCredentials = null)
        {
            if (connection is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
            if (string.IsNullOrEmpty(stream)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stream); }
            if (string.IsNullOrEmpty(topic)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.topic); }

            return AsyncContext.Run(
                async (conn, streamId, expectedVersion, meta, credentials)
                    => await conn.SetStreamMetadataAsync(streamId, expectedVersion, meta, credentials).ConfigureAwait(false),
                connection, stream.Combine(topic), expectedMetastreamVersion, metadata, userCredentials);
        }

        /// <summary>Sets the metadata for a stream.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="actualType">The actual type.</param>
        /// <param name="expectedMetastreamVersion">The expected version for the write to the metadata stream.</param>
        /// <param name="metadata">A <see cref="StreamMetadata"/> representing the new metadata.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <returns>A <see cref="WriteResult"/> containing the results of the write operation.</returns>
        public static WriteResult SetStreamMetadata(this IEventStoreConnectionBase connection,
            Type actualType, long expectedMetastreamVersion, StreamMetadata metadata, UserCredentials userCredentials = null)
        {
            return AsyncContext.Run(
                async (conn, aType, expectedVersion, meta, credentials)
                    => await conn.SetStreamMetadataAsync(aType, expectedVersion, meta, credentials).ConfigureAwait(false),
                connection, actualType, expectedMetastreamVersion, metadata, userCredentials);
        }

        /// <summary>Sets the metadata for a stream.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="actualType">The actual type.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="expectedMetastreamVersion">The expected version for the write to the metadata stream.</param>
        /// <param name="metadata">A <see cref="StreamMetadata"/> representing the new metadata.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <returns>A <see cref="WriteResult"/> containing the results of the write operation.</returns>
        public static WriteResult SetStreamMetadata(this IEventStoreConnectionBase connection,
            Type actualType, string topic, long expectedMetastreamVersion, StreamMetadata metadata, UserCredentials userCredentials = null)
        {
            return AsyncContext.Run(
                async (conn, typeWrapper, expectedVersion, meta, credentials)
                    => await conn.SetStreamMetadataAsync(typeWrapper.Item1, typeWrapper.Item2, expectedVersion, meta, credentials).ConfigureAwait(false),
                connection, Tuple.Create(actualType, topic), expectedMetastreamVersion, metadata, userCredentials);
        }

        #endregion

        #region -- SetStreamMetadata<TEvent> --

        /// <summary>Sets the metadata for a stream.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="expectedMetastreamVersion">The expected version for the write to the metadata stream.</param>
        /// <param name="metadata">A <see cref="StreamMetadata"/> representing the new metadata.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <returns>A <see cref="WriteResult"/> containing the results of the write operation.</returns>
        public static WriteResult SetStreamMetadata<TEvent>(this IEventStoreConnectionBase connection,
            long expectedMetastreamVersion, StreamMetadata metadata, UserCredentials userCredentials = null)
        {
            return AsyncContext.Run(
                async (conn, expectedVersion, meta, credentials)
                    => await conn.SetStreamMetadataAsync<TEvent>(expectedVersion, meta, credentials).ConfigureAwait(false),
                connection, expectedMetastreamVersion, metadata, userCredentials);
        }

        /// <summary>Sets the metadata for a stream.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="expectedMetastreamVersion">The expected version for the write to the metadata stream.</param>
        /// <param name="metadata">A <see cref="StreamMetadata"/> representing the new metadata.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <returns>A <see cref="WriteResult"/> containing the results of the write operation.</returns>
        public static WriteResult SetStreamMetadata<TEvent>(this IEventStoreConnectionBase connection,
            string topic, long expectedMetastreamVersion, StreamMetadata metadata, UserCredentials userCredentials = null)
        {
            return AsyncContext.Run(
                async (conn, innerTopic, expectedVersion, meta, credentials)
                    => await conn.SetStreamMetadataAsync<TEvent>(innerTopic, expectedVersion, meta, credentials).ConfigureAwait(false),
                connection, topic, expectedMetastreamVersion, metadata, userCredentials);
        }

        #endregion

        #region -- GetStreamMetadata --

        /// <summary>Reads the metadata for a stream and converts the metadata into a <see cref="StreamMetadata"/>.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="stream">The name of the stream for which to read metadata.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <returns>A <see cref="StreamMetadataResult"/> representing system and user-specified metadata as properties.</returns>
        public static StreamMetadataResult GetStreamMetadata(this IEventStoreConnectionBase connection,
            string stream, UserCredentials userCredentials = null)
        {
            if (connection is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }

            return AsyncContext.Run(
                async (conn, streamId, credentials)
                    => await conn.GetStreamMetadataAsync(streamId, credentials).ConfigureAwait(false),
                connection, stream, userCredentials);
        }

        /// <summary>Reads the metadata for a stream and converts the metadata into a <see cref="StreamMetadata"/>.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="stream">The name of the stream for which to read metadata.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <returns>A <see cref="StreamMetadataResult"/> representing system and user-specified metadata as properties.</returns>
        public static StreamMetadataResult GetStreamMetadata(this IEventStoreConnectionBase connection,
            string stream, string topic, UserCredentials userCredentials = null)
        {
            if (connection is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }

            return AsyncContext.Run(
                async (conn, streamId, innerTopic, credentials)
                    => await conn.GetStreamMetadataAsync(streamId, innerTopic, credentials).ConfigureAwait(false),
                connection, stream, topic, userCredentials);
        }

        /// <summary>Reads the metadata for a stream and converts the metadata into a <see cref="StreamMetadata"/>.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="actualType">The actual type.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <returns>A <see cref="StreamMetadataResult"/> representing system and user-specified metadata as properties.</returns>
        public static StreamMetadataResult GetStreamMetadata(this IEventStoreConnectionBase connection,
            Type actualType, UserCredentials userCredentials = null)
        {
            return AsyncContext.Run(
                async (conn, atype, credentials)
                    => await conn.GetStreamMetadataAsync(atype, credentials).ConfigureAwait(false),
                connection, actualType, userCredentials);
        }

        /// <summary>Reads the metadata for a stream and converts the metadata into a <see cref="StreamMetadata"/>.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="actualType">The actual type.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <returns>A <see cref="StreamMetadataResult"/> representing system and user-specified metadata as properties.</returns>
        public static StreamMetadataResult GetStreamMetadata(this IEventStoreConnectionBase connection,
            Type actualType, string topic, UserCredentials userCredentials = null)
        {
            return AsyncContext.Run(
                async (conn, atype, innerTopic, credentials)
                    => await conn.GetStreamMetadataAsync(atype, innerTopic, credentials).ConfigureAwait(false),
                connection, actualType, topic, userCredentials);
        }

        #endregion

        #region -- GetStreamMetadata<TEvent> --

        /// <summary>Reads the metadata for a stream and converts the metadata into a <see cref="StreamMetadata"/>.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <returns>A <see cref="StreamMetadataResult"/> representing system and user-specified metadata as properties.</returns>
        public static StreamMetadataResult GetStreamMetadata<TEvent>(this IEventStoreConnectionBase connection, UserCredentials userCredentials = null)
        {
            return AsyncContext.Run(
                async (conn, credentials)
                    => await conn.GetStreamMetadataAsync<TEvent>(credentials).ConfigureAwait(false),
                connection, userCredentials);
        }

        /// <summary>Reads the metadata for a stream and converts the metadata into a <see cref="StreamMetadata"/>.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <returns>A <see cref="StreamMetadataResult"/> representing system and user-specified metadata as properties.</returns>
        public static StreamMetadataResult GetStreamMetadata<TEvent>(this IEventStoreConnectionBase connection,
          string topic, UserCredentials userCredentials = null)
        {
            return AsyncContext.Run(
                async (conn, innerTopic, credentials)
                    => await conn.GetStreamMetadataAsync<TEvent>(innerTopic, credentials).ConfigureAwait(false),
                connection, topic, userCredentials);
        }

        #endregion


        #region -- Get(set)StreamMetadataAsRawBytes --

        /// <summary>Sets the metadata for a stream.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="stream">The name of the stream for which to set metadata.</param>
        /// <param name="expectedMetastreamVersion">The expected version for the write to the metadata stream.</param>
        /// <param name="metadata">A byte array representing the new metadata.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <returns>A <see cref="WriteResult"/> containing the results of the write operation.</returns>
        public static WriteResult SetStreamMetadata(this IEventStoreConnectionBase connection,
            string stream, long expectedMetastreamVersion, byte[] metadata, UserCredentials userCredentials = null)
        {
            if (connection is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }

            return AsyncContext.Run(
                async (conn, streamId, expectedVersion, meta, credentials)
                    => await conn.SetStreamMetadataAsync(streamId, expectedVersion, meta, credentials).ConfigureAwait(false),
                connection, stream, expectedMetastreamVersion, metadata, userCredentials);
        }

        /// <summary>Synchronously reads the metadata for a stream as a byte array.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="stream">The name of the stream for which to read metadata.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <returns>A <see cref="RawStreamMetadataResult"/> representing system metadata as properties and user-specified metadata as bytes.</returns>
        public static RawStreamMetadataResult GetStreamMetadataAsRawBytes(this IEventStoreConnectionBase connection,
          string stream, UserCredentials userCredentials = null)
        {
            if (connection is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }

            return AsyncContext.Run(
                async (conn, streamId, credentials)
                    => await conn.GetStreamMetadataAsRawBytesAsync(streamId, credentials).ConfigureAwait(false),
                connection, stream, userCredentials);
        }

        #endregion


        #region -- SetStreamMetadataAsync --

        /// <summary>Asynchronously sets the metadata for a stream.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="actualType">The actual type.</param>
        /// <param name="expectedMetastreamVersion">The expected version for the write to the metadata stream.</param>
        /// <param name="metadata">A <see cref="StreamMetadata"/> representing the new metadata.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <returns>A <see cref="Task&lt;WriteResult&gt;"/> containing the results of the write operation.</returns>
        public static Task<WriteResult> SetStreamMetadataAsync(this IEventStoreConnectionBase connection,
            Type actualType, long expectedMetastreamVersion, StreamMetadata metadata, UserCredentials userCredentials = null)
        {
            if (connection is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
            if (actualType is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.actualType); }
            return connection.SetStreamMetadataAsync(actualType.GetStreamId(), expectedMetastreamVersion, metadata, userCredentials);
        }

        /// <summary>Asynchronously sets the metadata for a stream.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="actualType">The actual type.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="expectedMetastreamVersion">The expected version for the write to the metadata stream.</param>
        /// <param name="metadata">A <see cref="StreamMetadata"/> representing the new metadata.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <returns>A <see cref="Task&lt;WriteResult&gt;"/> containing the results of the write operation.</returns>
        public static Task<WriteResult> SetStreamMetadataAsync(this IEventStoreConnectionBase connection,
            Type actualType, string topic, long expectedMetastreamVersion, StreamMetadata metadata, UserCredentials userCredentials = null)
        {
            if (connection is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
            if (actualType is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.actualType); }
            if (string.IsNullOrEmpty(topic)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.topic); }
            return connection.SetStreamMetadataAsync(actualType.GetStreamId(topic), expectedMetastreamVersion, metadata, userCredentials);
        }

        #endregion

        #region -- SetStreamMetadataAsync<TEvent> --

        /// <summary>Asynchronously sets the metadata for a stream.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="expectedMetastreamVersion">The expected version for the write to the metadata stream.</param>
        /// <param name="metadata">A <see cref="StreamMetadata"/> representing the new metadata.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <returns>A <see cref="Task&lt;WriteResult&gt;"/> containing the results of the write operation.</returns>
        public static Task<WriteResult> SetStreamMetadataAsync<TEvent>(this IEventStoreConnectionBase connection,
            long expectedMetastreamVersion, StreamMetadata metadata, UserCredentials userCredentials = null)
        {
            if (connection is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
            return connection.SetStreamMetadataAsync(EventManager.GetStreamId<TEvent>(), expectedMetastreamVersion, metadata, userCredentials);
        }

        /// <summary>Asynchronously sets the metadata for a stream.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="expectedMetastreamVersion">The expected version for the write to the metadata stream.</param>
        /// <param name="metadata">A <see cref="StreamMetadata"/> representing the new metadata.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <returns>A <see cref="Task&lt;WriteResult&gt;"/> containing the results of the write operation.</returns>
        public static Task<WriteResult> SetStreamMetadataAsync<TEvent>(this IEventStoreConnectionBase connection,
            string topic, long expectedMetastreamVersion, StreamMetadata metadata, UserCredentials userCredentials = null)
        {
            if (connection is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
            if (string.IsNullOrEmpty(topic)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.topic); }
            return connection.SetStreamMetadataAsync(EventManager.GetStreamId<TEvent>(topic), expectedMetastreamVersion, metadata, userCredentials);
        }

        #endregion

        #region -- GetStreamMetadataAsync --

        /// <summary>Asynchronously reads the metadata for a stream and converts the metadata into a <see cref="StreamMetadata"/>.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="stream">The name of the stream for which to read metadata.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <returns>A <see cref="Task&lt;StreamMetadataResult&gt;"/> representing system and user-specified metadata as properties.</returns>
        public static Task<StreamMetadataResult> GetStreamMetadataAsync(this IEventStoreConnectionBase connection,
            string stream, string topic, UserCredentials userCredentials = null)
        {
            if (connection is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
            if (string.IsNullOrEmpty(stream)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stream); }
            if (string.IsNullOrEmpty(topic)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.topic); }
            return connection.GetStreamMetadataAsync(stream.Combine(topic), userCredentials);
        }

        /// <summary>Asynchronously reads the metadata for a stream and converts the metadata into a <see cref="StreamMetadata"/>.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="actualType">The actual type.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <returns>A <see cref="Task&lt;StreamMetadataResult&gt;"/> representing system and user-specified metadata as properties.</returns>
        public static Task<StreamMetadataResult> GetStreamMetadataAsync(this IEventStoreConnectionBase connection,
            Type actualType, UserCredentials userCredentials = null)
        {
            if (connection is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
            if (actualType is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.actualType); }
            return connection.GetStreamMetadataAsync(actualType.GetStreamId(), userCredentials);
        }

        /// <summary>Asynchronously reads the metadata for a stream and converts the metadata into a <see cref="StreamMetadata"/>.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="actualType">The actual type.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <returns>A <see cref="Task&lt;StreamMetadataResult&gt;"/> representing system and user-specified metadata as properties.</returns>
        public static Task<StreamMetadataResult> GetStreamMetadataAsync(this IEventStoreConnectionBase connection,
            Type actualType, string topic, UserCredentials userCredentials = null)
        {
            if (connection is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
            if (actualType is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.actualType); }
            if (string.IsNullOrEmpty(topic)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.topic); }
            return connection.GetStreamMetadataAsync(actualType.GetStreamId(topic), userCredentials);
        }

        #endregion

        #region -- GetStreamMetadataAsync<TEvent> --

        /// <summary>Asynchronously reads the metadata for a stream and converts the metadata into a <see cref="StreamMetadata"/>.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <returns>A <see cref="Task&lt;StreamMetadataResult&gt;"/> representing system and user-specified metadata as properties.</returns>
        public static Task<StreamMetadataResult> GetStreamMetadataAsync<TEvent>(this IEventStoreConnectionBase connection, UserCredentials userCredentials = null)
        {
            if (connection is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
            return connection.GetStreamMetadataAsync(EventManager.GetStreamId<TEvent>(), userCredentials);
        }

        /// <summary>Asynchronously reads the metadata for a stream and converts the metadata into a <see cref="StreamMetadata"/>.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <returns>A <see cref="Task&lt;StreamMetadataResult&gt;"/> representing system and user-specified metadata as properties.</returns>
        public static Task<StreamMetadataResult> GetStreamMetadataAsync<TEvent>(this IEventStoreConnectionBase connection,
            string topic, UserCredentials userCredentials = null)
        {
            if (connection is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
            if (string.IsNullOrEmpty(topic)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.topic); }
            return connection.GetStreamMetadataAsync(EventManager.GetStreamId<TEvent>(topic), userCredentials);
        }

        #endregion
    }
}
