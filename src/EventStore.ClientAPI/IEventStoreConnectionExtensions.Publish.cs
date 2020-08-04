using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EventStore.ClientAPI.SystemData;

namespace EventStore.ClientAPI
{
    partial class IEventStoreConnectionExtensions
    {
        #region -- PublishEventAsync --

        public static Task<WriteResult> PublishEventAsync(this IEventStoreBus connection, object @event,
            Dictionary<string, object> eventContext, UserCredentials userCredentials = null)
        {
            if (connection is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }

            var eventData = connection.ToEventData(@event, eventContext);
            return connection.AppendToStreamAsync(@event.GetType().GetStreamId(), ExpectedVersion.Any, eventData, userCredentials);
        }

        public static Task<WriteResult> PublishEventAsync(this IEventStoreBus connection, object @event,
            IEventMetadata eventMeta = null, UserCredentials userCredentials = null)
        {
            if (connection is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }

            var eventData = connection.ToEventData(@event, eventMeta);
            return connection.AppendToStreamAsync(@event.GetType().GetStreamId(), ExpectedVersion.Any, eventData, userCredentials);
        }

        public static Task<WriteResult> PublishEventAsync<TEvent>(this IEventStoreBus connection, TEvent @event,
            Dictionary<string, object> eventContext, UserCredentials userCredentials = null)
        {
            if (connection is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }

            var eventData = connection.ToEventData(@event, eventContext);
            return connection.AppendToStreamAsync(EventManager.GetStreamId<TEvent>(), ExpectedVersion.Any, eventData, userCredentials);
        }

        public static Task<WriteResult> PublishEventAsync<TEvent>(this IEventStoreBus connection, TEvent @event,
            IEventMetadata eventMeta = null, UserCredentials userCredentials = null)
        {
            if (connection is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }

            var eventData = connection.ToEventData(@event, eventMeta);
            return connection.AppendToStreamAsync(EventManager.GetStreamId<TEvent>(), ExpectedVersion.Any, eventData, userCredentials);
        }

        #endregion

        #region -- PublishEventAsync(Topic) --

        public static Task<WriteResult> PublishEventAsync(this IEventStoreBus connection, string topic, object @event,
            Dictionary<string, object> eventContext, UserCredentials userCredentials = null)
        {
            if (connection is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }

            var eventData = connection.ToEventData(@event, eventContext);
            return connection.AppendToStreamAsync(@event.GetType().GetStreamId(topic), ExpectedVersion.Any, eventData, userCredentials);
        }

        public static Task<WriteResult> PublishEventAsync(this IEventStoreBus connection, string topic, object @event,
            IEventMetadata eventMeta = null, UserCredentials userCredentials = null)
        {
            if (connection is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }

            var eventData = connection.ToEventData(@event, eventMeta);
            return connection.AppendToStreamAsync(@event.GetType().GetStreamId(topic), ExpectedVersion.Any, eventData, userCredentials);
        }

        public static Task<WriteResult> PublishEventAsync<TEvent>(this IEventStoreBus connection, string topic, TEvent @event,
            Dictionary<string, object> eventContext, UserCredentials userCredentials = null)
        {
            if (connection is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }

            var eventData = connection.ToEventData(@event, eventContext);
            return connection.AppendToStreamAsync(EventManager.GetStreamId<TEvent>(topic), ExpectedVersion.Any, eventData, userCredentials);
        }

        public static Task<WriteResult> PublishEventAsync<TEvent>(this IEventStoreBus connection, string topic, TEvent @event,
            IEventMetadata eventMeta = null, UserCredentials userCredentials = null)
        {
            if (connection is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }

            var eventData = connection.ToEventData(@event, eventMeta);
            return connection.AppendToStreamAsync(EventManager.GetStreamId<TEvent>(topic), ExpectedVersion.Any, eventData, userCredentials);
        }

        #endregion

        #region -- PublishEventsAsync --

