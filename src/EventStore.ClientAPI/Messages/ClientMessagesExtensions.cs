using System;
using System.Runtime.CompilerServices;
using CuteAnt;
using CuteAnt.Collections;
using CuteAnt.Reflection;
using EventStore.ClientAPI.Internal;
using EventStore.Transport.Tcp.Messages;
using Microsoft.Extensions.Logging;

namespace EventStore.ClientAPI.Messages
{
    public static partial class ClientMessagesExtensions
    {
        private static ILogger s_logger = TraceLogger.GetLogger(typeof(ClientMessagesExtensions));

        #region == ToRawRecordedEvent ==

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static RecordedEvent ToRawRecordedEvent(this TcpClientMessageDto.EventRecord systemRecord)
        {
            return new RecordedEvent(
                systemRecord.EventStreamId,
                systemRecord.EventId,
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
            if (events == null || 0u >= (uint)events.Length)
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
            if (events == null || 0u >= (uint)events.Length)
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

        private static RecordedEvent<object> ToRecordedEvent(this TcpClientMessageDto.EventRecord systemRecord, IEventAdapter eventAdapter)
        {
            try
            {
                return new RecordedEvent<object>(
                    systemRecord.EventStreamId,
                    systemRecord.EventId,
                    systemRecord.EventNumber,
                    systemRecord.EventType,
                    systemRecord.Created,
                    systemRecord.CreatedEpoch,
                    eventAdapter.FromEventData(systemRecord.Data, systemRecord.Metadata),
                    systemRecord.DataContentType == 1);
            }
            catch (Exception exc)
            {
                if (s_logger.IsWarningLevelEnabled()) s_logger.CanotDeserializeTheRecordedEvent(systemRecord, exc);
                return new RecordedEvent<object>(
                    systemRecord.EventStreamId,
                    systemRecord.EventId,
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

        private static RecordedEvent<T> ToRecordedEvent<T>(this TcpClientMessageDto.EventRecord systemRecord, IEventAdapter eventAdapter)
        {
            try
            {
                return new RecordedEvent<T>(
                    systemRecord.EventStreamId,
                    systemRecord.EventId,
                    systemRecord.EventNumber,
                    systemRecord.EventType,
                    systemRecord.Created,
                    systemRecord.CreatedEpoch,
                    eventAdapter.FromEventData<T>(systemRecord.Data, systemRecord.Metadata),
                    systemRecord.DataContentType == 1);
            }
            catch (Exception exc)
            {
                if (s_logger.IsWarningLevelEnabled()) s_logger.CanotDeserializeTheRecordedEvent(systemRecord, exc);
                return new RecordedEvent<T>(
                    systemRecord.EventStreamId,
                    systemRecord.EventId,
                    systemRecord.EventNumber,
                    systemRecord.EventType,
                    systemRecord.Created,
                    systemRecord.CreatedEpoch,
                    DefaultFullEvent<T>.Null,
                    systemRecord.DataContentType == 1);
            }
        }

        private static RecordedEvent<T> ToRecordedEvent<T>(this TcpClientMessageDto.EventRecord systemRecord, IEventMetadata metadata, Type eventType, IEventAdapter eventAdapter)
        {
            try
            {
                return new RecordedEvent<T>(
                    systemRecord.EventStreamId,
                    systemRecord.EventId,
                    systemRecord.EventNumber,
                    systemRecord.EventType,
                    systemRecord.Created,
                    systemRecord.CreatedEpoch,
                    eventAdapter.FromEventData<T>(systemRecord.Data, metadata),
                    systemRecord.DataContentType == 1);
            }
            catch (Exception exc)
            {
                if (s_logger.IsWarningLevelEnabled()) s_logger.CanotDeserializeTheRecordedEvent(systemRecord, exc);
                return new RecordedEvent<T>(
                    systemRecord.EventStreamId,
                    systemRecord.EventId,
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

        internal static IResolvedEvent2 ToResolvedEvent2(this TcpClientMessageDto.ResolvedEvent evnt, IEventAdapter eventAdapter)
        {
            try
            {
                var systemRecord = evnt.Event;
                var eventMeta = systemRecord != null ? eventAdapter.ToEventMetadata(systemRecord.Metadata) : null;
                systemRecord = evnt.Link;
                var linkMeta = systemRecord != null ? eventAdapter.ToEventMetadata(systemRecord.Metadata) : null;

                var eventType = TypeUtils.ResolveType((linkMeta ?? eventMeta).ClrEventType);
                var deserializer = s_eventDeserializerCache.GetOrAdd(eventType, s_createEventDeserializer);
                return deserializer.ToResolvedEvent(evnt, eventMeta, linkMeta, eventType, eventAdapter);
            }
            catch { return evnt.ToResolvedEvent(eventAdapter); }
        }

        internal static IResolvedEvent2 ToResolvedEvent2(this TcpClientMessageDto.ResolvedIndexedEvent evnt, IEventAdapter eventAdapter)
        {
            try
            {
                var systemRecord = evnt.Event;
                var eventMeta = systemRecord != null ? eventAdapter.ToEventMetadata(systemRecord.Metadata) : null;
                systemRecord = evnt.Link;
                var linkMeta = systemRecord != null ? eventAdapter.ToEventMetadata(systemRecord.Metadata) : null;

                var eventType = TypeUtils.ResolveType((linkMeta ?? eventMeta).ClrEventType);
                var deserializer = s_eventDeserializerCache.GetOrAdd(eventType, s_createEventDeserializer);
                return deserializer.ToResolvedEvent(evnt, eventMeta, linkMeta, eventType, eventAdapter);
            }
            catch { return evnt.ToResolvedEvent(eventAdapter); }
        }

        internal static IResolvedEvent2 ToResolvedEvent2(this TcpClientMessageDto.ResolvedIndexedEvent evnt, EventReadStatus readStatus, IEventAdapter eventAdapter)
        {
            return readStatus == EventReadStatus.Success
                  ? evnt.ToResolvedEvent2(eventAdapter)
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

        internal static IPersistentSubscriptionResolvedEvent2 ToPersistentSubscriptionResolvedEvent2(this TcpClientMessageDto.ResolvedIndexedEvent evnt, int? retryCount, IEventAdapter eventAdapter)
        {
            try
            {
                var systemRecord = evnt.Event;
                var eventMeta = systemRecord != null ? eventAdapter.ToEventMetadata(systemRecord.Metadata) : null;
                systemRecord = evnt.Link;
                var linkMeta = systemRecord != null ? eventAdapter.ToEventMetadata(systemRecord.Metadata) : null;

                var eventType = TypeUtils.ResolveType((linkMeta ?? eventMeta).ClrEventType);
                var deserializer = s_persistentEventDeserializerCache.GetOrAdd(eventType, s_createPersistentEventDeserializer);
                return deserializer.ToResolvedEvent(evnt, eventMeta, linkMeta, eventType, retryCount, eventAdapter);
            }
            catch { return new PersistentSubscriptionResolvedEvent<object>(evnt.ToResolvedEvent(eventAdapter), retryCount); }
        }

        #endregion


        #region == ToResolvedEvent ==

        internal static ClientAPI.ResolvedEvent<object> ToResolvedEvent(this TcpClientMessageDto.ResolvedEvent evnt, IEventAdapter eventAdapter)
        {
            return new ClientAPI.ResolvedEvent<object>(
                       evnt.Event?.ToRecordedEvent(eventAdapter),
                       evnt.Link?.ToRecordedEvent(eventAdapter),
                       new Position(evnt.CommitPosition, evnt.PreparePosition));
        }

        internal static ClientAPI.ResolvedEvent<object> ToResolvedEvent(this TcpClientMessageDto.ResolvedIndexedEvent evnt, IEventAdapter eventAdapter)
        {
            return new ClientAPI.ResolvedEvent<object>(evnt.Event?.ToRecordedEvent(eventAdapter), evnt.Link?.ToRecordedEvent(eventAdapter), null);
        }

        internal static ClientAPI.ResolvedEvent<object>? ToResolvedEvent(this TcpClientMessageDto.ResolvedIndexedEvent evnt, EventReadStatus readStatus, IEventAdapter eventAdapter)
        {
            return readStatus == EventReadStatus.Success
                  ? new ClientAPI.ResolvedEvent<object>(evnt.Event?.ToRecordedEvent(eventAdapter), evnt.Link?.ToRecordedEvent(eventAdapter), null)
                  : default(ClientAPI.ResolvedEvent<object>?);
        }

        #endregion

        #region == ToResolvedEvent<T> ==

        internal static ClientAPI.ResolvedEvent<T> ToResolvedEvent<T>(this TcpClientMessageDto.ResolvedEvent evnt, IEventAdapter eventAdapter)
        {
            return new ClientAPI.ResolvedEvent<T>(
                       evnt.Event?.ToRecordedEvent<T>(eventAdapter),
                       evnt.Link?.ToRecordedEvent<T>(eventAdapter),
                       new Position(evnt.CommitPosition, evnt.PreparePosition));
        }

        internal static ClientAPI.ResolvedEvent<T> ToResolvedEvent<T>(this TcpClientMessageDto.ResolvedIndexedEvent evnt, IEventAdapter eventAdapter)
        {
            return new ClientAPI.ResolvedEvent<T>(evnt.Event?.ToRecordedEvent<T>(eventAdapter), evnt.Link?.ToRecordedEvent<T>(eventAdapter), null);
        }

        internal static ClientAPI.ResolvedEvent<T>? ToResolvedEvent<T>(this TcpClientMessageDto.ResolvedIndexedEvent evnt, EventReadStatus readStatus, IEventAdapter eventAdapter)
        {
            return readStatus == EventReadStatus.Success
                  ? new ClientAPI.ResolvedEvent<T>(evnt.Event?.ToRecordedEvent<T>(eventAdapter), evnt.Link?.ToRecordedEvent<T>(eventAdapter), null)
                  : default(ClientAPI.ResolvedEvent<T>?);
        }

        #endregion


        #region == ToResolvedEvents ==

        internal static ClientAPI.ResolvedEvent<object>[] ToResolvedEvents(this TcpClientMessageDto.ResolvedEvent[] events, IEventAdapter eventAdapter)
        {
            if (events == null || 0u >= (uint)events.Length)
            {
                return EmptyArray<ClientAPI.ResolvedEvent<object>>.Instance;
            }
            else
            {
                var result = new ClientAPI.ResolvedEvent<object>[events.Length];
                for (int i = 0; i < result.Length; ++i)
                {
                    result[i] = events[i].ToResolvedEvent(eventAdapter);
                }
                return result;
            }
        }

        internal static ClientAPI.ResolvedEvent<object>[] ToResolvedEvents(this TcpClientMessageDto.ResolvedIndexedEvent[] events, IEventAdapter eventAdapter)
        {
            if (events == null || 0u >= (uint)events.Length)
            {
                return EmptyArray<ClientAPI.ResolvedEvent<object>>.Instance;
            }
            else
            {
                var result = new ClientAPI.ResolvedEvent<object>[events.Length];
                for (int i = 0; i < result.Length; ++i)
                {
                    result[i] = events[i].ToResolvedEvent(eventAdapter);
                }
                return result;
            }
        }

        #endregion

        #region == ToResolvedEvents2 ==

        internal static IResolvedEvent2[] ToResolvedEvents2(this TcpClientMessageDto.ResolvedEvent[] events, IEventAdapter eventAdapter)
        {
            if (events == null || 0u >= (uint)events.Length)
            {
                return EmptyArray<IResolvedEvent2>.Instance;
            }
            else
            {
                var result = new IResolvedEvent2[events.Length];
                for (int i = 0; i < result.Length; ++i)
                {
                    result[i] = events[i].ToResolvedEvent2(eventAdapter);
                }
                return result;
            }
        }

        internal static IResolvedEvent2[] ToResolvedEvents2(this TcpClientMessageDto.ResolvedIndexedEvent[] events, IEventAdapter eventAdapter)
        {
            if (events == null || 0u >= (uint)events.Length)
            {
                return EmptyArray<IResolvedEvent2>.Instance;
            }
            else
            {
                var result = new IResolvedEvent2[events.Length];
                for (int i = 0; i < result.Length; ++i)
                {
                    result[i] = events[i].ToResolvedEvent2(eventAdapter);
                }
                return result;
            }
        }

        #endregion

        #region == ToResolvedEvents<T> ==

        internal static ClientAPI.ResolvedEvent<T>[] ToResolvedEvents<T>(this TcpClientMessageDto.ResolvedEvent[] events, IEventAdapter eventAdapter)
        {
            if (events == null || 0u >= (uint)events.Length)
            {
                return EmptyArray<ClientAPI.ResolvedEvent<T>>.Instance;
            }
            else
            {
                var result = new ClientAPI.ResolvedEvent<T>[events.Length];
                for (int i = 0; i < result.Length; ++i)
                {
                    result[i] = events[i].ToResolvedEvent<T>(eventAdapter);
                }
                return result;
            }
        }

        internal static ClientAPI.ResolvedEvent<T>[] ToResolvedEvents<T>(this TcpClientMessageDto.ResolvedIndexedEvent[] events, IEventAdapter eventAdapter)
        {
            if (events == null || 0u >= (uint)events.Length)
            {
                return EmptyArray<ClientAPI.ResolvedEvent<T>>.Instance;
            }
            else
            {
                var result = new ClientAPI.ResolvedEvent<T>[events.Length];
                for (int i = 0; i < result.Length; ++i)
                {
                    result[i] = events[i].ToResolvedEvent<T>(eventAdapter);
                }
                return result;
            }
        }

        #endregion


        #region == interface IResolvedEventDeserializer ==

        internal interface IResolvedEventDeserializer
        {
            IResolvedEvent2 ToResolvedEvent(TcpClientMessageDto.ResolvedEvent evnt, IEventMetadata eventMeta, IEventMetadata linkMeta, Type eventType, IEventAdapter eventAdapter);
            IResolvedEvent2 ToResolvedEvent(TcpClientMessageDto.ResolvedIndexedEvent evnt, IEventMetadata eventMeta, IEventMetadata linkMeta, Type eventType, IEventAdapter eventAdapter);
        }
        internal class ResolvedEventDeserializer<T> : IResolvedEventDeserializer
        {
            public IResolvedEvent2 ToResolvedEvent(TcpClientMessageDto.ResolvedEvent evnt, IEventMetadata eventMeta, IEventMetadata linkMeta, Type eventType, IEventAdapter eventAdapter)
            {
                return new ClientAPI.ResolvedEvent<T>(
                           evnt.Event?.ToRecordedEvent<T>(eventMeta, eventType, eventAdapter),
                           evnt.Link?.ToRecordedEvent<T>(linkMeta, eventType, eventAdapter),
                           new Position(evnt.CommitPosition, evnt.PreparePosition));
            }

            public IResolvedEvent2 ToResolvedEvent(TcpClientMessageDto.ResolvedIndexedEvent evnt, IEventMetadata eventMeta, IEventMetadata linkMeta, Type eventType, IEventAdapter eventAdapter)
            {
                return new ClientAPI.ResolvedEvent<T>(evnt.Event?.ToRecordedEvent<T>(eventMeta, eventType, eventAdapter),
                                                      evnt.Link?.ToRecordedEvent<T>(linkMeta, eventType, eventAdapter), null);
            }
        }

        #endregion

        #region == interface IPersistentSubscriptionResolvedEventDeserializer ==

        internal interface IPersistentSubscriptionResolvedEventDeserializer
        {
            IPersistentSubscriptionResolvedEvent2 ToResolvedEvent(TcpClientMessageDto.ResolvedIndexedEvent evnt, IEventMetadata eventMeta, IEventMetadata linkMeta, Type eventType, int? retryCount, IEventAdapter eventAdapter);
        }
        internal class PersistentSubscriptionResolvedEventDeserializer<T> : IPersistentSubscriptionResolvedEventDeserializer
        {
            public IPersistentSubscriptionResolvedEvent2 ToResolvedEvent(TcpClientMessageDto.ResolvedIndexedEvent evnt, IEventMetadata eventMeta, IEventMetadata linkMeta, Type eventType, int? retryCount, IEventAdapter eventAdapter)
            {
                return new ClientAPI.PersistentSubscriptionResolvedEvent<T>(
                    new ClientAPI.ResolvedEvent<T>(evnt.Event?.ToRecordedEvent<T>(eventMeta, eventType, eventAdapter),
                                                   evnt.Link?.ToRecordedEvent<T>(linkMeta, eventType, eventAdapter), null),
                    retryCount);
            }
        }

        #endregion
    }
}
