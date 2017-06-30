using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CuteAnt.Reflection;
using EventStore.ClientAPI.Serialization;
using EventStore.ClientAPI.SystemData;

namespace EventStore.ClientAPI
{
  partial class IEventStoreConnectionExtensions
  {
    #region -- SendEventAsync --

    public static Task<WriteResult> SendEventAsync<TEvent>(this IEventStoreConnectionBase connection, string stream, TEvent @event,
      Dictionary<string, object> eventContext = null, Type expectedType = null, UserCredentials userCredentials = null)
      where TEvent : class
    {
      var actualType = typeof(TEvent);
      if (actualType == TypeHelper.ObjectType)
      {
        if (null == @event) { throw new ArgumentNullException(nameof(@event)); }
        actualType = @event?.GetType();
      }
      return SendEventAsync(connection, stream, actualType, @event, eventContext, expectedType, userCredentials);
    }

    public static Task<WriteResult> SendEventAsync<TEvent>(this IEventStoreConnectionBase connection, string stream, Type actualType, TEvent @event,
      Dictionary<string, object> eventContext = null, Type expectedType = null, UserCredentials userCredentials = null)
      where TEvent : class
    {
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }
      //if (string.IsNullOrEmpty(stream)) { throw new ArgumentNullException(nameof(stream)); }
      if (null == actualType) { throw new ArgumentNullException(nameof(actualType)); }
      //if (null == @event) { throw new ArgumentNullException(nameof(@event)); }

      var streamAttr = SerializationManager.GetStreamProvider(actualType, expectedType);

      var eventData = SerializationManager.SerializeEvent(streamAttr, actualType, @event, eventContext, expectedType);

      return connection.AppendToStreamAsync(stream, streamAttr != null ? streamAttr.ExpectedVersion : ExpectedVersion.Any, userCredentials, eventData);
    }

    #endregion

    #region -- SendEventAsync(Topic) --

    public static Task<WriteResult> SendEventAsync<TEvent>(this IEventStoreConnectionBase connection, string stream, string topic, TEvent @event,
      Dictionary<string, object> eventContext = null, Type expectedType = null, UserCredentials userCredentials = null)
      where TEvent : class
    {
      if (string.IsNullOrEmpty(stream)) { throw new ArgumentNullException(nameof(stream)); }
      if (string.IsNullOrEmpty(topic)) { throw new ArgumentNullException(nameof(topic)); }
      return SendEventAsync(connection, CombineStreamId(stream, topic), @event, eventContext, expectedType, userCredentials);
    }

    public static Task<WriteResult> SendEventAsync<TEvent>(this IEventStoreConnectionBase connection, string stream, string topic, Type actualType, TEvent @event,
      Dictionary<string, object> eventContext = null, Type expectedType = null, UserCredentials userCredentials = null)
      where TEvent : class
    {
      if (string.IsNullOrEmpty(stream)) { throw new ArgumentNullException(nameof(stream)); }
      if (string.IsNullOrEmpty(topic)) { throw new ArgumentNullException(nameof(topic)); }
      return SendEventAsync(connection, CombineStreamId(stream, topic), actualType, @event, eventContext, expectedType, userCredentials);
    }
    #endregion

    #region -- SendEventsAsync --

    public static Task<WriteResult> SendEventsAsync<TEvent>(this IEventStoreConnectionBase connection, string stream, IEnumerable<TEvent> events,
      Dictionary<string, object> eventContext = null, Type expectedType = null, UserCredentials userCredentials = null)
      where TEvent : class
    {
      var actualType = typeof(TEvent);
      if (actualType == TypeHelper.ObjectType)
      {
        if (null == connection) { throw new ArgumentNullException(nameof(connection)); }
        //if (string.IsNullOrEmpty(stream)) { throw new ArgumentNullException(nameof(stream)); }
        if (null == events) { throw new ArgumentNullException(nameof(events)); }
        //var eventDatas = SerializationManager.SerializeEvents(events, eventContext, expectedType);
        var eventDatas = events.Select(_ => SerializationManager.SerializeEvent(_, eventContext, expectedType)).ToArray();
        return connection.AppendToStreamAsync(stream, ExpectedVersion.Any, eventDatas, userCredentials);
      }
      return SendEventsAsync(connection, stream, actualType, events, eventContext, expectedType, userCredentials);
    }
    public static Task<WriteResult> SendEventsAsync<TEvent>(this IEventStoreConnectionBase connection, string stream, Type actualType, IEnumerable<TEvent> events,
      Dictionary<string, object> eventContext = null, Type expectedType = null, UserCredentials userCredentials = null)
      where TEvent : class
    {
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }
      //if (string.IsNullOrEmpty(stream)) { throw new ArgumentNullException(nameof(stream)); }
      if (null == actualType) { throw new ArgumentNullException(nameof(actualType)); }
      //if (null == events) { throw new ArgumentNullException(nameof(events)); }

