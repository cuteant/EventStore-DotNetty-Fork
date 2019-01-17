using System;
using CuteAnt;
using CuteAnt.Collections;
using CuteAnt.Reflection;
using EventStore.ClientAPI.Internal;
using EventStore.ClientAPI.Serialization;
using EventStore.Transport.Tcp.Messages;
using Microsoft.Extensions.Logging;

namespace EventStore.ClientAPI.Messages
{
    public static partial class ClientMessagesExtensions
    {
        private static ILogger s_logger = TraceLogger.GetLogger(typeof(ClientMessagesExtensions));

        #region == ToRawRecordedEvent ==

        internal static RecordedEvent ToRawRecordedEvent(this TcpClientMessageDto.EventRecord systemRecord)
        {
            return new RecordedEvent(
                systemRecord.EventStreamId,
                new Guid(systemRecord.EventId),
                systemRecord.EventNumber,
                systemRecord.EventType,
                systemRecord.Created,
                systemRecord.CreatedEpoch,
                systemRecord.Data ?? Empty.ByteArray,
                systemRecord.Metadata ?? Empty.ByteArray,
                systemRecord.DataContentType == 1);
        }

        #endregion

        #region == ToRawResolvedEvent ==

        internal static ClientAPI.ResolvedEvent ToRawResolvedEvent(this TcpClientMessageDto.ResolvedEvent evnt)
        {
            return new ClientAPI.ResolvedEvent(
                evnt.Event?.ToRawRecordedEvent(),
                evnt.Link?.ToRawRecordedEvent(),
                new Position(evnt.CommitPosition, evnt.PreparePosition));
        }

        internal static ClientAPI.ResolvedEvent ToRawResolvedEvent(this TcpClientMessageDto.ResolvedIndexedEvent evnt)
        {
            return new ClientAPI.ResolvedEvent(evnt.Event?.ToRawRecordedEvent(), evnt.Link?.ToRawRecordedEvent(), null);
        }

        internal static ClientAPI.ResolvedEvent? ToRawResolvedEvent(this TcpClientMessageDto.ResolvedIndexedEvent evnt, EventReadStatus readStatus)
        {
            return readStatus == EventReadStatus.Success
                  ? new ClientAPI.ResolvedEvent(evnt.Event?.ToRawRecordedEvent(), evnt.Link?.ToRawRecordedEvent(), null)
                  : default(ClientAPI.ResolvedEvent?);
        }

        #endregion

        #region == ToRawResolvedEvents ==

        internal static ClientAPI.ResolvedEvent[] ToRawResolvedEvents(this TcpClientMessageDto.ResolvedEvent[] events)
        {
            if (events == null || events.Length == 0)
            {
                return Empty.ResolvedEvents;
            }
            else
            {
                var result = new ClientAPI.ResolvedEvent[events.Length];
                for (int i = 0; i < result.Length; ++i)
                {
                    result[i] = events[i].ToRawResolvedEvent();
                }
                return result;
            }
        }

        internal static ClientAPI.ResolvedEvent[] ToRawResolvedEvents(this TcpClientMessageDto.ResolvedIndexedEvent[] events)
        {
            if (events == null || events.Length == 0)
            {
                return Empty.ResolvedEvents;
            }
            else
            {
                var result = new ClientAPI.ResolvedEvent[events.Length];
                for (int i = 0; i < result.Length; ++i)
                {
                    result[i] = events[i].ToRawResolvedEvent();
                }
                return result;
            }
        }

        #endregion

        #region ** ToRecordedEvent **

        private static RecordedEvent<object> ToRecordedEvent(this TcpClientMessageDto.EventRecord systemRecord)
        {
            try
            {
                return new RecordedEvent<object>(
                    systemRecord.EventStreamId,
                    new Guid(systemRecord.EventId),
                    systemRecord.EventNumber,
                    systemRecord.EventType,
                    systemRecord.Created,
                    systemRecord.CreatedEpoch,
                    SerializationManager.DeserializeEvent(systemRecord.Metadata, systemRecord.Data),
                    systemRecord.DataContentType == 1);
            }
            catch (Exception exc)
            {
                if (s_logger.IsWarningLevelEnabled()) s_logger.CanotDeserializeTheRecordedEvent(systemRecord, exc);
                return new RecordedEvent<object>(
                    systemRecord.EventStreamId,
                    new Guid(systemRecord.EventId),
                    systemRecord.EventNumber,
                    systemRecord.EventType,
                    systemRecord.Created,
                    systemRecord.CreatedEpoch,
                    DefaultFullEvent.Null,
                    systemRecord.DataContentType == 1);
            }
        }

