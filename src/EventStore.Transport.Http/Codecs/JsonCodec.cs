using System;
using System.Text;
using CuteAnt.Pool;
using EventStore.Common.Utils;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace EventStore.Transport.Http.Codecs
{
    public class JsonCodec : ICodec
    {
        public static Formatting Formatting = Formatting.Indented;

        private static readonly ILogger Log;

        private static readonly JsonSerializerSettings FromSettings;
        private static readonly ObjectPool<JsonSerializer> _deserializerPool;

        private static readonly JsonSerializerSettings ToSettings;
        private static readonly ObjectPool<JsonSerializer> _serializerPool;
        private static readonly JsonSerializerSettings IndentedToSettings;
        public static readonly ObjectPool<JsonSerializer> IndentedSerializerPool;

        static JsonCodec()
        {
            Log = TraceLogger.GetLogger<JsonCodec>();

            FromSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                DateParseHandling = DateParseHandling.None,
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore,
                TypeNameHandling = TypeNameHandling.None,
                Converters = new JsonConverter[] { JsonConvertX.DefaultStringEnumConverter, JsonConvertX.DefaultCombGuidConverter }
            };
            _deserializerPool = JsonConvertX.GetJsonSerializerPool(FromSettings);
            ToSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Include,
                MissingMemberHandling = MissingMemberHandling.Ignore,
                TypeNameHandling = TypeNameHandling.None,
                Converters = new JsonConverter[] { JsonConvertX.DefaultStringEnumConverter, JsonConvertX.DefaultCombGuidConverter }
            };
            _serializerPool = JsonConvertX.GetJsonSerializerPool(ToSettings);
            IndentedToSettings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Include,
                MissingMemberHandling = MissingMemberHandling.Ignore,
                TypeNameHandling = TypeNameHandling.None,
                Converters = new JsonConverter[] { JsonConvertX.DefaultStringEnumConverter, JsonConvertX.DefaultCombGuidConverter }
            };
            IndentedSerializerPool = JsonConvertX.GetJsonSerializerPool(IndentedToSettings);
        }

        public string ContentType
        {
            get { return Http.ContentType.Json; }
        }

        public Encoding Encoding
        {
            get { return Helper.UTF8NoBom; }
        }

        public bool HasEventIds
        {
            get { return false; }
        }

        public bool HasEventTypes
        {
            get { return false; }
        }

        public bool CanParse(MediaType format)
        {
            return format != null && format.Matches(ContentType, Encoding);
        }

        public bool SuitableForResponse(MediaType component)
        {
            return component.Type == "*"
                   || (string.Equals(component.Type, "application", StringComparison.OrdinalIgnoreCase)
                       && (component.Subtype == "*"
                           || string.Equals(component.Subtype, "json", StringComparison.OrdinalIgnoreCase)));
        }

        public T From<T>(string text)
        {
            try
            {
                return (T)_deserializerPool.DeserializeObject(text, typeof(T));
            }
            catch (Exception e)
            {
                Log.IsNotAValidSerialized<T>(text, e);
                return default;
            }
        }

        public string To<T>(T value)
        {
            if (value == null) { return ""; }

            if ((object)value == Empty.Result) { return Empty.Json; }

            try
            {
                var serializerPool = Formatting == Formatting.Indented ? IndentedSerializerPool : _serializerPool;
                return serializerPool.SerializeObject(value);
            }
            catch (Exception ex)
            {
                Log.ErrorSerializingObjectOfType<T>(ex);
                return null;
            }
        }
    }
}
