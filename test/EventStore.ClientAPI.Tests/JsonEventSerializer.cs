using System;
using CuteAnt.Extensions.Serialization;
using Newtonsoft.Json;

namespace EventStore.ClientAPI.Serialization
{
  internal class JsonEventSerializer : IExternalSerializer
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
    public bool IsSupportedType(Type itemType) => true;

    /// <inheritdoc/>
    public object Deserialize(Type expectedType, byte[] data) => _jsonFormatter.Deserialize(expectedType, data);

    /// <inheritdoc/>
    public byte[] SerializeObject(object value) => _jsonFormatter.SerializeObject(value, c_bufferSize);
  }
}
