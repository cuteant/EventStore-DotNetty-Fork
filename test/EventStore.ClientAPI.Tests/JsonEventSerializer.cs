using System;
using CuteAnt.Extensions.Serialization;
using CuteAnt.IO;
using Newtonsoft.Json;

namespace EventStore.ClientAPI.Serialization
{
  internal class JsonEventSerializer : EventSerializer
  {
    private const int c_bufferSize = 1024 * 2;

    internal readonly JsonMessageFormatter _jsonFormatter;

    public JsonEventSerializer()
    {
      _jsonFormatter = new JsonMessageFormatter()
      {
        DefaultSerializerSettings = JsonConvertX.IndentedIncludeTypeNameStringEnumSettings,
        DefaultDeserializerSettings = JsonConvertX.IndentedIncludeTypeNameStringEnumSettings
      };
    }

    public JsonEventSerializer(JsonSerializerSettings serializerSettings, JsonSerializerSettings deserializerSettings)
    {
      if (null == serializerSettings) { throw new ArgumentNullException(nameof(serializerSettings)); }
      if (null == deserializerSettings) { throw new ArgumentNullException(nameof(deserializerSettings)); }

      _jsonFormatter = new JsonMessageFormatter()
      {
        DefaultSerializerSettings = serializerSettings,
        DefaultDeserializerSettings = deserializerSettings
      };
    }

    /// <inheritdoc/>
    public override bool IsSupportedType(Type itemType) => true;

    /// <inheritdoc/>
    public override object Deserialize(Type expectedType, byte[] data) => _jsonFormatter.Deserialize(expectedType, data);

    /// <inheritdoc/>
    public override byte[] Serialize(object value) => _jsonFormatter.SerializeObject(value, c_bufferSize);
  }
}
