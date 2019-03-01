using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using CuteAnt;
using CuteAnt.Pool;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Formatting = Newtonsoft.Json.Formatting;

namespace EventStore.ClientAPI.Common.Utils
{
    internal static class Json
    {
        private static readonly ObjectPool<JsonSerializer> _jsonSerializerPool;

        public static readonly JsonSerializerSettings JsonSettings;

        public static readonly IArrayPool<char> CharacterArrayPool;

        static Json()
        {
            CharacterArrayPool = new JsonArrayPool<char>(ArrayPool<char>.Shared);

            JsonSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore,
                TypeNameHandling = TypeNameHandling.None,
                Converters = new JsonConverter[] { new StringEnumConverter(), new CombGuidConverter() }
            };
            var settings = new JsonSerializerSettings
            {
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateParseHandling = DateParseHandling.None,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore,
                TypeNameHandling = TypeNameHandling.None,
                PreserveReferencesHandling = PreserveReferencesHandling.None,
                Converters = new JsonConverter[] { new StringEnumConverter(), new CombGuidConverter() }
            };
            _jsonSerializerPool = SynchronizedObjectPoolProvider.Default.Create(new JsonSerializerObjectPolicy(settings));
        }

        public static byte[] ToJsonBytes(this object source)
        {
            string instring = JsonConvert.SerializeObject(source, Formatting.Indented, JsonSettings);
            return Helper.UTF8NoBom.GetBytes(instring);
        }

        public static string ToJson(this object source)
        {
            string instring = JsonConvert.SerializeObject(source, Formatting.Indented, JsonSettings);
            return instring;
        }

        public static string ToCanonicalJson(this object source)
        {
            string instring = JsonConvert.SerializeObject(source);
            return instring;
        }

        public static T ParseJson<T>(this string json)
        {
            var result = JsonConvert.DeserializeObject<T>(json, JsonSettings);
            return result;
        }

        public static T ParseJson<T>(this byte[] json)
        {
            var result = JsonConvert.DeserializeObject<T>(Helper.UTF8NoBom.GetString(json), JsonSettings);
            return result;
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
                var jsonDeserializer = _jsonSerializerPool.Take();

                try
                {
                    return jsonDeserializer.Deserialize(jsonReader, objectType);
                }
                finally
                {
                    _jsonSerializerPool.Return(jsonDeserializer);
                }
            }
        }

        private static bool TryGetValueCamelCase(string key, IDictionary<string, object> dictionary, out object value)
        {
            if (char.IsUpper(key[0]))
            {
                var camelCaseKey = ToCamelCase(key);
                return dictionary.TryGetValue(camelCaseKey, out value);
            }

            value = null;
            return false;
        }

        internal static string ToCamelCase(string s)
        {
            if (string.IsNullOrEmpty(s) || !char.IsUpper(s[0]))
            {
                return s;
            }

            char[] chars = s.ToCharArray();

            for (int i = 0; i < chars.Length; i++)
            {
                if (i == 1 && !char.IsUpper(chars[i]))
                {
                    break;
                }

                bool hasNext = (i + 1 < chars.Length);
                if (i > 0 && hasNext && !char.IsUpper(chars[i + 1]))
                {
                    // if the next character is a space, which is not considered uppercase 
                    // (otherwise we wouldn't be here...)
                    // we want to ensure that the following:
                    // 'FOO bar' is rewritten as 'foo bar', and not as 'foO bar'
                    // The code was written in such a way that the first word in uppercase
                    // ends when if finds an uppercase letter followed by a lowercase letter.
                    // now a ' ' (space, (char)32) is considered not upper
                    // but in that case we still want our current character to become lowercase
                    if (char.IsSeparator(chars[i + 1]))
                    {
                        chars[i] = char.ToLower(chars[i], CultureInfo.InvariantCulture);
                    }

                    break;
                }

                chars[i] = char.ToLower(chars[i], CultureInfo.InvariantCulture);
            }

            return new string(chars);
        }
    }

    sealed class JsonArrayPool<T> : IArrayPool<T>
    {
        private readonly ArrayPool<T> _inner;

        public JsonArrayPool(ArrayPool<T> inner)
        {
            if (inner == null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.inner); }

            _inner = inner;
        }

        public T[] Rent(int minimumLength) => _inner.Rent(minimumLength);

        public void Return(T[] array)
        {
            if (array == null) { return; }

            _inner.Return(array);
        }
    }

    sealed class JsonSerializationBinder : CuteAnt.Serialization.DefaultSerializationBinder, ISerializationBinder
    {
        public static readonly JsonSerializationBinder Instance = new JsonSerializationBinder();

        private JsonSerializationBinder() { }
    }

    sealed class JsonSerializerObjectPolicy : IPooledObjectPolicy<JsonSerializer>
    {
        private readonly JsonSerializerSettings _serializerSettings;

        public JsonSerializerObjectPolicy(JsonSerializerSettings serializerSettings)
        {
            _serializerSettings = serializerSettings;
        }

        public JsonSerializer Create() => JsonSerializer.Create(_serializerSettings); // CreateDefault

        public JsonSerializer PreGetting(JsonSerializer serializer) => serializer;

        public bool Return(JsonSerializer serializer) => serializer != null;
    }

    /// <summary>Converts a <see cref="CombGuid"/> to and from a string.</summary>
    sealed class CombGuidConverter : JsonConverter
    {
        /// <summary>Reads the JSON representation of the object.</summary>
        /// <param name="reader">The <see cref="JsonReader"/> to read from.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="existingValue">The existing property value of the JSON that is being converted.</param>
        /// <param name="serializer">The calling serializer.</param>
        /// <returns>The object value.</returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return CombGuid.Empty;
            }
            else
            {
                JToken token = JToken.Load(reader);
                var str = token.Value<string>();
                if (CombGuid.TryParse(str, CombGuidSequentialSegmentType.Comb, out CombGuid v))
                {
                    return v;
                }
                if (CombGuid.TryParse(str, CombGuidSequentialSegmentType.Guid, out v))
                {
                    return v;
                }
                return CombGuid.Empty;
            }
        }

        /// <summary>Writes the JSON representation of the object.</summary>
        /// <param name="writer">The <see cref="JsonWriter"/> to write to.</param>
        /// <param name="value">The value.</param>
        /// <param name="serializer">The calling serializer.</param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }
            CombGuid comb = (CombGuid)value;
            writer.WriteValue(comb.ToString());
        }

        /// <summary>Determines whether this instance can convert the specified object type.</summary>
        /// <param name="objectType">Type of the object.</param>
        /// <returns><c>true</c> if this instance can convert the specified object type; otherwise, <c>false</c>.</returns>
        public override Boolean CanConvert(Type objectType)
        {
            return objectType == TypeConstants.CombGuidType || objectType == typeof(CombGuid?);
        }
    }
}
