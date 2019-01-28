using System;
using System.Collections.Generic;
using CuteAnt.Extensions.Serialization;

namespace EventStore.ClientAPI
{
    internal sealed class DefaultEventDescriptor : IEventDescriptor
    {
        private readonly Dictionary<string, object> _eventContext;
        private static readonly IObjectTypeDeserializer _objectTypeDeserializer = JsonObjectTypeDeserializer.Instance;

        public DefaultEventDescriptor(Dictionary<string, object> context)
        {
            if (null == context) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.context); }
            _eventContext = context;
        }

        public T GetValue<T>(string key)
        {
            return _objectTypeDeserializer.Deserialize<T>(_eventContext, key);
        }

        public T GetValue<T>(string key, T defaultValue)
        {
            return _objectTypeDeserializer.Deserialize<T>(_eventContext, key, defaultValue);
        }

        public bool TryGetValue<T>(string key, out T value)
        {
            return _objectTypeDeserializer.TryDeserialize<T>(_eventContext, key, out value);
        }
    }

    internal sealed class NullEventDescriptor : IEventDescriptor
    {
        internal static readonly IEventDescriptor Instance = new NullEventDescriptor();

        public T GetValue<T>(string key)
        {
            CoreThrowHelper.ThrowKeyNotFoundException(key); return default;
        }

        public T GetValue<T>(string key, T defaultValue)
        {
            return default;
        }

        public bool TryGetValue<T>(string key, out T value)
        {
            value = default;
            return false;
        }
    }
}
