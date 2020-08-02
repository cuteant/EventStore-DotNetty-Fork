using System;
using System.Text;
using CuteAnt.Pool;
using EventStore.Common.Utils;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using SpanJson.Serialization;

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

            FromSettings = JsonComplexSerializer.Instance.CreateDeserializerSettings(settings =>
            {
                settings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                settings.DateParseHandling = DateParseHandling.None;
                settings.NullValueHandling = NullValueHandling.Ignore;
                settings.DefaultValueHandling = DefaultValueHandling.Ignore;
                settings.MissingMemberHandling = MissingMemberHandling.Ignore;
                settings.TypeNameHandling = TypeNameHandling.None;
            });
            FromSettings.Converters.Add(new StringEnumConverter());
            _deserializerPool = JsonConvertX.GetJsonSerializerPool(FromSettings);
            ToSettings = JsonComplexSerializer.Instance.CreateSerializerSettings(settings =>
            {
                settings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                settings.DateFormatHandling = DateFormatHandling.IsoDateFormat;
                settings.NullValueHandling = NullValueHandling.Ignore;
                settings.DefaultValueHandling = DefaultValueHandling.Include;
                settings.MissingMemberHandling = MissingMemberHandling.Ignore;
                settings.TypeNameHandling = TypeNameHandling.None;
            });
            ToSettings.Converters.Add(new StringEnumConverter());
            _serializerPool = JsonConvertX.GetJsonSerializerPool(ToSettings);
            IndentedToSettings = JsonComplexSerializer.Instance.CreateSerializerSettings(settings =>
            {
                settings.Formatting = Formatting.Indented;
                settings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                settings.DateFormatHandling = DateFormatHandling.IsoDateFormat;
                settings.NullValueHandling = NullValueHandling.Ignore;
                settings.DefaultValueHandling = DefaultValueHandling.Include;
                settings.MissingMemberHandling = MissingMemberHandling.Ignore;
                settings.TypeNameHandling = TypeNameHandling.None;
            });
            IndentedToSettings.Converters.Add(new StringEnumConverter());
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