        #endregion

        #region ** ToRecordedEvent<T> **

        private static RecordedEvent<T> ToRecordedEvent<T>(this TcpClientMessageDto.EventRecord systemRecord) where T : class
        {
            try
            {
                return new RecordedEvent<T>(
                    systemRecord.EventStreamId,
                    new Guid(systemRecord.EventId),
                    systemRecord.EventNumber,
                    systemRecord.EventType,
                    systemRecord.Created,
                    systemRecord.CreatedEpoch,
                    SerializationManager.DeserializeEvent<T>(systemRecord.Metadata, systemRecord.Data),
                    systemRecord.DataContentType == 1);
            }
            catch (Exception exc)
            {
                if (s_logger.IsWarningLevelEnabled()) s_logger.CanotDeserializeTheRecordedEvent(systemRecord, exc);
                return new RecordedEvent<T>(
                    systemRecord.EventStreamId,
                    new Guid(systemRecord.EventId),
                    systemRecord.EventNumber,
                    systemRecord.EventType,
                    systemRecord.Created,
                    systemRecord.CreatedEpoch,
                    DefaultFullEvent<T>.Null,
                    systemRecord.DataContentType == 1);
            }
        }

        private static RecordedEvent<T> ToRecordedEvent<T>(this TcpClientMessageDto.EventRecord systemRecord, EventMetadata metadata, Type eventType) where T : class
        {
            try
            {
                return new RecordedEvent<T>(
                    systemRecord.EventStreamId,
                    new Guid(systemRecord.EventId),
                    systemRecord.EventNumber,
                    systemRecord.EventType,
                    systemRecord.Created,
                    systemRecord.CreatedEpoch,
                    SerializationManager.DeserializeEvent<T>(metadata, eventType, systemRecord.Data),
                    systemRecord.DataContentType == 1);
            }
            catch (Exception exc)
            {
                if (s_logger.IsWarningLevelEnabled()) s_logger.CanotDeserializeTheRecordedEvent(systemRecord, exc);
                return new RecordedEvent<T>(
                    systemRecord.EventStreamId,
                    new Guid(systemRecord.EventId),
                    systemRecord.EventNumber,
                    systemRecord.EventType,
                    systemRecord.Created,
                    systemRecord.CreatedEpoch,
                    DefaultFullEvent<T>.Null,
                    systemRecord.DataContentType == 1);
            }
        }

        #endregion

        #region ** IResolvedEventDeserializer cache **

        private static readonly CachedReadConcurrentDictionary<Type, IResolvedEventDeserializer> s_eventDeserializerCache =
            new CachedReadConcurrentDictionary<Type, IResolvedEventDeserializer>(DictionaryCacheConstants.SIZE_MEDIUM);
        private static readonly Func<Type, IResolvedEventDeserializer> s_createEventDeserializer = CreateEventDeserializer;

        private static IResolvedEventDeserializer CreateEventDeserializer(Type eventType)
        {
            var deserializerType = typeof(ResolvedEventDeserializer<>).GetCachedGenericType(eventType);
            return ActivatorUtils.FastCreateInstance<IResolvedEventDeserializer>(deserializerType);
        }

        #endregion

        #region == ToResolvedEvent2 ==

        internal static IResolvedEvent2 ToResolvedEvent2(this TcpClientMessageDto.ResolvedEvent evnt)
        {
            try
            {
                var systemRecord = evnt.Event;
                var eventMeta = systemRecord != null ? SerializationManager.DeserializeMetadata(systemRecord.Metadata) : null;
                systemRecord = evnt.Event;
                var linkMeta = systemRecord != null ? SerializationManager.DeserializeMetadata(systemRecord.Metadata) : null;

                var eventType = TypeUtils.ResolveType((linkMeta ?? eventMeta).EventType);
                var deserializer = s_eventDeserializerCache.GetOrAdd(eventType, s_createEventDeserializer);
                return deserializer.ToResolvedEvent(evnt, eventMeta, linkMeta, eventType);
            }
            catch { return evnt.ToResolvedEvent(); }
        }

