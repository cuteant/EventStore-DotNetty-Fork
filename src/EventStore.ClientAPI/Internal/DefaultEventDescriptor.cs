using System.Collections.Generic;
using SpanJson.Internal;
using SpanJson.Linq;

namespace EventStore.ClientAPI
{
    internal sealed class DefaultEventDescriptor : IEventDescriptor
    {
        private static readonly Dictionary<string, object> s_empty = new Dictionary<string, object>();

        private readonly IEventMetadata _eventMeta;
        private readonly Dictionary<string, object> _eventContext;

        public DefaultEventDescriptor(IEventMetadata eventMeta)
        {
            if (null == eventMeta) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.eventMeta); }
            _eventMeta = eventMeta;
            _eventContext = eventMeta.Context ?? s_empty;
        }

        public IEventMetadata Metadata => _eventMeta;

        public TMeta AsMetaData<TMeta>() where TMeta : IEventMetadata => (TMeta)_eventMeta;

        public T GetValue<T>(string key) => GetValue<T>(key, default);

        public T GetValue<T>(string key, T defaultValue)
        {
            if (null == key) { goto Defalut; }

            if (_eventContext.TryGetValue(key, out object rawValue) || TryGetValueCamelCase(key, _eventContext, out rawValue))
            {
                return JToken.FromObject(rawValue).ToObject<T>();
            }

        Defalut:
            return defaultValue; ;
        }

        public bool TryGetValue<T>(string key, out T value)
        {
            if (null == key) { goto Defalut; }

            if (_eventContext.TryGetValue(key, out object rawValue) || TryGetValueCamelCase(key, _eventContext, out rawValue))
            {
                value = JToken.FromObject(rawValue).ToObject<T>();
                return true;
            }

        Defalut:
            value = default; return false;
        }

        private static bool TryGetValueCamelCase(string key, IDictionary<string, object> dictionary, out object value)
        {
            if (char.IsUpper(key[0]))
            {
                var camelCaseKey = StringMutator.ToCamelCaseWithCache(key);
                return dictionary.TryGetValue(camelCaseKey, out value);
            }
            value = null; return false;
        }
    }

    internal sealed class NullEventDescriptor : IEventDescriptor
    {
        internal static readonly IEventDescriptor Instance = new NullEventDescriptor();

        public IEventMetadata Metadata => null;

        public TMeta AsMetaData<TMeta>() where TMeta : IEventMetadata => default;

        public T GetValue<T>(string key)
        {
            CoreThrowHelper.ThrowKeyNotFoundException(key); return default;
        }

        public T GetValue<T>(string key, T defaultValue) => default;

        public bool TryGetValue<T>(string key, out T value)
        {
            value = default; return false;
        }
    }
}
