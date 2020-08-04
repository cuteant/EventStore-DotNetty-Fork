using System.Collections.Generic;
using System.Threading.Tasks;
using EventStore.ClientAPI.SystemData;

namespace EventStore.ClientAPI
{
    partial class IEventStoreConnectionExtensions
    {
        #region -- SendEventAsync --

        public static Task<WriteResult> SendEventAsync(this IEventStoreBus connection, string stream, object @event,
            Dictionary<string, object> eventContext, UserCredentials userCredentials = null)
        {
            if (connection is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }

            var eventData = connection.ToEventData(@event, eventContext);
            return connection.AppendToStreamAsync(stream, ExpectedVersion.Any, eventData, userCredentials);
        }

        public static Task<WriteResult> SendEventAsync(this IEventStoreBus connection, string stream, object @event,
            IEventMetadata eventMeta = null, UserCredentials userCredentials = null)
        {
            if (connection is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }

            var eventData = connection.ToEventData(@event, eventMeta);
            return connection.AppendToStreamAsync(stream, ExpectedVersion.Any, eventData, userCredentials);
        }

        #endregion

        #region -- SendEventAsync(Topic) --

        public static Task<WriteResult> SendEventAsync(this IEventStoreBus connection, string stream, string topic, object @event,
            Dictionary<string, object> eventContext, UserCredentials userCredentials = null)
        {
            if (connection is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }

            var eventData = connection.ToEventData(@event, eventContext);
            return connection.AppendToStreamAsync(stream.Combine(topic), ExpectedVersion.Any, eventData, userCredentials);
        }

        public static Task<WriteResult> SendEventAsync(this IEventStoreBus connection, string stream, string topic, object @event,
            IEventMetadata eventMeta = null, UserCredentials userCredentials = null)
        {
            if (connection is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }

            var eventData = connection.ToEventData(@event, eventMeta);
            return connection.AppendToStreamAsync(stream.Combine(topic), ExpectedVersion.Any, eventData, userCredentials);
        }

        #endregion

        #region -- SendEventsAsync --

        public static Task<WriteResult> SendEventsAsync(this IEventStoreBus connection, string stream, IList<object> events,
            IList<Dictionary<string, object>> eventContexts, UserCredentials userCredentials = null)
        {
            if (connection is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }

            var eventDatas = connection.ToEventDatas(events, eventContexts);
            return connection.AppendToStreamAsync(stream, ExpectedVersion.Any, eventDatas, userCredentials);
        }

        public static Task<WriteResult> SendEventsAsync(this IEventStoreBus connection, string stream, IList<object> events,
            IList<IEventMetadata> eventMetas = null, UserCredentials userCredentials = null)
        {
            if (connection is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }

            var eventDatas = connection.ToEventDatas(events, eventMetas);
            return connection.AppendToStreamAsync(stream, ExpectedVersion.Any, eventDatas, userCredentials);
        }

        public static Task<WriteResult> SendEventsAsync<TEvent>(this IEventStoreBus connection, string stream, IList<TEvent> events,
            IList<Dictionary<string, object>> eventContexts, UserCredentials userCredentials = null)
        {
            if (connection is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }

            var eventDatas = connection.ToEventDatas(events, eventContexts);
            return connection.AppendToStreamAsync(stream, ExpectedVersion.Any, eventDatas, userCredentials);
        }

        public static Task<WriteResult> SendEventsAsync<TEvent>(this IEventStoreBus connection, string stream, IList<TEvent> events,
            IList<IEventMetadata> eventMetas = null, UserCredentials userCredentials = null)
        {
            if (connection is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }

            var eventDatas = connection.ToEventDatas(events, eventMetas);
            return connection.AppendToStreamAsync(stream, ExpectedVersion.Any, eventDatas, userCredentials);
        }

        #endregion

        #region -- SendEventsAsync(Topic) --

        public static Task<WriteResult> SendEventsAsync(this IEventStoreBus connection, string stream, string topic, IList<object> events,
            IList<Dictionary<string, object>> eventContexts, UserCredentials userCredentials = null)
        {
            if (connection is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }

            var eventDatas = connection.ToEventDatas(events, eventContexts);
            return connection.AppendToStreamAsync(stream.Combine(topic), ExpectedVersion.Any, eventDatas, userCredentials);
        }

        public static Task<WriteResult> SendEventsAsync(this IEventStoreBus connection, string stream, string topic, IList<object> events,
            IList<IEventMetadata> eventMetas = null, UserCredentials userCredentials = null)
        {
            if (connection is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }

            var eventDatas = connection.ToEventDatas(events, eventMetas);
            return connection.AppendToStreamAsync(stream.Combine(topic), ExpectedVersion.Any, eventDatas, userCredentials);
        }

        public static Task<WriteResult> SendEventsAsync<TEvent>(this IEventStoreBus connection, string stream, string topic, IList<TEvent> events,
            IList<Dictionary<string, object>> eventContexts, UserCredentials userCredentials = null)
        {
            if (connection is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }

            var eventDatas = connection.ToEventDatas(events, eventContexts);
            return connection.AppendToStreamAsync(stream.Combine(topic), ExpectedVersion.Any, eventDatas, userCredentials);
        }