      var streamAttr = SerializationManager.GetStreamProvider(actualType, expectedType);

      var eventDatas = SerializationManager.SerializeEvents(streamAttr, actualType, events, eventContext, expectedType);

      return connection.AppendToStreamAsync(stream, streamAttr != null ? streamAttr.ExpectedVersion : ExpectedVersion.Any, eventDatas, userCredentials);
    }




    public static Task<WriteResult> SendEventsAsync<TEvent>(this IEventStoreConnectionBase connection, string stream, IList<TEvent> events,
      IList<Dictionary<string, object>> eventContexts, Type expectedType = null, UserCredentials userCredentials = null)
      where TEvent : class
    {
      var actualType = typeof(TEvent);
      if (actualType == TypeHelper.ObjectType)
      {
        if (null == connection) { throw new ArgumentNullException(nameof(connection)); }
        //if (string.IsNullOrEmpty(stream)) { throw new ArgumentNullException(nameof(stream)); }
        //if (null == events) { throw new ArgumentNullException(nameof(events)); }

        var eventDatas = SerializationManager.SerializeEvents(events, eventContexts, expectedType);
        return connection.AppendToStreamAsync(stream, ExpectedVersion.Any, eventDatas, userCredentials);
      }
      return SendEventsAsync(connection, stream, actualType, events, eventContexts, expectedType, userCredentials);
    }
    public static Task<WriteResult> SendEventsAsync<TEvent>(this IEventStoreConnectionBase connection, string stream, Type actualType, IList<TEvent> events,
      IList<Dictionary<string, object>> eventContexts, Type expectedType = null, UserCredentials userCredentials = null)
      where TEvent : class
    {
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }
      //if (string.IsNullOrEmpty(stream)) { throw new ArgumentNullException(nameof(stream)); }
      if (null == actualType) { throw new ArgumentNullException(nameof(actualType)); }
      //if (null == events) { throw new ArgumentNullException(nameof(events)); }

      var streamAttr = SerializationManager.GetStreamProvider(actualType, expectedType);

      var eventDatas = SerializationManager.SerializeEvents(streamAttr, actualType, events, eventContexts, expectedType);

      return connection.AppendToStreamAsync(stream, streamAttr != null ? streamAttr.ExpectedVersion : ExpectedVersion.Any, eventDatas, userCredentials);
    }

    #endregion

    #region -- SendEventsAsync(Topic) --

    public static Task<WriteResult> SendEventsAsync<TEvent>(this IEventStoreConnectionBase connection, string stream, string topic, IEnumerable<TEvent> events,
      Dictionary<string, object> eventContext = null, Type expectedType = null, UserCredentials userCredentials = null)
      where TEvent : class
    {
      if (string.IsNullOrEmpty(stream)) { throw new ArgumentNullException(nameof(stream)); }
      if (string.IsNullOrEmpty(topic)) { throw new ArgumentNullException(nameof(topic)); }
      return SendEventsAsync(connection, CombineStreamId(stream, topic), events, eventContext, expectedType, userCredentials);
    }
    public static Task<WriteResult> SendEventsAsync<TEvent>(this IEventStoreConnectionBase connection, string stream, string topic, Type actualType, IEnumerable<TEvent> events,
      Dictionary<string, object> eventContext = null, Type expectedType = null, UserCredentials userCredentials = null)
      where TEvent : class
    {
      if (string.IsNullOrEmpty(stream)) { throw new ArgumentNullException(nameof(stream)); }
      if (string.IsNullOrEmpty(topic)) { throw new ArgumentNullException(nameof(topic)); }
      return SendEventsAsync(connection, CombineStreamId(stream, topic), actualType, events, eventContext, expectedType, userCredentials);
    }




    public static Task<WriteResult> SendEventsAsync<TEvent>(this IEventStoreConnectionBase connection, string stream, string topic, IList<TEvent> events,
      IList<Dictionary<string, object>> eventContexts, Type expectedType = null, UserCredentials userCredentials = null)
      where TEvent : class
    {
      if (string.IsNullOrEmpty(stream)) { throw new ArgumentNullException(nameof(stream)); }
      if (string.IsNullOrEmpty(topic)) { throw new ArgumentNullException(nameof(topic)); }
      return SendEventsAsync(connection, CombineStreamId(stream, topic), events, eventContexts, expectedType, userCredentials);
    }
    public static Task<WriteResult> SendEventsAsync<TEvent>(this IEventStoreConnectionBase connection, string stream, string topic, Type actualType, IList<TEvent> events,
      IList<Dictionary<string, object>> eventContexts, Type expectedType = null, UserCredentials userCredentials = null)
      where TEvent : class
    {
      if (string.IsNullOrEmpty(stream)) { throw new ArgumentNullException(nameof(stream)); }
      if (string.IsNullOrEmpty(topic)) { throw new ArgumentNullException(nameof(topic)); }
      return SendEventsAsync(connection, CombineStreamId(stream, topic), actualType, events, eventContexts, expectedType, userCredentials);
    }

    #endregion

    #region -- SendEventsAsync(Transaction) --

    public static async Task<WriteResult> SendEventsAsync<TEvent>(this IEventStoreConnectionBase connection, string stream, int batchSize,
      ICollection<TEvent> events, Dictionary<string, object> eventContext = null, Type expectedType = null, UserCredentials userCredentials = null)
      where TEvent : class
    {
      var actualType = typeof(TEvent);
      if (actualType == TypeHelper.ObjectType)
      {
        if (null == connection) { throw new ArgumentNullException(nameof(connection)); }
        //if (string.IsNullOrEmpty(stream)) { throw new ArgumentNullException(nameof(stream)); }
        if (batchSize <= 0) { throw new ArgumentOutOfRangeException(nameof(batchSize)); }
        if (null == events) { throw new ArgumentNullException(nameof(events)); }

        //var eventDatas = SerializationManager.SerializeEvents(events, eventContext, expectedType);
        var eventDatas = events.Select(_ => SerializationManager.SerializeEvent(_, eventContext, expectedType)).ToArray();
        if (events.Count <= batchSize)
        {
          return await connection.AppendToStreamAsync(stream, ExpectedVersion.Any, eventDatas, userCredentials).ConfigureAwait(false);
        }
        else
        {
          return await DoWriteAsync(connection, stream, ExpectedVersion.Any, eventDatas, batchSize, userCredentials).ConfigureAwait(false);
        }
      }
      return await SendEventsAsync(connection, stream, actualType, batchSize, events, eventContext, expectedType, userCredentials).ConfigureAwait(false);
    }
    public static async Task<WriteResult> SendEventsAsync<TEvent>(this IEventStoreConnectionBase connection, string stream, Type actualType, int batchSize,
      ICollection<TEvent> events, Dictionary<string, object> eventContext = null, Type expectedType = null, UserCredentials userCredentials = null)
      where TEvent : class
    {
      if (batchSize <= 0) { throw new ArgumentOutOfRangeException(nameof(batchSize)); }
      if (null == events) { throw new ArgumentNullException(nameof(events)); }

      if (events.Count <= batchSize)
      {
        return await SendEventsAsync(connection, stream, actualType, events, eventContext, expectedType, userCredentials).ConfigureAwait(false);
      }

      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }
      //if (string.IsNullOrEmpty(stream)) { throw new ArgumentNullException(nameof(stream)); }
      if (null == actualType) { throw new ArgumentNullException(nameof(actualType)); }

      var streamAttr = SerializationManager.GetStreamProvider(actualType, expectedType);
      var expectedVersion = streamAttr != null ? streamAttr.ExpectedVersion : ExpectedVersion.Any;
      var eventDatas = SerializationManager.SerializeEvents(streamAttr, actualType, events, eventContext, expectedType);
      return await DoWriteAsync(connection, stream, expectedVersion, eventDatas, batchSize, userCredentials).ConfigureAwait(false);
    }




    public static async Task<WriteResult> SendEventsAsync<TEvent>(this IEventStoreConnectionBase connection, string stream, int batchSize,
      IList<TEvent> events, IList<Dictionary<string, object>> eventContexts, Type expectedType = null, UserCredentials userCredentials = null)
      where TEvent : class
    {
      var actualType = typeof(TEvent);
      if (actualType == TypeHelper.ObjectType)
      {
        if (null == connection) { throw new ArgumentNullException(nameof(connection)); }
        //if (string.IsNullOrEmpty(stream)) { throw new ArgumentNullException(nameof(stream)); }
        if (batchSize <= 0) { throw new ArgumentOutOfRangeException(nameof(batchSize)); }
        if (null == events) { throw new ArgumentNullException(nameof(events)); }

        var eventDatas = SerializationManager.SerializeEvents(events, eventContexts, expectedType);
        if (events.Count <= batchSize)
        {
          return await connection.AppendToStreamAsync(stream, ExpectedVersion.Any, eventDatas, userCredentials).ConfigureAwait(false);
        }
        else
        {
          return await DoWriteAsync(connection, stream, ExpectedVersion.Any, eventDatas, batchSize, userCredentials).ConfigureAwait(false);
        }
      }
      return await SendEventsAsync(connection, stream, actualType, batchSize, events, eventContexts, expectedType, userCredentials).ConfigureAwait(false);
    }
    public static async Task<WriteResult> SendEventsAsync<TEvent>(this IEventStoreConnectionBase connection, string stream, Type actualType, int batchSize,
      IList<TEvent> events, IList<Dictionary<string, object>> eventContexts, Type expectedType = null, UserCredentials userCredentials = null)
      where TEvent : class
    {
      if (batchSize <= 0) { throw new ArgumentOutOfRangeException(nameof(batchSize)); }
      if (null == events) { throw new ArgumentNullException(nameof(events)); }

      if (events.Count <= batchSize)
      {
        return await SendEventsAsync(connection, stream, actualType, events, eventContexts, expectedType, userCredentials).ConfigureAwait(false);
      }

      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }
      //if (string.IsNullOrEmpty(stream)) { throw new ArgumentNullException(nameof(stream)); }
      if (null == actualType) { throw new ArgumentNullException(nameof(actualType)); }

      var streamAttr = SerializationManager.GetStreamProvider(actualType, expectedType);
      var expectedVersion = streamAttr != null ? streamAttr.ExpectedVersion : ExpectedVersion.Any;
      var eventDatas = SerializationManager.SerializeEvents(streamAttr, actualType, events, eventContexts, expectedType);
      return await DoWriteAsync(connection, stream, expectedVersion, eventDatas, batchSize, userCredentials).ConfigureAwait(false);
    }

    #endregion

    #region -- SendEventsAsync(Transaction & Topic) --

    public static async Task<WriteResult> SendEventsAsync<TEvent>(this IEventStoreConnectionBase connection, string stream, string topic, int batchSize,
      ICollection<TEvent> events, Dictionary<string, object> eventContext = null, Type expectedType = null, UserCredentials userCredentials = null)
      where TEvent : class
    {
      if (string.IsNullOrEmpty(stream)) { throw new ArgumentNullException(nameof(stream)); }
      if (string.IsNullOrEmpty(topic)) { throw new ArgumentNullException(nameof(topic)); }
      return await SendEventsAsync(connection, CombineStreamId(stream, topic), batchSize, events, eventContext, expectedType, userCredentials).ConfigureAwait(false);
    }
    public static async Task<WriteResult> SendEventsAsync<TEvent>(this IEventStoreConnectionBase connection, string stream, string topic, Type actualType, int batchSize,
      ICollection<TEvent> events, Dictionary<string, object> eventContext = null, Type expectedType = null, UserCredentials userCredentials = null)
      where TEvent : class
    {
      if (string.IsNullOrEmpty(stream)) { throw new ArgumentNullException(nameof(stream)); }
      if (string.IsNullOrEmpty(topic)) { throw new ArgumentNullException(nameof(topic)); }
      return await SendEventsAsync(connection, CombineStreamId(stream, topic), actualType, batchSize, events, eventContext, expectedType, userCredentials).ConfigureAwait(false);
    }




    public static async Task<WriteResult> SendEventsAsync<TEvent>(this IEventStoreConnectionBase connection, string stream, string topic, int batchSize,
      IList<TEvent> events, IList<Dictionary<string, object>> eventContexts, Type expectedType = null, UserCredentials userCredentials = null)
      where TEvent : class
    {
      if (string.IsNullOrEmpty(stream)) { throw new ArgumentNullException(nameof(stream)); }
      if (string.IsNullOrEmpty(topic)) { throw new ArgumentNullException(nameof(topic)); }
      return await SendEventsAsync(connection, CombineStreamId(stream, topic), batchSize, events, eventContexts, expectedType, userCredentials).ConfigureAwait(false);
    }
    public static async Task<WriteResult> SendEventsAsync<TEvent>(this IEventStoreConnectionBase connection, string stream, string topic, Type actualType, int batchSize,
      IList<TEvent> events, IList<Dictionary<string, object>> eventContexts, Type expectedType = null, UserCredentials userCredentials = null)
      where TEvent : class
    {
      if (string.IsNullOrEmpty(stream)) { throw new ArgumentNullException(nameof(stream)); }
      if (string.IsNullOrEmpty(topic)) { throw new ArgumentNullException(nameof(topic)); }
      return await SendEventsAsync(connection, CombineStreamId(stream, topic), actualType, batchSize, events, eventContexts, expectedType, userCredentials).ConfigureAwait(false);
    }

    #endregion
  }
}
