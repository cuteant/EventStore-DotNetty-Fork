using System;
using System.Xml;
using CuteAnt.Buffers;
using CuteAnt.Extensions.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Formatting = Newtonsoft.Json.Formatting;

namespace EventStore.Common.Utils
{
  public static class Json
  {
    public static readonly JsonSerializerSettings JsonSettings;
    private static readonly JsonMessageFormatter _jsonFormatter;

    static Json()
    {
      JsonSettings = new JsonSerializerSettings
      {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        DateFormatHandling = DateFormatHandling.IsoDateFormat,
        NullValueHandling = NullValueHandling.Ignore,
        DefaultValueHandling = DefaultValueHandling.Ignore,
        MissingMemberHandling = MissingMemberHandling.Ignore,
        TypeNameHandling = TypeNameHandling.None,
        Converters = new JsonConverter[]
        {
          new StringEnumConverter(),
          new Newtonsoft.Json.Converters.IPAddressConverter(),
          new Newtonsoft.Json.Converters.IPEndPointConverter(),
          new CombGuidConverter()
        }
      };

      _jsonFormatter = new JsonMessageFormatter()
      {
        DefaultSerializerSettings = JsonSettings,
        Indent = true
      };
    }

    public static byte[] ToJsonBytes(this object source)
    {
      //return JsonConvertX.SerializeObjectToBytes(source, Formatting.Indented, JsonSettings);
      return _jsonFormatter.SerializeToBytes(source);
    }

    public static string ToJson(this object source)
    {
      return JsonConvertX.SerializeObject(source, Formatting.Indented, JsonSettings);
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
      //return JsonConvertX.ParseJson<T>(json, JsonSettings);
      return _jsonFormatter.DeserializeFromBytes<T>(json);
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
        return DeserializeObject(value, type, (JsonSerializerSettings)null);
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
