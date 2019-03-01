using System;
using System.Threading.Tasks;
using CuteAnt.AsyncEx;
using EventStore.ClientAPI.SystemData;

namespace EventStore.ClientAPI
{
  partial class IEventStoreConnectionExtensions
  {
    #region -- GetEvent --

    /// <summary>Asynchronously reads a single event from a stream.</summary>
    /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
    /// <param name="stream">The stream to read from.</param>
    /// <param name="eventNumber">The event number to read, <see cref="StreamPosition">StreamPosition.End</see> to read the last event in the stream</param>
    /// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically.</param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    /// <returns>A <see cref="EventReadResult"/> containing the results of the read operation.</returns>
    public static EventReadResult<object> GetEvent(this IEventStoreBus bus,
      string stream, long eventNumber, bool resolveLinkTos, UserCredentials userCredentials = null)
    {
      if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }
      return AsyncContext.Run(
                async (conn, streamId, eventNum, resolveLinkToEvents, credentials)
                  => await conn.GetEventAsync(streamId, eventNum, resolveLinkToEvents, credentials).ConfigureAwait(false),
                bus, stream, eventNumber, resolveLinkTos, userCredentials);
    }

    /// <summary>Asynchronously reads a single event from a stream.</summary>
    /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
    /// <param name="eventNumber">The event number to read, <see cref="StreamPosition">StreamPosition.End</see> to read the last event in the stream</param>
    /// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically.</param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    /// <returns>A <see cref="StreamEventsSlice"/> containing the results of the read operation.</returns>
    public static EventReadResult<TEvent> GetEvent<TEvent>(this IEventStoreBus bus,
      long eventNumber, bool resolveLinkTos, UserCredentials userCredentials = null)
    {
      if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }
      return AsyncContext.Run(
                async (conn, eventNum, resolveLinkToEvents, credentials)
                  => await conn.GetEventAsync<TEvent>(null, eventNum, resolveLinkToEvents, credentials).ConfigureAwait(false),
                bus, eventNumber, resolveLinkTos, userCredentials);
    }

    #endregion

    #region -- GetEvent(Topic) --

    /// <summary>Asynchronously reads a single event from a stream.</summary>
    /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
    /// <param name="stream">The stream to read from.</param>
    /// <param name="topic">The topic.</param>
    /// <param name="eventNumber">The event number to read, <see cref="StreamPosition">StreamPosition.End</see> to read the last event in the stream</param>
    /// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically.</param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    /// <returns>A <see cref="EventReadResult"/> containing the results of the read operation.</returns>
    public static EventReadResult<object> GetEvent(this IEventStoreBus bus,
      string stream, string topic, long eventNumber, bool resolveLinkTos, UserCredentials userCredentials = null)
    {
      if (string.IsNullOrEmpty(stream)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stream); }
      if (string.IsNullOrEmpty(topic)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.topic); }
      return AsyncContext.Run(
                async (conn, streamId, eventNum, resolveLinkToEvents, credentials)
                  => await conn.GetEventAsync(streamId, eventNum, resolveLinkToEvents, credentials).ConfigureAwait(false),
                bus, stream.Combine(topic), eventNumber, resolveLinkTos, userCredentials);
    }

    /// <summary>Asynchronously reads a single event from a stream.</summary>
    /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
    /// <param name="topic">The topic.</param>
    /// <param name="eventNumber">The event number to read, <see cref="StreamPosition">StreamPosition.End</see> to read the last event in the stream</param>
    /// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically.</param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    /// <returns>A <see cref="StreamEventsSlice"/> containing the results of the read operation.</returns>
    public static EventReadResult<TEvent> GetEvent<TEvent>(this IEventStoreBus bus,
      string topic, long eventNumber, bool resolveLinkTos, UserCredentials userCredentials = null)
    {
      if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }
      return AsyncContext.Run(
                async (conn, innerTopic, eventNum, resolveLinkToEvents, credentials)
                  => await conn.GetEventAsync<TEvent>(innerTopic, eventNum, resolveLinkToEvents, credentials).ConfigureAwait(false),
                bus, topic, eventNumber, resolveLinkTos, userCredentials);
    }

    #endregion

    #region -- GetStreamEventsForward --

    /// <summary>Reads count Events from an Event Stream forwards (e.g. oldest to newest) starting from position start.</summary>
    /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
    /// <param name="stream">The stream to read from.</param>
    /// <param name="start">The starting point to read from.</param>
    /// <param name="count">The count of items to read</param>
    /// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically.</param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    /// <returns>A <see cref="StreamEventsSlice"/> containing the results of the read operation.</returns>
    public static StreamEventsSlice<object> GetStreamEventsForward(this IEventStoreBus bus,
      string stream, long start, int count, bool resolveLinkTos, UserCredentials userCredentials = null)
    {
      if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }
      return AsyncContext.Run(
                async (conn, streamId, pointer, eventCount, resolveLinkToEvents, credentials)
                  => await conn.GetStreamEventsForwardAsync(streamId, pointer, eventCount, resolveLinkToEvents, credentials).ConfigureAwait(false),
                bus, stream, start, count, resolveLinkTos, userCredentials);
    }

    /// <summary>Reads count Events from an Event Stream forwards (e.g. oldest to newest) starting from position start.</summary>
    /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
    /// <param name="start">The starting point to read from.</param>
    /// <param name="count">The count of items to read</param>
    /// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically.</param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    /// <returns>A <see cref="StreamEventsSlice"/> containing the results of the read operation.</returns>
    public static StreamEventsSlice<TEvent> GetStreamEventsForward<TEvent>(this IEventStoreBus bus,
      long start, int count, bool resolveLinkTos, UserCredentials userCredentials = null)
    {
      if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }
      return AsyncContext.Run(
                async (conn, pointer, eventCount, resolveLinkToEvents, credentials)
                  => await conn.GetStreamEventsForwardAsync<TEvent>(null, pointer, eventCount, resolveLinkToEvents, credentials).ConfigureAwait(false),
                bus, start, count, resolveLinkTos, userCredentials);
    }

    #endregion

    #region -- GetStreamEventsForward(Topic) --

    /// <summary>Reads count Events from an Event Stream forwards (e.g. oldest to newest) starting from position start.</summary>
    /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
    /// <param name="stream">The stream to read from.</param>
    /// <param name="topic">The topic.</param>
    /// <param name="start">The starting point to read from.</param>
    /// <param name="count">The count of items to read</param>
    /// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically.</param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    /// <returns>A <see cref="StreamEventsSlice"/> containing the results of the read operation.</returns>
    public static StreamEventsSlice<object> GetStreamEventsForward(this IEventStoreBus bus,
      string stream, string topic, long start, int count, bool resolveLinkTos, UserCredentials userCredentials = null)
    {
      if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }
      if (string.IsNullOrEmpty(stream)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stream); }
      if (string.IsNullOrEmpty(topic)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.topic); }
      return AsyncContext.Run(
                async (conn, streamId, pointer, eventCount, resolveLinkToEvents, credentials)
                  => await conn.GetStreamEventsForwardAsync(streamId, pointer, eventCount, resolveLinkToEvents, credentials).ConfigureAwait(false),
                bus, stream.Combine(topic), start, count, resolveLinkTos, userCredentials);
    }

    /// <summary>Reads count Events from an Event Stream forwards (e.g. oldest to newest) starting from position start.</summary>
    /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
    /// <param name="topic">The topic.</param>
    /// <param name="start">The starting point to read from.</param>
    /// <param name="count">The count of items to read</param>
    /// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically.</param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    /// <returns>A <see cref="StreamEventsSlice"/> containing the results of the read operation.</returns>
    public static StreamEventsSlice<TEvent> GetStreamEventsForward<TEvent>(this IEventStoreBus bus,
      string topic, long start, int count, bool resolveLinkTos, UserCredentials userCredentials = null)
    {
      if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }
      return AsyncContext.Run(
                async (conn, innerTopic, pointer, eventCount, resolveLinkToEvents, credentials)
                  => await conn.GetStreamEventsForwardAsync<TEvent>(innerTopic, pointer, eventCount, resolveLinkToEvents, credentials).ConfigureAwait(false),
                bus, topic, start, count, resolveLinkTos, userCredentials);
    }

    #endregion

    #region -- GetStreamEventsBackward --

    /// <summary>Reads count events from an Event Stream backwards (e.g. newest to oldest) from position asynchronously.</summary>
    /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
    /// <param name="stream">The Event Stream to read from.</param>
    /// <param name="start">The position to start reading from.</param>
    /// <param name="count">The count to read from the position</param>
    /// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically.</param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    /// <returns>A <see cref="StreamEventsSlice"/> containing the results of the read operation.</returns>
    public static StreamEventsSlice<object> GetStreamEventsBackward(this IEventStoreBus bus,
      string stream, long start, int count, bool resolveLinkTos, UserCredentials userCredentials = null)
    {
      if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }
      return AsyncContext.Run(
                async (conn, streamId, pointer, eventCount, resolveLinkToEvents, credentials)
                  => await conn.GetStreamEventsBackwardAsync(streamId, pointer, eventCount, resolveLinkToEvents, credentials).ConfigureAwait(false),
                bus, stream, start, count, resolveLinkTos, userCredentials);
    }

    /// <summary>Reads count events from an Event Stream backwards (e.g. newest to oldest) from position asynchronously.</summary>
    /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
    /// <param name="start">The position to start reading from.</param>
    /// <param name="count">The count to read from the position</param>
    /// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically.</param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    /// <returns>A <see cref="StreamEventsSlice"/> containing the results of the read operation.</returns>
    public static StreamEventsSlice<TEvent> GetStreamEventsBackward<TEvent>(this IEventStoreBus bus,
      long start, int count, bool resolveLinkTos, UserCredentials userCredentials = null)
    {
      if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }
      return AsyncContext.Run(
                async (conn, pointer, eventCount, resolveLinkToEvents, credentials)
                  => await conn.GetStreamEventsBackwardAsync<TEvent>(null, pointer, eventCount, resolveLinkToEvents, credentials).ConfigureAwait(false),
                bus, start, count, resolveLinkTos, userCredentials);
    }

    #endregion

    #region -- GetStreamEventsBackward(Topic) --

    /// <summary>Reads count events from an Event Stream backwards (e.g. newest to oldest) from position asynchronously.</summary>
    /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
    /// <param name="stream">The Event Stream to read from.</param>
    /// <param name="topic">The topic.</param>
    /// <param name="start">The position to start reading from.</param>
    /// <param name="count">The count to read from the position</param>
    /// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically.</param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    /// <returns>A <see cref="StreamEventsSlice"/> containing the results of the read operation.</returns>
    public static StreamEventsSlice<object> GetStreamEventsBackward(this IEventStoreBus bus,
      string stream, string topic, long start, int count, bool resolveLinkTos, UserCredentials userCredentials = null)
    {
      if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }
      if (string.IsNullOrEmpty(stream)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stream); }
      if (string.IsNullOrEmpty(topic)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.topic); }
      return AsyncContext.Run(
                async (conn, streamId, pointer, eventCount, resolveLinkToEvents, credentials)
                  => await conn.GetStreamEventsBackwardAsync(streamId, pointer, eventCount, resolveLinkToEvents, credentials).ConfigureAwait(false),
                bus, stream.Combine(topic), start, count, resolveLinkTos, userCredentials);
    }

    /// <summary>Reads count events from an Event Stream backwards (e.g. newest to oldest) from position asynchronously.</summary>
    /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
    /// <param name="topic">The topic.</param>
    /// <param name="start">The position to start reading from.</param>
    /// <param name="count">The count to read from the position</param>
    /// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically.</param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    /// <returns>A <see cref="StreamEventsSlice"/> containing the results of the read operation.</returns>
    public static StreamEventsSlice<TEvent> GetStreamEventsBackward<TEvent>(this IEventStoreBus bus,
      string topic, long start, int count, bool resolveLinkTos, UserCredentials userCredentials = null)
    {
      if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }
      return AsyncContext.Run(
                async (conn, innerTopic, pointer, eventCount, resolveLinkToEvents, credentials)
                  => await conn.GetStreamEventsBackwardAsync<TEvent>(innerTopic, pointer, eventCount, resolveLinkToEvents, credentials).ConfigureAwait(false),
                bus, topic, start, count, resolveLinkTos, userCredentials);
    }

    #endregion

    #region -- GetEventAsync<TEvent> --

    /// <summary>Asynchronously reads a single event from a stream.</summary>
    /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
    /// <param name="eventNumber">The event number to read, <see cref="StreamPosition">StreamPosition.End</see> to read the last event in the stream</param>
    /// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically.</param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    /// <returns>A <see cref="Task&lt;EventReadResult&gt;"/> containing the results of the read operation.</returns>
    public static Task<EventReadResult<TEvent>> GetEventAsync<TEvent>(this IEventStoreBus bus,
      long eventNumber, bool resolveLinkTos, UserCredentials userCredentials = null)
    {
      if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }
      return bus.GetEventAsync<TEvent>(null, eventNumber, resolveLinkTos, userCredentials);
    }

    #endregion

    #region -- GetEventAsync(Topic) --

    /// <summary>Asynchronously reads a single event from a stream.</summary>
    /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
    /// <param name="stream">The stream to read from.</param>
    /// <param name="topic">The topic.</param>
    /// <param name="eventNumber">The event number to read, <see cref="StreamPosition">StreamPosition.End</see> to read the last event in the stream</param>
    /// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically.</param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    /// <returns>A <see cref="Task&lt;EventReadResult&gt;"/> containing the results of the read operation.</returns>
    public static Task<EventReadResult<object>> GetEventAsync(this IEventStoreBus bus,
      string stream, string topic, long eventNumber, bool resolveLinkTos, UserCredentials userCredentials = null)
    {
      if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }
      if (string.IsNullOrEmpty(stream)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stream); }
      if (string.IsNullOrEmpty(topic)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.topic); }
      return bus.GetEventAsync(stream.Combine(topic), eventNumber, resolveLinkTos, userCredentials);
    }

    #endregion

    #region -- GetStreamEventsForwardAsync<TEvent> --

    /// <summary>Reads count Events from an Event Stream forwards (e.g. oldest to newest) starting from position start.</summary>
    /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
    /// <param name="start">The starting point to read from.</param>
    /// <param name="count">The count of items to read</param>
    /// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically.</param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    /// <returns>A <see cref="Task&lt;StreamEventsSlice&gt;"/> containing the results of the read operation.</returns>
    public static Task<StreamEventsSlice<TEvent>> GetStreamEventsForwardAsync<TEvent>(this IEventStoreBus bus,
      long start, int count, bool resolveLinkTos, UserCredentials userCredentials = null)
    {
      if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }
      return bus.GetStreamEventsForwardAsync<TEvent>(null, start, count, resolveLinkTos, userCredentials);
    }

    #endregion

    #region -- GetStreamEventsForwardAsync(Topic) --

    /// <summary>Reads count Events from an Event Stream forwards (e.g. oldest to newest) starting from position start.</summary>
    /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
    /// <param name="stream">The stream to read from.</param>
    /// <param name="topic">The topic.</param>
    /// <param name="start">The starting point to read from.</param>
    /// <param name="count">The count of items to read</param>
    /// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically.</param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    /// <returns>A <see cref="Task&lt;StreamEventsSlice&gt;"/> containing the results of the read operation.</returns>
    public static Task<StreamEventsSlice<object>> GetStreamEventsForwardAsync(this IEventStoreBus bus,
      string stream, string topic, long start, int count, bool resolveLinkTos, UserCredentials userCredentials = null)
    {
      if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }
      if (string.IsNullOrEmpty(stream)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stream); }
      if (string.IsNullOrEmpty(topic)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.topic); }
      return bus.GetStreamEventsForwardAsync(stream.Combine(topic), start, count, resolveLinkTos, userCredentials);
    }

    #endregion

    #region -- GetStreamEventsBackwardAsync<TEvent> --

    /// <summary>Reads count events from an Event Stream backwards (e.g. newest to oldest) from position asynchronously.</summary>
    /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
    /// <param name="start">The position to start reading from.</param>
    /// <param name="count">The count to read from the position</param>
    /// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically.</param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    /// <returns>A <see cref="Task&lt;StreamEventsSlice&gt;"/> containing the results of the read operation.</returns>
    public static Task<StreamEventsSlice<TEvent>> GetStreamEventsBackwardAsync<TEvent>(this IEventStoreBus bus,
      long start, int count, bool resolveLinkTos, UserCredentials userCredentials = null)
    {
      if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }
      return bus.GetStreamEventsBackwardAsync<TEvent>(null, start, count, resolveLinkTos, userCredentials);
    }

    #endregion

    #region -- GetStreamEventsBackwardAsync(Topic) --

    /// <summary>Reads count events from an Event Stream backwards (e.g. newest to oldest) from position asynchronously.</summary>
    /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
    /// <param name="stream">The stream to read from.</param>
    /// <param name="topic">The topic.</param>
    /// <param name="start">The position to start reading from.</param>
    /// <param name="count">The count to read from the position</param>
    /// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically.</param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    /// <returns>A <see cref="Task&lt;StreamEventsSlice&gt;"/> containing the results of the read operation.</returns>
    public static Task<StreamEventsSlice<object>> GetStreamEventsBackwardAsync(this IEventStoreBus bus,
      string stream, string topic, long start, int count, bool resolveLinkTos, UserCredentials userCredentials = null)
    {
      if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }
      if (string.IsNullOrEmpty(stream)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stream); }
      if (string.IsNullOrEmpty(topic)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.topic); }
      return bus.GetStreamEventsBackwardAsync(stream.Combine(topic), start, count, resolveLinkTos, userCredentials);
    }

    #endregion

    #region -- GetFirstEvent --

    /// <summary>Reads the frist event from a stream.</summary>
    /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
    /// <param name="stream">The stream to read from.</param>
    /// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically.</param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    /// <returns>A result of the read operation.</returns>
    public static EventReadResult<object> GetFirstEvent(this IEventStoreBus bus,
      string stream, bool resolveLinkTos, UserCredentials userCredentials = null)
    {
      if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }

      return AsyncContext.Run(
        async (conn, streamId, eventNum, resolveLinkToEvents, credentials)
          => await conn.GetEventAsync(streamId, eventNum, resolveLinkToEvents, credentials).ConfigureAwait(false),
        bus, stream, StreamPosition.Start, resolveLinkTos, userCredentials);
    }

    /// <summary>Reads the frist event from a stream.</summary>
    /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
    /// <param name="stream">The stream to read from.</param>
    /// <param name="topic">The topic.</param>
    /// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically.</param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    /// <returns>A result of the read operation.</returns>
    public static EventReadResult<object> GetFirstEvent(this IEventStoreBus bus,
      string stream, string topic, bool resolveLinkTos, UserCredentials userCredentials = null)
    {
      if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }
      if (string.IsNullOrEmpty(stream)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stream); }
      if (string.IsNullOrEmpty(topic)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.topic); }

      return AsyncContext.Run(
        async (conn, streamId, eventNum, resolveLinkToEvents, credentials)
          => await conn.GetEventAsync(streamId, eventNum, resolveLinkToEvents, credentials).ConfigureAwait(false),
        bus, stream.Combine(topic), StreamPosition.Start, resolveLinkTos, userCredentials);
    }

    /// <summary>Reads the frist event from a stream.</summary>
    /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
    /// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically.</param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    /// <returns>A result of the read operation.</returns>
    public static EventReadResult<TEvent> GetFirstEvent<TEvent>(this IEventStoreBus bus,
      bool resolveLinkTos, UserCredentials userCredentials = null)
    {
      if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }

      return AsyncContext.Run(
        async (conn, eventNum, resolveLinkToEvents, credentials)
          => await conn.GetEventAsync<TEvent>(eventNum, resolveLinkToEvents, credentials).ConfigureAwait(false),
        bus, StreamPosition.Start, resolveLinkTos, userCredentials);
    }

    /// <summary>Reads the frist event from a stream.</summary>
    /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
    /// <param name="topic">The topic.</param>
    /// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically.</param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    /// <returns>A result of the read operation.</returns>
    public static EventReadResult<TEvent> GetFirstEvent<TEvent>(this IEventStoreBus bus,
      string topic, bool resolveLinkTos, UserCredentials userCredentials = null)
    {
      if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }

      return AsyncContext.Run(
        async (conn, innerTopic, eventNum, resolveLinkToEvents, credentials)
          => await conn.GetEventAsync<TEvent>(innerTopic, eventNum, resolveLinkToEvents, credentials).ConfigureAwait(false),
        bus, topic, StreamPosition.Start, resolveLinkTos, userCredentials);
    }

    #endregion

    #region -- GetFirstEventAsync --

    /// <summary>Asynchronously reads the frist event from a stream.</summary>
    /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
    /// <param name="stream">The stream to read from.</param>
    /// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically.</param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    /// <returns>A <see cref="Task&lt;EventReadResult&gt;"/> containing the results of the read operation.</returns>
    public static Task<EventReadResult<object>> GetFirstEventAsync(this IEventStoreBus bus,
      string stream, bool resolveLinkTos, UserCredentials userCredentials = null)
    {
      if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }

      return bus.GetEventAsync(stream, StreamPosition.Start, resolveLinkTos, userCredentials);
    }

    /// <summary>Asynchronously reads the frist event from a stream.</summary>
    /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
    /// <param name="stream">The stream to read from.</param>
    /// <param name="topic">The topic.</param>
    /// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically.</param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    /// <returns>A <see cref="Task&lt;EventReadResult&gt;"/> containing the results of the read operation.</returns>
    public static Task<EventReadResult<object>> GetFirstEventAsync(this IEventStoreBus bus,
      string stream, string topic, bool resolveLinkTos, UserCredentials userCredentials = null)
    {
      if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }
      if (string.IsNullOrEmpty(stream)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stream); }
      if (string.IsNullOrEmpty(topic)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.topic); }

      return bus.GetEventAsync(stream.Combine(topic), StreamPosition.Start, resolveLinkTos, userCredentials);
    }

    /// <summary>Asynchronously reads the frist event from a stream.</summary>
    /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
    /// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically.</param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    /// <returns>A <see cref="Task&lt;EventReadResult&gt;"/> containing the results of the read operation.</returns>
    public static Task<EventReadResult<TEvent>> GetFirstEventAsync<TEvent>(this IEventStoreBus bus,
      bool resolveLinkTos, UserCredentials userCredentials = null)
    {
      if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }

      return bus.GetEventAsync<TEvent>(StreamPosition.Start, resolveLinkTos, userCredentials);
    }

    /// <summary>Asynchronously reads the frist event from a stream.</summary>
    /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
    /// <param name="topic">The topic.</param>
    /// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically.</param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    /// <returns>A <see cref="Task&lt;EventReadResult&gt;"/> containing the results of the read operation.</returns>
    public static Task<EventReadResult<TEvent>> GetFirstEventAsync<TEvent>(this IEventStoreBus bus,
      string topic, bool resolveLinkTos, UserCredentials userCredentials = null)
    {
      if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }
      if (string.IsNullOrEmpty(topic)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.topic); }

      return bus.GetEventAsync<TEvent>(topic, StreamPosition.Start, resolveLinkTos, userCredentials);
    }

    #endregion

    #region -- GetLastEvent --

    /// <summary>Reads the last event from a stream.</summary>
    /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
    /// <param name="stream">The stream to read from.</param>
    /// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically.</param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    /// <returns>A <see cref="Task&lt;EventReadResult&gt;"/> containing the results of the read operation.</returns>
    public static EventReadResult<object> GetLastEvent(this IEventStoreBus bus,
      string stream, bool resolveLinkTos, UserCredentials userCredentials = null)
    {
      if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }
      return AsyncContext.Run(
        async (conn, streamId, resolveLinkToEvents, credentials)
          => await conn.GetLastEventAsync(streamId, resolveLinkToEvents, credentials).ConfigureAwait(false),
        bus, stream, resolveLinkTos, userCredentials);
    }

    /// <summary>Reads the last event from a stream.</summary>
    /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
    /// <param name="stream">The stream to read from.</param>
    /// <param name="topic">The topic.</param>
    /// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically.</param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    /// <returns>A <see cref="Task&lt;EventReadResult&gt;"/> containing the results of the read operation.</returns>
    public static EventReadResult<object> GetLastEvent(this IEventStoreBus bus,
      string stream, string topic, bool resolveLinkTos, UserCredentials userCredentials = null)
    {
      if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }
      return AsyncContext.Run(
        async (conn, streamId, innerTopic, resolveLinkToEvents, credentials)
          => await conn.GetLastEventAsync(streamId, innerTopic, resolveLinkToEvents, credentials).ConfigureAwait(false),
        bus, stream, topic, resolveLinkTos, userCredentials);
    }

    /// <summary>Reads the last event from a stream.</summary>
    /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
    /// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically.</param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    /// <returns>A <see cref="Task&lt;EventReadResult&gt;"/> containing the results of the read operation.</returns>
    public static EventReadResult<TEvent> GetLastEvent<TEvent>(this IEventStoreBus bus,
      bool resolveLinkTos, UserCredentials userCredentials = null)
    {
      if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }
      return AsyncContext.Run(
        async (conn, resolveLinkToEvents, credentials)
          => await conn.GetLastEventAsync<TEvent>(null, resolveLinkToEvents, credentials).ConfigureAwait(false),
        bus, resolveLinkTos, userCredentials);
    }

    /// <summary>Reads the last event from a stream.</summary>
    /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
    /// <param name="topic">The topic.</param>
    /// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically.</param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    /// <returns>A <see cref="Task&lt;EventReadResult&gt;"/> containing the results of the read operation.</returns>
    public static EventReadResult<TEvent> GetLastEvent<TEvent>(this IEventStoreBus bus,
      string topic, bool resolveLinkTos, UserCredentials userCredentials = null)
    {
      if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }
      return AsyncContext.Run(
        async (conn, innerTopic, resolveLinkToEvents, credentials)
          => await conn.GetLastEventAsync<TEvent>(innerTopic, resolveLinkToEvents, credentials).ConfigureAwait(false),
        bus, topic, resolveLinkTos, userCredentials);
    }

    #endregion

    #region -- GetLastEventAsync --

    /// <summary>Asynchronously reads the last event from a stream.</summary>
    /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
    /// <param name="stream">The stream to read from.</param>
    /// <param name="topic">The topic.</param>
    /// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically.</param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    /// <returns>A <see cref="Task&lt;EventReadResult&gt;"/> containing the results of the read operation.</returns>
    public static Task<EventReadResult<object>> GetLastEventAsync(this IEventStoreBus bus,
      string stream, string topic, bool resolveLinkTos, UserCredentials userCredentials = null)
    {
      if (string.IsNullOrEmpty(stream)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stream); }
      if (string.IsNullOrEmpty(topic)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.topic); }

      return GetLastEventAsync(bus, stream.Combine(topic), resolveLinkTos, userCredentials);
    }

    /// <summary>Asynchronously reads the last event from a stream.</summary>
    /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
    /// <param name="stream">The stream to read from.</param>
    /// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically.</param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    /// <returns>A <see cref="Task&lt;EventReadResult&gt;"/> containing the results of the read operation.</returns>
    public static async Task<EventReadResult<object>> GetLastEventAsync(this IEventStoreBus bus,
      string stream, bool resolveLinkTos, UserCredentials userCredentials = null)
    {
      if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }

      var slice = await bus.GetStreamEventsBackwardAsync(stream, StreamPosition.End, 1, resolveLinkTos, userCredentials)
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
          if (0u >= (uint)sliceEvents.Length) { readStatus = EventReadStatus.NotFound; }
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
    /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
    /// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically.</param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    /// <returns>A <see cref="Task&lt;EventReadResult&gt;"/> containing the results of the read operation.</returns>
    public static Task<EventReadResult<TEvent>> GetLastEventAsync<TEvent>(this IEventStoreBus bus,
      bool resolveLinkTos, UserCredentials userCredentials = null)
    {
      return GetLastEventAsync<TEvent>(bus, null, resolveLinkTos, userCredentials);
    }

    /// <summary>Asynchronously reads the last event from a stream.</summary>
    /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
    /// <param name="topic">The topic.</param>
    /// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically.</param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    /// <returns>A <see cref="Task&lt;EventReadResult&gt;"/> containing the results of the read operation.</returns>
    public static async Task<EventReadResult<TEvent>> GetLastEventAsync<TEvent>(this IEventStoreBus bus,
      string topic, bool resolveLinkTos, UserCredentials userCredentials = null)
    {
      if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }

      var slice = await bus.GetStreamEventsBackwardAsync<TEvent>(topic, StreamPosition.End, 1, resolveLinkTos, userCredentials).ConfigureAwait(false);
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
          if (0u >= (uint)sliceEvents.Length) { readStatus = EventReadStatus.NotFound; }
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