        public static Task<WriteResult> SendEventsAsync<TEvent>(this IEventStoreBus connection, string stream, string topic, IList<TEvent> events,
            IList<IEventMetadata> eventMetas = null, UserCredentials userCredentials = null)
        {
            if (connection is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }

            var eventDatas = connection.ToEventDatas(events, eventMetas);
            return connection.AppendToStreamAsync(stream.Combine(topic), ExpectedVersion.Any, eventDatas, userCredentials);
        }

        #endregion

        #region -- SendEventsAsync(Transaction) --

        public static Task<WriteResult> SendEventsAsync(this IEventStoreBus connection, string stream, int batchSize,
            IList<object> events, IList<Dictionary<string, object>> eventContexts, UserCredentials userCredentials = null)
        {
            if (connection is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
            if ((uint)(batchSize - 1) >= Consts.TooBigOrNegative) { ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.batchSize); }

            var eventDatas = connection.ToEventDatas(events, eventContexts);
            return DoWriteAsync(connection, stream, ExpectedVersion.Any, eventDatas, batchSize, userCredentials);
        }

        public static Task<WriteResult> SendEventsAsync(this IEventStoreBus connection, string stream, int batchSize,
            IList<object> events, IList<IEventMetadata> eventMetas = null, UserCredentials userCredentials = null)
        {
            if (connection is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
            if ((uint)(batchSize - 1) >= Consts.TooBigOrNegative) { ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.batchSize); }

            var eventDatas = connection.ToEventDatas(events, eventMetas);
            return DoWriteAsync(connection, stream, ExpectedVersion.Any, eventDatas, batchSize, userCredentials);
        }

        public static Task<WriteResult> SendEventsAsync<TEvent>(this IEventStoreBus connection, string stream, int batchSize,
            IList<TEvent> events, IList<Dictionary<string, object>> eventContexts, UserCredentials userCredentials = null)
        {
            if (connection is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
            if ((uint)(batchSize - 1) >= Consts.TooBigOrNegative) { ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.batchSize); }

            var eventDatas = connection.ToEventDatas(events, eventContexts);
            return DoWriteAsync(connection, stream, ExpectedVersion.Any, eventDatas, batchSize, userCredentials);
        }

        public static Task<WriteResult> SendEventsAsync<TEvent>(this IEventStoreBus connection, string stream, int batchSize,
            IList<TEvent> events, IList<IEventMetadata> eventMetas = null, UserCredentials userCredentials = null)
        {
            if (connection is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
            if ((uint)(batchSize - 1) >= Consts.TooBigOrNegative) { ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.batchSize); }

            var eventDatas = connection.ToEventDatas(events, eventMetas);
            return DoWriteAsync(connection, stream, ExpectedVersion.Any, eventDatas, batchSize, userCredentials);
        }

        #endregion

        #region -- SendEventsAsync(Transaction & Topic) --

        public static Task<WriteResult> SendEventsAsync(this IEventStoreBus connection, string stream, string topic, int batchSize,
            IList<object> events, IList<Dictionary<string, object>> eventContexts, UserCredentials userCredentials = null)
        {
            if (connection is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
            if ((uint)(batchSize - 1) >= Consts.TooBigOrNegative) { ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.batchSize); }

            var eventDatas = connection.ToEventDatas(events, eventContexts);
            return DoWriteAsync(connection, stream.Combine(topic), ExpectedVersion.Any, eventDatas, batchSize, userCredentials);
        }

        public static Task<WriteResult> SendEventsAsync(this IEventStoreBus connection, string stream, string topic, int batchSize,
            IList<object> events, IList<IEventMetadata> eventMetas = null, UserCredentials userCredentials = null)
        {
            if (connection is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
            if ((uint)(batchSize - 1) >= Consts.TooBigOrNegative) { ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.batchSize); }

            var eventDatas = connection.ToEventDatas(events, eventMetas);
            return DoWriteAsync(connection, stream.Combine(topic), ExpectedVersion.Any, eventDatas, batchSize, userCredentials);
        }

        public static Task<WriteResult> SendEventsAsync<TEvent>(this IEventStoreBus connection, string stream, string topic, int batchSize,
            IList<TEvent> events, IList<Dictionary<string, object>> eventContexts, UserCredentials userCredentials = null)
        {
            if (connection is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
            if ((uint)(batchSize - 1) >= Consts.TooBigOrNegative) { ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.batchSize); }

            var eventDatas = connection.ToEventDatas(events, eventContexts);
            return DoWriteAsync(connection, stream.Combine(topic), ExpectedVersion.Any, eventDatas, batchSize, userCredentials);
        }

        public static Task<WriteResult> SendEventsAsync<TEvent>(this IEventStoreBus connection, string stream, string topic, int batchSize,
            IList<TEvent> events, IList<IEventMetadata> eventMetas = null, UserCredentials userCredentials = null)
        {
            if (connection is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
            if ((uint)(batchSize - 1) >= Consts.TooBigOrNegative) { ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.batchSize); }

            var eventDatas = connection.ToEventDatas(events, eventMetas);
            return DoWriteAsync(connection, stream.Combine(topic), ExpectedVersion.Any, eventDatas, batchSize, userCredentials);
        }

        #endregion
    }
}
