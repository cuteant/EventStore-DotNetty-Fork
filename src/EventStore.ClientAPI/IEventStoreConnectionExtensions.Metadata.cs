using System;
using System.Threading.Tasks;
using CuteAnt.AsyncEx;
using EventStore.ClientAPI.Serialization;
using EventStore.ClientAPI.SystemData;

namespace EventStore.ClientAPI
{
  partial class IEventStoreConnectionExtensions
  {
    #region -- SetStreamMetadata --

    /// <summary>Sets the metadata for a stream.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="stream">The name of the stream for which to set metadata.</param>
    /// <param name="topic">The topic</param>
    /// <param name="expectedMetastreamVersion">The expected version for the write to the metadata stream.</param>
    /// <param name="metadata">A <see cref="StreamMetadata"/> representing the new metadata.</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <returns>A <see cref="WriteResult"/> containing the results of the write operation.</returns>
    public static WriteResult SetStreamMetadata(this IEventStoreConnectionBase connection,
      string stream, string topic, long expectedMetastreamVersion, StreamMetadata metadata, UserCredentials userCredentials = null)
    {
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }
      if (string.IsNullOrEmpty(stream)) { throw new ArgumentNullException(nameof(stream)); }
      if (string.IsNullOrEmpty(topic)) { throw new ArgumentNullException(nameof(topic)); }

      return AsyncContext.Run(
                async (conn, streamId, expectedVersion, meta, credentials)
                  => await conn.SetStreamMetadataAsync(streamId, expectedVersion, meta, credentials).ConfigureAwait(false),
                connection, CombineStreamId(stream, topic), expectedMetastreamVersion, metadata, userCredentials);
    }

    /// <summary>Sets the metadata for a stream.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="stream">The name of the stream for which to set metadata.</param>
    /// <param name="expectedMetastreamVersion">The expected version for the write to the metadata stream.</param>
    /// <param name="metadata">A <see cref="StreamMetadata"/> representing the new metadata.</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <returns>A <see cref="WriteResult"/> containing the results of the write operation.</returns>
    public static WriteResult SetStreamMetadata(this IEventStoreConnectionBase connection,
      string stream, long expectedMetastreamVersion, StreamMetadata metadata, UserCredentials userCredentials = null)
    {
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }

