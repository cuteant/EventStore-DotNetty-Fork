using System;
using System.Collections.Generic;
using CuteAnt.AsyncEx;
using EventStore.ClientAPI.SystemData;

namespace EventStore.ClientAPI
{
  partial class IEventStoreConnectionExtensions
  {
    #region -- AppendToStream --

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
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }
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
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }
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
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }
      return AsyncContext.Run(
                async (conn, streamId, version, eventList, credentials)
                  => await conn.AppendToStreamAsync(streamId, version, eventList, credentials).ConfigureAwait(false),
                connection, stream, expectedVersion, events, userCredentials);
    }

    #endregion

    #region -- ConditionalAppendToStream --

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
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }
      return AsyncContext.Run(
                async (conn, streamId, version, eventList, credentials)
                  => await conn.ConditionalAppendToStreamAsync(streamId, version, eventList, credentials).ConfigureAwait(false),
                connection, stream, expectedVersion, events, userCredentials);
    }

    #endregion
  }
}
