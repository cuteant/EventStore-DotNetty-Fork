using System;
using CuteAnt.Extensions.Serialization;
using CuteAnt.IO;
using LZ4;
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
    public override object Deserialize(Type expectedType, byte[] data) => _jsonFormatter.DeserializeFromBytes(expectedType, data);

    /// <inheritdoc/>
    public override byte[] Serialize(object value) => _jsonFormatter.SerializeToBytes(value, c_bufferSize);
  }

  internal class GzJsonEventSerializer : JsonEventSerializer
  {
    public GzJsonEventSerializer()
      : base()
    {
    }

    public GzJsonEventSerializer(JsonSerializerSettings serializerSettings, JsonSerializerSettings deserializerSettings)
      : base(serializerSettings, deserializerSettings)
    {
    }

    /// <inheritdoc/>
    public override object Deserialize(Type expectedType, byte[] data)
    {
      if (null == data) { throw new ArgumentNullException(nameof(data)); }

      var compressedData = this.Decompression(data);

      return base.Deserialize(expectedType, compressedData);
    }

    /// <inheritdoc/>
    public override byte[] Serialize(object value)
    {
      using (var ms = MemoryStreamManager.GetStream())
      {
        _jsonFormatter.WriteToStream(value.GetType(), value, ms, null, null);
        var getBuffer = ms.GetBuffer();
        return Compression(getBuffer, 0, (int)ms.Length);
      }
    }

    protected virtual byte[] Compression(byte[] data, int offset, int count)
    {
      return GZipHelper.Compression(data, offset, count);
    }

    protected virtual byte[] Decompression(byte[] compressedData)
    {
      return GZipHelper.Decompression(compressedData);
    }
  }

  internal class LZ4JsonEventSerializer : GzJsonEventSerializer
  {
    public LZ4JsonEventSerializer()
      : base()
    {
    }

    public LZ4JsonEventSerializer(JsonSerializerSettings serializerSettings, JsonSerializerSettings deserializerSettings)
      : base(serializerSettings, deserializerSettings)
    {
    }

    /// <inheritdoc/>
    protected override byte[] Compression(byte[] data, int offset, int count)
    {
      return LZ4Codec.Wrap(data, offset, count);
    }

    /// <inheritdoc/>
    protected override byte[] Decompression(byte[] compressedData)
    {
      return LZ4Codec.Unwrap(compressedData);
    }
  }
}