      return AsyncContext.Run(
                async (conn, streamId, expectedVersion, meta, credentials)
                  => await conn.SetStreamMetadataAsync(streamId, expectedVersion, meta, credentials).ConfigureAwait(false),
                connection, stream, expectedMetastreamVersion, metadata, userCredentials);
    }

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
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }

      return AsyncContext.Run(
                async (conn, streamId, expectedVersion, meta, credentials)
                  => await conn.SetStreamMetadataAsync(streamId, expectedVersion, meta, credentials).ConfigureAwait(false),
                connection, stream, expectedMetastreamVersion, metadata, userCredentials);
    }

    /// <summary>Sets the metadata for a stream.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="actualType">The actual type</param>
    /// <param name="expectedMetastreamVersion">The expected version for the write to the metadata stream.</param>
    /// <param name="metadata">A <see cref="StreamMetadata"/> representing the new metadata.</param>
    /// <param name="expectedType">The expected type</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <returns>A <see cref="WriteResult"/> containing the results of the write operation.</returns>
    public static WriteResult SetStreamMetadata(this IEventStoreConnectionBase connection,
      Type actualType, long expectedMetastreamVersion, StreamMetadata metadata, Type expectedType = null, UserCredentials userCredentials = null)
    {
      return AsyncContext.Run(
                async (conn, aType, expectedVersion, meta, eType, credentials)
                  => await conn.SetStreamMetadataAsync(aType, expectedVersion, meta, eType, credentials).ConfigureAwait(false),
                connection, actualType, expectedMetastreamVersion, metadata, expectedType, userCredentials);
    }

    /// <summary>Sets the metadata for a stream.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="actualType">The actual type</param>
    /// <param name="topic">The topic</param>
    /// <param name="expectedMetastreamVersion">The expected version for the write to the metadata stream.</param>
    /// <param name="metadata">A <see cref="StreamMetadata"/> representing the new metadata.</param>
    /// <param name="expectedType">The expected type</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <returns>A <see cref="WriteResult"/> containing the results of the write operation.</returns>
    public static WriteResult SetStreamMetadata(this IEventStoreConnectionBase connection,
      Type actualType, string topic, long expectedMetastreamVersion, StreamMetadata metadata, Type expectedType = null, UserCredentials userCredentials = null)
    {
      return AsyncContext.Run(
                async (conn, typeWrapper, expectedVersion, meta, eType, credentials)
                  => await conn.SetStreamMetadataAsync(typeWrapper.Item1, typeWrapper.Item2, expectedVersion, meta, eType, credentials).ConfigureAwait(false),
                connection, Tuple.Create(actualType, topic), expectedMetastreamVersion, metadata, expectedType, userCredentials);
    }

    #endregion

    #region -- SetStreamMetadata<TEvent> --

    /// <summary>Sets the metadata for a stream.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="expectedMetastreamVersion">The expected version for the write to the metadata stream.</param>
    /// <param name="metadata">A <see cref="StreamMetadata"/> representing the new metadata.</param>
    /// <param name="expectedType">The expected type</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <returns>A <see cref="WriteResult"/> containing the results of the write operation.</returns>
    public static WriteResult SetStreamMetadata<TEvent>(this IEventStoreConnectionBase connection,
      long expectedMetastreamVersion, StreamMetadata metadata, Type expectedType = null, UserCredentials userCredentials = null)
      where TEvent : class
    {
      return AsyncContext.Run(
                async (conn, expectedVersion, meta, eType, credentials)
                  => await conn.SetStreamMetadataAsync<TEvent>(expectedVersion, meta, eType, credentials).ConfigureAwait(false),
                connection, expectedMetastreamVersion, metadata, expectedType, userCredentials);
    }

    /// <summary>Sets the metadata for a stream.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="topic">The topic</param>
    /// <param name="expectedMetastreamVersion">The expected version for the write to the metadata stream.</param>
    /// <param name="metadata">A <see cref="StreamMetadata"/> representing the new metadata.</param>
    /// <param name="expectedType">The expected type</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <returns>A <see cref="WriteResult"/> containing the results of the write operation.</returns>
    public static WriteResult SetStreamMetadata<TEvent>(this IEventStoreConnectionBase connection,
      string topic, long expectedMetastreamVersion, StreamMetadata metadata, Type expectedType = null, UserCredentials userCredentials = null)
      where TEvent : class
    {
      return AsyncContext.Run(
                async (conn, innerTopic, expectedVersion, meta, eType, credentials)
                  => await conn.SetStreamMetadataAsync<TEvent>(innerTopic, expectedVersion, meta, eType, credentials).ConfigureAwait(false),
                connection, topic, expectedMetastreamVersion, metadata, expectedType, userCredentials);
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
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }

      return AsyncContext.Run(
                async (conn, streamId, credentials)
                  => await conn.GetStreamMetadataAsync(streamId, credentials).ConfigureAwait(false),
                connection, stream, userCredentials);
    }

    /// <summary>Reads the metadata for a stream and converts the metadata into a <see cref="StreamMetadata"/>.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="actualType">The actual type</param>
    /// <param name="expectedType">The expected type</param>
    /// <param name="userCredentials">User credentials to use for the operation.</param>
    /// <returns>A <see cref="StreamMetadataResult"/> representing system and user-specified metadata as properties.</returns>
    public static StreamMetadataResult GetStreamMetadata(this IEventStoreConnectionBase connection,
      Type actualType, Type expectedType = null, UserCredentials userCredentials = null)
    {
      return AsyncContext.Run(
                async (conn, atype, etype, credentials)
                  => await conn.GetStreamMetadataAsync(atype, etype, credentials).ConfigureAwait(false),
                connection, actualType, expectedType, userCredentials);
    }

    /// <summary>Reads the metadata for a stream and converts the metadata into a <see cref="StreamMetadata"/>.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="actualType">The actual type</param>
    /// <param name="topic">The topic</param>
    /// <param name="expectedType">The expected type</param>
    /// <param name="userCredentials">User credentials to use for the operation.</param>
    /// <returns>A <see cref="StreamMetadataResult"/> representing system and user-specified metadata as properties.</returns>
    public static StreamMetadataResult GetStreamMetadata(this IEventStoreConnectionBase connection,
      Type actualType, string topic, Type expectedType = null, UserCredentials userCredentials = null)
    {
      return AsyncContext.Run(
                async (conn, atype, innerTopic, etype, credentials)
                  => await conn.GetStreamMetadataAsync(atype, innerTopic, etype, credentials).ConfigureAwait(false),
                connection, actualType, topic, expectedType, userCredentials);
    }

    #endregion

    #region -- GetStreamMetadata<TEvent> --

    /// <summary>Reads the metadata for a stream and converts the metadata into a <see cref="StreamMetadata"/>.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="expectedType">The expected type</param>
    /// <param name="userCredentials">User credentials to use for the operation.</param>
    /// <returns>A <see cref="StreamMetadataResult"/> representing system and user-specified metadata as properties.</returns>
    public static StreamMetadataResult GetStreamMetadata<TEvent>(this IEventStoreConnectionBase connection,
      Type expectedType = null, UserCredentials userCredentials = null) where TEvent : class
    {
      return AsyncContext.Run(
                async (conn, etype, credentials)
                  => await conn.GetStreamMetadataAsync<TEvent>(etype, credentials).ConfigureAwait(false),
                connection, expectedType, userCredentials);
    }

    /// <summary>Reads the metadata for a stream and converts the metadata into a <see cref="StreamMetadata"/>.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="topic">The topic</param>
    /// <param name="expectedType">The expected type</param>
    /// <param name="userCredentials">User credentials to use for the operation.</param>
    /// <returns>A <see cref="StreamMetadataResult"/> representing system and user-specified metadata as properties.</returns>
    public static StreamMetadataResult GetStreamMetadata<TEvent>(this IEventStoreConnectionBase connection,
      string topic, Type expectedType = null, UserCredentials userCredentials = null) where TEvent : class
    {
      return AsyncContext.Run(
                async (conn, innerTopic, etype, credentials)
                  => await conn.GetStreamMetadataAsync<TEvent>(innerTopic, etype, credentials).ConfigureAwait(false),
                connection, topic, expectedType, userCredentials);
    }

    #endregion

    #region -- GetStreamMetadataAsRawBytes --

    /// <summary>Synchronously reads the metadata for a stream as a byte array.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="stream">The name of the stream for which to read metadata.</param>
    /// <param name="userCredentials">User credentials to use for the operation.</param>
    /// <returns>A <see cref="RawStreamMetadataResult"/> representing system metadata as properties and user-specified metadata as bytes.</returns>
    public static RawStreamMetadataResult GetStreamMetadataAsRawBytes(this IEventStoreConnectionBase connection,
      string stream, UserCredentials userCredentials = null)
    {
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }

      return AsyncContext.Run(
                async (conn, streamId, credentials)
                  => await conn.GetStreamMetadataAsRawBytesAsync(streamId, credentials).ConfigureAwait(false),
                connection, stream, userCredentials);
    }

    #endregion


    #region -- SetStreamMetadataAsync --

    /// <summary>Asynchronously sets the metadata for a stream.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="actualType">The actual type</param>
    /// <param name="expectedMetastreamVersion">The expected version for the write to the metadata stream.</param>
    /// <param name="metadata">A <see cref="StreamMetadata"/> representing the new metadata.</param>
    /// <param name="expectedType">The expected type</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <returns>A <see cref="Task&lt;WriteResult&gt;"/> containing the results of the write operation.</returns>
    public static Task<WriteResult> SetStreamMetadataAsync(this IEventStoreConnectionBase connection,
      Type actualType, long expectedMetastreamVersion, StreamMetadata metadata, Type expectedType = null, UserCredentials userCredentials = null)
    {
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }
      if (null == actualType) { throw new ArgumentNullException(nameof(actualType)); }
      return connection.SetStreamMetadataAsync(SerializationManager.GetStreamId(actualType, expectedType), expectedMetastreamVersion, metadata, userCredentials);
    }

    /// <summary>Asynchronously sets the metadata for a stream.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="actualType">The actual type</param>
    /// <param name="topic">The topic</param>
    /// <param name="expectedMetastreamVersion">The expected version for the write to the metadata stream.</param>
    /// <param name="metadata">A <see cref="StreamMetadata"/> representing the new metadata.</param>
    /// <param name="expectedType">The expected type</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <returns>A <see cref="Task&lt;WriteResult&gt;"/> containing the results of the write operation.</returns>
    public static Task<WriteResult> SetStreamMetadataAsync(this IEventStoreConnectionBase connection,
      Type actualType, string topic, long expectedMetastreamVersion, StreamMetadata metadata, Type expectedType = null, UserCredentials userCredentials = null)
    {
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }
      if (null == actualType) { throw new ArgumentNullException(nameof(actualType)); }
      if (string.IsNullOrEmpty(topic)) { throw new ArgumentNullException(nameof(topic)); }
      return connection.SetStreamMetadataAsync(CombineStreamId(SerializationManager.GetStreamId(actualType, expectedType), topic), expectedMetastreamVersion, metadata, userCredentials);
    }

    #endregion

    #region -- SetStreamMetadataAsync<TEvent> --

    /// <summary>Asynchronously sets the metadata for a stream.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="expectedMetastreamVersion">The expected version for the write to the metadata stream.</param>
    /// <param name="metadata">A <see cref="StreamMetadata"/> representing the new metadata.</param>
    /// <param name="expectedType">The expected type</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <returns>A <see cref="Task&lt;WriteResult&gt;"/> containing the results of the write operation.</returns>
    public static Task<WriteResult> SetStreamMetadataAsync<TEvent>(this IEventStoreConnectionBase connection,
      long expectedMetastreamVersion, StreamMetadata metadata, Type expectedType = null, UserCredentials userCredentials = null)
      where TEvent : class
    {
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }
      return connection.SetStreamMetadataAsync(SerializationManager.GetStreamId(typeof(TEvent), expectedType), expectedMetastreamVersion, metadata, userCredentials);
    }

    /// <summary>Asynchronously sets the metadata for a stream.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="topic">The topic</param>
    /// <param name="expectedMetastreamVersion">The expected version for the write to the metadata stream.</param>
    /// <param name="metadata">A <see cref="StreamMetadata"/> representing the new metadata.</param>
    /// <param name="expectedType">The expected type</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <returns>A <see cref="Task&lt;WriteResult&gt;"/> containing the results of the write operation.</returns>
    public static Task<WriteResult> SetStreamMetadataAsync<TEvent>(this IEventStoreConnectionBase connection,
      string topic, long expectedMetastreamVersion, StreamMetadata metadata, Type expectedType = null, UserCredentials userCredentials = null)
      where TEvent : class
    {
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }
      if (string.IsNullOrEmpty(topic)) { throw new ArgumentNullException(nameof(topic)); }
      return connection.SetStreamMetadataAsync(CombineStreamId(SerializationManager.GetStreamId(typeof(TEvent), expectedType), topic), expectedMetastreamVersion, metadata, userCredentials);
    }

    #endregion

    #region -- GetStreamMetadataAsync --

    /// <summary>Asynchronously reads the metadata for a stream and converts the metadata into a <see cref="StreamMetadata"/>.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="stream">The name of the stream for which to read metadata.</param>
    /// <param name="topic">The topic</param>
    /// <param name="userCredentials">User credentials to use for the operation.</param>
    /// <returns>A <see cref="Task&lt;StreamMetadataResult&gt;"/> representing system and user-specified metadata as properties.</returns>
    public static Task<StreamMetadataResult> GetStreamMetadataAsync(this IEventStoreConnectionBase connection,
      string stream, string topic, UserCredentials userCredentials = null)
    {
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }
      if (string.IsNullOrEmpty(stream)) { throw new ArgumentNullException(nameof(stream)); }
      if (string.IsNullOrEmpty(topic)) { throw new ArgumentNullException(nameof(topic)); }
      return connection.GetStreamMetadataAsync(CombineStreamId(stream, topic), userCredentials);
    }

    /// <summary>Asynchronously reads the metadata for a stream and converts the metadata into a <see cref="StreamMetadata"/>.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="actualType">The actual type</param>
    /// <param name="expectedType">The expected type</param>
    /// <param name="userCredentials">User credentials to use for the operation.</param>
    /// <returns>A <see cref="Task&lt;StreamMetadataResult&gt;"/> representing system and user-specified metadata as properties.</returns>
    public static Task<StreamMetadataResult> GetStreamMetadataAsync(this IEventStoreConnectionBase connection,
      Type actualType, Type expectedType = null, UserCredentials userCredentials = null)
    {
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }
      if (null == actualType) { throw new ArgumentNullException(nameof(actualType)); }
      return connection.GetStreamMetadataAsync(SerializationManager.GetStreamId(actualType, expectedType), userCredentials);
    }

    /// <summary>Asynchronously reads the metadata for a stream and converts the metadata into a <see cref="StreamMetadata"/>.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="actualType">The actual type</param>
    /// <param name="topic">The topic</param>
    /// <param name="expectedType">The expected type</param>
    /// <param name="userCredentials">User credentials to use for the operation.</param>
    /// <returns>A <see cref="Task&lt;StreamMetadataResult&gt;"/> representing system and user-specified metadata as properties.</returns>
    public static Task<StreamMetadataResult> GetStreamMetadataAsync(this IEventStoreConnectionBase connection,
      Type actualType, string topic, Type expectedType = null, UserCredentials userCredentials = null)
    {
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }
      if (null == actualType) { throw new ArgumentNullException(nameof(actualType)); }
      if (string.IsNullOrEmpty(topic)) { throw new ArgumentNullException(nameof(topic)); }
      return connection.GetStreamMetadataAsync(CombineStreamId(SerializationManager.GetStreamId(actualType, expectedType), topic), userCredentials);
    }

    #endregion

    #region -- GetStreamMetadataAsync<TEvent> --

    /// <summary>Asynchronously reads the metadata for a stream and converts the metadata into a <see cref="StreamMetadata"/>.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="expectedType">The expected type</param>
    /// <param name="userCredentials">User credentials to use for the operation.</param>
    /// <returns>A <see cref="Task&lt;StreamMetadataResult&gt;"/> representing system and user-specified metadata as properties.</returns>
    public static Task<StreamMetadataResult> GetStreamMetadataAsync<TEvent>(this IEventStoreConnectionBase connection,
      Type expectedType = null, UserCredentials userCredentials = null) where TEvent : class
    {
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }
      return connection.GetStreamMetadataAsync(SerializationManager.GetStreamId(typeof(TEvent), expectedType), userCredentials);
    }

    /// <summary>Asynchronously reads the metadata for a stream and converts the metadata into a <see cref="StreamMetadata"/>.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="topic">The topic</param>
    /// <param name="expectedType">The expected type</param>
    /// <param name="userCredentials">User credentials to use for the operation.</param>
    /// <returns>A <see cref="Task&lt;StreamMetadataResult&gt;"/> representing system and user-specified metadata as properties.</returns>
    public static Task<StreamMetadataResult> GetStreamMetadataAsync<TEvent>(this IEventStoreConnectionBase connection,
      string topic, Type expectedType = null, UserCredentials userCredentials = null) where TEvent : class
    {
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }
      if (string.IsNullOrEmpty(topic)) { throw new ArgumentNullException(nameof(topic)); }
      return connection.GetStreamMetadataAsync(CombineStreamId(SerializationManager.GetStreamId(typeof(TEvent), expectedType), topic), userCredentials);
    }

    #endregion
  }
}
