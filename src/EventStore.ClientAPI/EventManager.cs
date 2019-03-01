using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using CuteAnt.Collections;
using CuteAnt.Reflection;
using CuteAnt.Text;
using EventStore.ClientAPI.Internal;

namespace EventStore.ClientAPI
{
    /// <summary>EventManager to oversee the EventStore serializer system.</summary>
    public static class EventManager
    {
        #region @@ Core @@

        private static readonly CachedReadConcurrentDictionary<Type, string> _streamMapCache;

        internal static IEventAdapter _defaultEventAdapter;

        static EventManager()
        {
            _streamMapCache = new CachedReadConcurrentDictionary<Type, string>(DictionaryCacheConstants.SIZE_MEDIUM);
            _defaultEventAdapter = DefaultEventAdapter.Instance;
        }

        public static IEventAdapter EventAdapter
        {
            get => _defaultEventAdapter;
            set => Interlocked.Exchange(ref _defaultEventAdapter, value);
        }

        #endregion

        #region -- GetStreamId --

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetStreamId(this Type actualType, Type expectedType = null)
        {
            return LookupStreamId(expectedType ?? actualType);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetStreamId(this Type actualType, string topic, Type expectedType = null)
        {
            var streamId = LookupStreamId(expectedType ?? actualType);
            return streamId.Combine(topic);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetStreamId<TEvent>(Type expectedType = null)
        {
            return LookupStreamId(expectedType ?? typeof(TEvent));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetStreamId<TEvent>(string topic, Type expectedType = null)
        {
            var streamId = LookupStreamId(expectedType ?? typeof(TEvent));
            return streamId.Combine(topic);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static string Combine(this string stream, string topic)
        {
            const char _separator = '-';

            if (string.IsNullOrEmpty(topic)) { return stream; }

            var sb = StringBuilderCache.Acquire();
            sb.Append(stream);
            sb.Append(_separator);
            sb.Append(topic);
            return StringBuilderCache.GetStringAndRelease(sb);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string LookupStreamId(Type expectedType)
        {
            if (_streamMapCache.TryGetValue(expectedType, out var streamId)) { return streamId; }
            return EnsureStreamIdGenerated(expectedType);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string EnsureStreamIdGenerated(Type expectedType)
        {
            // FirstAttribute 可获取动态添加的attr
            var streamAttr = expectedType.FirstAttribute<StreamAttribute>();
            var streamId = streamAttr != null ? streamAttr.StreamId : RuntimeTypeNameFormatter.Serialize(expectedType);
            if (_streamMapCache.TryAdd(expectedType, streamId)) { return streamId; }

            _streamMapCache.TryGetValue(expectedType, out streamId); return streamId;

        }

        #endregion

        #region -- ToEventMetadata --

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEventMetadata ToEventMetadata(this Dictionary<string, object> eventContext)
        {
            return _defaultEventAdapter.ToEventMetadata(eventContext);
        }

        public static IEventMetadata ToEventMetadata(byte[] metadata) => _defaultEventAdapter.ToEventMetadata(metadata);

        #endregion

        #region -- ToEventData --

        public static EventData ToEventData(object evt, IEventMetadata eventMeta = null)
        {
            if (null == evt) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.evt); }

            return _defaultEventAdapter.Adapt(evt, eventMeta);
        }

        #endregion

        #region -- ToEventDatas --

        public static EventData[] ToEventDatas(IList<object> events, Dictionary<string, object> eventContext)
        {
            if (null == events) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.events); }

            var list = new EventData[events.Count];
            for (var idx = 0; idx < events.Count; idx++)
            {
                list[idx] = ToEventData(events[idx], _defaultEventAdapter.ToEventMetadata(eventContext));
            }

            return list;
        }

        public static EventData[] ToEventDatas(IList<object> events, IEventMetadata eventMeta = null)
        {
            if (null == events) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.events); }

            var context = eventMeta?.Context;
            var list = new EventData[events.Count];
            for (var idx = 0; idx < events.Count; idx++)
            {
                list[idx] = ToEventData(events[idx], _defaultEventAdapter.ToEventMetadata(context));
            }

            return list;
        }

