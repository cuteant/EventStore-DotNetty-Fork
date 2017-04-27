using System;
using System.Buffers;
using System.Xml;
using CuteAnt.Extensions.Serialization.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Formatting = Newtonsoft.Json.Formatting;

namespace EventStore.Common.Utils
{
  public static class Json
  {
    public static readonly IArrayPool<char> GlobalCharacterArrayPool = new JsonArrayPool<char>(ArrayPool<char>.Shared);

    public static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
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

    public static byte[] ToJsonBytes(this object source)
    {
      string instring = JsonConvert.SerializeObject(source, Formatting.Indented, JsonSettings);
      return Helper.UTF8NoBom.GetBytes(instring);
    }

    public static string ToJson(this object source)
    {
      return JsonConvert.SerializeObject(source, Formatting.Indented, JsonSettings);
    }

    public static string ToCanonicalJson(this object source)
    {
      return JsonConvert.SerializeObject(source);
    }

    public static T ParseJson<T>(this string json)
    {
      return JsonConvert.DeserializeObject<T>(json, JsonSettings);
    }

    public static T ParseJson<T>(this byte[] json)
    {
      return JsonConvert.DeserializeObject<T>(Helper.UTF8NoBom.GetStringWithBuffer(json), JsonSettings);
    }

    public static object DeserializeObject(JObject value, Type type, JsonSerializerSettings settings)
    {
      var jsonSerializer = JsonSerializer.Create(settings);
      return jsonSerializer.Deserialize(new JTokenReader(value), type);
    }

    public static object DeserializeObject(JObject value, Type type, params JsonConverter[] converters)
    {
      var settings = converters == null || converters.Length <= 0
                                       ? null
                                       : new JsonSerializerSettings { Converters = converters };
      return DeserializeObject(value, type, settings);
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
