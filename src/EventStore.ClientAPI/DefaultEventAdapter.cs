using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using CuteAnt.Pool;
using CuteAnt.Reflection;
using EventStore.ClientAPI.Internal;
using JsonExtensions;
using JsonExtensions.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace EventStore.ClientAPI
{
    public class DefaultEventAdapter : EventAdapter<EventMetadata>
    {
        public static readonly DefaultEventAdapter Instance = new DefaultEventAdapter();

        private static readonly UTF8Encoding UTF8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        private const int c_metaBufferSize = 1024;
        private const int c_dataBufferSize = 64 * 1024;
        private const int c_charBufferSize = 1024;

        private readonly JsonSerializerSettings _metaSettings;
        private readonly JsonSerializerSettings _dataSettings;

        private ObjectPool<JsonSerializer> _metaSerializerPool;
        private ObjectPool<JsonSerializer> _dataSerializerPool;

        public DefaultEventAdapter() : this(
            new JsonSerializerSettings
            {
                Formatting = Formatting.None,

                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateParseHandling = DateParseHandling.None,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore,

                ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
                PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                TypeNameHandling = TypeNameHandling.All,
                TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                Converters = new JsonConverter[] { new StringEnumConverter(), new CombGuidConverter() }
            }, ArrayPool<char>.Shared, SynchronizedObjectPoolProvider.Default)
        {
        }

        public DefaultEventAdapter(JsonSerializerSettings serializerSettings, ArrayPool<char> charPool, ObjectPoolProvider objectPoolProvider)
        {
            if (serializerSettings == null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.serializerSettings); }
            if (charPool == null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.charPool); }
            if (objectPoolProvider == null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.objectPoolProvider); }

            _metaSettings = new JsonSerializerSettings
            {
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateParseHandling = DateParseHandling.None,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore,
                TypeNameHandling = TypeNameHandling.None,
                PreserveReferencesHandling = PreserveReferencesHandling.None,

                SerializationBinder = JsonSerializationBinder.Instance,
                Converters = new JsonConverter[] { new StringEnumConverter(), new CombGuidConverter() }
            };

            if (serializerSettings.SerializationBinder == null)
            {
                serializerSettings.SerializationBinder = JsonSerializationBinder.Instance;
            }

            _dataSettings = serializerSettings;
        }

        protected JsonSerializerSettings MetaSerializerSettings => _metaSettings;

        protected JsonSerializerSettings DataSerializerSettings => _dataSettings;

        public override EventData Adapt(object message, EventMetadata eventMeta)
        {
            if (message == null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.message); }

            var actualType = message.GetType();
            if (null == eventMeta) { eventMeta = new EventMetadata(); }
            eventMeta.ClrEventType = RuntimeTypeNameFormatter.Serialize(actualType);

            var metaSerializer = CreateMetaSerializer();
            var dataSerializer = CreateDataSerializer();
            try
            {
                var metaData = metaSerializer.SerializeToByteArray(eventMeta);
                var eventData = dataSerializer.SerializeToByteArray(message);

                return new EventData(Guid.NewGuid(), StringUtils.ToCamelCase(actualType.Name), true, eventData, metaData);
            }
            finally
            {
                ReleaseMetaSerializer(metaSerializer);
                ReleaseDataSerializer(dataSerializer);
            }
        }

        public override object Adapt(byte[] eventData, EventMetadata eventMeta)
        {
            if (eventData == null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.eventData); }

            TypeUtils.TryResolveType(eventMeta?.ClrEventType, out var dataType);
            var jsonSerializer = CreateDataSerializer();
            try
            {
                return jsonSerializer.DeserializeFromByteArray(eventData, dataType);
            }
            finally { ReleaseDataSerializer(jsonSerializer); }
        }

        public override IEventMetadata ToEventMetadata(Dictionary<string, object> context) => new EventMetadata { Context = context };

        public override IEventMetadata ToEventMetadata(byte[] metadata)
        {
            if (metadata == null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.metadata); }

            var jsonSerializer = CreateMetaSerializer();
            try
            {
                return (IEventMetadata)jsonSerializer.DeserializeFromByteArray(metadata, typeof(EventMetadata));
            }
            finally { ReleaseMetaSerializer(jsonSerializer); }
        }

        protected virtual JsonSerializer CreateMetaSerializer()
        {
            if (_metaSerializerPool == null)
            {
                Interlocked.Exchange(ref _metaSerializerPool, JsonConvertX.GetJsonSerializerPool(_metaSettings));
            }

            return _metaSerializerPool.Take();
        }

        protected virtual void ReleaseMetaSerializer(JsonSerializer serializer) => _metaSerializerPool.Return(serializer);

        protected virtual JsonSerializer CreateDataSerializer()
        {
            if (_dataSerializerPool == null)
            {
                Interlocked.Exchange(ref _dataSerializerPool, JsonConvertX.GetJsonSerializerPool(_dataSettings));
            }

            return _dataSerializerPool.Take();
        }

        protected virtual void ReleaseDataSerializer(JsonSerializer serializer) => _dataSerializerPool.Return(serializer);
    }
}