        public static EventData[] ToEventDatas(IList<object> events, IList<Dictionary<string, object>> eventContexts)
        {
            if (null == events) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.events); }
            if (null == eventContexts) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.eventContexts); }
            if (events.Count != eventContexts.Count) { ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.eventContexts); }

            var list = new EventData[events.Count];
            for (var idx = 0; idx < events.Count; idx++)
            {
                list[idx] = ToEventData(events[idx], _defaultEventAdapter.ToEventMetadata(eventContexts[idx]));
            }

            return list;
        }

        public static EventData[] ToEventDatas(IList<object> events, IList<IEventMetadata> eventMetas = null)
        {
            if (null == events) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.events); }
            if (null == eventMetas) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.eventMetas); }
            if (events.Count != eventMetas.Count) { ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.eventMetas); }

            var list = new EventData[events.Count];
            for (var idx = 0; idx < events.Count; idx++)
            {
                list[idx] = ToEventData(events[idx], eventMetas[idx]);
            }

            return list;
        }


        public static EventData[] ToEventDatas<TEvent>(IList<TEvent> events, Dictionary<string, object> eventContext)
        {
            if (null == events) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.events); }

            var list = new EventData[events.Count];
            for (var idx = 0; idx < events.Count; idx++)
            {
                list[idx] = ToEventData(events[idx], _defaultEventAdapter.ToEventMetadata(eventContext));
            }

            return list;
        }

        public static EventData[] ToEventDatas<TEvent>(IList<TEvent> events, IEventMetadata eventMeta = null)
        {
            if (null == events) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.events); }

            var context = eventMeta?.Context;
            var list = new EventData[events.Count];
            for (var idx = 0; idx < events.Count; idx++)
            {
                list[idx] = ToEventData(events[idx], _defaultEventAdapter.ToEventMetadata(context));
            }

            return list;
        }

        public static EventData[] ToEventDatas<TEvent>(IList<TEvent> events, IList<Dictionary<string, object>> eventContexts)
        {
            if (null == events) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.events); }
            if (null == eventContexts) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.eventContexts); }
            if (events.Count != eventContexts.Count) { ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.eventContexts); }

            var list = new EventData[events.Count];
            for (var idx = 0; idx < events.Count; idx++)
            {
                list[idx] = ToEventData(events[idx], _defaultEventAdapter.ToEventMetadata(eventContexts[idx]));
            }

            return list;
        }

        public static EventData[] ToEventDatas<TEvent>(IList<TEvent> events, IList<IEventMetadata> eventMetas)
        {
            if (null == events) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.events); }
            if (null == eventMetas) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.eventMetas); }
            if (events.Count != eventMetas.Count) { ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.eventMetas); }

            var list = new EventData[events.Count];
            for (var idx = 0; idx < events.Count; idx++)
            {
                list[idx] = ToEventData(events[idx], eventMetas[idx]);
            }

            return list;
        }

        #endregion

        #region -- FromEventData --

        public static IFullEvent FromEventData(EventData eventData)
        {
            return FromEventData(eventData.Data, eventData.Metadata);
        }
        public static IFullEvent<T> FromEventData<T>(EventData eventData)
        {
            return FromEventData<T>(eventData.Data, eventData.Metadata);
        }

        public static IFullEvent FromEventData(byte[] data, byte[] metadata)
        {
            var eventMeta = _defaultEventAdapter.ToEventMetadata(metadata);
            ToFullEvent(data, eventMeta, out IEventDescriptor eventDescriptor, out object obj);
            return new DefaultFullEvent { Descriptor = eventDescriptor, Value = obj };
        }

        public static IFullEvent<T> FromEventData<T>(byte[] data, byte[] metadata)
        {
            var eventMeta = _defaultEventAdapter.ToEventMetadata(metadata);
            ToFullEvent(data, eventMeta, out IEventDescriptor eventDescriptor, out object obj);
            return new DefaultFullEvent<T> { Descriptor = eventDescriptor, Value = (T)obj };
        }


        public static IFullEvent FromEventData(byte[] data, IEventMetadata metadata)
        {
            ToFullEvent(data, metadata, out IEventDescriptor eventDescriptor, out object obj);
            return new DefaultFullEvent { Descriptor = eventDescriptor, Value = obj };
        }

        public static IFullEvent<T> FromEventData<T>(byte[] data, IEventMetadata metadata)
        {
            ToFullEvent(data, metadata, out IEventDescriptor eventDescriptor, out object obj);
            return new DefaultFullEvent<T> { Descriptor = eventDescriptor, Value = (T)obj };
        }

        private static void ToFullEvent(byte[] data, IEventMetadata meta, out IEventDescriptor eventDescriptor, out object obj)
        {
            eventDescriptor = (null == meta) ? NullEventDescriptor.Instance : new DefaultEventDescriptor(meta);

            obj = null;
            if (null == data || 0u >= (uint)data.Length) { return; }

            try { obj = _defaultEventAdapter.Adapt(data, meta); }
            catch (Exception exc) { CoreThrowHelper.ThrowEventDataDeserializationException(exc); }
        }

        #endregion
    }
}