        internal static IResolvedEvent2 ToResolvedEvent2(this TcpClientMessageDto.ResolvedIndexedEvent evnt)
        {
            try
            {
                var systemRecord = evnt.Event;
                var eventMeta = systemRecord != null ? SerializationManager.DeserializeMetadata(systemRecord.Metadata) : null;
                systemRecord = evnt.Event;
                var linkMeta = systemRecord != null ? SerializationManager.DeserializeMetadata(systemRecord.Metadata) : null;

                var eventType = TypeUtils.ResolveType((linkMeta ?? eventMeta).EventType);
                var deserializer = s_eventDeserializerCache.GetOrAdd(eventType, s_createEventDeserializer);
                return deserializer.ToResolvedEvent(evnt, eventMeta, linkMeta, eventType);
            }
            catch { return evnt.ToResolvedEvent(); }
        }

        internal static IResolvedEvent2 ToResolvedEvent2(this TcpClientMessageDto.ResolvedIndexedEvent evnt, EventReadStatus readStatus)
        {
            return readStatus == EventReadStatus.Success
                  ? evnt.ToResolvedEvent2()
                  : null;
        }

        #endregion

        #region ** IPersistentSubscriptionResolvedEventDeserializer cache **

        private static readonly CachedReadConcurrentDictionary<Type, IPersistentSubscriptionResolvedEventDeserializer> s_persistentEventDeserializerCache =
            new CachedReadConcurrentDictionary<Type, IPersistentSubscriptionResolvedEventDeserializer>(DictionaryCacheConstants.SIZE_MEDIUM);
        private static readonly Func<Type, IPersistentSubscriptionResolvedEventDeserializer> s_createPersistentEventDeserializer = CreatePersistentEventDeserializer;

        private static IPersistentSubscriptionResolvedEventDeserializer CreatePersistentEventDeserializer(Type eventType)
        {
            var deserializerType = typeof(PersistentSubscriptionResolvedEventDeserializer<>).GetCachedGenericType(eventType);
            return ActivatorUtils.FastCreateInstance<IPersistentSubscriptionResolvedEventDeserializer>(deserializerType);
        }

        #endregion

        #region == ToPersistentSubscriptionResolvedEvent2 ==

        internal static IPersistentSubscriptionResolvedEvent2 ToPersistentSubscriptionResolvedEvent2(this TcpClientMessageDto.ResolvedIndexedEvent evnt, int? retryCount)
        {
            try
            {
                var systemRecord = evnt.Event;
                var eventMeta = systemRecord != null ? SerializationManager.DeserializeMetadata(systemRecord.Metadata) : null;
                systemRecord = evnt.Event;
                var linkMeta = systemRecord != null ? SerializationManager.DeserializeMetadata(systemRecord.Metadata) : null;

                var eventType = TypeUtils.ResolveType((linkMeta ?? eventMeta).EventType);
                var deserializer = s_persistentEventDeserializerCache.GetOrAdd(eventType, s_createPersistentEventDeserializer);
                return deserializer.ToResolvedEvent(evnt, eventMeta, linkMeta, eventType, retryCount);
            }
            catch { return new PersistentSubscriptionResolvedEvent<object>(evnt.ToResolvedEvent(), retryCount); }
        }

        #endregion

        #region == ToResolvedEvent ==

        internal static ClientAPI.ResolvedEvent<object> ToResolvedEvent(this TcpClientMessageDto.ResolvedEvent evnt)
        {
            return new ClientAPI.ResolvedEvent<object>(
                       evnt.Event?.ToRecordedEvent(),
                       evnt.Link?.ToRecordedEvent(),
                       new Position(evnt.CommitPosition, evnt.PreparePosition));
        }

