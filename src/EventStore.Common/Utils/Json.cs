using System;
using System.Xml;
using CuteAnt.Pool;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using SpanJson.Serialization;
using Formatting = Newtonsoft.Json.Formatting;

namespace EventStore.Common.Utils
{
    public static class Json
    {
        private static readonly JsonSerializerSettings FromSettings;
        private static readonly ObjectPool<JsonSerializer> _deserializerPool;
        private static readonly JsonSerializerSettings ToSettings;
        private static readonly ObjectPool<JsonSerializer> _serializerPool;

        static Json()
        {

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
                settings.Formatting = Formatting.Indented;
                settings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                settings.DateFormatHandling = DateFormatHandling.IsoDateFormat;
                settings.NullValueHandling = NullValueHandling.Ignore;
                settings.DefaultValueHandling = DefaultValueHandling.Ignore;
                settings.MissingMemberHandling = MissingMemberHandling.Ignore;
                settings.TypeNameHandling = TypeNameHandling.None;
            });
            ToSettings.Converters.Add(new StringEnumConverter());
            _serializerPool = JsonConvertX.GetJsonSerializerPool(ToSettings);
        }

        public static byte[] ToJsonBytes(this object source)
        {
            return _serializerPool.SerializeToByteArray(source);
        }

        public static string ToJson(this object source)
        {
            return _serializerPool.SerializeObject(source);
        }

        public static string ToCanonicalJson(this object source)
        {
            return JsonConvertX.SerializeObject(source);
        }

        public static T ParseJson<T>(this string json)
        {
            return (T)_deserializerPool.DeserializeObject(json, typeof(T));
        }

        public static T ParseJson<T>(this byte[] json)
        {
            return (T)_deserializerPool.DeserializeFromByteArray(json, typeof(T));
        }

        public static object DeserializeObject(JObject value, Type type, JsonSerializerSettings settings)
        {
            JsonSerializer jsonSerializer = JsonSerializer.Create(settings);
            return jsonSerializer.Deserialize(new JTokenReader(value), type);
        }

        public static object DeserializeObject(JObject value, Type type, params JsonConverter[] converters)
        {
            var settings = converters is null || 0u >= (uint)converters.Length
                ? null
                : new JsonSerializerSettings { Converters = converters };
            return DeserializeObject(value, type, settings);
        }

        public static XmlDocument ToXmlDocument(this JObject value, string deserializeRootElementName,
            bool writeArrayAttribute)
        {
            return (XmlDocument)DeserializeObject(value, typeof(XmlDocument), new JsonConverter[] {
                new XmlNodeConverter {
                    DeserializeRootElementName = deserializeRootElementName,
                    WriteArrayAttribute = writeArrayAttribute
                }
            });
        }

        public static bool IsValidJson(this byte[] value)
        {
            try
            {
                JToken.Parse(Helper.UTF8NoBom.GetString(value));
            }
            catch
            {
                return false;
            }

            return true;
        }
    }
}
