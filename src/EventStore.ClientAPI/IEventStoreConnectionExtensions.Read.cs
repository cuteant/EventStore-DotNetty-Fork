using System;
using System.Threading.Tasks;
using CuteAnt.AsyncEx;
using EventStore.ClientAPI.SystemData;

namespace EventStore.ClientAPI
{
  partial class IEventStoreConnectionExtensions
  {
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
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }
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
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }
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
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }
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
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }
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
        return new EventReadResult(readStatus, slice.Stream, lastEvent.OriginalEventNumber, lastEvent);
      }
      else
      {
        return new EventReadResult(readStatus, slice.Stream, -1, null);
      }
    }

    #endregion
  }
}
