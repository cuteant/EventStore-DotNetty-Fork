using System;
using System.Xml;
using CuteAnt.Buffers;
using CuteAnt.Extensions.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace EventStore.Common.Utils
{
    public static class Json
    {
        private const int c_initialBufferSize = 1024 * 64;
        public const int MinBufferSize = 128;
        public const int TinyBufferSize = 256;
        public const int SmallBufferSize = 512;
        public const int MediumBufferSize = 1024;

        public static readonly JsonSerializerSettings JsonSettings;
        private static readonly JsonMessageFormatter _jsonFormatter;

        static Json()
        {
            JsonSettings = JsonConvertX.CreateSerializerSettings(Newtonsoft.Json.Formatting.Indented, TypeNameHandling.None, null, true);
            JsonSettings.Converters.Add(JsonConvertX.DefaultStringEnumConverter);

            _jsonFormatter = new JsonMessageFormatter() { DefaultSerializerSettings = JsonSettings, DefaultDeserializerSettings = JsonSettings };
        }

        public static byte[] ToJsonBytes(this object source, int initialBufferSize = c_initialBufferSize)
        {
            return _jsonFormatter.SerializeObject(source, initialBufferSize);
        }

        public static string ToJson(this object source)
        {
            return JsonConvertX.SerializeObject(source, JsonSettings);
        }

        public static string ToCanonicalJson(this object source)
        {
            return JsonConvertX.SerializeObject(source);
        }

        public static T ParseJson<T>(this string json)
        {
            return JsonConvertX.DeserializeObject<T>(json, JsonSettings);
        }

        public static T ParseJson<T>(this byte[] json)
        {
            return _jsonFormatter.Deserialize<T>(json);
        }

        public static object DeserializeObject(JObject value, Type type, JsonSerializerSettings settings)
        {
            var jsonSerializer = JsonConvertX.AllocateSerializer(settings);
            try
            {
                return jsonSerializer.Deserialize(new JTokenReader(value), type);
            }
            finally
            {
                JsonConvertX.FreeSerializer(settings, jsonSerializer);
            }
        }

        public static object DeserializeObject(JObject value, Type type, params JsonConverter[] converters)
        {
            if (converters == null || converters.Length <= 0)
            {
                return DeserializeObject(value, type, settings: null);
            }
            else
            {
                var settings = new JsonSerializerSettings { Converters = converters };
                var jsonSerializer = JsonSerializer.Create(settings);
                return jsonSerializer.Deserialize(new JTokenReader(value), type);
            }
        }

        public static XmlDocument ToXmlDocument(this JObject value, string deserializeRootElementName, bool writeArrayAttribute)
        {
            return (XmlDocument)DeserializeObject(value, typeof(XmlDocument), new JsonConverter[]
            {
        new XmlNodeConverter
        {
          DeserializeRootElementName = deserializeRootElementName,
          WriteArrayAttribute = writeArrayAttribute
        }
            });
        }

        public static bool IsValidJson(this byte[] value)
        {
            try
            {
                JToken.Parse(Helper.UTF8NoBom.GetStringWithBuffer(value));
            }
            catch
            {
                return false;
            }
            return true;
        }
    }
}