        internal static ClientAPI.ResolvedEvent<object> ToResolvedEvent(this TcpClientMessageDto.ResolvedIndexedEvent evnt)
        {
            return new ClientAPI.ResolvedEvent<object>(evnt.Event?.ToRecordedEvent(), evnt.Link?.ToRecordedEvent(), null);
        }

        internal static ClientAPI.ResolvedEvent<object>? ToResolvedEvent(this TcpClientMessageDto.ResolvedIndexedEvent evnt, EventReadStatus readStatus)
        {
            return readStatus == EventReadStatus.Success
                  ? new ClientAPI.ResolvedEvent<object>(evnt.Event?.ToRecordedEvent(), evnt.Link?.ToRecordedEvent(), null)
                  : default(ClientAPI.ResolvedEvent<object>?);
        }

        #endregion

        #region == ToResolvedEvent<T> ==

        internal static ClientAPI.ResolvedEvent<T> ToResolvedEvent<T>(this TcpClientMessageDto.ResolvedEvent evnt) where T : class
        {
            return new ClientAPI.ResolvedEvent<T>(
                       evnt.Event?.ToRecordedEvent<T>(),
                       evnt.Link?.ToRecordedEvent<T>(),
                       new Position(evnt.CommitPosition, evnt.PreparePosition));
        }

        internal static ClientAPI.ResolvedEvent<T> ToResolvedEvent<T>(this TcpClientMessageDto.ResolvedIndexedEvent evnt) where T : class
        {
            return new ClientAPI.ResolvedEvent<T>(evnt.Event?.ToRecordedEvent<T>(), evnt.Link?.ToRecordedEvent<T>(), null);
        }

        internal static ClientAPI.ResolvedEvent<T>? ToResolvedEvent<T>(this TcpClientMessageDto.ResolvedIndexedEvent evnt, EventReadStatus readStatus) where T : class
        {
            return readStatus == EventReadStatus.Success
                  ? new ClientAPI.ResolvedEvent<T>(evnt.Event?.ToRecordedEvent<T>(), evnt.Link?.ToRecordedEvent<T>(), null)
                  : default(ClientAPI.ResolvedEvent<T>?);
        }

        #endregion

        #region == ToResolvedEvents ==

        internal static ClientAPI.ResolvedEvent<object>[] ToResolvedEvents(this TcpClientMessageDto.ResolvedEvent[] events)
        {
            if (events == null || events.Length == 0)
            {
                return EmptyArray<ClientAPI.ResolvedEvent<object>>.Instance;
            }
            else
            {
                var result = new ClientAPI.ResolvedEvent<object>[events.Length];
                for (int i = 0; i < result.Length; ++i)
                {
                    result[i] = events[i].ToResolvedEvent();
                }
                return result;
            }
        }

        internal static ClientAPI.ResolvedEvent<object>[] ToResolvedEvents(this TcpClientMessageDto.ResolvedIndexedEvent[] events)
        {
            if (events == null || events.Length == 0)
            {
                return EmptyArray<ClientAPI.ResolvedEvent<object>>.Instance;
            }
            else
            {
                var result = new ClientAPI.ResolvedEvent<object>[events.Length];
                for (int i = 0; i < result.Length; ++i)
                {
                    result[i] = events[i].ToResolvedEvent();
                }
                return result;
            }
        }

        #endregion

        #region == ToResolvedEvents2 ==

        internal static IResolvedEvent2[] ToResolvedEvents2(this TcpClientMessageDto.ResolvedEvent[] events)
        {
            if (events == null || events.Length == 0)
            {
                return EmptyArray<IResolvedEvent2>.Instance;
            }
            else
            {
                var result = new IResolvedEvent2[events.Length];
                for (int i = 0; i < result.Length; ++i)
                {
                    result[i] = events[i].ToResolvedEvent2();
                }
                return result;
            }
        }

        internal static IResolvedEvent2[] ToResolvedEvents2(this TcpClientMessageDto.ResolvedIndexedEvent[] events)
        {
            if (events == null || events.Length == 0)
            {
                return EmptyArray<IResolvedEvent2>.Instance;
            }
            else
            {
                var result = new IResolvedEvent2[events.Length];
                for (int i = 0; i < result.Length; ++i)
                {
                    result[i] = events[i].ToResolvedEvent2();
                }
                return result;
            }
        }

