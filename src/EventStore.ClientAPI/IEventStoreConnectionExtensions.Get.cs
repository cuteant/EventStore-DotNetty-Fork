using System;
using System.Threading.Tasks;
using CuteAnt.AsyncEx;
using EventStore.ClientAPI.SystemData;

namespace EventStore.ClientAPI
{
  partial class IEventStoreConnectionExtensions
  {
    #region -- Get event(s) --

    /// <summary>Asynchronously reads a single event from a stream.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase2"/> responsible for raising the event.</param>
    /// <param name="stream">The stream to read from</param>
    /// <param name="eventNumber">The event number to read, <see cref="StreamPosition">StreamPosition.End</see> to read the last event in the stream</param>
    /// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically</param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    /// <returns>A <see cref="EventReadResult"/> containing the results of the read operation.</returns>
    public static EventReadResult<object> GetEvent(this IEventStoreConnectionBase2 connection,
      string stream, long eventNumber, bool resolveLinkTos, UserCredentials userCredentials = null)
    {
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }
      return AsyncContext.Run(
                async (conn, streamId, eventNum, resolveLinkToEvents, credentials)
                  => await conn.GetEventAsync(streamId, eventNum, resolveLinkToEvents, credentials).ConfigureAwait(false),
                connection, stream, eventNumber, resolveLinkTos, userCredentials);
    }

