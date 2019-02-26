using System.Collections.Generic;
using System.Threading.Tasks;
using EventStore.ClientAPI.SystemData;

namespace EventStore.ClientAPI
{
    partial class IEventStoreConnectionExtensions
    {
        #region -- SendEventAsync --

        public static Task<WriteResult> SendEventAsync(this IEventStoreConnectionBase connection, string stream, object @event,
            Dictionary<string, object> eventContext, UserCredentials userCredentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }

            var eventData = EventManager.ToEventData(@event, eventContext.ToEventMetadata());
            return connection.AppendToStreamAsync(stream, ExpectedVersion.Any, eventData, userCredentials);
        }

        public static Task<WriteResult> SendEventAsync(this IEventStoreConnectionBase connection, string stream, object @event,
            IEventMetadata eventMeta = null, UserCredentials userCredentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }

            var eventData = EventManager.ToEventData(@event, eventMeta);
            return connection.AppendToStreamAsync(stream, ExpectedVersion.Any, eventData, userCredentials);
        }

        #endregion

        #region -- SendEventAsync(Topic) --

        public static Task<WriteResult> SendEventAsync(this IEventStoreConnectionBase connection, string stream, string topic, object @event,
            Dictionary<string, object> eventContext, UserCredentials userCredentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }

            var eventData = EventManager.ToEventData(@event, eventContext.ToEventMetadata());
            return connection.AppendToStreamAsync(stream.Combine(topic), ExpectedVersion.Any, eventData, userCredentials);
        }

        public static Task<WriteResult> SendEventAsync(this IEventStoreConnectionBase connection, string stream, string topic, object @event,
            IEventMetadata eventMeta = null, UserCredentials userCredentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }

            var eventData = EventManager.ToEventData(@event, eventMeta);
            return connection.AppendToStreamAsync(stream.Combine(topic), ExpectedVersion.Any, eventData, userCredentials);
        }

        #endregion

        #region -- SendEventsAsync --

        public static Task<WriteResult> SendEventsAsync(this IEventStoreConnectionBase connection, string stream, IList<object> events,
            Dictionary<string, object> eventContext, UserCredentials userCredentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }

            var eventDatas = EventManager.ToEventDatas(events, eventContext);
            return connection.AppendToStreamAsync(stream, ExpectedVersion.Any, eventDatas, userCredentials);
        }

        public static Task<WriteResult> SendEventsAsync(this IEventStoreConnectionBase connection, string stream, IList<object> events,
            IEventMetadata eventMeta = null, UserCredentials userCredentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }

            var eventDatas = EventManager.ToEventDatas(events, eventMeta);
            return connection.AppendToStreamAsync(stream, ExpectedVersion.Any, eventDatas, userCredentials);
        }


        public static Task<WriteResult> SendEventsAsync(this IEventStoreConnectionBase connection, string stream, IList<object> events,
            IList<Dictionary<string, object>> eventContexts, UserCredentials userCredentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }

            var eventDatas = EventManager.ToEventDatas(events, eventContexts);
            return connection.AppendToStreamAsync(stream, ExpectedVersion.Any, eventDatas, userCredentials);
        }

        public static Task<WriteResult> SendEventsAsync(this IEventStoreConnectionBase connection, string stream, IList<object> events,
            IList<IEventMetadata> eventMetas, UserCredentials userCredentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }

            var eventDatas = EventManager.ToEventDatas(events, eventMetas);
            return connection.AppendToStreamAsync(stream, ExpectedVersion.Any, eventDatas, userCredentials);
        }


        public static Task<WriteResult> SendEventsAsync<TEvent>(this IEventStoreConnectionBase connection, string stream, IList<TEvent> events,
            Dictionary<string, object> eventContext, UserCredentials userCredentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }

            var eventDatas = EventManager.ToEventDatas(events, eventContext);
            return connection.AppendToStreamAsync(stream, ExpectedVersion.Any, eventDatas, userCredentials);
        }

        public static Task<WriteResult> SendEventsAsync<TEvent>(this IEventStoreConnectionBase connection, string stream, IList<TEvent> events,
            IEventMetadata eventMeta = null, UserCredentials userCredentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }

            var eventDatas = EventManager.ToEventDatas(events, eventMeta);
            return connection.AppendToStreamAsync(stream, ExpectedVersion.Any, eventDatas, userCredentials);
        }

        public static Task<WriteResult> SendEventsAsync<TEvent>(this IEventStoreConnectionBase connection, string stream, IList<TEvent> events,
            IList<Dictionary<string, object>> eventContexts, UserCredentials userCredentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }

            var eventDatas = EventManager.ToEventDatas(events, eventContexts);
            return connection.AppendToStreamAsync(stream, ExpectedVersion.Any, eventDatas, userCredentials);
        }

        public static Task<WriteResult> SendEventsAsync<TEvent>(this IEventStoreConnectionBase connection, string stream, IList<TEvent> events,
            IList<IEventMetadata> eventMetas, UserCredentials userCredentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }

            var eventDatas = EventManager.ToEventDatas(events, eventMetas);
            return connection.AppendToStreamAsync(stream, ExpectedVersion.Any, eventDatas, userCredentials);
        }

        #endregion

        #region -- SendEventsAsync(Topic) --

        public static Task<WriteResult> SendEventsAsync(this IEventStoreConnectionBase connection, string stream, string topic, IList<object> events,
            Dictionary<string, object> eventContext, UserCredentials userCredentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }

            var eventDatas = EventManager.ToEventDatas(events, eventContext);
            return connection.AppendToStreamAsync(stream.Combine(topic), ExpectedVersion.Any, eventDatas, userCredentials);
        }

        public static Task<WriteResult> SendEventsAsync(this IEventStoreConnectionBase connection, string stream, string topic, IList<object> events,
            IEventMetadata eventMeta = null, UserCredentials userCredentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }

            var eventDatas = EventManager.ToEventDatas(events, eventMeta);
            return connection.AppendToStreamAsync(stream.Combine(topic), ExpectedVersion.Any, eventDatas, userCredentials);
        }

        public static Task<WriteResult> SendEventsAsync(this IEventStoreConnectionBase connection, string stream, string topic, IList<object> events,
            IList<Dictionary<string, object>> eventContexts, UserCredentials userCredentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }

            var eventDatas = EventManager.ToEventDatas(events, eventContexts);
            return connection.AppendToStreamAsync(stream.Combine(topic), ExpectedVersion.Any, eventDatas, userCredentials);
        }

        public static Task<WriteResult> SendEventsAsync(this IEventStoreConnectionBase connection, string stream, string topic, IList<object> events,
            IList<IEventMetadata> eventMetas, UserCredentials userCredentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }

            var eventDatas = EventManager.ToEventDatas(events, eventMetas);
            return connection.AppendToStreamAsync(stream.Combine(topic), ExpectedVersion.Any, eventDatas, userCredentials);
        }

        public static Task<WriteResult> SendEventsAsync<TEvent>(this IEventStoreConnectionBase connection, string stream, string topic, IList<TEvent> events,
            Dictionary<string, object> eventContext, UserCredentials userCredentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }

            var eventDatas = EventManager.ToEventDatas(events, eventContext);
            return connection.AppendToStreamAsync(stream.Combine(topic), ExpectedVersion.Any, eventDatas, userCredentials);
        }

        public static Task<WriteResult> SendEventsAsync<TEvent>(this IEventStoreConnectionBase connection, string stream, string topic, IList<TEvent> events,
            IEventMetadata eventMeta = null, UserCredentials userCredentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }

            var eventDatas = EventManager.ToEventDatas(events, eventMeta);
            return connection.AppendToStreamAsync(stream.Combine(topic), ExpectedVersion.Any, eventDatas, userCredentials);
        }

        public static Task<WriteResult> SendEventsAsync<TEvent>(this IEventStoreConnectionBase connection, string stream, string topic, IList<TEvent> events,
            IList<Dictionary<string, object>> eventContexts, UserCredentials userCredentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }

            var eventDatas = EventManager.ToEventDatas(events, eventContexts);
            return connection.AppendToStreamAsync(stream.Combine(topic), ExpectedVersion.Any, eventDatas, userCredentials);
        }

        public static Task<WriteResult> SendEventsAsync<TEvent>(this IEventStoreConnectionBase connection, string stream, string topic, IList<TEvent> events,
            IList<IEventMetadata> eventMetas, UserCredentials userCredentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }

            var eventDatas = EventManager.ToEventDatas(events, eventMetas);
            return connection.AppendToStreamAsync(stream.Combine(topic), ExpectedVersion.Any, eventDatas, userCredentials);
        }

        #endregion

        #region -- SendEventsAsync(Transaction) --

        public static Task<WriteResult> SendEventsAsync(this IEventStoreConnectionBase connection, string stream, int batchSize,
            IList<object> events, Dictionary<string, object> eventContext, UserCredentials userCredentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
            if (batchSize <= 0) { ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.batchSize); }

            var eventDatas = EventManager.ToEventDatas(events, eventContext);
            return DoWriteAsync(connection, stream, ExpectedVersion.Any, eventDatas, batchSize, userCredentials);
        }

        public static Task<WriteResult> SendEventsAsync(this IEventStoreConnectionBase connection, string stream, int batchSize,
            IList<object> events, IEventMetadata eventMeta = null, UserCredentials userCredentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
            if (batchSize <= 0) { ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.batchSize); }

            var eventDatas = EventManager.ToEventDatas(events, eventMeta);
            return DoWriteAsync(connection, stream, ExpectedVersion.Any, eventDatas, batchSize, userCredentials);
        }

        public static Task<WriteResult> SendEventsAsync(this IEventStoreConnectionBase connection, string stream, int batchSize,
            IList<object> events, IList<Dictionary<string, object>> eventContexts, UserCredentials userCredentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
            if (batchSize <= 0) { ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.batchSize); }

            var eventDatas = EventManager.ToEventDatas(events, eventContexts);
            return DoWriteAsync(connection, stream, ExpectedVersion.Any, eventDatas, batchSize, userCredentials);
        }

        public static Task<WriteResult> SendEventsAsync(this IEventStoreConnectionBase connection, string stream, int batchSize,
            IList<object> events, IList<IEventMetadata> eventMetas, UserCredentials userCredentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
            if (batchSize <= 0) { ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.batchSize); }

            var eventDatas = EventManager.ToEventDatas(events, eventMetas);
            return DoWriteAsync(connection, stream, ExpectedVersion.Any, eventDatas, batchSize, userCredentials);
        }


        public static Task<WriteResult> SendEventsAsync<TEvent>(this IEventStoreConnectionBase connection, string stream, int batchSize,
            IList<TEvent> events, Dictionary<string, object> eventContext, UserCredentials userCredentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
            if (batchSize <= 0) { ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.batchSize); }

            var eventDatas = EventManager.ToEventDatas(events, eventContext);
            return DoWriteAsync(connection, stream, ExpectedVersion.Any, eventDatas, batchSize, userCredentials);
        }

        public static Task<WriteResult> SendEventsAsync<TEvent>(this IEventStoreConnectionBase connection, string stream, int batchSize,
            IList<TEvent> events, IEventMetadata eventMeta = null, UserCredentials userCredentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
            if (batchSize <= 0) { ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.batchSize); }

            var eventDatas = EventManager.ToEventDatas(events, eventMeta);
            return DoWriteAsync(connection, stream, ExpectedVersion.Any, eventDatas, batchSize, userCredentials);
        }

        public static Task<WriteResult> SendEventsAsync<TEvent>(this IEventStoreConnectionBase connection, string stream, int batchSize,
            IList<TEvent> events, IList<Dictionary<string, object>> eventContexts, UserCredentials userCredentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
            if (batchSize <= 0) { ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.batchSize); }

            var eventDatas = EventManager.ToEventDatas(events, eventContexts);
            return DoWriteAsync(connection, stream, ExpectedVersion.Any, eventDatas, batchSize, userCredentials);
        }

        public static Task<WriteResult> SendEventsAsync<TEvent>(this IEventStoreConnectionBase connection, string stream, int batchSize,
            IList<TEvent> events, IList<IEventMetadata> eventMetas, UserCredentials userCredentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
            if (batchSize <= 0) { ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.batchSize); }

            var eventDatas = EventManager.ToEventDatas(events, eventMetas);
            return DoWriteAsync(connection, stream, ExpectedVersion.Any, eventDatas, batchSize, userCredentials);
        }

        #endregion

        #region -- SendEventsAsync(Transaction & Topic) --

        public static Task<WriteResult> SendEventsAsync(this IEventStoreConnectionBase connection, string stream, string topic, int batchSize,
            IList<object> events, Dictionary<string, object> eventContext, UserCredentials userCredentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
            if (batchSize <= 0) { ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.batchSize); }

            var eventDatas = EventManager.ToEventDatas(events, eventContext);
            return DoWriteAsync(connection, stream.Combine(topic), ExpectedVersion.Any, eventDatas, batchSize, userCredentials);
        }

        public static Task<WriteResult> SendEventsAsync(this IEventStoreConnectionBase connection, string stream, string topic, int batchSize,
            IList<object> events, IEventMetadata eventMeta = null, UserCredentials userCredentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
            if (batchSize <= 0) { ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.batchSize); }

            var eventDatas = EventManager.ToEventDatas(events, eventMeta);
            return DoWriteAsync(connection, stream.Combine(topic), ExpectedVersion.Any, eventDatas, batchSize, userCredentials);
        }

        public static Task<WriteResult> SendEventsAsync(this IEventStoreConnectionBase connection, string stream, string topic, int batchSize,
            IList<object> events, IList<Dictionary<string, object>> eventContexts, UserCredentials userCredentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
            if (batchSize <= 0) { ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.batchSize); }

            var eventDatas = EventManager.ToEventDatas(events, eventContexts);
            return DoWriteAsync(connection, stream.Combine(topic), ExpectedVersion.Any, eventDatas, batchSize, userCredentials);
        }

        public static Task<WriteResult> SendEventsAsync(this IEventStoreConnectionBase connection, string stream, string topic, int batchSize,
            IList<object> events, IList<IEventMetadata> eventMetas, UserCredentials userCredentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
            if (batchSize <= 0) { ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.batchSize); }

            var eventDatas = EventManager.ToEventDatas(events, eventMetas);
            return DoWriteAsync(connection, stream.Combine(topic), ExpectedVersion.Any, eventDatas, batchSize, userCredentials);
        }


        public static Task<WriteResult> SendEventsAsync<TEvent>(this IEventStoreConnectionBase connection, string stream, string topic, int batchSize,
            IList<TEvent> events, Dictionary<string, object> eventContext, UserCredentials userCredentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
            if (batchSize <= 0) { ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.batchSize); }

            var eventDatas = EventManager.ToEventDatas(events, eventContext);
            return DoWriteAsync(connection, stream.Combine(topic), ExpectedVersion.Any, eventDatas, batchSize, userCredentials);
        }

        public static Task<WriteResult> SendEventsAsync<TEvent>(this IEventStoreConnectionBase connection, string stream, string topic, int batchSize,
            IList<TEvent> events, IEventMetadata eventMeta = null, UserCredentials userCredentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
            if (batchSize <= 0) { ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.batchSize); }

            var eventDatas = EventManager.ToEventDatas(events, eventMeta);
            return DoWriteAsync(connection, stream.Combine(topic), ExpectedVersion.Any, eventDatas, batchSize, userCredentials);
        }

        public static Task<WriteResult> SendEventsAsync<TEvent>(this IEventStoreConnectionBase connection, string stream, string topic, int batchSize,
            IList<TEvent> events, IList<Dictionary<string, object>> eventContexts, UserCredentials userCredentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
            if (batchSize <= 0) { ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.batchSize); }

            var eventDatas = EventManager.ToEventDatas(events, eventContexts);
            return DoWriteAsync(connection, stream.Combine(topic), ExpectedVersion.Any, eventDatas, batchSize, userCredentials);
        }

        public static Task<WriteResult> SendEventsAsync<TEvent>(this IEventStoreConnectionBase connection, string stream, string topic, int batchSize,
            IList<TEvent> events, IList<IEventMetadata> eventMetas, UserCredentials userCredentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
            if (batchSize <= 0) { ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.batchSize); }

            var eventDatas = EventManager.ToEventDatas(events, eventMetas);
            return DoWriteAsync(connection, stream.Combine(topic), ExpectedVersion.Any, eventDatas, batchSize, userCredentials);
        }

        #endregion
    }
}
