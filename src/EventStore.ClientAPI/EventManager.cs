using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using CuteAnt;
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

        private static readonly CachedReadConcurrentDictionary<Type, string> s_streamMapCache;

        static EventManager()
        {
            s_streamMapCache = new CachedReadConcurrentDictionary<Type, string>(DictionaryCacheConstants.SIZE_MEDIUM);
        }

        #endregion

        #region -- TaskScheduler -- 

        private static TaskScheduler s_defaultTaskScheduler = TaskScheduler.Default;

        /// <summary>DefaultTaskScheduler</summary>
        public static TaskScheduler DefaultTaskScheduler
        {
            get => Volatile.Read(ref s_defaultTaskScheduler);
            set => Interlocked.Exchange(ref s_defaultTaskScheduler, value);
        }

        // The default concurrency level is DEFAULT_CONCURRENCY_MULTIPLIER * #CPUs. The higher the
        // DEFAULT_CONCURRENCY_MULTIPLIER, the more concurrent writes can take place without interference
        // and blocking, but also the more expensive operations that require all locks become (e.g. table
        // resizing, ToArray, Count, etc). According to brief benchmarks that we ran, 4 seems like a good
        // compromise.
        private const Int32 DEFAULT_CONCURRENCY_MULTIPLIER = 4;

        /// <summary>The number of concurrent writes for which to optimize by default.</summary>
        private static Int32 DefaultConcurrencyLevel => DEFAULT_CONCURRENCY_MULTIPLIER * PlatformHelper.ProcessorCount;

        private static ConcurrentExclusiveSchedulerPair s_taskSchedulerPair;

        /// <summary>TaskSchedulerPair</summary>
        internal static ConcurrentExclusiveSchedulerPair TaskSchedulerPair
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Volatile.Read(ref s_taskSchedulerPair) ?? EnsureSchedulerPairCreated();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ConcurrentExclusiveSchedulerPair EnsureSchedulerPairCreated()
        {
            Interlocked.CompareExchange(ref s_taskSchedulerPair, new ConcurrentExclusiveSchedulerPair(DefaultTaskScheduler, DefaultConcurrencyLevel), null);
            return s_taskSchedulerPair;
        }

        #endregion

        #region -- GetStreamId --

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetStreamId(this Type actualType)
        {
            return LookupStreamId(actualType);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetStreamId(this Type actualType, string topic)
        {
            var streamId = LookupStreamId(actualType);
            return streamId.Combine(topic);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetStreamId<TEvent>()
        {
            return LookupStreamId(typeof(TEvent));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetStreamId<TEvent>(string topic)
        {
            var streamId = LookupStreamId(typeof(TEvent));
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
            if (s_streamMapCache.TryGetValue(expectedType, out var streamId)) { return streamId; }
            return EnsureStreamIdGenerated(expectedType);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string EnsureStreamIdGenerated(Type expectedType)
        {
            // FirstAttribute 可获取动态添加的attr
            var streamAttr = expectedType.FirstAttribute<StreamAttribute>();
            var streamId = streamAttr != null ? streamAttr.StreamId : RuntimeTypeNameFormatter.Serialize(expectedType);
            if (s_streamMapCache.TryAdd(expectedType, streamId)) { return streamId; }

            s_streamMapCache.TryGetValue(expectedType, out streamId); return streamId;

        }

        #endregion

        #region == ToEventDatas ==

        internal static EventData[] ToEventDatas(IEventAdapter eventAdapter, IList<object> events, IList<Dictionary<string, object>> eventContexts)
        {
            if (null == events) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.events); }
            if (eventContexts != null && events.Count != eventContexts.Count) { ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.eventContexts); }

            var evts = new EventData[events.Count];
            if (eventContexts != null)
            {
                for (var idx = 0; idx < events.Count; idx++)
                {
                    evts[idx] = eventAdapter.Adapt(events[idx], eventAdapter.ToEventMetadata(eventContexts[idx]));
                }
            }
            else
            {
                for (var idx = 0; idx < events.Count; idx++)
                {
                    evts[idx] = eventAdapter.Adapt(events[idx], null);
                }
            }
            return evts;
        }

        internal static EventData[] ToEventDatas(IEventAdapter eventAdapter, IList<object> events, IList<IEventMetadata> eventMetas = null)
        {
            if (null == events) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.events); }
            if (eventMetas != null && events.Count != eventMetas.Count) { ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.eventMetas); }

            var evts = new EventData[events.Count];
            if (eventMetas == null)
            {
                for (var idx = 0; idx < events.Count; idx++)
                {
                    evts[idx] = eventAdapter.Adapt(events[idx], null);
                }
            }
            else
            {
                for (var idx = 0; idx < events.Count; idx++)
                {
                    evts[idx] = eventAdapter.Adapt(events[idx], eventMetas[idx]);
                }
            }
            return evts;
        }

        internal static EventData[] ToEventDatas<TEvent>(IEventAdapter eventAdapter, IList<TEvent> events, IList<Dictionary<string, object>> eventContexts)
        {
            if (null == events) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.events); }
            if (eventContexts != null && events.Count != eventContexts.Count) { ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.eventContexts); }

            var evts = new EventData[events.Count];
            if (eventContexts != null)
            {
                for (var idx = 0; idx < events.Count; idx++)
                {
                    evts[idx] = eventAdapter.Adapt(events[idx], eventAdapter.ToEventMetadata(eventContexts[idx]));
                }
            }
            else
            {
                for (var idx = 0; idx < events.Count; idx++)
                {
                    evts[idx] = eventAdapter.Adapt(events[idx], null);
                }
            }
            return evts;
        }

        internal static EventData[] ToEventDatas<TEvent>(IEventAdapter eventAdapter, IList<TEvent> events, IList<IEventMetadata> eventMetas = null)
        {
            if (null == events) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.events); }
            if (eventMetas != null && events.Count != eventMetas.Count) { ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.eventMetas); }

            var evts = new EventData[events.Count];
            if (eventMetas == null)
            {
                for (var idx = 0; idx < events.Count; idx++)
                {
                    evts[idx] = eventAdapter.Adapt(events[idx], null);
                }
            }
            else
            {
                for (var idx = 0; idx < events.Count; idx++)
                {
                    evts[idx] = eventAdapter.Adapt(events[idx], eventMetas[idx]);
                }
            }
            return evts;
        }

        #endregion

        #region == FromEventData ==

        internal static IFullEvent FromEventData(this IEventAdapter eventAdapter, EventData eventData)
        {
            return FromEventData(eventAdapter, eventData.Data, eventData.Metadata);
        }
        internal static IFullEvent<T> FromEventData<T>(this IEventAdapter eventAdapter, EventData eventData)
        {
            return FromEventData<T>(eventAdapter, eventData.Data, eventData.Metadata);
        }

        internal static IFullEvent FromEventData(this IEventAdapter eventAdapter, byte[] data, byte[] metadata)
        {
            var eventMeta = eventAdapter.ToEventMetadata(metadata);
            ToFullEvent(eventAdapter, data, eventMeta, out IEventDescriptor eventDescriptor, out object obj);
            return new DefaultFullEvent { Descriptor = eventDescriptor, Value = obj };
        }

        internal static IFullEvent<T> FromEventData<T>(this IEventAdapter eventAdapter, byte[] data, byte[] metadata)
        {
            var eventMeta = eventAdapter.ToEventMetadata(metadata);
            ToFullEvent(eventAdapter, data, eventMeta, out IEventDescriptor eventDescriptor, out object obj);
            return new DefaultFullEvent<T> { Descriptor = eventDescriptor, Value = (T)obj };
        }


        internal static IFullEvent FromEventData(this IEventAdapter eventAdapter, byte[] data, IEventMetadata metadata)
        {
            ToFullEvent(eventAdapter, data, metadata, out IEventDescriptor eventDescriptor, out object obj);
            return new DefaultFullEvent { Descriptor = eventDescriptor, Value = obj };
        }

        internal static IFullEvent<T> FromEventData<T>(this IEventAdapter eventAdapter, byte[] data, IEventMetadata metadata)
        {
            ToFullEvent(eventAdapter, data, metadata, out IEventDescriptor eventDescriptor, out object obj);
            return new DefaultFullEvent<T> { Descriptor = eventDescriptor, Value = (T)obj };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ToFullEvent(IEventAdapter eventAdapter, byte[] data, IEventMetadata meta, out IEventDescriptor eventDescriptor, out object obj)
        {
            eventDescriptor = (null == meta) ? NullEventDescriptor.Instance : new DefaultEventDescriptor(meta);

            obj = null;
            if (null == data || 0u >= (uint)data.Length) { return; }

            try { obj = eventAdapter.Adapt(data, meta); }
            catch (Exception exc) { CoreThrowHelper.ThrowEventDataDeserializationException(exc); }
        }

        #endregion
    }
}
