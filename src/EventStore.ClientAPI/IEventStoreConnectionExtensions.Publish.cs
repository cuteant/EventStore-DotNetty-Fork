using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EventStore.ClientAPI.SystemData;

namespace EventStore.ClientAPI
{
    partial class IEventStoreConnectionExtensions
    {
        #region -- PublishEventAsync --

        public static Task<WriteResult> PublishEventAsync(this IEventStoreConnectionBase connection, object @event,
            Dictionary<string, object> eventContext, Type expectedType = null, UserCredentials userCredentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }

            var eventData = EventManager.ToEventData(@event, eventContext.ToEventMetadata());
            return connection.AppendToStreamAsync(@event.GetType().GetStreamId(expectedType), ExpectedVersion.Any, eventData, userCredentials);
        }

        public static Task<WriteResult> PublishEventAsync(this IEventStoreConnectionBase connection, object @event,
            IEventMetadata eventMeta = null, Type expectedType = null, UserCredentials userCredentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }

            var eventData = EventManager.ToEventData(@event, eventMeta);
            return connection.AppendToStreamAsync(@event.GetType().GetStreamId(expectedType), ExpectedVersion.Any, eventData, userCredentials);
        }

        public static Task<WriteResult> PublishEventAsync<TEvent>(this IEventStoreConnectionBase connection, TEvent @event,
            Dictionary<string, object> eventContext, Type expectedType = null, UserCredentials userCredentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }

            var eventData = EventManager.ToEventData(@event, eventContext.ToEventMetadata());
            return connection.AppendToStreamAsync(EventManager.GetStreamId<TEvent>(expectedType), ExpectedVersion.Any, eventData, userCredentials);
        }

        public static Task<WriteResult> PublishEventAsync<TEvent>(this IEventStoreConnectionBase connection, TEvent @event,
            IEventMetadata eventMeta = null, Type expectedType = null, UserCredentials userCredentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }

            var eventData = EventManager.ToEventData(@event, eventMeta);
            return connection.AppendToStreamAsync(EventManager.GetStreamId<TEvent>(expectedType), ExpectedVersion.Any, eventData, userCredentials);
        }

        #endregion

        #region -- PublishEventAsync(Topic) --

        public static Task<WriteResult> PublishEventAsync(this IEventStoreConnectionBase connection, string topic, object @event,
            Dictionary<string, object> eventContext, Type expectedType = null, UserCredentials userCredentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }

            var eventData = EventManager.ToEventData(@event, eventContext.ToEventMetadata());
            return connection.AppendToStreamAsync(@event.GetType().GetStreamId(topic, expectedType), ExpectedVersion.Any, eventData, userCredentials);
        }

        public static Task<WriteResult> PublishEventAsync(this IEventStoreConnectionBase connection, string topic, object @event,
            IEventMetadata eventMeta = null, Type expectedType = null, UserCredentials userCredentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }

            var eventData = EventManager.ToEventData(@event, eventMeta);
            return connection.AppendToStreamAsync(@event.GetType().GetStreamId(topic, expectedType), ExpectedVersion.Any, eventData, userCredentials);
        }

        public static Task<WriteResult> PublishEventAsync<TEvent>(this IEventStoreConnectionBase connection, string topic, TEvent @event,
            Dictionary<string, object> eventContext, Type expectedType = null, UserCredentials userCredentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }

            var eventData = EventManager.ToEventData(@event, eventContext.ToEventMetadata());
            return connection.AppendToStreamAsync(EventManager.GetStreamId<TEvent>(topic, expectedType), ExpectedVersion.Any, eventData, userCredentials);
        }

        public static Task<WriteResult> PublishEventAsync<TEvent>(this IEventStoreConnectionBase connection, string topic, TEvent @event,
            IEventMetadata eventMeta = null, Type expectedType = null, UserCredentials userCredentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }

            var eventData = EventManager.ToEventData(@event, eventMeta);
            return connection.AppendToStreamAsync(EventManager.GetStreamId<TEvent>(topic, expectedType), ExpectedVersion.Any, eventData, userCredentials);
        }

        #endregion

        #region -- PublishEventsAsync --

        public static Task<WriteResult> PublishEventsAsync<TEvent>(this IEventStoreConnectionBase connection, IList<TEvent> events,
            Dictionary<string, object> eventContext, Type expectedType = null, UserCredentials userCredentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }

            var eventDatas = EventManager.ToEventDatas(events, eventContext);
            return connection.AppendToStreamAsync(EventManager.GetStreamId<TEvent>(expectedType), ExpectedVersion.Any, eventDatas, userCredentials);
        }
        public static Task<WriteResult> PublishEventsAsync<TEvent>(this IEventStoreConnectionBase connection, IList<TEvent> events,
            IEventMetadata eventMeta = null, Type expectedType = null, UserCredentials userCredentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }

            var eventDatas = EventManager.ToEventDatas(events, eventMeta);
            return connection.AppendToStreamAsync(EventManager.GetStreamId<TEvent>(expectedType), ExpectedVersion.Any, eventDatas, userCredentials);
        }

        public static Task<WriteResult> PublishEventsAsync<TEvent>(this IEventStoreConnectionBase connection, IList<TEvent> events,
            IList<Dictionary<string, object>> eventContexts, Type expectedType = null, UserCredentials userCredentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }

            var eventDatas = EventManager.ToEventDatas(events, eventContexts);
            return connection.AppendToStreamAsync(EventManager.GetStreamId<TEvent>(expectedType), ExpectedVersion.Any, eventDatas, userCredentials);
        }
        public static Task<WriteResult> PublishEventsAsync<TEvent>(this IEventStoreConnectionBase connection, IList<TEvent> events,
            IList<IEventMetadata> eventMetas, Type expectedType = null, UserCredentials userCredentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }

            var eventDatas = EventManager.ToEventDatas(events, eventMetas);
            return connection.AppendToStreamAsync(EventManager.GetStreamId<TEvent>(expectedType), ExpectedVersion.Any, eventDatas, userCredentials);
        }

        #endregion

        #region -- PublishEventsAsync(Topic) --

        public static Task<WriteResult> PublishEventsAsync<TEvent>(this IEventStoreConnectionBase connection, string topic, IList<TEvent> events,
          Dictionary<string, object> eventContext, Type expectedType = null, UserCredentials userCredentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }

            var eventDatas = EventManager.ToEventDatas(events, eventContext);
            return connection.AppendToStreamAsync(EventManager.GetStreamId<TEvent>(topic, expectedType), ExpectedVersion.Any, eventDatas, userCredentials);
        }

        public static Task<WriteResult> PublishEventsAsync<TEvent>(this IEventStoreConnectionBase connection, string topic, IList<TEvent> events,
          IEventMetadata eventMeta = null, Type expectedType = null, UserCredentials userCredentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }

            var eventDatas = EventManager.ToEventDatas(events, eventMeta);
            return connection.AppendToStreamAsync(EventManager.GetStreamId<TEvent>(topic, expectedType), ExpectedVersion.Any, eventDatas, userCredentials);
        }

        public static Task<WriteResult> PublishEventsAsync<TEvent>(this IEventStoreConnectionBase connection, string topic, IList<TEvent> events,
          IList<Dictionary<string, object>> eventContexts, Type expectedType = null, UserCredentials userCredentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }

            var eventDatas = EventManager.ToEventDatas(events, eventContexts);
            return connection.AppendToStreamAsync(EventManager.GetStreamId<TEvent>(topic, expectedType), ExpectedVersion.Any, eventDatas, userCredentials);
        }

        public static Task<WriteResult> PublishEventsAsync<TEvent>(this IEventStoreConnectionBase connection, string topic, IList<TEvent> events,
          IList<IEventMetadata> eventMetas, Type expectedType = null, UserCredentials userCredentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }

            var eventDatas = EventManager.ToEventDatas(events, eventMetas);
            return connection.AppendToStreamAsync(EventManager.GetStreamId<TEvent>(topic, expectedType), ExpectedVersion.Any, eventDatas, userCredentials);
        }

        #endregion

        #region -- PublishEventsAsync(Transaction) --

        public static Task<WriteResult> PublishEventsAsync<TEvent>(this IEventStoreConnectionBase connection, int batchSize, IList<TEvent> events,
            Dictionary<string, object> eventContext, Type expectedType = null, UserCredentials userCredentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
            if (batchSize <= 0) { ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.batchSize); }

            var eventDatas = EventManager.ToEventDatas(events, eventContext);
            return DoWriteAsync(connection, EventManager.GetStreamId<TEvent>(expectedType), ExpectedVersion.Any, eventDatas, batchSize, userCredentials);
        }

        public static Task<WriteResult> PublishEventsAsync<TEvent>(this IEventStoreConnectionBase connection, int batchSize, IList<TEvent> events,
            IEventMetadata eventMeta = null, Type expectedType = null, UserCredentials userCredentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
            if (batchSize <= 0) { ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.batchSize); }

            var eventDatas = EventManager.ToEventDatas(events, eventMeta);
            return DoWriteAsync(connection, EventManager.GetStreamId<TEvent>(expectedType), ExpectedVersion.Any, eventDatas, batchSize, userCredentials);
        }

        public static Task<WriteResult> PublishEventsAsync<TEvent>(this IEventStoreConnectionBase connection, int batchSize, IList<TEvent> events,
            IList<Dictionary<string, object>> eventContexts, Type expectedType = null, UserCredentials userCredentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
            if (batchSize <= 0) { ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.batchSize); }

            var eventDatas = EventManager.ToEventDatas(events, eventContexts);
            return DoWriteAsync(connection, EventManager.GetStreamId<TEvent>(expectedType), ExpectedVersion.Any, eventDatas, batchSize, userCredentials);
        }

        public static Task<WriteResult> PublishEventsAsync<TEvent>(this IEventStoreConnectionBase connection, int batchSize, IList<TEvent> events,
            IList<IEventMetadata> eventMetas, Type expectedType = null, UserCredentials userCredentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
            if (batchSize <= 0) { ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.batchSize); }

            var eventDatas = EventManager.ToEventDatas(events, eventMetas);
            return DoWriteAsync(connection, EventManager.GetStreamId<TEvent>(expectedType), ExpectedVersion.Any, eventDatas, batchSize, userCredentials);
        }

        #endregion

        #region -- PublishEventsAsync(Transaction & Topic) --

        public static Task<WriteResult> PublishEventsAsync<TEvent>(this IEventStoreConnectionBase connection, string topic, int batchSize, IList<TEvent> events,
          Dictionary<string, object> eventContext, Type expectedType = null, UserCredentials userCredentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
            if (batchSize <= 0) { ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.batchSize); }

            var eventDatas = EventManager.ToEventDatas(events, eventContext);
            return DoWriteAsync(connection, EventManager.GetStreamId<TEvent>(topic, expectedType), ExpectedVersion.Any, eventDatas, batchSize, userCredentials);
        }
        public static Task<WriteResult> PublishEventsAsync<TEvent>(this IEventStoreConnectionBase connection, string topic, int batchSize, IList<TEvent> events,
          IEventMetadata eventMeta = null, Type expectedType = null, UserCredentials userCredentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
            if (batchSize <= 0) { ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.batchSize); }

            var eventDatas = EventManager.ToEventDatas(events, eventMeta);
            return DoWriteAsync(connection, EventManager.GetStreamId<TEvent>(topic, expectedType), ExpectedVersion.Any, eventDatas, batchSize, userCredentials);
        }

        public static Task<WriteResult> PublishEventsAsync<TEvent>(this IEventStoreConnectionBase connection, string topic, int batchSize, IList<TEvent> events,
          IList<Dictionary<string, object>> eventContexts, Type expectedType = null, UserCredentials userCredentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
            if (batchSize <= 0) { ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.batchSize); }

            var eventDatas = EventManager.ToEventDatas(events, eventContexts);
            return DoWriteAsync(connection, EventManager.GetStreamId<TEvent>(topic, expectedType), ExpectedVersion.Any, eventDatas, batchSize, userCredentials);
        }

        public static Task<WriteResult> PublishEventsAsync<TEvent>(this IEventStoreConnectionBase connection, string topic, int batchSize, IList<TEvent> events,
          IList<IEventMetadata> eventMetas, Type expectedType = null, UserCredentials userCredentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
            if (batchSize <= 0) { ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.batchSize); }

            var eventDatas = EventManager.ToEventDatas(events, eventMetas);
            return DoWriteAsync(connection, EventManager.GetStreamId<TEvent>(topic, expectedType), ExpectedVersion.Any, eventDatas, batchSize, userCredentials);
        }

        #endregion
    }
}
