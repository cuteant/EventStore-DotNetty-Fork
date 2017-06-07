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
    #region -- SendAsync --

    public static Task<WriteResult> SendAsync<TEvent>(this IEventStoreConnectionBase connection, string stream, TEvent @event,
      Dictionary<string, object> eventContext = null, Type expectedType = null, UserCredentials userCredentials = null)
      where TEvent : class
    {
      var actualType = typeof(TEvent);
      if (actualType == TypeHelper.ObjectType)
      {
        if (null == @event) { throw new ArgumentNullException(nameof(@event)); }
        actualType = @event?.GetType();
      }
      return SendAsync(connection, stream, actualType, @event, eventContext, expectedType, userCredentials);
    }
    public static Task<WriteResult> SendAsync<TEvent>(this IEventStoreConnectionBase connection, string stream, Type actualType, TEvent @event,
      Dictionary<string, object> eventContext = null, Type expectedType = null, UserCredentials userCredentials = null)
      where TEvent : class
    {
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }
      if (string.IsNullOrWhiteSpace(stream)) { throw new ArgumentNullException(nameof(stream)); }
      if (null == actualType) { throw new ArgumentNullException(nameof(actualType)); }
      //if (null == @event) { throw new ArgumentNullException(nameof(@event)); }

      var streamAttr = SerializationManager.GetStreamProvider(actualType, expectedType);

      var eventData = SerializationManager.SerializeEvent(streamAttr, actualType, @event, eventContext, expectedType);

      return connection.AppendToStreamAsync(stream, streamAttr != null ? streamAttr.ExpectedVersion : ExpectedVersion.Any, userCredentials, eventData);
    }




    public static Task<WriteResult> SendAsync<TEvent>(this IEventStoreConnectionBase connection, string stream, IEnumerable<TEvent> events,
      Dictionary<string, object> eventContext = null, Type expectedType = null, UserCredentials userCredentials = null)
      where TEvent : class
    {
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }
      if (string.IsNullOrWhiteSpace(stream)) { throw new ArgumentNullException(nameof(stream)); }

      var actualType = typeof(TEvent);
      if (actualType == TypeHelper.ObjectType)
      {
        if (null == events) { throw new ArgumentNullException(nameof(events)); }
        //var eventDatas = SerializationManager.SerializeEvents(events, eventContext, expectedType);
        var eventDatas = events.Select(_ => SerializationManager.SerializeEvent(_, eventContext, expectedType)).ToArray();
        return connection.AppendToStreamAsync(stream, ExpectedVersion.Any, eventDatas, userCredentials);
      }
      return SendAsync(connection, stream, actualType, events, eventContext, expectedType, userCredentials);
    }
    public static Task<WriteResult> SendAsync<TEvent>(this IEventStoreConnectionBase connection, string stream, Type actualType, IEnumerable<TEvent> events,
      Dictionary<string, object> eventContext = null, Type expectedType = null, UserCredentials userCredentials = null)
      where TEvent : class
    {
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }
      if (string.IsNullOrWhiteSpace(stream)) { throw new ArgumentNullException(nameof(stream)); }
      if (null == actualType) { throw new ArgumentNullException(nameof(actualType)); }
      //if (null == events) { throw new ArgumentNullException(nameof(events)); }

      var streamAttr = SerializationManager.GetStreamProvider(actualType, expectedType);

      var eventDatas = SerializationManager.SerializeEvents(streamAttr, actualType, events, eventContext, expectedType);

      return connection.AppendToStreamAsync(stream, streamAttr != null ? streamAttr.ExpectedVersion : ExpectedVersion.Any, eventDatas, userCredentials);
    }




    public static Task<WriteResult> SendAsync<TEvent>(this IEventStoreConnectionBase connection, string stream, IList<TEvent> events,
      IList<Dictionary<string, object>> eventContexts, Type expectedType = null, UserCredentials userCredentials = null)
      where TEvent : class
    {
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }
      if (string.IsNullOrWhiteSpace(stream)) { throw new ArgumentNullException(nameof(stream)); }
      //if (null == events) { throw new ArgumentNullException(nameof(events)); }

      var actualType = typeof(TEvent);
      if (actualType == TypeHelper.ObjectType)
      {
        var eventDatas = SerializationManager.SerializeEvents(events, eventContexts, expectedType);
        return connection.AppendToStreamAsync(stream, ExpectedVersion.Any, eventDatas, userCredentials);
      }
      return SendAsync(connection, stream, actualType, events, eventContexts, expectedType, userCredentials);
    }
    public static Task<WriteResult> SendAsync<TEvent>(this IEventStoreConnectionBase connection, string stream, Type actualType, IList<TEvent> events,
      IList<Dictionary<string, object>> eventContexts, Type expectedType = null, UserCredentials userCredentials = null)
      where TEvent : class
    {
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }
      if (string.IsNullOrWhiteSpace(stream)) { throw new ArgumentNullException(nameof(stream)); }
      if (null == actualType) { throw new ArgumentNullException(nameof(actualType)); }
      //if (null == events) { throw new ArgumentNullException(nameof(events)); }

      var streamAttr = SerializationManager.GetStreamProvider(actualType, expectedType);

      var eventDatas = SerializationManager.SerializeEvents(streamAttr, actualType, events, eventContexts, expectedType);

      return connection.AppendToStreamAsync(stream, streamAttr != null ? streamAttr.ExpectedVersion : ExpectedVersion.Any, eventDatas, userCredentials);
    }

    #endregion
  }
}
