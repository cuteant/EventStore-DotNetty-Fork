using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CuteAnt;
using CuteAnt.Reflection;
using EventStore.ClientAPI.Serialization;
using EventStore.ClientAPI.SystemData;

namespace EventStore.ClientAPI
{
  partial class IEventStoreConnectionExtensions
  {
    #region -- PublishEventAsync --

    public static Task<WriteResult> PublishEventAsync<TEvent>(this IEventStoreConnectionBase connection, TEvent @event,
      Dictionary<string, object> eventContext = null, Type expectedType = null, UserCredentials userCredentials = null)
      where TEvent : class
    {
      var actualType = typeof(TEvent);
      if (actualType == TypeConstants.ObjectType)
      {
        if (null == @event) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.@event); }
        actualType = @event.GetType();
      }
      return PublishEventAsync(connection, actualType, @event, eventContext, expectedType, userCredentials);
    }
    public static Task<WriteResult> PublishEventAsync<TEvent>(this IEventStoreConnectionBase connection, Type actualType, TEvent @event,
      Dictionary<string, object> eventContext = null, Type expectedType = null, UserCredentials userCredentials = null)
      where TEvent : class
    {
      if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
      if (null == actualType) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.actualType); }
      //if (null == @event) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.@event); }

      var streamAttr = SerializationManager.GetStreamProvider(actualType, expectedType);

      var eventData = SerializationManager.SerializeEvent(streamAttr, actualType, @event, eventContext, expectedType);

      if (streamAttr != null)
      {
        return connection.AppendToStreamAsync(streamAttr.StreamId, streamAttr.ExpectedVersion, userCredentials, eventData);
      }
      return connection.AppendToStreamAsync(RuntimeTypeNameFormatter.Serialize(expectedType ?? actualType), ExpectedVersion.Any, userCredentials, eventData);
    }

    #endregion

    #region -- PublishEventAsync(Topic) --

    public static Task<WriteResult> PublishEventAsync<TEvent>(this IEventStoreConnectionBase connection, string topic, TEvent @event,
      Dictionary<string, object> eventContext = null, Type expectedType = null, UserCredentials userCredentials = null)
      where TEvent : class
    {
      var actualType = typeof(TEvent);
      if (actualType == TypeConstants.ObjectType)
      {
        if (null == @event) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.@event); }
        actualType = @event.GetType();
      }
      return PublishEventAsync(connection, topic, actualType, @event, eventContext, expectedType, userCredentials);
    }
    public static Task<WriteResult> PublishEventAsync<TEvent>(this IEventStoreConnectionBase connection, string topic, Type actualType, TEvent @event,
      Dictionary<string, object> eventContext = null, Type expectedType = null, UserCredentials userCredentials = null)
      where TEvent : class
    {
      if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
      if (string.IsNullOrEmpty(topic)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.topic); }
      if (null == actualType) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.actualType); }
      //if (null == @event) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.@event); }

      var streamAttr = SerializationManager.GetStreamProvider(actualType, expectedType);

      var eventData = SerializationManager.SerializeEvent(streamAttr, actualType, @event, eventContext, expectedType);

      if (streamAttr != null)
      {
        return connection.AppendToStreamAsync(CombineStreamId(streamAttr.StreamId, topic), streamAttr.ExpectedVersion, userCredentials, eventData);
      }
      return connection.AppendToStreamAsync(CombineStreamId(RuntimeTypeNameFormatter.Serialize(expectedType ?? actualType), topic), ExpectedVersion.Any, userCredentials, eventData);
    }

    #endregion

    #region -- PublishEventsAsync --

    public static async Task<WriteResult> PublishEventsAsync<TEvent>(this IEventStoreConnectionBase connection, IEnumerable<TEvent> events,
      Dictionary<string, object> eventContext = null, Type expectedType = null, UserCredentials userCredentials = null)
      where TEvent : class
    {
      var actualType = typeof(TEvent);
      if (actualType == TypeConstants.ObjectType)
      {
        if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
        if (null == events) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.events); }

        WriteResult result = default(WriteResult);
        foreach (var @event in events)
        {
          result = await PublishEventAsync(connection, @event.GetType(), @event, eventContext, expectedType, userCredentials).ConfigureAwait(false);
        }
        return result;
      }

      return await PublishEventsAsync(connection, actualType, events, eventContext, expectedType, userCredentials).ConfigureAwait(false);
    }
    public static Task<WriteResult> PublishEventsAsync<TEvent>(this IEventStoreConnectionBase connection, Type actualType, IEnumerable<TEvent> events,
      Dictionary<string, object> eventContext = null, Type expectedType = null, UserCredentials userCredentials = null)
      where TEvent : class
    {
      if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
      if (null == actualType) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.actualType); }
      //if (null == events) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.events); }

      var streamAttr = SerializationManager.GetStreamProvider(actualType, expectedType);

      var eventDatas = SerializationManager.SerializeEvents(streamAttr, actualType, events, eventContext, expectedType);

      if (streamAttr != null)
      {
        return connection.AppendToStreamAsync(streamAttr.StreamId, streamAttr.ExpectedVersion, eventDatas, userCredentials);
      }
      return connection.AppendToStreamAsync(RuntimeTypeNameFormatter.Serialize(expectedType ?? actualType), ExpectedVersion.Any, eventDatas, userCredentials);
    }




    public static async Task<WriteResult> PublishEventsAsync<TEvent>(this IEventStoreConnectionBase connection, IList<TEvent> events,
      IList<Dictionary<string, object>> eventContexts, Type expectedType = null, UserCredentials userCredentials = null)
      where TEvent : class
    {
      var actualType = typeof(TEvent);
      if (actualType == TypeConstants.ObjectType)
      {
        if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
        if (null == events) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.events); }
        if (null == eventContexts) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.eventContexts); }
        if (events.Count != eventContexts.Count) { ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.eventContexts); }

        WriteResult result = default(WriteResult);
        for (var idx = 0; idx < events.Count; idx++)
        {
          var @event = events[idx];
          result = await PublishEventAsync(connection, @event.GetType(), @event, eventContexts[idx], expectedType, userCredentials).ConfigureAwait(false);
        }
        return result;
      }
      return await PublishEventsAsync(connection, actualType, events, eventContexts, expectedType, userCredentials).ConfigureAwait(false);
    }
    public static Task<WriteResult> PublishEventsAsync<TEvent>(this IEventStoreConnectionBase connection, Type actualType, IList<TEvent> events,
      IList<Dictionary<string, object>> eventContexts, Type expectedType = null, UserCredentials userCredentials = null)
      where TEvent : class
    {
      if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
      if (null == actualType) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.actualType); }
      //if (null == events) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.events); }

      var streamAttr = SerializationManager.GetStreamProvider(actualType, expectedType);

      var eventDatas = SerializationManager.SerializeEvents(streamAttr, actualType, events, eventContexts, expectedType);

      if (streamAttr != null)
      {
        return connection.AppendToStreamAsync(streamAttr.StreamId, streamAttr.ExpectedVersion, eventDatas, userCredentials);
      }
      return connection.AppendToStreamAsync(RuntimeTypeNameFormatter.Serialize(expectedType ?? actualType), ExpectedVersion.Any, eventDatas, userCredentials);
    }

    #endregion

    #region -- PublishEventsAsync(Topic) --

    public static async Task<WriteResult> PublishEventsAsync<TEvent>(this IEventStoreConnectionBase connection, string topic, IEnumerable<TEvent> events,
      Dictionary<string, object> eventContext = null, Type expectedType = null, UserCredentials userCredentials = null)
      where TEvent : class
    {
      var actualType = typeof(TEvent);
      if (actualType == TypeConstants.ObjectType)
      {
        if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
        if (string.IsNullOrEmpty(topic)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.topic); }
        if (null == events) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.events); }

        WriteResult result = default(WriteResult);
        foreach (var @event in events)
        {
          result = await PublishEventAsync(connection, topic, @event.GetType(), @event, eventContext, expectedType, userCredentials).ConfigureAwait(false);
        }
        return result;
      }

      return await PublishEventsAsync(connection, topic, actualType, events, eventContext, expectedType, userCredentials).ConfigureAwait(false);
    }
    public static Task<WriteResult> PublishEventsAsync<TEvent>(this IEventStoreConnectionBase connection, string topic, Type actualType, IEnumerable<TEvent> events,
      Dictionary<string, object> eventContext = null, Type expectedType = null, UserCredentials userCredentials = null)
      where TEvent : class
    {
      if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
      if (string.IsNullOrEmpty(topic)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.topic); }
      if (null == actualType) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.actualType); }
      //if (null == events) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.events); }

      var streamAttr = SerializationManager.GetStreamProvider(actualType, expectedType);

      var eventDatas = SerializationManager.SerializeEvents(streamAttr, actualType, events, eventContext, expectedType);

      if (streamAttr != null)
      {
        return connection.AppendToStreamAsync(CombineStreamId(streamAttr.StreamId, topic), streamAttr.ExpectedVersion, eventDatas, userCredentials);
      }
      return connection.AppendToStreamAsync(CombineStreamId(RuntimeTypeNameFormatter.Serialize(expectedType ?? actualType), topic), ExpectedVersion.Any, eventDatas, userCredentials);
    }




    public static async Task<WriteResult> PublishEventsAsync<TEvent>(this IEventStoreConnectionBase connection, string topic, IList<TEvent> events,
      IList<Dictionary<string, object>> eventContexts, Type expectedType = null, UserCredentials userCredentials = null)
      where TEvent : class
    {
      var actualType = typeof(TEvent);
      if (actualType == TypeConstants.ObjectType)
      {
        if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
        if (string.IsNullOrEmpty(topic)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.topic); }
        if (null == events) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.events); }
        if (null == eventContexts) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.eventContexts); }
        if (events.Count != eventContexts.Count) { ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.eventContexts); }

        WriteResult result = default(WriteResult);
        for (var idx = 0; idx < events.Count; idx++)
        {
          var @event = events[idx];
          result = await PublishEventAsync(connection, topic, @event.GetType(), @event, eventContexts[idx], expectedType, userCredentials).ConfigureAwait(false);
        }
        return result;
      }
      return await PublishEventsAsync(connection, topic, actualType, events, eventContexts, expectedType, userCredentials).ConfigureAwait(false);
    }
    public static Task<WriteResult> PublishEventsAsync<TEvent>(this IEventStoreConnectionBase connection, string topic, Type actualType, IList<TEvent> events,
      IList<Dictionary<string, object>> eventContexts, Type expectedType = null, UserCredentials userCredentials = null)
      where TEvent : class
    {
      if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
      if (string.IsNullOrEmpty(topic)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.topic); }
      if (null == actualType) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.actualType); }
      //if (null == events) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.events); }

      var streamAttr = SerializationManager.GetStreamProvider(actualType, expectedType);

      var eventDatas = SerializationManager.SerializeEvents(streamAttr, actualType, events, eventContexts, expectedType);

      if (streamAttr != null)
      {
        return connection.AppendToStreamAsync(CombineStreamId(streamAttr.StreamId, topic), streamAttr.ExpectedVersion, eventDatas, userCredentials);
      }
      return connection.AppendToStreamAsync(CombineStreamId(RuntimeTypeNameFormatter.Serialize(expectedType ?? actualType), topic), ExpectedVersion.Any, eventDatas, userCredentials);
    }

    #endregion

    #region -- PublishEventsAsync(Transaction) --

    public static async Task<WriteResult> PublishEventsAsync<TEvent>(this IEventStoreConnectionBase connection, int batchSize, ICollection<TEvent> events,
      Dictionary<string, object> eventContext = null, Type expectedType = null, UserCredentials userCredentials = null)
      where TEvent : class
    {
      var actualType = typeof(TEvent);
      if (actualType == TypeConstants.ObjectType)
      {
        if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
        if (null == events) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.events); }

        // 无法确定所发布事件是否在同一 stream 中，不能采用事物
        WriteResult result = default(WriteResult);
        foreach (var @event in events)
        {
          result = await PublishEventAsync(connection, @event.GetType(), @event, eventContext, expectedType, userCredentials).ConfigureAwait(false);
        }
        return result;
      }

      return await PublishEventsAsync(connection, actualType, batchSize, events, eventContext, expectedType, userCredentials).ConfigureAwait(false);
    }
    public static async Task<WriteResult> PublishEventsAsync<TEvent>(this IEventStoreConnectionBase connection, Type actualType, int batchSize, ICollection<TEvent> events,
      Dictionary<string, object> eventContext = null, Type expectedType = null, UserCredentials userCredentials = null)
      where TEvent : class
    {
      if (batchSize <= 0) { ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.batchSize); }
      if (null == events) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.events); }

      if (events.Count <= batchSize)
      {
        return await PublishEventsAsync(connection, actualType, events, eventContext, expectedType, userCredentials).ConfigureAwait(false);
      }

      if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
      if (null == actualType) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.actualType); }

      var streamAttr = SerializationManager.GetStreamProvider(actualType, expectedType);
      var eventDatas = SerializationManager.SerializeEvents(streamAttr, actualType, events, eventContext, expectedType);
      if (streamAttr != null)
      {
        return await DoWriteAsync(connection, streamAttr.StreamId, streamAttr.ExpectedVersion, eventDatas, batchSize, userCredentials).ConfigureAwait(false);
      }
      else
      {
        return await DoWriteAsync(connection, RuntimeTypeNameFormatter.Serialize(expectedType ?? actualType), ExpectedVersion.Any, eventDatas, batchSize, userCredentials).ConfigureAwait(false);
      }
    }




    public static async Task<WriteResult> PublishEventsAsync<TEvent>(this IEventStoreConnectionBase connection, int batchSize, IList<TEvent> events,
      IList<Dictionary<string, object>> eventContexts, Type expectedType = null, UserCredentials userCredentials = null)
      where TEvent : class
    {
      var actualType = typeof(TEvent);
      if (actualType == TypeConstants.ObjectType)
      {
        if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
        if (null == events) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.events); }
        if (null == eventContexts) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.eventContexts); }
        if (events.Count != eventContexts.Count) { ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.eventContexts); }

        // 无法确定所发布事件是否在同一 stream 中，不能采用事物
        WriteResult result = default(WriteResult);
        for (var idx = 0; idx < events.Count; idx++)
        {
          var @event = events[idx];
          result = await PublishEventAsync(connection, @event.GetType(), @event, eventContexts[idx], expectedType, userCredentials).ConfigureAwait(false);
        }
        return result;
      }
      return await PublishEventsAsync(connection, actualType, batchSize, events, eventContexts, expectedType, userCredentials).ConfigureAwait(false);
    }
    public static async Task<WriteResult> PublishEventsAsync<TEvent>(this IEventStoreConnectionBase connection, Type actualType, int batchSize, IList<TEvent> events,
      IList<Dictionary<string, object>> eventContexts, Type expectedType = null, UserCredentials userCredentials = null)
      where TEvent : class
    {
      if (batchSize <= 0) { ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.batchSize); }
      if (null == events) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.events); }

      if (events.Count <= batchSize)
      {
        return await PublishEventsAsync(connection, actualType, events, eventContexts, expectedType, userCredentials).ConfigureAwait(false);
      }

      if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
      if (null == actualType) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.actualType); }

      var streamAttr = SerializationManager.GetStreamProvider(actualType, expectedType);
      var eventDatas = SerializationManager.SerializeEvents(streamAttr, actualType, events, eventContexts, expectedType);
      if (streamAttr != null)
      {
        return await DoWriteAsync(connection, streamAttr.StreamId, streamAttr.ExpectedVersion, eventDatas, batchSize, userCredentials).ConfigureAwait(false);
      }
      else
      {
        return await DoWriteAsync(connection, RuntimeTypeNameFormatter.Serialize(expectedType ?? actualType), ExpectedVersion.Any, eventDatas, batchSize, userCredentials).ConfigureAwait(false);
      }
    }

    #endregion

    #region -- PublishEventsAsync(Transaction & Topic) --

    public static async Task<WriteResult> PublishEventsAsync<TEvent>(this IEventStoreConnectionBase connection, string topic, int batchSize, ICollection<TEvent> events,
      Dictionary<string, object> eventContext = null, Type expectedType = null, UserCredentials userCredentials = null)
      where TEvent : class
    {
      var actualType = typeof(TEvent);
      if (actualType == TypeConstants.ObjectType)
      {
        if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
        if (string.IsNullOrEmpty(topic)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.topic); }
        if (null == events) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.events); }

        // 无法确定所发布事件是否在同一 stream 中，不能采用事物
        WriteResult result = default(WriteResult);
        foreach (var @event in events)
        {
          result = await PublishEventAsync(connection, topic, @event.GetType(), @event, eventContext, expectedType, userCredentials).ConfigureAwait(false);
        }
        return result;
      }

      return await PublishEventsAsync(connection, topic, actualType, batchSize, events, eventContext, expectedType, userCredentials).ConfigureAwait(false);
    }
    public static async Task<WriteResult> PublishEventsAsync<TEvent>(this IEventStoreConnectionBase connection, string topic, Type actualType, int batchSize, ICollection<TEvent> events,
      Dictionary<string, object> eventContext = null, Type expectedType = null, UserCredentials userCredentials = null)
      where TEvent : class
    {
      if (batchSize <= 0) { ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.batchSize); }
      if (null == events) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.events); }

      if (events.Count <= batchSize)
      {
        return await PublishEventsAsync(connection, topic, actualType, events, eventContext, expectedType, userCredentials).ConfigureAwait(false);
      }

      if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
      if (string.IsNullOrEmpty(topic)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.topic); }
      if (null == actualType) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.actualType); }

      var streamAttr = SerializationManager.GetStreamProvider(actualType, expectedType);
      var eventDatas = SerializationManager.SerializeEvents(streamAttr, actualType, events, eventContext, expectedType);
      if (streamAttr != null)
      {
        return await DoWriteAsync(connection, CombineStreamId(streamAttr.StreamId, topic), streamAttr.ExpectedVersion, eventDatas, batchSize, userCredentials).ConfigureAwait(false);
      }
      else
      {
        return await DoWriteAsync(connection, CombineStreamId(RuntimeTypeNameFormatter.Serialize(expectedType ?? actualType), topic), ExpectedVersion.Any, eventDatas, batchSize, userCredentials).ConfigureAwait(false);
      }
    }




    public static async Task<WriteResult> PublishEventsAsync<TEvent>(this IEventStoreConnectionBase connection, string topic, int batchSize, IList<TEvent> events,
      IList<Dictionary<string, object>> eventContexts, Type expectedType = null, UserCredentials userCredentials = null)
      where TEvent : class
    {
      var actualType = typeof(TEvent);
      if (actualType == TypeConstants.ObjectType)
      {
        if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
        if (string.IsNullOrEmpty(topic)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.topic); }
        if (null == events) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.events); }
        if (null == eventContexts) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.eventContexts); }
        if (events.Count != eventContexts.Count) { ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.eventContexts); }

        // 无法确定所发布事件是否在同一 stream 中，不能采用事物
        WriteResult result = default(WriteResult);
        for (var idx = 0; idx < events.Count; idx++)
        {
          var @event = events[idx];
          result = await PublishEventAsync(connection, topic, @event.GetType(), @event, eventContexts[idx], expectedType, userCredentials).ConfigureAwait(false);
        }
        return result;
      }
      return await PublishEventsAsync(connection, topic, actualType, batchSize, events, eventContexts, expectedType, userCredentials).ConfigureAwait(false);
    }
    public static async Task<WriteResult> PublishEventsAsync<TEvent>(this IEventStoreConnectionBase connection, string topic, Type actualType, int batchSize, IList<TEvent> events,
      IList<Dictionary<string, object>> eventContexts, Type expectedType = null, UserCredentials userCredentials = null)
      where TEvent : class
    {
      if (batchSize <= 0) { ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.batchSize); }
      if (null == events) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.events); }

      if (events.Count <= batchSize)
      {
        return await PublishEventsAsync(connection, topic, actualType, events, eventContexts, expectedType, userCredentials).ConfigureAwait(false);
      }

      if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
      if (string.IsNullOrEmpty(topic)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.topic); }
      if (null == actualType) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.actualType); }

      var streamAttr = SerializationManager.GetStreamProvider(actualType, expectedType);
      var eventDatas = SerializationManager.SerializeEvents(streamAttr, actualType, events, eventContexts, expectedType);
      if (streamAttr != null)
      {
        return await DoWriteAsync(connection, CombineStreamId(streamAttr.StreamId, topic), streamAttr.ExpectedVersion, eventDatas, batchSize, userCredentials).ConfigureAwait(false);
      }
      else
      {
        return await DoWriteAsync(connection, CombineStreamId(RuntimeTypeNameFormatter.Serialize(expectedType ?? actualType), topic), ExpectedVersion.Any, eventDatas, batchSize, userCredentials).ConfigureAwait(false);
      }
    }

    #endregion
  }
}
