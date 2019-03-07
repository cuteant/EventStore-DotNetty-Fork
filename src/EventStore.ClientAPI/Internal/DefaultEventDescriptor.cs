using System.Collections.Generic;
using JsonExtensions;

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
            _eventContext = eventMeta.Context ?? s_empty;
        }

        public IEventMetadata Metadata => _eventMeta;

        public TMeta AsMetaData<TMeta>() where TMeta : IEventMetadata => (TMeta)_eventMeta;

        public T GetValue<T>(string key) => _eventContext.Deserialize<T>(key);

        public T GetValue<T>(string key, T defaultValue) => _eventContext.Deserialize(key, defaultValue);

        public bool TryGetValue<T>(string key, out T value) => _eventContext.TryDeserialize(key, out value);
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
