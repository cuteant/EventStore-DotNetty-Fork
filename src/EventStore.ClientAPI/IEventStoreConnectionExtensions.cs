using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using CuteAnt.AsyncEx;
using EventStore.ClientAPI.SystemData;

namespace EventStore.ClientAPI
{
  public static class IEventStoreConnectionExtensions
  {
    #region -- Connect --

    /// <summary>Connects the <see cref="IEventStoreConnection"/> asynchronously to a destination.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    public static void Connect(this IEventStoreConnectionBase connection)
    {
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }

      AsyncContext.Run(conn => conn.ConnectAsync(), connection);
    }

    #endregion

    #region -- StreamMetadata --

    /// <summary>Synchronously sets the metadata for a stream.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="stream">The name of the stream for which to set metadata.</param>
    /// <param name="expectedMetastreamVersion">The expected version for the write to the metadata stream.</param>
    /// <param name="metadata">A <see cref="StreamMetadata"/> representing the new metadata.</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <returns>A <see cref="WriteResult"/>.</returns>
    public static WriteResult SetStreamMetadata(this IEventStoreConnectionBase connection,
      string stream, long expectedMetastreamVersion, StreamMetadata metadata, UserCredentials userCredentials = null)
    {
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }

      return AsyncContext.Run(
                async (conn, streamId, expectedVersion, meta, credentials)
                  => await conn.SetStreamMetadataAsync(streamId, expectedVersion, meta, credentials).ConfigureAwait(false),
                connection, stream, expectedMetastreamVersion, metadata, userCredentials);
    }

    /// <summary>Synchronously sets the metadata for a stream.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="stream">The name of the stream for which to set metadata.</param>
    /// <param name="expectedMetastreamVersion">The expected version for the write to the metadata stream.</param>
    /// <param name="metadata">A byte array representing the new metadata.</param>
    /// <param name="userCredentials">User credentials to use for the operation.</param>
    /// <returns>A <see cref="WriteResult"/>.</returns>
    public static WriteResult SetStreamMetadata(this IEventStoreConnectionBase connection,
      string stream, long expectedMetastreamVersion, byte[] metadata, UserCredentials userCredentials = null)
    {
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }

      return AsyncContext.Run(
                async (conn, streamId, expectedVersion, meta, credentials)
                  => await conn.SetStreamMetadataAsync(streamId, expectedVersion, meta, credentials).ConfigureAwait(false),
                connection, stream, expectedMetastreamVersion, metadata, userCredentials);
    }

    /// <summary>Synchronously reads the metadata for a stream and converts the metadata into a <see cref="StreamMetadata"/>.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="stream">The name of the stream for which to read metadata.</param>
    /// <param name="userCredentials">User credentials to use for the operation.</param>
    /// <returns>A <see cref="StreamMetadataResult"/> representing the result of the operation.</returns>
    public static StreamMetadataResult GetStreamMetadata(this IEventStoreConnectionBase connection,
      string stream, UserCredentials userCredentials = null)
    {
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }

      return AsyncContext.Run(
                async (conn, streamId, credentials)
                  => await conn.GetStreamMetadataAsync(streamId, credentials).ConfigureAwait(false),
                connection, stream, userCredentials);
    }

    /// <summary>Synchronously reads the metadata for a stream as a byte array.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="stream">The name of the stream for which to read metadata.</param>
    /// <param name="userCredentials">User credentials to use for the operation.</param>
    /// <returns>A <see cref="StreamMetadataResult"/> representing the result of the operation.</returns>
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

    #region -- SetSystemSettings --

    /// <summary>Sets the global settings for the server or cluster to which the <see cref="IEventStoreConnection"/> is connected.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="sysSettings">The <see cref="SystemSettings"/> to apply.</param>
    /// <param name="userCredentials">User credentials to use for the operation.</param>
    public static void SetSystemSettings(this IEventStoreConnectionBase connection, SystemSettings sysSettings, UserCredentials userCredentials = null)
    {
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }

      AsyncContext.Run(
        async (conn, settings, credentials)
          => await conn.SetSystemSettingsAsync(settings, credentials).ConfigureAwait(false),
        connection, sysSettings, userCredentials);
    }

    #endregion

    #region -- DeleteStream --

    /// <summary>Deletes a stream from the Event Store synchronously.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="stream">The name of the stream to delete.</param>
    /// <param name="expectedVersion">The expected version that the streams should have when being deleted. <see cref="ExpectedVersion"/></param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    /// <returns>A <see cref="Task"/> that can be awaited upon by the caller.</returns>
    public static DeleteResult DeleteStream(this IEventStoreConnectionBase connection,
      string stream, long expectedVersion, UserCredentials userCredentials = null)
    {
      return AsyncContext.Run(
                async (conn, streamId, version, credentials)
                  => await conn.DeleteStreamAsync(streamId, version, credentials).ConfigureAwait(false),
                connection, stream, expectedVersion, userCredentials);

    }

    /// <summary>Deletes a stream from the Event Store synchronously.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="stream">The name of the stream to delete.</param>
    /// <param name="expectedVersion">The expected version that the streams should have when being deleted. <see cref="ExpectedVersion"/></param>
    /// <param name="hardDelete">Indicator for tombstoning vs soft-deleting the stream. Tombstoned streams can never be recreated. Soft-deleted streams
    /// can be written to again, but the EventNumber sequence will not start from 0.</param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    /// <returns>A <see cref="Task"/> that can be awaited upon by the caller.</returns>
    public static DeleteResult DeleteStream(this IEventStoreConnectionBase connection,
      string stream, long expectedVersion, bool hardDelete, UserCredentials userCredentials = null)
    {
      return AsyncContext.Run(
                async (conn, streamId, version, hardDel, credentials)
                  => await conn.DeleteStreamAsync(streamId, version, hardDel, credentials).ConfigureAwait(false),
                connection, stream, expectedVersion, hardDelete, userCredentials);
    }

    #endregion

    #region -- AppendToStream / ConditionalAppendToStream --

    /// <summary>Appends Events synchronously to a stream.</summary>
    /// <remarks>When appending events to a stream the <see cref="ExpectedVersion"/> choice can
    /// make a very large difference in the observed behavior. For example, if no stream exists
    /// and ExpectedVersion.Any is used, a new stream will be implicitly created when appending.
    ///
    /// There are also differences in idempotency between different types of calls.
    /// If you specify an ExpectedVersion aside from ExpectedVersion.Any the Event Store
    /// will give you an idempotency guarantee. If using ExpectedVersion.Any the Event Store
    /// will do its best to provide idempotency but does not guarantee idempotency.</remarks>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="stream">The name of the stream to append events to</param>
    /// <param name="expectedVersion">The <see cref="ExpectedVersion"/> of the stream to append to</param>
    /// <param name="events">The events to append to the stream</param>
    public static WriteResult AppendToStream(this IEventStoreConnectionBase connection, string stream, long expectedVersion, params EventData[] events)
    {
      return AsyncContext.Run(
                async (conn, streamId, version, eventArray)
                  => await conn.AppendToStreamAsync(streamId, version, eventArray).ConfigureAwait(false),
                connection, stream, expectedVersion, events);
    }

    /// <summary>Appends Events synchronously to a stream.</summary>
    /// <remarks>When appending events to a stream the <see cref="ExpectedVersion"/> choice can
    /// make a very large difference in the observed behavior. For example, if no stream exists
    /// and ExpectedVersion.Any is used, a new stream will be implicitly created when appending.
    ///
    /// There are also differences in idempotency between different types of calls.
    /// If you specify an ExpectedVersion aside from ExpectedVersion.Any the Event Store
    /// will give you an idempotency guarantee. If using ExpectedVersion.Any the Event Store
    /// will do its best to provide idempotency but does not guarantee idempotency.</remarks>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="stream">The name of the stream to append events to</param>
    /// <param name="expectedVersion">The <see cref="ExpectedVersion"/> of the stream to append to</param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    /// <param name="events">The events to append to the stream</param>
    public static WriteResult AppendToStream(this IEventStoreConnectionBase connection,
      string stream, long expectedVersion, UserCredentials userCredentials, params EventData[] events)
    {
      return AsyncContext.Run(
                async (conn, streamId, version, credentials, eventArray)
                  => await conn.AppendToStreamAsync(streamId, version, credentials, eventArray).ConfigureAwait(false),
                connection, stream, expectedVersion, userCredentials, events);
    }

    /// <summary>Appends Events synchronously to a stream.</summary>
    /// <remarks>When appending events to a stream the <see cref="ExpectedVersion"/> choice can
    /// make a very large difference in the observed behavior. For example, if no stream exists
    /// and ExpectedVersion.Any is used, a new stream will be implicitly created when appending.
    ///
    /// There are also differences in idempotency between different types of calls.
    /// If you specify an ExpectedVersion aside from ExpectedVersion.Any the Event Store
    /// will give you an idempotency guarantee. If using ExpectedVersion.Any the Event Store
    /// will do its best to provide idempotency but does not guarantee idempotency.</remarks>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="stream">The name of the stream to append events to</param>
    /// <param name="expectedVersion">The <see cref="ExpectedVersion"/> of the stream to append to</param>
    /// <param name="events">The events to append to the stream</param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    public static WriteResult AppendToStream(this IEventStoreConnectionBase connection,
      string stream, long expectedVersion, IEnumerable<EventData> events, UserCredentials userCredentials = null)
    {
      return AsyncContext.Run(
                async (conn, streamId, version, eventList, credentials)
                  => await conn.AppendToStreamAsync(streamId, version, eventList, credentials).ConfigureAwait(false),
                connection, stream, expectedVersion, events, userCredentials);
    }

    /// <summary>Appends Events synchronously to a stream if the stream version matches the <paramref name="expectedVersion"/>.</summary>
    /// <remarks>When appending events to a stream the <see cref="ExpectedVersion"/> choice can
    /// make a very large difference in the observed behavior. For example, if no stream exists
    /// and ExpectedVersion.Any is used, a new stream will be implicitly created when appending.
    ///
    /// There are also differences in idempotency between different types of calls.
    /// If you specify an ExpectedVersion aside from ExpectedVersion.Any the Event Store
    /// will give you an idempotency guarantee. If using ExpectedVersion.Any the Event Store
    /// will do its best to provide idempotency but does not guarantee idempotency.</remarks>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="stream">The name of the stream to append events to</param>
    /// <param name="expectedVersion">The <see cref="ExpectedVersion"/> of the stream to append to</param>
    /// <param name="events">The events to append to the stream</param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    /// <returns>If the operation succeeded and, if not, the reason for failure (which can be either stream version mismatch or trying to write to a deleted stream)</returns>
    public static ConditionalWriteResult ConditionalAppendToStream(this IEventStoreConnectionBase connection,
      string stream, long expectedVersion, IEnumerable<EventData> events, UserCredentials userCredentials = null)
    {
      return AsyncContext.Run(
                async (conn, streamId, version, eventList, credentials)
                  => await conn.ConditionalAppendToStreamAsync(streamId, version, eventList, credentials).ConfigureAwait(false),
                connection, stream, expectedVersion, events, userCredentials);
    }

    #endregion

    #region -- Read event(s) --

    /// <summary>Reads a single event from a stream.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="stream">The stream to read from</param>
    /// <param name="eventNumber">The event number to read, <see cref="StreamPosition">StreamPosition.End</see> to read the last event in the stream</param>
    /// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically</param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    /// <returns>A result of the read operation</returns>
    public static EventReadResult ReadEvent(this IEventStoreConnectionBase connection,
      string stream, long eventNumber, bool resolveLinkTos, UserCredentials userCredentials = null)
    {
      return AsyncContext.Run(
                async (conn, streamId, eventNum, resolveLinkToEvents, credentials)
                  => await conn.ReadEventAsync(streamId, eventNum, resolveLinkToEvents, credentials).ConfigureAwait(false),
                connection, stream, eventNumber, resolveLinkTos, userCredentials);
    }

    /// <summary>Reads count Events from an Event Stream forwards (e.g. oldest to newest) starting from position start.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="stream">The stream to read from</param>
    /// <param name="start">The starting point to read from</param>
    /// <param name="count">The count of items to read</param>
    /// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically</param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    /// <returns>A result of the read operation</returns>
    public static StreamEventsSlice ReadStreamEventsForward(this IEventStoreConnectionBase connection,
      string stream, long start, int count, bool resolveLinkTos, UserCredentials userCredentials = null)
    {
      return AsyncContext.Run(
                async (conn, streamId, pointer, eventCount, resolveLinkToEvents, credentials)
                  => await conn.ReadStreamEventsForwardAsync(streamId, pointer, eventCount, resolveLinkToEvents, credentials).ConfigureAwait(false),
                connection, stream, start, count, resolveLinkTos, userCredentials);
    }

    /// <summary>Reads count events from an Event Stream backwards (e.g. newest to oldest) from position asynchronously.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="stream">The Event Stream to read from</param>
    /// <param name="start">The position to start reading from</param>
    /// <param name="count">The count to read from the position</param>
    /// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically</param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    /// <returns>An result of the read operation</returns>
    public static StreamEventsSlice ReadStreamEventsBackward(this IEventStoreConnectionBase connection,
      string stream, long start, int count, bool resolveLinkTos, UserCredentials userCredentials = null)
    {
      return AsyncContext.Run(
                async (conn, streamId, pointer, eventCount, resolveLinkToEvents, credentials)
                  => await conn.ReadStreamEventsBackwardAsync(streamId, pointer, eventCount, resolveLinkToEvents, credentials).ConfigureAwait(false),
                connection, stream, start, count, resolveLinkTos, userCredentials);
    }

    #endregion

    #region -- ReadFirstEvent --

    /// <summary>Reads the frist event from a stream.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="stream">The stream to read from</param>
    /// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically</param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    /// <returns>A result of the read operation</returns>
    public static EventReadResult ReadFirstEvent(this IEventStoreConnectionBase connection,
      string stream, bool resolveLinkTos, UserCredentials userCredentials = null)
    {
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }

      return AsyncContext.Run(
        async (conn, streamId, eventNum, resolveLinkToEvents, credentials)
          => await conn.ReadEventAsync(streamId, eventNum, resolveLinkToEvents, credentials).ConfigureAwait(false),
        connection, stream, StreamPosition.Start, resolveLinkTos, userCredentials);
    }

    #endregion

    #region -- ReadFirstEventAsync --

    /// <summary>Asynchronously reads the frist event from a stream.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="stream">The stream to read from</param>
    /// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically</param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    /// <returns>A <see cref="Task&lt;EventReadResult&gt;"/> containing the results of the read operation</returns>
    public static Task<EventReadResult> ReadFirstEventAsync(this IEventStoreConnectionBase connection,
      string stream, bool resolveLinkTos, UserCredentials userCredentials = null)
    {
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }

      return connection.ReadEventAsync(stream, StreamPosition.Start, resolveLinkTos, userCredentials);
    }

    #endregion

    #region -- ReadLastEvent --

    /// <summary>Reads the last event from a stream.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="stream">The stream to read from</param>
    /// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically</param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    /// <returns>A <see cref="Task&lt;EventReadResult&gt;"/> containing the results of the read operation</returns>
    public static EventReadResult ReadLastEvent(this IEventStoreConnectionBase connection,
      string stream, bool resolveLinkTos, UserCredentials userCredentials = null)
    {
      return AsyncContext.Run(
        async (conn, streamId, resolveLinkToEvents, credentials)
          => await conn.ReadLastEventAsync(streamId, resolveLinkToEvents, credentials).ConfigureAwait(false),
        connection, stream, resolveLinkTos, userCredentials);
    }

    #endregion

    #region -- ReadLastEventAsync --

    /// <summary>Asynchronously reads the last event from a stream.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="stream">The stream to read from</param>
    /// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically</param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    /// <returns>A <see cref="Task&lt;EventReadResult&gt;"/> containing the results of the read operation</returns>
    public static async Task<EventReadResult> ReadLastEventAsync(this IEventStoreConnectionBase connection,
      string stream, bool resolveLinkTos, UserCredentials userCredentials = null)
    {
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }

      var slice = await connection.ReadStreamEventsBackwardAsync(stream, StreamPosition.End, 1, resolveLinkTos, userCredentials)
                                  .ConfigureAwait(false);
      var readStatus = EventReadStatus.Success;
      var sliceEvents = slice.Events;
      switch (slice.Status)
      {
        case SliceReadStatus.StreamNotFound:
          readStatus = EventReadStatus.NoStream;
          break;
        case SliceReadStatus.StreamDeleted:
          readStatus = EventReadStatus.StreamDeleted;
          break;
        case SliceReadStatus.Success:
        default:
          if (sliceEvents.Length == 0) { readStatus = EventReadStatus.NotFound; }
          break;
      }
      if (EventReadStatus.Success == readStatus)
      {
        var lastEvent = sliceEvents[0];
        return EventReadResult.Create(readStatus, slice.Stream, lastEvent.OriginalEventNumber, resolvedEvent: lastEvent);
      }
      else
      {
        return EventReadResult.Create(readStatus, slice.Stream, -1, resolvedEvent: null);
      }
    }

    #endregion

    #region -- CreatePersistentSubscription --

    /// <summary>Synchronous create a persistent subscription group on a stream.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="stream">The name of the stream to create the persistent subscription on</param>
    /// <param name="groupName">The name of the group to create</param>
    /// <param name="subscriptionSettings">The <see cref="PersistentSubscriptionSettings"></see> for the subscription</param>
    /// <param name="userCredentials">The credentials to be used for this operation.</param>
    public static void CreatePersistentSubscription(this IEventStoreConnectionBase connection,
      string stream, string groupName, PersistentSubscriptionSettings subscriptionSettings, UserCredentials userCredentials = null)
    {
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }

      try
      {
        AsyncContext.Run(
          async (conn, streamId, group, settings, credentials)
            => await conn.CreatePersistentSubscriptionAsync(streamId, group, settings, credentials).ConfigureAwait(false),
          connection, stream, groupName, subscriptionSettings, userCredentials);
      }
      catch (InvalidOperationException ex)
      {
        if (!string.Equals(ex.Message,
                    string.Format(CultureInfo.InvariantCulture, Consts.PersistentSubscriptionAlreadyExists, groupName, stream),
                    StringComparison.Ordinal))
        {
          throw ex;
        }
      }
    }

    #endregion

    #region -- DeletePersistentSubscription --

    /// <summary>Synchronous delete a persistent subscription group on a stream.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="stream">The name of the stream to delete the persistent subscription on</param>
    /// <param name="groupName">The name of the group to delete</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    public static void DeletePersistentSubscription(this IEventStoreConnectionBase connection,
      string stream, string groupName, UserCredentials userCredentials = null)
    {
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }

      try
      {
        AsyncContext.Run(
          async (conn, streamId, group, credentials)
            => await conn.DeletePersistentSubscriptionAsync(streamId, group, credentials).ConfigureAwait(false),
          connection, stream, groupName, userCredentials);
      }
      catch (InvalidOperationException ex)
      {
        if (!string.Equals(ex.Message,
                           string.Format(CultureInfo.InvariantCulture, Consts.PersistentSubscriptionDoesNotExist, groupName, stream),
                           StringComparison.Ordinal))
        {
          throw ex;
        }
      }
    }

    #endregion

    #region -- UpdatePersistentSubscription --

    /// <summary>Synchronous update a persistent subscription group on a stream.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="stream">The name of the stream to create the persistent subscription on</param>
    /// <param name="groupName">The name of the group to create</param>
    /// <param name="subscriptionSettings">The <see cref="PersistentSubscriptionSettings"></see> for the subscription</param>
    /// <param name="userCredentials">The credentials to be used for this operation.</param>
    public static void UpdatePersistentSubscription(this IEventStoreConnectionBase connection,
      string stream, string groupName, PersistentSubscriptionSettings subscriptionSettings, UserCredentials userCredentials = null)
    {
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }

      try
      {
        AsyncContext.Run(
          async (conn, streamId, group, settings, credentials)
            => await conn.UpdatePersistentSubscriptionAsync(streamId, group, settings, credentials).ConfigureAwait(false),
          connection, stream, groupName, subscriptionSettings, userCredentials);
      }
      catch (InvalidOperationException ex)
      {
        if (string.Equals(ex.Message,
                          string.Format(CultureInfo.InvariantCulture, Consts.PersistentSubscriptionDoesNotExist, groupName, stream),
                          StringComparison.Ordinal))
        {
          CreatePersistentSubscription(connection, stream, groupName, subscriptionSettings, userCredentials);
        }
      }
    }

    #endregion

    #region -- ConnectToPersistentSubscription --

    /// <summary>Subscribes to a persistent subscription(competing consumer) on event store.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="groupName">The subscription group to connect to</param>
    /// <param name="stream">The stream to subscribe to</param>
    /// <param name="eventAppeared">An action invoked when an event appears</param>
    /// <param name="subscriptionDropped">An action invoked if the subscription is dropped</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <param name="bufferSize">The buffer size to use for the persistent subscription</param>
    /// <param name="autoAck">Whether the subscription should automatically acknowledge messages processed.
    /// If not set the receiver is required to explicitly acknowledge messages through the subscription.</param>
    /// <remarks>This will connect you to a persistent subscription group for a stream. The subscription group
    /// must first be created with CreatePersistentSubscriptionAsync many connections
    /// can connect to the same group and they will be treated as competing consumers within the group.
    /// If one connection dies work will be balanced across the rest of the consumers in the group. If
    /// you attempt to connect to a group that does not exist you will be given an exception.</remarks>
    /// <param name="verboseLogging">Enables verbose logging on the subscription</param>
    /// <returns>An <see cref="EventStorePersistentSubscriptionBase"/> representing the subscription</returns>
    public static EventStorePersistentSubscriptionBase ConnectToPersistentSubscription(this IEventStoreConnectionBase connection,
      string stream, string groupName,
      Action<EventStorePersistentSubscriptionBase, ResolvedEvent> eventAppeared,
      Action<EventStorePersistentSubscriptionBase, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null, int bufferSize = 10, bool autoAck = true, bool verboseLogging = false)
    {
      var subscriptionSettings = new ConnectToPersistentSubscriptionSettings(bufferSize, autoAck, verboseLogging);

      return AsyncContext.Run(
        async (conn, streamWrapper, settings, eAppeared, subDropped, credentials)
          => await conn.ConnectToPersistentSubscriptionAsync(streamWrapper.Item1, streamWrapper.Item2, settings, eAppeared, subDropped, credentials).ConfigureAwait(false),
        connection, Tuple.Create(stream, groupName), subscriptionSettings, eventAppeared, subscriptionDropped, userCredentials);
    }

    /// <summary>Subscribes to a persistent subscription(competing consumer) on event store.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="groupName">The subscription group to connect to</param>
    /// <param name="stream">The stream to subscribe to</param>
    /// <param name="eventAppearedAsync">A Task invoked and awaited when an event appears</param>
    /// <param name="subscriptionDropped">An action invoked if the subscription is dropped</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <param name="bufferSize">The buffer size to use for the persistent subscription</param>
    /// <param name="autoAck">Whether the subscription should automatically acknowledge messages processed.
    /// If not set the receiver is required to explicitly acknowledge messages through the subscription.</param>
    /// <remarks>This will connect you to a persistent subscription group for a stream. The subscription group
    /// must first be created with CreatePersistentSubscriptionAsync many connections
    /// can connect to the same group and they will be treated as competing consumers within the group.
    /// If one connection dies work will be balanced across the rest of the consumers in the group. If
    /// you attempt to connect to a group that does not exist you will be given an exception.</remarks>
    /// <param name="verboseLogging">Enables verbose logging on the subscription</param>
    /// <returns>An <see cref="EventStorePersistentSubscriptionBase"/> representing the subscription</returns>
    public static EventStorePersistentSubscriptionBase ConnectToPersistentSubscription(this IEventStoreConnectionBase connection,
      string stream, string groupName,
      Func<EventStorePersistentSubscriptionBase, ResolvedEvent, Task> eventAppearedAsync,
      Action<EventStorePersistentSubscriptionBase, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null, int bufferSize = 10, bool autoAck = true, bool verboseLogging = false)
    {
      var subscriptionSettings = new ConnectToPersistentSubscriptionSettings(bufferSize, autoAck, verboseLogging);
      return AsyncContext.Run(
        async (conn, streamWrapper, settings, eAppeared, subDropped, credentials)
          => await conn.ConnectToPersistentSubscriptionAsync(streamWrapper.Item1, streamWrapper.Item2, settings, eAppeared, subDropped, credentials).ConfigureAwait(false),
        connection, Tuple.Create(stream, groupName), subscriptionSettings, eventAppearedAsync, subscriptionDropped, userCredentials);
    }

    /// <summary>Subscribes to a persistent subscription(competing consumer) on event store.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="groupName">The subscription group to connect to</param>
    /// <param name="stream">The stream to subscribe to</param>
    /// <param name="subscriptionSettings">The <see cref="ConnectToPersistentSubscriptionSettings"/> for the subscription</param>
    /// <param name="eventAppeared">An action invoked when an event appears</param>
    /// <param name="subscriptionDropped">An action invoked if the subscription is dropped</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <remarks>This will connect you to a persistent subscription group for a stream. The subscription group
    /// must first be created with CreatePersistentSubscriptionAsync many connections
    /// can connect to the same group and they will be treated as competing consumers within the group.
    /// If one connection dies work will be balanced across the rest of the consumers in the group. If
    /// you attempt to connect to a group that does not exist you will be given an exception.</remarks>
    /// <returns>An <see cref="EventStorePersistentSubscriptionBase"/> representing the subscription</returns>
    public static EventStorePersistentSubscriptionBase ConnectToPersistentSubscription(this IEventStoreConnectionBase connection,
      string stream, string groupName,
      ConnectToPersistentSubscriptionSettings subscriptionSettings,
      Action<EventStorePersistentSubscriptionBase, ResolvedEvent> eventAppeared,
      Action<EventStorePersistentSubscriptionBase, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null)
    {
      return AsyncContext.Run(
        async (conn, streamWrapper, settings, eAppeared, subDropped, credentials)
          => await conn.ConnectToPersistentSubscriptionAsync(streamWrapper.Item1, streamWrapper.Item2, settings, eAppeared, subDropped, credentials).ConfigureAwait(false),
        connection, Tuple.Create(stream, groupName), subscriptionSettings, eventAppeared, subscriptionDropped, userCredentials);
    }

    /// <summary>Subscribes to a persistent subscription(competing consumer) on event store.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="groupName">The subscription group to connect to</param>
    /// <param name="stream">The stream to subscribe to</param>
    /// <param name="subscriptionSettings">The <see cref="ConnectToPersistentSubscriptionSettings"/> for the subscription</param>
    /// <param name="eventAppearedAsync">A Task invoked and awaited when an event appears</param>
    /// <param name="subscriptionDropped">An action invoked if the subscription is dropped</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <remarks>This will connect you to a persistent subscription group for a stream. The subscription group
    /// must first be created with CreatePersistentSubscriptionAsync many connections
    /// can connect to the same group and they will be treated as competing consumers within the group.
    /// If one connection dies work will be balanced across the rest of the consumers in the group. If
    /// you attempt to connect to a group that does not exist you will be given an exception.</remarks>
    /// <returns>An <see cref="EventStorePersistentSubscriptionBase"/> representing the subscription</returns>
    public static EventStorePersistentSubscriptionBase ConnectToPersistentSubscription(this IEventStoreConnectionBase connection,
      string stream, string groupName,
      ConnectToPersistentSubscriptionSettings subscriptionSettings,
      Func<EventStorePersistentSubscriptionBase, ResolvedEvent, Task> eventAppearedAsync,
      Action<EventStorePersistentSubscriptionBase, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null)
    {
      return AsyncContext.Run(
        async (conn, streamWrapper, settings, eAppeared, subDropped, credentials)
          => await conn.ConnectToPersistentSubscriptionAsync(streamWrapper.Item1, streamWrapper.Item2, settings, eAppeared, subDropped, credentials).ConfigureAwait(false),
        connection, Tuple.Create(stream, groupName), subscriptionSettings, eventAppearedAsync, subscriptionDropped, userCredentials);
    }

    #endregion

    #region -- ConnectToPersistentSubscriptionAsync --

    /// <summary>Asynchronously subscribes to a persistent subscription(competing consumer) on event store.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="groupName">The subscription group to connect to</param>
    /// <param name="stream">The stream to subscribe to</param>
    /// <param name="eventAppeared">An action invoked when an event appears</param>
    /// <param name="subscriptionDropped">An action invoked if the subscription is dropped</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <param name="bufferSize">The buffer size to use for the persistent subscription</param>
    /// <param name="autoAck">Whether the subscription should automatically acknowledge messages processed.
    /// If not set the receiver is required to explicitly acknowledge messages through the subscription.</param>
    /// <remarks>This will connect you to a persistent subscription group for a stream. The subscription group
    /// must first be created with CreatePersistentSubscriptionAsync many connections
    /// can connect to the same group and they will be treated as competing consumers within the group.
    /// If one connection dies work will be balanced across the rest of the consumers in the group. If
    /// you attempt to connect to a group that does not exist you will be given an exception.</remarks>
    /// <param name="verboseLogging">Enables verbose logging on the subscription</param>
    /// <returns>An <see cref="EventStorePersistentSubscriptionBase"/> representing the subscription</returns>
    public static Task<EventStorePersistentSubscriptionBase> ConnectToPersistentSubscriptionAsync(this IEventStoreConnectionBase connection,
      string stream, string groupName,
      Action<EventStorePersistentSubscriptionBase, ResolvedEvent> eventAppeared,
      Action<EventStorePersistentSubscriptionBase, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null, int bufferSize = 10, bool autoAck = true, bool verboseLogging = false)
    {
      var settings = new ConnectToPersistentSubscriptionSettings(bufferSize, autoAck, verboseLogging);
      return connection.ConnectToPersistentSubscriptionAsync(stream, groupName, settings, eventAppeared, subscriptionDropped, userCredentials);
    }

    /// <summary>Asynchronously subscribes to a persistent subscription(competing consumer) on event store.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="groupName">The subscription group to connect to</param>
    /// <param name="stream">The stream to subscribe to</param>
    /// <param name="eventAppearedAsync">A Task invoked and awaited when an event appears</param>
    /// <param name="subscriptionDropped">An action invoked if the subscription is dropped</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <param name="bufferSize">The buffer size to use for the persistent subscription</param>
    /// <param name="autoAck">Whether the subscription should automatically acknowledge messages processed.
    /// If not set the receiver is required to explicitly acknowledge messages through the subscription.</param>
    /// <remarks>This will connect you to a persistent subscription group for a stream. The subscription group
    /// must first be created with CreatePersistentSubscriptionAsync many connections
    /// can connect to the same group and they will be treated as competing consumers within the group.
    /// If one connection dies work will be balanced across the rest of the consumers in the group. If
    /// you attempt to connect to a group that does not exist you will be given an exception.</remarks>
    /// <param name="verboseLogging">Enables verbose logging on the subscription</param>
    /// <returns>An <see cref="EventStorePersistentSubscriptionBase"/> representing the subscription</returns>
    public static Task<EventStorePersistentSubscriptionBase> ConnectToPersistentSubscriptionAsync(this IEventStoreConnectionBase connection,
      string stream, string groupName,
      Func<EventStorePersistentSubscriptionBase, ResolvedEvent, Task> eventAppearedAsync,
      Action<EventStorePersistentSubscriptionBase, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null, int bufferSize = 10, bool autoAck = true, bool verboseLogging = false)
    {
      var settings = new ConnectToPersistentSubscriptionSettings(bufferSize, autoAck, verboseLogging);
      return connection.ConnectToPersistentSubscriptionAsync(stream, groupName, settings, eventAppearedAsync, subscriptionDropped, userCredentials);
    }

    #endregion

    #region -- SubscribeToStream --

    /// <summary>Subscribes to a single event stream. New events
    /// written to the stream while the subscription is active will be pushed to the client.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="stream">The stream to subscribe to</param>
    /// <param name="resolveLinkTos">Whether to resolve Link events automatically</param>
    /// <param name="eventAppeared">An action invoked when a new event is received over the subscription</param>
    /// <param name="subscriptionDropped">An action invoked if the subscription is dropped</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <param name="verboseLogging">Enables verbose logging on the subscription</param>
    /// <returns>An <see cref="EventStoreSubscription"/> representing the subscription</returns>
    public static EventStoreSubscription SubscribeToStream(this IEventStoreConnectionBase connection, string stream, bool resolveLinkTos,
      Action<EventStoreSubscription, ResolvedEvent> eventAppeared,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null, bool verboseLogging = false)
    {
      var subscriptionSettings = new SubscriptionSettings { ResolveLinkTos = resolveLinkTos, VerboseLogging = verboseLogging };
      return AsyncContext.Run(
        async (conn, streamId, settings, eAppeared, subDropped, credentials)
          => await conn.SubscribeToStreamAsync(streamId, settings, eAppeared, subDropped, credentials).ConfigureAwait(false),
        connection, stream, subscriptionSettings, eventAppeared, subscriptionDropped, userCredentials);
    }

    /// <summary>Subscribes to a single event stream. New events
    /// written to the stream while the subscription is active will be pushed to the client.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="stream">The stream to subscribe to</param>
    /// <param name="resolveLinkTos">Whether to resolve Link events automatically</param>
    /// <param name="eventAppearedAsync">A Task invoked and awaited when a new event is received over the subscription</param>
    /// <param name="subscriptionDropped">An action invoked if the subscription is dropped</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <param name="verboseLogging">Enables verbose logging on the subscription</param>
    /// <returns>An <see cref="EventStoreSubscription"/> representing the subscription</returns>
    public static EventStoreSubscription SubscribeToStream(this IEventStoreConnectionBase connection, string stream, bool resolveLinkTos,
      Func<EventStoreSubscription, ResolvedEvent, Task> eventAppearedAsync,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null, bool verboseLogging = false)
    {
      var subscriptionSettings = new SubscriptionSettings { ResolveLinkTos = resolveLinkTos, VerboseLogging = verboseLogging };
      return AsyncContext.Run(
        async (conn, streamId, settings, eAppeared, subDropped, credentials)
          => await conn.SubscribeToStreamAsync(streamId, settings, eAppeared, subDropped, credentials).ConfigureAwait(false),
        connection, stream, subscriptionSettings, eventAppearedAsync, subscriptionDropped, userCredentials);
    }

    /// <summary>Subscribes to a single event stream. New events
    /// written to the stream while the subscription is active will be pushed to the client.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="stream">The stream to subscribe to</param>
    /// <param name="subscriptionSettings">The <see cref="SubscriptionSettings"/> for the subscription</param>
    /// <param name="eventAppeared">An action invoked when a new event is received over the subscription</param>
    /// <param name="subscriptionDropped">An action invoked if the subscription is dropped</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <returns>An <see cref="EventStoreSubscription"/> representing the subscription</returns>
    public static EventStoreSubscription SubscribeToStream(this IEventStoreConnectionBase connection,
      string stream, SubscriptionSettings subscriptionSettings,
      Action<EventStoreSubscription, ResolvedEvent> eventAppeared,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null)
    {
      return AsyncContext.Run(
        async (conn, streamId, settings, eAppeared, subDropped, credentials)
          => await conn.SubscribeToStreamAsync(streamId, settings, eAppeared, subDropped, credentials).ConfigureAwait(false),
        connection, stream, subscriptionSettings, eventAppeared, subscriptionDropped, userCredentials);
    }

    /// <summary>Subscribes to a single event stream. New events
    /// written to the stream while the subscription is active will be pushed to the client.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="stream">The stream to subscribe to</param>
    /// <param name="subscriptionSettings">The <see cref="SubscriptionSettings"/> for the subscription</param>
    /// <param name="eventAppearedAsync">A Task invoked and awaited when a new event is received over the subscription</param>
    /// <param name="subscriptionDropped">An action invoked if the subscription is dropped</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <returns>An <see cref="EventStoreSubscription"/> representing the subscription</returns>
    public static EventStoreSubscription SubscribeToStream(this IEventStoreConnectionBase connection,
      string stream, SubscriptionSettings subscriptionSettings,
      Func<EventStoreSubscription, ResolvedEvent, Task> eventAppearedAsync,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null)
    {
      return AsyncContext.Run(
        async (conn, streamId, settings, eAppeared, subDropped, credentials)
          => await conn.SubscribeToStreamAsync(streamId, settings, eAppeared, subDropped, credentials).ConfigureAwait(false),
        connection, stream, subscriptionSettings, eventAppearedAsync, subscriptionDropped, userCredentials);
    }

    #endregion

    #region -- SubscribeToStreamAsync --

    /// <summary>Asynchronously subscribes to a single event stream. New events
    /// written to the stream while the subscription is active will be pushed to the client.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="stream">The stream to subscribe to</param>
    /// <param name="resolveLinkTos">Whether to resolve Link events automatically</param>
    /// <param name="eventAppeared">An action invoked when a new event is received over the subscription</param>
    /// <param name="subscriptionDropped">An action invoked if the subscription is dropped</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <returns>An <see cref="EventStoreSubscription"/> representing the subscription</returns>
    public static Task<EventStoreSubscription> SubscribeToStreamAsync(this IEventStoreConnectionBase connection, string stream, bool resolveLinkTos,
      Action<EventStoreSubscription, ResolvedEvent> eventAppeared,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null)
    {
      var settings = new SubscriptionSettings { ResolveLinkTos = resolveLinkTos };
      return connection.SubscribeToStreamAsync(stream, settings, eventAppeared, subscriptionDropped, userCredentials);
    }

    /// <summary>Asynchronously subscribes to a single event stream. New events
    /// written to the stream while the subscription is active will be pushed to the client.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="stream">The stream to subscribe to</param>
    /// <param name="resolveLinkTos">Whether to resolve Link events automatically</param>
    /// <param name="eventAppearedAsync">A Task invoked and awaited when a new event is received over the subscription</param>
    /// <param name="subscriptionDropped">An action invoked if the subscription is dropped</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <returns>An <see cref="EventStoreSubscription"/> representing the subscription</returns>
    public static Task<EventStoreSubscription> SubscribeToStreamAsync(this IEventStoreConnectionBase connection, string stream, bool resolveLinkTos,
      Func<EventStoreSubscription, ResolvedEvent, Task> eventAppearedAsync,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null)
    {
      var settings = new SubscriptionSettings { ResolveLinkTos = resolveLinkTos };
      return connection.SubscribeToStreamAsync(stream, settings, eventAppearedAsync, subscriptionDropped, userCredentials);
    }

    #endregion

    #region -- SubscribeToStreamStart --

    /// <summary>Subscribes to a single event stream. Existing events from
    /// lastCheckpoint onwards are read from the stream
    /// and presented to the user of <see cref="EventStoreCatchUpSubscription"/>
    /// as if they had been pushed.
    ///
    /// Once the end of the stream is read the subscription is
    /// transparently (to the user) switched to push new events as
    /// they are written.
    ///
    /// The action liveProcessingStarted is called when the
    /// <see cref="EventStoreCatchUpSubscription"/> switches from the reading
    /// phase to the live subscription phase.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="stream">The stream to subscribe to</param>
    /// <param name="eventAppeared">An action invoked when an event is received over the subscription</param>
    /// <param name="liveProcessingStarted">An action invoked when the subscription switches to newly-pushed events</param>
    /// <param name="subscriptionDropped">An action invoked if the subscription is dropped</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <param name="settings">The <see cref="CatchUpSubscriptionSettings"/> for the subscription</param>
    /// <returns>An <see cref="EventStoreSubscription"/> representing the subscription</returns>
    public static EventStoreStreamCatchUpSubscription SubscribeToStreamStart(this IEventStoreConnectionBase connection,
      string stream, CatchUpSubscriptionSettings settings,
      Action<EventStoreCatchUpSubscription, ResolvedEvent> eventAppeared,
      Action<EventStoreCatchUpSubscription> liveProcessingStarted = null,
      Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null)
    {
      return connection.SubscribeToStreamFrom(stream, StreamPosition.Start, settings, eventAppeared, liveProcessingStarted, subscriptionDropped, userCredentials);
    }

    /// <summary>Subscribes to a single event stream. Existing events from
    /// lastCheckpoint onwards are read from the stream
    /// and presented to the user of <see cref="EventStoreCatchUpSubscription"/>
    /// as if they had been pushed.
    ///
    /// Once the end of the stream is read the subscription is
    /// transparently (to the user) switched to push new events as
    /// they are written.
    ///
    /// The action liveProcessingStarted is called when the
    /// <see cref="EventStoreCatchUpSubscription"/> switches from the reading
    /// phase to the live subscription phase.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="stream">The stream to subscribe to</param>
    /// <param name="eventAppearedAsync">A Task invoked and awaited when an event is received over the subscription</param>
    /// <param name="liveProcessingStarted">An action invoked when the subscription switches to newly-pushed events</param>
    /// <param name="subscriptionDropped">An action invoked if the subscription is dropped</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <param name="settings">The <see cref="CatchUpSubscriptionSettings"/> for the subscription</param>
    /// <returns>An <see cref="EventStoreSubscription"/> representing the subscription</returns>
    public static EventStoreStreamCatchUpSubscription SubscribeToStreamStart(this IEventStoreConnectionBase connection,
      string stream, CatchUpSubscriptionSettings settings,
      Func<EventStoreCatchUpSubscription, ResolvedEvent, Task> eventAppearedAsync,
      Action<EventStoreCatchUpSubscription> liveProcessingStarted = null,
      Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null)
    {
      return connection.SubscribeToStreamFrom(stream, StreamPosition.Start, settings, eventAppearedAsync, liveProcessingStarted, subscriptionDropped, userCredentials);
    }

    #endregion

    #region -- SubscribeToStreamEnd --

    /// <summary>Subscribes to a single event stream. Existing events from
    /// lastCheckpoint onwards are read from the stream
    /// and presented to the user of <see cref="EventStoreCatchUpSubscription"/>
    /// as if they had been pushed.
    ///
    /// Once the end of the stream is read the subscription is
    /// transparently (to the user) switched to push new events as
    /// they are written.
    ///
    /// The action liveProcessingStarted is called when the
    /// <see cref="EventStoreCatchUpSubscription"/> switches from the reading
    /// phase to the live subscription phase.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="stream">The stream to subscribe to</param>
    /// <param name="eventAppeared">An action invoked when an event is received over the subscription</param>
    /// <param name="liveProcessingStarted">An action invoked when the subscription switches to newly-pushed events</param>
    /// <param name="subscriptionDropped">An action invoked if the subscription is dropped</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <param name="subscriptionSettings">The <see cref="CatchUpSubscriptionSettings"/> for the subscription</param>
    /// <returns>An <see cref="EventStoreSubscription"/> representing the subscription</returns>
    public static EventStoreStreamCatchUpSubscription SubscribeToStreamEnd(this IEventStoreConnectionBase connection,
      string stream, CatchUpSubscriptionSettings subscriptionSettings,
      Action<EventStoreCatchUpSubscription, ResolvedEvent> eventAppeared,
      Action<EventStoreCatchUpSubscription> liveProcessingStarted = null,
      Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null)
    {
      return AsyncContext.Run(
        async (conn, streamId, settings, processingFunc, credentials)
          => await conn.SubscribeToStreamEndAsync(streamId, settings, processingFunc.Item1, processingFunc.Item2, processingFunc.Item3, credentials).ConfigureAwait(false),
        connection, stream, subscriptionSettings, Tuple.Create(eventAppeared, liveProcessingStarted, subscriptionDropped), userCredentials);
    }

    /// <summary>Subscribes to a single event stream. Existing events from
    /// lastCheckpoint onwards are read from the stream
    /// and presented to the user of <see cref="EventStoreCatchUpSubscription"/>
    /// as if they had been pushed.
    ///
    /// Once the end of the stream is read the subscription is
    /// transparently (to the user) switched to push new events as
    /// they are written.
    ///
    /// The action liveProcessingStarted is called when the
    /// <see cref="EventStoreCatchUpSubscription"/> switches from the reading
    /// phase to the live subscription phase.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="stream">The stream to subscribe to</param>
    /// <param name="eventAppearedAsync">A Task invoked and awaited when an event is received over the subscription</param>
    /// <param name="liveProcessingStarted">An action invoked when the subscription switches to newly-pushed events</param>
    /// <param name="subscriptionDropped">An action invoked if the subscription is dropped</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <param name="subscriptionSettings">The <see cref="CatchUpSubscriptionSettings"/> for the subscription</param>
    /// <returns>An <see cref="EventStoreSubscription"/> representing the subscription</returns>
    public static EventStoreStreamCatchUpSubscription SubscribeToStreamEnd(this IEventStoreConnectionBase connection,
      string stream, CatchUpSubscriptionSettings subscriptionSettings,
      Func<EventStoreCatchUpSubscription, ResolvedEvent, Task> eventAppearedAsync,
      Action<EventStoreCatchUpSubscription> liveProcessingStarted = null,
      Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null)
    {
      return AsyncContext.Run(
        async (conn, streamId, settings, processingFunc, credentials)
          => await conn.SubscribeToStreamEndAsync(streamId, settings, processingFunc.Item1, processingFunc.Item2, processingFunc.Item3, credentials).ConfigureAwait(false),
        connection, stream, subscriptionSettings, Tuple.Create(eventAppearedAsync, liveProcessingStarted, subscriptionDropped), userCredentials);
    }

    #endregion

    #region -- SubscribeToStreamEndAsync --

    /// <summary>Asynchronously subscribes to a single event stream. Existing events from
    /// lastCheckpoint onwards are read from the stream
    /// and presented to the user of <see cref="EventStoreCatchUpSubscription"/>
    /// as if they had been pushed.
    ///
    /// Once the end of the stream is read the subscription is
    /// transparently (to the user) switched to push new events as
    /// they are written.
    ///
    /// The action liveProcessingStarted is called when the
    /// <see cref="EventStoreCatchUpSubscription"/> switches from the reading
    /// phase to the live subscription phase.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="stream">The stream to subscribe to</param>
    /// <param name="eventAppeared">An action invoked when an event is received over the subscription</param>
    /// <param name="liveProcessingStarted">An action invoked when the subscription switches to newly-pushed events</param>
    /// <param name="subscriptionDropped">An action invoked if the subscription is dropped</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <param name="settings">The <see cref="CatchUpSubscriptionSettings"/> for the subscription</param>
    /// <returns>An <see cref="EventStoreSubscription"/> representing the subscription</returns>
    public static async Task<EventStoreStreamCatchUpSubscription> SubscribeToStreamEndAsync(this IEventStoreConnectionBase connection,
      string stream, CatchUpSubscriptionSettings settings,
      Action<EventStoreCatchUpSubscription, ResolvedEvent> eventAppeared,
      Action<EventStoreCatchUpSubscription> liveProcessingStarted = null,
      Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null)
    {
      if (null == settings) { throw new ArgumentNullException(nameof(settings)); }

      long lastCheckpoint = StreamPosition.Start;
      var readResult = await ReadLastEventAsync(connection, stream, settings.ResolveLinkTos, userCredentials);
      if (EventReadStatus.Success == readResult.Status)
      {
        lastCheckpoint = readResult.EventNumber;
      }

      return connection.SubscribeToStreamFrom(stream, lastCheckpoint, settings, eventAppeared, liveProcessingStarted, subscriptionDropped, userCredentials);
    }

    /// <summary>Asynchronously subscribes to a single event stream. Existing events from
    /// lastCheckpoint onwards are read from the stream
    /// and presented to the user of <see cref="EventStoreCatchUpSubscription"/>
    /// as if they had been pushed.
    ///
    /// Once the end of the stream is read the subscription is
    /// transparently (to the user) switched to push new events as
    /// they are written.
    ///
    /// The action liveProcessingStarted is called when the
    /// <see cref="EventStoreCatchUpSubscription"/> switches from the reading
    /// phase to the live subscription phase.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="stream">The stream to subscribe to</param>
    /// <param name="eventAppearedAsync">A Task invoked and awaited when an event is received over the subscription</param>
    /// <param name="liveProcessingStarted">An action invoked when the subscription switches to newly-pushed events</param>
    /// <param name="subscriptionDropped">An action invoked if the subscription is dropped</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <param name="settings">The <see cref="CatchUpSubscriptionSettings"/> for the subscription</param>
    /// <returns>An <see cref="EventStoreSubscription"/> representing the subscription</returns>
    public static async Task<EventStoreStreamCatchUpSubscription> SubscribeToStreamEndAsync(this IEventStoreConnectionBase connection,
      string stream, CatchUpSubscriptionSettings settings,
      Func<EventStoreCatchUpSubscription, ResolvedEvent, Task> eventAppearedAsync,
      Action<EventStoreCatchUpSubscription> liveProcessingStarted = null,
      Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null)
    {
      if (null == settings) { throw new ArgumentNullException(nameof(settings)); }

      long lastCheckpoint = StreamPosition.Start;
      var readResult = await ReadLastEventAsync(connection, stream, settings.ResolveLinkTos, userCredentials);
      if (EventReadStatus.Success == readResult.Status)
      {
        lastCheckpoint = readResult.EventNumber;
      }

      return connection.SubscribeToStreamFrom(stream, lastCheckpoint, settings, eventAppearedAsync, liveProcessingStarted, subscriptionDropped, userCredentials);
    }

    #endregion

    #region -- SubscribeToStreamFrom --

    /// <summary>Subscribes to a single event stream. Existing events from
    /// lastCheckpoint onwards are read from the stream
    /// and presented to the user of <see cref="EventStoreCatchUpSubscription"/>
    /// as if they had been pushed.
    ///
    /// Once the end of the stream is read the subscription is
    /// transparently (to the user) switched to push new events as
    /// they are written.
    ///
    /// The action liveProcessingStarted is called when the
    /// <see cref="EventStoreCatchUpSubscription"/> switches from the reading
    /// phase to the live subscription phase.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="stream">The stream to subscribe to</param>
    /// <param name="lastCheckpoint">The event number from which to start.
    ///
    /// To receive all events in the stream, use <see cref="StreamCheckpoint.StreamStart" />.
    /// If events have already been received and resubscription from the same point
    /// is desired, use the event number of the last event processed which
    /// appeared on the subscription.
    ///
    /// NOTE: Using <see cref="StreamPosition.Start" /> here will result in missing
    /// the first event in the stream.</param>
    /// <param name="resolveLinkTos">Whether to resolve Link events automatically</param>
    /// <param name="eventAppeared">An action invoked when an event is received over the subscription</param>
    /// <param name="liveProcessingStarted">An action invoked when the subscription switches to newly-pushed events</param>
    /// <param name="subscriptionDropped">An action invoked if the subscription is dropped</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <param name="readBatchSize">The batch size to use during the read phase</param>
    /// <param name="subscriptionName">The name of subscription</param>
    /// <param name="verboseLogging">Enables verbose logging on the subscription</param>
    /// <returns>An <see cref="EventStoreSubscription"/> representing the subscription</returns>
    //[Obsolete("This method will be obsoleted in the next major version please switch to the overload with a settings object")]
    public static EventStoreStreamCatchUpSubscription SubscribeToStreamFrom(this IEventStoreConnectionBase connection,
      string stream, long? lastCheckpoint, bool resolveLinkTos,
      Action<EventStoreCatchUpSubscription, ResolvedEvent> eventAppeared,
      Action<EventStoreCatchUpSubscription> liveProcessingStarted = null,
      Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null, int readBatchSize = 500, string subscriptionName = "", bool verboseLogging = false)
    {
      var settings = CatchUpSubscriptionSettings.Create(readBatchSize, resolveLinkTos, subscriptionName, verboseLogging);

      return connection.SubscribeToStreamFrom(stream, lastCheckpoint, settings, eventAppeared, liveProcessingStarted, subscriptionDropped, userCredentials);
    }

    /// <summary>Subscribes to a single event stream. Existing events from
    /// lastCheckpoint onwards are read from the stream
    /// and presented to the user of <see cref="EventStoreCatchUpSubscription"/>
    /// as if they had been pushed.
    ///
    /// Once the end of the stream is read the subscription is
    /// transparently (to the user) switched to push new events as
    /// they are written.
    ///
    /// The action liveProcessingStarted is called when the
    /// <see cref="EventStoreCatchUpSubscription"/> switches from the reading
    /// phase to the live subscription phase.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="stream">The stream to subscribe to</param>
    /// <param name="lastCheckpoint">The event number from which to start.
    ///
    /// To receive all events in the stream, use <see cref="StreamCheckpoint.StreamStart" />.
    /// If events have already been received and resubscription from the same point
    /// is desired, use the event number of the last event processed which
    /// appeared on the subscription.
    ///
    /// NOTE: Using <see cref="StreamPosition.Start" /> here will result in missing
    /// the first event in the stream.</param>
    /// <param name="resolveLinkTos">Whether to resolve Link events automatically</param>
    /// <param name="eventAppearedAsync">A Task invoked and awaited when an event is received over the subscription</param>
    /// <param name="liveProcessingStarted">An action invoked when the subscription switches to newly-pushed events</param>
    /// <param name="subscriptionDropped">An action invoked if the subscription is dropped</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <param name="readBatchSize">The batch size to use during the read phase</param>
    /// <param name="subscriptionName">The name of subscription</param>
    /// <param name="verboseLogging">Enables verbose logging on the subscription</param>
    /// <returns>An <see cref="EventStoreSubscription"/> representing the subscription</returns>
    public static EventStoreStreamCatchUpSubscription SubscribeToStreamFrom(this IEventStoreConnectionBase connection,
      string stream, long? lastCheckpoint, bool resolveLinkTos,
      Func<EventStoreCatchUpSubscription, ResolvedEvent, Task> eventAppearedAsync,
      Action<EventStoreCatchUpSubscription> liveProcessingStarted = null,
      Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null, int readBatchSize = 500, string subscriptionName = "", bool verboseLogging = false)
    {
      var settings = CatchUpSubscriptionSettings.Create(readBatchSize, resolveLinkTos, subscriptionName, verboseLogging);

      return connection.SubscribeToStreamFrom(stream, lastCheckpoint, settings, eventAppearedAsync, liveProcessingStarted, subscriptionDropped, userCredentials);
    }

    #endregion

    #region -- SubscribeToAll --

    /// <summary>Subscribes to all events in the Event Store. New
    /// events written to the stream while the subscription is active
    /// will be pushed to the client.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnection"/> responsible for raising the event.</param>
    /// <param name="resolveLinkTos">Whether to resolve Link events automatically</param>
    /// <param name="eventAppeared">An action invoked when a new event is received over the subscription</param>
    /// <param name="subscriptionDropped">An action invoked if the subscription is dropped</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <param name="verboseLogging">Enables verbose logging on the subscription</param>
    /// <returns>An <see cref="EventStoreSubscription"/> representing the subscription</returns>
    public static EventStoreSubscription SubscribeToAll(this IEventStoreConnection connection, bool resolveLinkTos,
      Action<EventStoreSubscription, ResolvedEvent> eventAppeared,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null, bool verboseLogging = false)
    {
      var settings = new SubscriptionSettings { ResolveLinkTos = resolveLinkTos, VerboseLogging = verboseLogging };
      return connection.SubscribeToAllAsync(settings, eventAppeared, subscriptionDropped, userCredentials)
                       .ConfigureAwait(false).GetAwaiter().GetResult();
    }

    /// <summary>Subscribes to all events in the Event Store. New
    /// events written to the stream while the subscription is active
    /// will be pushed to the client.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnection"/> responsible for raising the event.</param>
    /// <param name="resolveLinkTos">Whether to resolve Link events automatically</param>
    /// <param name="eventAppearedAsync">A Task invoked and awaited when a new event is received over the subscription</param>
    /// <param name="subscriptionDropped">An action invoked if the subscription is dropped</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <param name="verboseLogging">Enables verbose logging on the subscription</param>
    /// <returns>An <see cref="EventStoreSubscription"/> representing the subscription</returns>
    public static EventStoreSubscription SubscribeToAll(this IEventStoreConnection connection, bool resolveLinkTos,
      Func<EventStoreSubscription, ResolvedEvent, Task> eventAppearedAsync,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null, bool verboseLogging = false)
    {
      var settings = new SubscriptionSettings { ResolveLinkTos = resolveLinkTos, VerboseLogging = verboseLogging };
      return connection.SubscribeToAllAsync(settings, eventAppearedAsync, subscriptionDropped, userCredentials)
                       .ConfigureAwait(false).GetAwaiter().GetResult();
    }

    /// <summary>Subscribes to all events in the Event Store. New
    /// events written to the stream while the subscription is active
    /// will be pushed to the client.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnection"/> responsible for raising the event.</param>
    /// <param name="settings">The <see cref="SubscriptionSettings"/> for the subscription</param>
    /// <param name="eventAppeared">An action invoked when a new event is received over the subscription</param>
    /// <param name="subscriptionDropped">An action invoked if the subscription is dropped</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <returns>An <see cref="EventStoreSubscription"/> representing the subscription</returns>
    public static EventStoreSubscription SubscribeToAll(this IEventStoreConnection connection, SubscriptionSettings settings,
      Action<EventStoreSubscription, ResolvedEvent> eventAppeared,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null)
    {
      return connection.SubscribeToAllAsync(settings, eventAppeared, subscriptionDropped, userCredentials)
                       .ConfigureAwait(false).GetAwaiter().GetResult();
    }

    /// <summary>Subscribes to all events in the Event Store. New
    /// events written to the stream while the subscription is active
    /// will be pushed to the client.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnection"/> responsible for raising the event.</param>
    /// <param name="settings">The <see cref="SubscriptionSettings"/> for the subscription</param>
    /// <param name="eventAppearedAsync">A Task invoked and awaited when a new event is received over the subscription</param>
    /// <param name="subscriptionDropped">An action invoked if the subscription is dropped</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <returns>An <see cref="EventStoreSubscription"/> representing the subscription</returns>
    public static EventStoreSubscription SubscribeToAll(this IEventStoreConnection connection, SubscriptionSettings settings,
      Func<EventStoreSubscription, ResolvedEvent, Task> eventAppearedAsync,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null)
    {
      return connection.SubscribeToAllAsync(settings, eventAppearedAsync, subscriptionDropped, userCredentials)
                       .ConfigureAwait(false).GetAwaiter().GetResult();
    }

    #endregion

    #region -- SubscribeToAllAsync --

    /// <summary>Asynchronously subscribes to all events in the Event Store. New
    /// events written to the stream while the subscription is active
    /// will be pushed to the client.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnection"/> responsible for raising the event.</param>
    /// <param name="resolveLinkTos">Whether to resolve Link events automatically</param>
    /// <param name="eventAppeared">An action invoked when a new event is received over the subscription</param>
    /// <param name="subscriptionDropped">An action invoked if the subscription is dropped</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <returns>An <see cref="EventStoreSubscription"/> representing the subscription</returns>
    public static Task<EventStoreSubscription> SubscribeToAllAsync(this IEventStoreConnection connection, bool resolveLinkTos,
      Action<EventStoreSubscription, ResolvedEvent> eventAppeared,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null)
    {
      var settings = new SubscriptionSettings { ResolveLinkTos = resolveLinkTos };
      return connection.SubscribeToAllAsync(settings, eventAppeared, subscriptionDropped, userCredentials);
    }

    /// <summary>Asynchronously subscribes to all events in the Event Store. New
    /// events written to the stream while the subscription is active
    /// will be pushed to the client.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnection"/> responsible for raising the event.</param>
    /// <param name="resolveLinkTos">Whether to resolve Link events automatically</param>
    /// <param name="eventAppearedAsync">A Task invoked and awaited when a new event is received over the subscription</param>
    /// <param name="subscriptionDropped">An action invoked if the subscription is dropped</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <returns>An <see cref="EventStoreSubscription"/> representing the subscription</returns>
    public static Task<EventStoreSubscription> SubscribeToAllAsync(this IEventStoreConnection connection, bool resolveLinkTos,
      Func<EventStoreSubscription, ResolvedEvent, Task> eventAppearedAsync,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null)
    {
      var settings = new SubscriptionSettings { ResolveLinkTos = resolveLinkTos };
      return connection.SubscribeToAllAsync(settings, eventAppearedAsync, subscriptionDropped, userCredentials);
    }

    #endregion

    #region -- SubscribeToAllFrom --

    /// <summary>Subscribes to all events. Existing events from lastCheckpoint
    /// onwards are read from the Event Store and presented to the user of
    /// <see cref="EventStoreCatchUpSubscription"/> as if they had been pushed.
    ///
    /// Once the end of the stream is read the subscription is
    /// transparently (to the user) switched to push new events as
    /// they are written.
    ///
    /// The action liveProcessingStarted is called when the
    /// <see cref="EventStoreCatchUpSubscription"/> switches from the reading
    /// phase to the live subscription phase.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnection"/> responsible for raising the event.</param>
    /// <param name="lastCheckpoint">The position from which to start.
    ///
    /// To receive all events in the database, use <see cref="AllCheckpoint.AllStart" />.
    /// If events have already been received and resubscription from the same point
    /// is desired, use the position representing the last event processed which
    /// appeared on the subscription.
    ///
    /// NOTE: Using <see cref="Position.Start" /> here will result in missing
    /// the first event in the stream.</param>
    /// <param name="resolveLinkTos">Whether to resolve Link events automatically</param>
    /// <param name="eventAppeared">An action invoked when an event is received over the subscription</param>
    /// <param name="liveProcessingStarted">An action invoked when the subscription switches to newly-pushed events</param>
    /// <param name="subscriptionDropped">An action invoked if the subscription is dropped</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <param name="readBatchSize">The batch size to use during the read phase</param>
    /// <param name="subscriptionName">The name of subscription</param>
    /// <param name="verboseLogging">Enables verbose logging on the subscription</param>
    /// <returns>An <see cref="EventStoreSubscription"/> representing the subscription</returns>
    //[Obsolete("This overload will be removed in the next major release please use the overload with a settings object")]
    public static EventStoreAllCatchUpSubscription SubscribeToAllFrom(this IEventStoreConnection connection,
      Position? lastCheckpoint, bool resolveLinkTos,
      Action<EventStoreCatchUpSubscription, ResolvedEvent> eventAppeared,
      Action<EventStoreCatchUpSubscription> liveProcessingStarted = null,
      Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null, int readBatchSize = 500, string subscriptionName = "", bool verboseLogging = false)
    {
      var settings = CatchUpSubscriptionSettings.Create(readBatchSize, resolveLinkTos, subscriptionName, verboseLogging);
      return connection.SubscribeToAllFrom(lastCheckpoint, settings, eventAppeared, liveProcessingStarted, subscriptionDropped, userCredentials);
    }

    /// <summary>Subscribes to all events. Existing events from lastCheckpoint
    /// onwards are read from the Event Store and presented to the user of
    /// <see cref="EventStoreCatchUpSubscription"/> as if they had been pushed.
    ///
    /// Once the end of the stream is read the subscription is
    /// transparently (to the user) switched to push new events as
    /// they are written.
    ///
    /// The action liveProcessingStarted is called when the
    /// <see cref="EventStoreCatchUpSubscription"/> switches from the reading
    /// phase to the live subscription phase.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnection"/> responsible for raising the event.</param>
    /// <param name="lastCheckpoint">The position from which to start.
    ///
    /// To receive all events in the database, use <see cref="AllCheckpoint.AllStart" />.
    /// If events have already been received and resubscription from the same point
    /// is desired, use the position representing the last event processed which
    /// appeared on the subscription.
    ///
    /// NOTE: Using <see cref="Position.Start" /> here will result in missing
    /// the first event in the stream.</param>
    /// <param name="resolveLinkTos">Whether to resolve Link events automatically</param>
    /// <param name="eventAppearedAsync">A Task invoked and awaited when an event is received over the subscription</param>
    /// <param name="liveProcessingStarted">An action invoked when the subscription switches to newly-pushed events</param>
    /// <param name="subscriptionDropped">An action invoked if the subscription is dropped</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <param name="readBatchSize">The batch size to use during the read phase</param>
    /// <param name="subscriptionName">The name of subscription</param>
    /// <param name="verboseLogging">Enables verbose logging on the subscription</param>
    /// <returns>An <see cref="EventStoreSubscription"/> representing the subscription</returns>
    public static EventStoreAllCatchUpSubscription SubscribeToAllFrom(this IEventStoreConnection connection,
      Position? lastCheckpoint, bool resolveLinkTos,
      Func<EventStoreCatchUpSubscription, ResolvedEvent, Task> eventAppearedAsync,
      Action<EventStoreCatchUpSubscription> liveProcessingStarted = null,
      Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null, int readBatchSize = 500, string subscriptionName = "", bool verboseLogging = false)
    {
      var settings = CatchUpSubscriptionSettings.Create(readBatchSize, resolveLinkTos, subscriptionName, verboseLogging);
      return connection.SubscribeToAllFrom(lastCheckpoint, settings, eventAppearedAsync, liveProcessingStarted, subscriptionDropped, userCredentials);
    }

    #endregion
  }
}
