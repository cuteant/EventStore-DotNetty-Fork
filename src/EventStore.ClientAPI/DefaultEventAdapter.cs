using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using CuteAnt.Buffers;
using CuteAnt.Pool;
using CuteAnt.Reflection;
using EventStore.ClientAPI.Common.Utils;
using EventStore.ClientAPI.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace EventStore.ClientAPI
{
    public class DefaultEventAdapter : EventAdapter<EventMetadata>
    {
        public static readonly DefaultEventAdapter Instance = new DefaultEventAdapter();

        private const int c_metaBufferSize = 1024;
        private const int c_dataBufferSize = 64 * 1024;
        private const int c_charBufferSize = 1024;

        private readonly JsonSerializerSettings _metaSettings;
        private readonly JsonSerializerSettings _serializerSettings;
        private readonly IArrayPool<char> _charPool;
        private readonly ObjectPoolProvider _objectPoolProvider;

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

            _serializerSettings = serializerSettings;
            _charPool = new JsonArrayPool<char>(charPool);
            _objectPoolProvider = objectPoolProvider;
        }

        protected JsonSerializerSettings MetaSerializerSettings => _metaSettings;

        protected JsonSerializerSettings DataSerializerSettings => _serializerSettings;

        public override EventData Adapt(object message, EventMetadata eventMeta)
        {
            //if (message == null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.message); }

            byte[] metaData = null;
            var actualType = message.GetType();
            if (null == eventMeta) { eventMeta = new EventMetadata(); }
            eventMeta.ClrEventType = TypeUtils.SerializeTypeName(actualType);

            using (var pooledStream = BufferManagerOutputStreamManager.Create())
            {
                var outputStream = pooledStream.Object;
                outputStream.Reinitialize(c_metaBufferSize);

                using (JsonWriter jsonWriter = CreateJsonWriter(outputStream))
                {
                    var jsonSerializer = CreateMetaSerializer();
                    try
                    {
                        jsonSerializer.Serialize(jsonWriter, eventMeta, typeof(EventMetadata));
                        jsonWriter.Flush();
                    }
                    finally { ReleaseMetaSerializer(jsonSerializer); }
                }
                metaData = outputStream.ToByteArray();
            }

            using (var pooledStream = BufferManagerOutputStreamManager.Create())
            {
                var outputStream = pooledStream.Object;
                outputStream.Reinitialize(c_dataBufferSize);

                using (JsonWriter jsonWriter = CreateJsonWriter(outputStream))
                {
                    var jsonSerializer = CreateDataSerializer();
                    try
                    {
                        jsonSerializer.Serialize(jsonWriter, message);
                        jsonWriter.Flush();
                    }
                    finally { ReleaseDataSerializer(jsonSerializer); }
                }

                var eventData = outputStream.ToByteArray();

                return new EventData(Guid.NewGuid(), Json.ToCamelCase(actualType.Name), true, eventData, metaData);
            }
        }

        public override object Adapt(byte[] eventData, EventMetadata eventMeta)
        {
            if (eventData == null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.eventData); }

            using (var jsonReader = CreateJsonReader(new MemoryStream(eventData)))
            {
                var jsonSerializer = CreateDataSerializer();
                try
                {
                    TypeUtils.TryResolveType(eventMeta?.ClrEventType, out var dataType);
                    return jsonSerializer.Deserialize(jsonReader, dataType);
                }
                finally { ReleaseDataSerializer(jsonSerializer); }
            }
        }

        public override IEventMetadata ToEventMetadata(Dictionary<string, object> context) => new EventMetadata { Context = context };

        public override IEventMetadata ToEventMetadata(byte[] metadata)
        {
            if (metadata == null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.metadata); }

            using (var jsonReader = CreateJsonReader(new MemoryStream(metadata)))
            {
                var jsonSerializer = CreateMetaSerializer();
                try
                {
                    return jsonSerializer.Deserialize<EventMetadata>(jsonReader);
                }
                finally { ReleaseMetaSerializer(jsonSerializer); }
            }
        }

        protected virtual JsonSerializer CreateMetaSerializer()
        {
            if (_metaSerializerPool == null)
            {
                Interlocked.Exchange(ref _metaSerializerPool, _objectPoolProvider.Create(new JsonSerializerObjectPolicy(_metaSettings)));
            }

            return _metaSerializerPool.Take();
        }

        protected virtual void ReleaseMetaSerializer(JsonSerializer serializer) => _metaSerializerPool.Return(serializer);

        protected virtual JsonSerializer CreateDataSerializer()
        {
            if (_dataSerializerPool == null)
            {
                Interlocked.Exchange(ref _dataSerializerPool, _objectPoolProvider.Create(new JsonSerializerObjectPolicy(_serializerSettings)));
            }

            return _dataSerializerPool.Take();
        }

        protected virtual void ReleaseDataSerializer(JsonSerializer serializer) => _dataSerializerPool.Return(serializer);

        protected virtual JsonWriter CreateJsonWriter(Stream writer)
        {
            if (writer == null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.writer); }

            var jsonWriter = new JsonTextWriter(new StreamWriter(writer, Encoding.UTF8, c_charBufferSize, true))
            {
                ArrayPool = _charPool,
                CloseOutput = false,
                //AutoCompleteOnClose = false
            };

            return jsonWriter;
        }

        protected virtual JsonReader CreateJsonReader(Stream reader)
        {
            if (reader == null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.reader); }

            var jsonReader = new JsonTextReader(new StreamReader(reader, Encoding.UTF8, true, c_charBufferSize, true))
            {
                ArrayPool = _charPool,
                CloseInput = false
            };
            return jsonReader;
        }
    }
}
