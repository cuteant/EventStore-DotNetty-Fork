using System;
using System.Collections.Generic;
using System.Xml;
using CuteAnt;
using CuteAnt.Pool;
using JsonExtensions.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Formatting = Newtonsoft.Json.Formatting;

namespace EventStore.ClientAPI.Common.Utils
{
    internal static class Json
    {
        private static readonly ObjectPool<JsonSerializer> _defaultPool;

        private static readonly JsonSerializerSettings FromSettings;
        private static readonly ObjectPool<JsonSerializer> _deserializerPool;
        private static readonly JsonSerializerSettings ToSettings;
        private static readonly ObjectPool<JsonSerializer> _serializerPool;

        static Json()
        {
            var settings = new JsonSerializerSettings
            {
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateParseHandling = DateParseHandling.None,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore,
                TypeNameHandling = TypeNameHandling.None,
                PreserveReferencesHandling = PreserveReferencesHandling.None,
                Converters = new JsonConverter[] { JsonConvertX.DefaultStringEnumConverter, JsonConvertX.DefaultCombGuidConverter }
            };
            _defaultPool = JsonConvertX.GetJsonSerializerPool(settings);

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
                Formatting = Formatting.Indented,
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore,
                TypeNameHandling = TypeNameHandling.None,
                Converters = new JsonConverter[] { JsonConvertX.DefaultStringEnumConverter, JsonConvertX.DefaultCombGuidConverter }
            };
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
            string instring = JsonConvert.SerializeObject(source);
            return instring;
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
            var settings = converters == null || 0u >= (uint)converters.Length
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

        public static TValue Deserialize<TValue>(this IDictionary<string, object> context, string key)
        {
            //if (null == context) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.context); }
            if (null == key) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.key); }

            if (context.TryGetValue(key, out object value) || TryGetValueCamelCase(key, context, out value))
            {
                return (TValue)Deserialize(value, typeof(TValue), false);
            }
            throw new KeyNotFoundException($"The key was not present: {key}");
        }

        public static TValue Deserialize<TValue>(this IDictionary<string, object> context, string key, TValue defaultValue)
        {
            //if (null == context) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.context); }
            if (null == key) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.key); }

            if (context.TryGetValue(key, out object value) || TryGetValueCamelCase(key, context, out value))
            {
                return (TValue)Deserialize(value, typeof(TValue), false);
            }
            return defaultValue;
        }

        public static bool TryDeserialize<TValue>(this IDictionary<string, object> context, string key, out TValue value)
        {
            if (/*null == context ||*/ null == key) { value = default; return false; }

            if (context.TryGetValue(key, out object rawValue) || TryGetValueCamelCase(key, context, out rawValue))
            {
                value = (TValue)Deserialize(rawValue, typeof(TValue), false);
                return true;
            }
            value = default; return false;
        }

        private static object Deserialize(object value, Type objectType, bool allowNull = false)
        {
            if (value?.GetType() == TypeConstants.CombGuidType)
            {
                if (objectType == TypeConstants.CombGuidType)
                {
                    return value;
                }
                else if (objectType == TypeConstants.StringType)
                {
                    return value.ToString();
                }
                else if (objectType == TypeConstants.GuidType)
                {
                    return (Guid)((CombGuid)value);
                }
                else if (objectType == TypeConstants.ByteArrayType)
                {
                    //return ((CombGuid)value).ToByteArray();
                    return ((CombGuid)value).GetByteArray();
                }
                else if (objectType == typeof(DateTime))
                {
                    return ((CombGuid)value).DateTime;
                }
                else if (objectType == typeof(DateTimeOffset))
                {
                    return new DateTimeOffset(((CombGuid)value).DateTime);
                }
                value = value.ToString();
            }
            var token = value as JToken ?? new JValue(value);
            if (token.Type == JTokenType.Null && allowNull) { return null; }

            using (var jsonReader = new JTokenReader(token))
            {
                var jsonDeserializer = _defaultPool.Take();

                try
                {
                    return jsonDeserializer.Deserialize(jsonReader, objectType);
                }
                finally
                {
                    _defaultPool.Return(jsonDeserializer);
                }
            }
        }

        private static bool TryGetValueCamelCase(string key, IDictionary<string, object> dictionary, out object value)
        {
            if (char.IsUpper(key[0]))
            {
                var camelCaseKey = StringUtils.ToCamelCase(key);
                return dictionary.TryGetValue(camelCaseKey, out value);
            }

            value = null;
            return false;
        }
    }
}
