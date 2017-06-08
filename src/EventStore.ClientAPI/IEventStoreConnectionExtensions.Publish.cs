using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CuteAnt.Reflection;
using CuteAnt.Text;
using EventStore.ClientAPI.Serialization;
using EventStore.ClientAPI.SystemData;
using Newtonsoft.Json;

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
      if (actualType == TypeHelper.ObjectType)
      {
        if (null == @event) { throw new ArgumentNullException(nameof(@event)); }
        actualType = @event.GetType();
      }
      return PublishEventAsync(connection, actualType, @event, eventContext, expectedType, userCredentials);
    }
    public static Task<WriteResult> PublishEventAsync<TEvent>(this IEventStoreConnectionBase connection, Type actualType, TEvent @event,
      Dictionary<string, object> eventContext = null, Type expectedType = null, UserCredentials userCredentials = null)
      where TEvent : class
    {
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }
      if (null == actualType) { throw new ArgumentNullException(nameof(actualType)); }
      //if (null == @event) { throw new ArgumentNullException(nameof(@event)); }

      var streamAttr = SerializationManager.GetStreamProvider(actualType, expectedType);

      var eventData = SerializationManager.SerializeEvent(streamAttr, actualType, @event, eventContext, expectedType);

      if (streamAttr != null)
      {
        return connection.AppendToStreamAsync(streamAttr.StreamId, streamAttr.ExpectedVersion, userCredentials, eventData);
      }
      return connection.AppendToStreamAsync(JsonConvertX.SerializeTypeName(expectedType ?? actualType), ExpectedVersion.Any, userCredentials, eventData);
    }




    public static Task<WriteResult> PublishEventAsync<TEvent>(this IEventStoreConnectionBase connection, string topic, TEvent @event,
      Dictionary<string, object> eventContext = null, Type expectedType = null, UserCredentials userCredentials = null)
      where TEvent : class
    {
      var actualType = typeof(TEvent);
      if (actualType == TypeHelper.ObjectType)
      {
        if (null == @event) { throw new ArgumentNullException(nameof(@event)); }
        actualType = @event.GetType();
      }
      return PublishEventAsync(connection, topic, actualType, @event, eventContext, expectedType, userCredentials);
    }
    public static Task<WriteResult> PublishEventAsync<TEvent>(this IEventStoreConnectionBase connection, string topic, Type actualType, TEvent @event,
      Dictionary<string, object> eventContext = null, Type expectedType = null, UserCredentials userCredentials = null)
      where TEvent : class
    {
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }
      if (null == actualType) { throw new ArgumentNullException(nameof(actualType)); }
      //if (null == @event) { throw new ArgumentNullException(nameof(@event)); }

      var streamAttr = SerializationManager.GetStreamProvider(actualType, expectedType);

      var eventData = SerializationManager.SerializeEvent(streamAttr, actualType, @event, eventContext, expectedType);

      if (streamAttr != null)
      {
        return connection.AppendToStreamAsync(CombineStreamId(streamAttr.StreamId, topic), streamAttr.ExpectedVersion, userCredentials, eventData);
      }
      return connection.AppendToStreamAsync(CombineStreamId(JsonConvertX.SerializeTypeName(expectedType ?? actualType), topic), ExpectedVersion.Any, userCredentials, eventData);
    }

    #endregion

    #region -- PublishEventsAsync --

    public static async Task<WriteResult> PublishEventsAsync<TEvent>(this IEventStoreConnectionBase connection, IEnumerable<TEvent> events,
      Dictionary<string, object> eventContext = null, Type expectedType = null, UserCredentials userCredentials = null)
      where TEvent : class
    {
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }

      var actualType = typeof(TEvent);
      if (actualType == TypeHelper.ObjectType)
      {
        if (null == events) { throw new ArgumentNullException(nameof(events)); }
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
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }
      if (null == actualType) { throw new ArgumentNullException(nameof(actualType)); }
      //if (null == events) { throw new ArgumentNullException(nameof(events)); }

      var streamAttr = SerializationManager.GetStreamProvider(actualType, expectedType);

      var eventDatas = SerializationManager.SerializeEvents(streamAttr, actualType, events, eventContext, expectedType);

      if (streamAttr != null)
      {
        return connection.AppendToStreamAsync(streamAttr.StreamId, streamAttr.ExpectedVersion, eventDatas, userCredentials);
      }
      return connection.AppendToStreamAsync(JsonConvertX.SerializeTypeName(expectedType ?? actualType), ExpectedVersion.Any, eventDatas, userCredentials);
    }




    public static async Task<WriteResult> PublishEventsAsync<TEvent>(this IEventStoreConnectionBase connection, IList<TEvent> events,
      IList<Dictionary<string, object>> eventContexts, Type expectedType = null, UserCredentials userCredentials = null)
      where TEvent : class
    {
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }

      var actualType = typeof(TEvent);
      if (actualType == TypeHelper.ObjectType)
      {
        if (null == events) { throw new ArgumentNullException(nameof(events)); }
        if (null == eventContexts) { throw new ArgumentNullException(nameof(eventContexts)); }
        if (events.Count != eventContexts.Count) { throw new ArgumentOutOfRangeException(nameof(eventContexts)); }

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
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }
      if (null == actualType) { throw new ArgumentNullException(nameof(actualType)); }
      //if (null == events) { throw new ArgumentNullException(nameof(events)); }

      var streamAttr = SerializationManager.GetStreamProvider(actualType, expectedType);

      var eventDatas = SerializationManager.SerializeEvents(streamAttr, actualType, events, eventContexts, expectedType);

      if (streamAttr != null)
      {
        return connection.AppendToStreamAsync(streamAttr.StreamId, streamAttr.ExpectedVersion, eventDatas, userCredentials);
      }
      return connection.AppendToStreamAsync(JsonConvertX.SerializeTypeName(expectedType ?? actualType), ExpectedVersion.Any, eventDatas, userCredentials);
    }




    public static async Task<WriteResult> PublishEventsAsync<TEvent>(this IEventStoreConnectionBase connection, string topic, IEnumerable<TEvent> events,
      Dictionary<string, object> eventContext = null, Type expectedType = null, UserCredentials userCredentials = null)
      where TEvent : class
    {
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }

      var actualType = typeof(TEvent);
      if (actualType == TypeHelper.ObjectType)
      {
        if (null == events) { throw new ArgumentNullException(nameof(events)); }
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
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }
      if (null == actualType) { throw new ArgumentNullException(nameof(actualType)); }
      //if (null == events) { throw new ArgumentNullException(nameof(events)); }

      var streamAttr = SerializationManager.GetStreamProvider(actualType, expectedType);

      var eventDatas = SerializationManager.SerializeEvents(streamAttr, actualType, events, eventContext, expectedType);

      if (streamAttr != null)
      {
        return connection.AppendToStreamAsync(CombineStreamId(streamAttr.StreamId, topic), streamAttr.ExpectedVersion, eventDatas, userCredentials);
      }
      return connection.AppendToStreamAsync(CombineStreamId(JsonConvertX.SerializeTypeName(expectedType ?? actualType), topic), ExpectedVersion.Any, eventDatas, userCredentials);
    }




    public static async Task<WriteResult> PublishEventsAsync<TEvent>(this IEventStoreConnectionBase connection, string topic, IList<TEvent> events,
      IList<Dictionary<string, object>> eventContexts, Type expectedType = null, UserCredentials userCredentials = null)
      where TEvent : class
    {
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }

      var actualType = typeof(TEvent);
      if (actualType == TypeHelper.ObjectType)
      {
        if (null == events) { throw new ArgumentNullException(nameof(events)); }
        if (null == eventContexts) { throw new ArgumentNullException(nameof(eventContexts)); }
        if (events.Count != eventContexts.Count) { throw new ArgumentOutOfRangeException(nameof(eventContexts)); }

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
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }
      if (null == actualType) { throw new ArgumentNullException(nameof(actualType)); }
      //if (null == events) { throw new ArgumentNullException(nameof(events)); }

      var streamAttr = SerializationManager.GetStreamProvider(actualType, expectedType);

      var eventDatas = SerializationManager.SerializeEvents(streamAttr, actualType, events, eventContexts, expectedType);

      if (streamAttr != null)
      {
        return connection.AppendToStreamAsync(CombineStreamId(streamAttr.StreamId, topic), streamAttr.ExpectedVersion, eventDatas, userCredentials);
      }
      return connection.AppendToStreamAsync(CombineStreamId(JsonConvertX.SerializeTypeName(expectedType ?? actualType), topic), ExpectedVersion.Any, eventDatas, userCredentials);
    }

    #endregion

    #region ** CombineStreamId **

    private static string CombineStreamId(string stream, string topic)
    {
      const char _separator = '-';

      var sb = StringBuilderCache.Acquire();
      sb.Append(stream);
      sb.Append(_separator);
      sb.Append(topic);
      return StringBuilderCache.GetStringAndRelease(sb);
    }

    #endregion
  }
}