        #endregion

        #region == ToResolvedEvents<T> ==

        internal static ClientAPI.ResolvedEvent<T>[] ToResolvedEvents<T>(this TcpClientMessageDto.ResolvedEvent[] events) where T : class
        {
            if (events == null || events.Length == 0)
            {
                return EmptyArray<ClientAPI.ResolvedEvent<T>>.Instance;
            }
            else
            {
                var result = new ClientAPI.ResolvedEvent<T>[events.Length];
                for (int i = 0; i < result.Length; ++i)
                {
                    result[i] = events[i].ToResolvedEvent<T>();
                }
                return result;
            }
        }

        internal static ClientAPI.ResolvedEvent<T>[] ToResolvedEvents<T>(this TcpClientMessageDto.ResolvedIndexedEvent[] events) where T : class
        {
            if (events == null || events.Length == 0)
            {
                return EmptyArray<ClientAPI.ResolvedEvent<T>>.Instance;
            }
            else
            {
                var result = new ClientAPI.ResolvedEvent<T>[events.Length];
                for (int i = 0; i < result.Length; ++i)
                {
                    result[i] = events[i].ToResolvedEvent<T>();
                }
                return result;
            }
        }

        #endregion

        #region == interface IResolvedEventDeserializer ==

        internal interface IResolvedEventDeserializer
        {
            IResolvedEvent2 ToResolvedEvent(TcpClientMessageDto.ResolvedEvent evnt, EventMetadata eventMeta, EventMetadata linkMeta, Type eventType);
            IResolvedEvent2 ToResolvedEvent(TcpClientMessageDto.ResolvedIndexedEvent evnt, EventMetadata eventMeta, EventMetadata linkMeta, Type eventType);
        }
        internal class ResolvedEventDeserializer<T> : IResolvedEventDeserializer where T : class
        {
            public IResolvedEvent2 ToResolvedEvent(TcpClientMessageDto.ResolvedEvent evnt, EventMetadata eventMeta, EventMetadata linkMeta, Type eventType)
            {
                return new ClientAPI.ResolvedEvent<T>(
                           evnt.Event?.ToRecordedEvent<T>(eventMeta, eventType),
                           evnt.Link?.ToRecordedEvent<T>(linkMeta, eventType),
                           new Position(evnt.CommitPosition, evnt.PreparePosition));
            }

            public IResolvedEvent2 ToResolvedEvent(TcpClientMessageDto.ResolvedIndexedEvent evnt, EventMetadata eventMeta, EventMetadata linkMeta, Type eventType)
            {
                return new ClientAPI.ResolvedEvent<T>(evnt.Event?.ToRecordedEvent<T>(eventMeta, eventType),
                                                      evnt.Link?.ToRecordedEvent<T>(linkMeta, eventType), null);
            }
        }

        #endregion

        #region == interface IPersistentSubscriptionResolvedEventDeserializer ==

        internal interface IPersistentSubscriptionResolvedEventDeserializer
        {
            IPersistentSubscriptionResolvedEvent2 ToResolvedEvent(TcpClientMessageDto.ResolvedIndexedEvent evnt, EventMetadata eventMeta, EventMetadata linkMeta, Type eventType, int? retryCount);
        }
        internal class PersistentSubscriptionResolvedEventDeserializer<T> : IPersistentSubscriptionResolvedEventDeserializer where T : class
        {
            public IPersistentSubscriptionResolvedEvent2 ToResolvedEvent(TcpClientMessageDto.ResolvedIndexedEvent evnt, EventMetadata eventMeta, EventMetadata linkMeta, Type eventType, int? retryCount)
            {
                return new ClientAPI.PersistentSubscriptionResolvedEvent<T>(
                    new ClientAPI.ResolvedEvent<T>(evnt.Event?.ToRecordedEvent<T>(eventMeta, eventType),
                                                   evnt.Link?.ToRecordedEvent<T>(linkMeta, eventType), null),
                    retryCount);
            }
        }

        #endregion
    }
}