        public static Task<WriteResult> PublishEventsAsync<TEvent>(this IEventStoreBus connection, IList<TEvent> events,
            IList<Dictionary<string, object>> eventContexts, UserCredentials userCredentials = null)
        {
            if (connection is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }

            var eventDatas = connection.ToEventDatas(events, eventContexts);
            return connection.AppendToStreamAsync(EventManager.GetStreamId<TEvent>(), ExpectedVersion.Any, eventDatas, userCredentials);
        }
        public static Task<WriteResult> PublishEventsAsync<TEvent>(this IEventStoreBus connection, IList<TEvent> events,
            IList<IEventMetadata> eventMetas = null, UserCredentials userCredentials = null)
        {
            if (connection is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }

            var eventDatas = connection.ToEventDatas(events, eventMetas);
            return connection.AppendToStreamAsync(EventManager.GetStreamId<TEvent>(), ExpectedVersion.Any, eventDatas, userCredentials);
        }

        #endregion

        #region -- PublishEventsAsync(Topic) --

        public static Task<WriteResult> PublishEventsAsync<TEvent>(this IEventStoreBus connection, string topic, IList<TEvent> events,
          IList<Dictionary<string, object>> eventContexts, UserCredentials userCredentials = null)
        {
            if (connection is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }

            var eventDatas = connection.ToEventDatas(events, eventContexts);
            return connection.AppendToStreamAsync(EventManager.GetStreamId<TEvent>(topic), ExpectedVersion.Any, eventDatas, userCredentials);
        }

        public static Task<WriteResult> PublishEventsAsync<TEvent>(this IEventStoreBus connection, string topic, IList<TEvent> events,
          IList<IEventMetadata> eventMetas = null, UserCredentials userCredentials = null)
        {
            if (connection is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }

            var eventDatas = connection.ToEventDatas(events, eventMetas);
            return connection.AppendToStreamAsync(EventManager.GetStreamId<TEvent>(topic), ExpectedVersion.Any, eventDatas, userCredentials);
        }

        #endregion

        #region -- PublishEventsAsync(Transaction) --

        public static Task<WriteResult> PublishEventsAsync<TEvent>(this IEventStoreBus connection, int batchSize, IList<TEvent> events,
            IList<Dictionary<string, object>> eventContexts, UserCredentials userCredentials = null)
        {
            if (connection is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
            if ((uint)(batchSize - 1) >= Consts.TooBigOrNegative) { ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.batchSize); }

            var eventDatas = connection.ToEventDatas(events, eventContexts);
            return DoWriteAsync(connection, EventManager.GetStreamId<TEvent>(), ExpectedVersion.Any, eventDatas, batchSize, userCredentials);
        }

        public static Task<WriteResult> PublishEventsAsync<TEvent>(this IEventStoreBus connection, int batchSize, IList<TEvent> events,
            IList<IEventMetadata> eventMetas = null, UserCredentials userCredentials = null)
        {
            if (connection is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
            if ((uint)(batchSize - 1) >= Consts.TooBigOrNegative) { ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.batchSize); }

            var eventDatas = connection.ToEventDatas(events, eventMetas);
            return DoWriteAsync(connection, EventManager.GetStreamId<TEvent>(), ExpectedVersion.Any, eventDatas, batchSize, userCredentials);
        }

        #endregion

        #region -- PublishEventsAsync(Transaction & Topic) --

        public static Task<WriteResult> PublishEventsAsync<TEvent>(this IEventStoreBus connection, string topic, int batchSize, IList<TEvent> events,
          IList<Dictionary<string, object>> eventContexts, UserCredentials userCredentials = null)
        {
            if (connection is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
            if ((uint)(batchSize - 1) >= Consts.TooBigOrNegative) { ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.batchSize); }

            var eventDatas = connection.ToEventDatas(events, eventContexts);
            return DoWriteAsync(connection, EventManager.GetStreamId<TEvent>(topic), ExpectedVersion.Any, eventDatas, batchSize, userCredentials);
        }

        public static Task<WriteResult> PublishEventsAsync<TEvent>(this IEventStoreBus connection, string topic, int batchSize, IList<TEvent> events,
          IList<IEventMetadata> eventMetas = null, UserCredentials userCredentials = null)
        {
            if (connection is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
            if ((uint)(batchSize - 1) >= Consts.TooBigOrNegative) { ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.batchSize); }

            var eventDatas = connection.ToEventDatas(events, eventMetas);
            return DoWriteAsync(connection, EventManager.GetStreamId<TEvent>(topic), ExpectedVersion.Any, eventDatas, batchSize, userCredentials);
        }

        #endregion
    }
}