    /// <summary>Reads count Events from an Event Stream forwards (e.g. oldest to newest) starting from position start.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase2"/> responsible for raising the event.</param>
    /// <param name="stream">The stream to read from</param>
    /// <param name="start">The starting point to read from</param>
    /// <param name="count">The count of items to read</param>
    /// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically</param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    /// <returns>A <see cref="StreamEventsSlice"/> containing the results of the read operation.</returns>
    public static StreamEventsSlice<object> GetStreamEventsForward(this IEventStoreConnectionBase2 connection,
      string stream, long start, int count, bool resolveLinkTos, UserCredentials userCredentials = null)
    {
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }
      return AsyncContext.Run(
                async (conn, streamId, pointer, eventCount, resolveLinkToEvents, credentials)
                  => await conn.GetStreamEventsForwardAsync(streamId, pointer, eventCount, resolveLinkToEvents, credentials).ConfigureAwait(false),
                connection, stream, start, count, resolveLinkTos, userCredentials);
    }

    /// <summary>Reads count events from an Event Stream backwards (e.g. newest to oldest) from position asynchronously.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase2"/> responsible for raising the event.</param>
    /// <param name="stream">The Event Stream to read from</param>
    /// <param name="start">The position to start reading from</param>
    /// <param name="count">The count to read from the position</param>
    /// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically</param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    /// <returns>A <see cref="StreamEventsSlice"/> containing the results of the read operation.</returns>
    public static StreamEventsSlice<object> GetStreamEventsBackward(this IEventStoreConnectionBase2 connection,
      string stream, long start, int count, bool resolveLinkTos, UserCredentials userCredentials = null)
    {
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }
      return AsyncContext.Run(
                async (conn, streamId, pointer, eventCount, resolveLinkToEvents, credentials)
                  => await conn.GetStreamEventsBackwardAsync(streamId, pointer, eventCount, resolveLinkToEvents, credentials).ConfigureAwait(false),
                connection, stream, start, count, resolveLinkTos, userCredentials);
    }

    /// <summary>Asynchronously reads a single event from a stream.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase2"/> responsible for raising the event.</param>
    /// <param name="eventNumber">The event number to read, <see cref="StreamPosition">StreamPosition.End</see> to read the last event in the stream</param>
    /// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically</param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    /// <returns>A <see cref="StreamEventsSlice"/> containing the results of the read operation.</returns>
    public static EventReadResult<TEvent> GetEvent<TEvent>(this IEventStoreConnectionBase2 connection,
      long eventNumber, bool resolveLinkTos, UserCredentials userCredentials = null) where TEvent : class
    {
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }
      return AsyncContext.Run(
                async (conn, eventNum, resolveLinkToEvents, credentials)
                  => await conn.GetEventAsync<TEvent>(eventNum, resolveLinkToEvents, credentials).ConfigureAwait(false),
                connection, eventNumber, resolveLinkTos, userCredentials);
    }

    /// <summary>Reads count Events from an Event Stream forwards (e.g. oldest to newest) starting from position start.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase2"/> responsible for raising the event.</param>
    /// <param name="start">The starting point to read from</param>
    /// <param name="count">The count of items to read</param>
    /// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically</param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    /// <returns>A <see cref="StreamEventsSlice"/> containing the results of the read operation.</returns>
    public static StreamEventsSlice<TEvent> GetStreamEventsForward<TEvent>(this IEventStoreConnectionBase2 connection,
      long start, int count, bool resolveLinkTos, UserCredentials userCredentials = null) where TEvent : class
    {
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }
      return AsyncContext.Run(
                async (conn, pointer, eventCount, resolveLinkToEvents, credentials)
                  => await conn.GetStreamEventsForwardAsync<TEvent>(pointer, eventCount, resolveLinkToEvents, credentials).ConfigureAwait(false),
                connection, start, count, resolveLinkTos, userCredentials);
    }

    /// <summary>Reads count events from an Event Stream backwards (e.g. newest to oldest) from position asynchronously.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase2"/> responsible for raising the event.</param>
    /// <param name="start">The position to start reading from</param>
    /// <param name="count">The count to read from the position</param>
    /// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically</param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    /// <returns>A <see cref="StreamEventsSlice"/> containing the results of the read operation.</returns>
    public static StreamEventsSlice<TEvent> GetStreamEventsBackward<TEvent>(this IEventStoreConnectionBase2 connection,
      long start, int count, bool resolveLinkTos, UserCredentials userCredentials = null) where TEvent : class
    {
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }
      return AsyncContext.Run(
                async (conn, pointer, eventCount, resolveLinkToEvents, credentials)
                  => await conn.GetStreamEventsBackwardAsync<TEvent>(pointer, eventCount, resolveLinkToEvents, credentials).ConfigureAwait(false),
                connection, start, count, resolveLinkTos, userCredentials);
    }

    #endregion

    #region -- GetFirstEvent --

    /// <summary>Reads the frist event from a stream.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase2"/> responsible for raising the event.</param>
    /// <param name="stream">The stream to read from</param>
    /// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically</param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    /// <returns>A result of the read operation</returns>
    public static EventReadResult<object> GetFirstEvent(this IEventStoreConnectionBase2 connection,
      string stream, bool resolveLinkTos, UserCredentials userCredentials = null)
    {
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }

      return AsyncContext.Run(
        async (conn, streamId, eventNum, resolveLinkToEvents, credentials)
          => await conn.GetEventAsync(streamId, eventNum, resolveLinkToEvents, credentials).ConfigureAwait(false),
        connection, stream, StreamPosition.Start, resolveLinkTos, userCredentials);
    }

    /// <summary>Reads the frist event from a stream.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase2"/> responsible for raising the event.</param>
    /// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically</param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    /// <returns>A result of the read operation</returns>
    public static EventReadResult<TEvent> GetFirstEvent<TEvent>(this IEventStoreConnectionBase2 connection,
      bool resolveLinkTos, UserCredentials userCredentials = null)
      where TEvent : class
    {
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }

      return AsyncContext.Run(
        async (conn, eventNum, resolveLinkToEvents, credentials)
          => await conn.GetEventAsync<TEvent>(eventNum, resolveLinkToEvents, credentials).ConfigureAwait(false),
        connection, StreamPosition.Start, resolveLinkTos, userCredentials);
    }

    #endregion

    #region -- GetFirstEventAsync --

    /// <summary>Asynchronously reads the frist event from a stream.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase2"/> responsible for raising the event.</param>
    /// <param name="stream">The stream to read from</param>
    /// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically</param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    /// <returns>A <see cref="Task&lt;EventReadResult&gt;"/> containing the results of the read operation</returns>
    public static Task<EventReadResult<object>> GetFirstEventAsync(this IEventStoreConnectionBase2 connection,
      string stream, bool resolveLinkTos, UserCredentials userCredentials = null)
    {
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }

      return connection.GetEventAsync(stream, StreamPosition.Start, resolveLinkTos, userCredentials);
    }

    /// <summary>Asynchronously reads the frist event from a stream.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase2"/> responsible for raising the event.</param>
    /// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically</param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    /// <returns>A <see cref="Task&lt;EventReadResult&gt;"/> containing the results of the read operation</returns>
    public static Task<EventReadResult<TEvent>> GetFirstEventAsync<TEvent>(this IEventStoreConnectionBase2 connection,
      bool resolveLinkTos, UserCredentials userCredentials = null)
      where TEvent : class
    {
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }

      return connection.GetEventAsync<TEvent>(StreamPosition.Start, resolveLinkTos, userCredentials);
    }

    #endregion

    #region -- GetLastEvent --

    /// <summary>Reads the last event from a stream.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase2"/> responsible for raising the event.</param>
    /// <param name="stream">The stream to read from</param>
    /// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically</param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    /// <returns>A <see cref="Task&lt;EventReadResult&gt;"/> containing the results of the read operation</returns>
    public static EventReadResult<object> GetLastEvent(this IEventStoreConnectionBase2 connection,
      string stream, bool resolveLinkTos, UserCredentials userCredentials = null)
    {
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }
      return AsyncContext.Run(
        async (conn, streamId, resolveLinkToEvents, credentials)
          => await conn.GetLastEventAsync(streamId, resolveLinkToEvents, credentials).ConfigureAwait(false),
        connection, stream, resolveLinkTos, userCredentials);
    }

    /// <summary>Reads the last event from a stream.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase2"/> responsible for raising the event.</param>
    /// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically</param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    /// <returns>A <see cref="Task&lt;EventReadResult&gt;"/> containing the results of the read operation</returns>
    public static EventReadResult<TEvent> GetLastEvent<TEvent>(this IEventStoreConnectionBase2 connection,
      bool resolveLinkTos, UserCredentials userCredentials = null)
      where TEvent : class
    {
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }
      return AsyncContext.Run(
        async (conn, resolveLinkToEvents, credentials)
          => await conn.GetLastEventAsync<TEvent>(resolveLinkToEvents, credentials).ConfigureAwait(false),
        connection, resolveLinkTos, userCredentials);
    }

    #endregion

    #region -- GetLastEventAsync --

    /// <summary>Asynchronously reads the last event from a stream.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase2"/> responsible for raising the event.</param>
    /// <param name="stream">The stream to read from</param>
    /// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically</param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    /// <returns>A <see cref="Task&lt;EventReadResult&gt;"/> containing the results of the read operation</returns>
    public static async Task<EventReadResult<object>> GetLastEventAsync(this IEventStoreConnectionBase2 connection,
      string stream, bool resolveLinkTos, UserCredentials userCredentials = null)
    {
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }

      var slice = await connection.GetStreamEventsBackwardAsync(stream, StreamPosition.End, 1, resolveLinkTos, userCredentials)
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
        return new EventReadResult<object>(readStatus, slice.Stream, lastEvent.OriginalEventNumber, lastEvent);
      }
      else
      {
        return new EventReadResult<object>(readStatus, slice.Stream, -1, null);
      }
    }

    /// <summary>Asynchronously reads the last event from a stream.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase2"/> responsible for raising the event.</param>
    /// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically</param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    /// <returns>A <see cref="Task&lt;EventReadResult&gt;"/> containing the results of the read operation</returns>
    public static async Task<EventReadResult<TEvent>> GetLastEventAsync<TEvent>(this IEventStoreConnectionBase2 connection,
      bool resolveLinkTos, UserCredentials userCredentials = null)
      where TEvent : class
    {
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }

      var slice = await connection.GetStreamEventsBackwardAsync<TEvent>(StreamPosition.End, 1, resolveLinkTos, userCredentials)
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
        return new EventReadResult<TEvent>(readStatus, slice.Stream, lastEvent.OriginalEventNumber, lastEvent);
      }
      else
      {
        return new EventReadResult<TEvent>(readStatus, slice.Stream, -1, null);
      }
    }

    #endregion
  }
}
