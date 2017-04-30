using System;
using System.Text;
using Microsoft.Extensions.Logging;
using EventStore.Common.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace EventStore.Transport.Http.Codecs
{
  public class JsonCodec : ICodec
  {
    public static Formatting Formatting = Formatting.Indented;

    private static readonly ILogger Log = TraceLogger.GetLogger<JsonCodec>();
    private static readonly JsonSerializerSettings FromSettings = new JsonSerializerSettings
    {
      ContractResolver = new CamelCasePropertyNamesContractResolver(),
      DateParseHandling = DateParseHandling.None,
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

    public static readonly JsonSerializerSettings ToSettings = new JsonSerializerSettings
    {
      ContractResolver = new CamelCasePropertyNamesContractResolver(),
      DateFormatHandling = DateFormatHandling.IsoDateFormat,
      NullValueHandling = NullValueHandling.Ignore,
      DefaultValueHandling = DefaultValueHandling.Include,
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


    public string ContentType { get { return Http.ContentType.Json; } }
    public Encoding Encoding { get { return Helper.UTF8NoBom; } }
    public bool HasEventIds { get { return false; } }
    public bool HasEventTypes { get { return false; } }

    public bool CanParse(MediaType format)
    {
      return format != null && format.Matches(ContentType, Encoding);
    }

    public bool SuitableForResponse(MediaType component)
    {
      return string.Equals(component.Type, "*", StringComparison.Ordinal)
             || (string.Equals(component.Type, "application", StringComparison.OrdinalIgnoreCase)
                 && (string.Equals(component.Subtype, "*", StringComparison.Ordinal)
                     || string.Equals(component.Subtype, "json", StringComparison.OrdinalIgnoreCase)));
    }

    public T From<T>(string text)
    {
      try
      {
        return JsonConvertX.DeserializeObject<T>(text, FromSettings);
      }
      catch (Exception e)
      {
        Log.LogError(e, "'{0}' is not a valid serialized {1}", text, typeof(T).FullName);
        return default(T);
      }
    }

    public string To<T>(T value)
    {
      if (value == null) { return ""; }

      if ((object)value == Empty.Result) { return Empty.Json; }

      try
      {
        return JsonConvertX.SerializeObject(value, Formatting, ToSettings);
      }
      catch (Exception ex)
      {
        Log.LogError(ex, "Error serializing object {0}", value);
        return null;
      }
    }
  }
}