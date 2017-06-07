using System;
using System.IO;
using CuteAnt.IO;
using LZ4;
using ProtoBuf;

namespace EventStore.ClientAPI.Serialization
{
  internal class ProtobufEventSerializer : EventSerializer
  {
    /// <inheritdoc/>
    public override bool IsSupportedType(Type itemType) => true;

    /// <inheritdoc/>
    public override object Deserialize(Type expectedType, byte[] data)
    {
      using (var stream = new MemoryStream(data))
      {
        return Serializer.Deserialize(expectedType, stream);
      }
    }

    /// <inheritdoc/>
    public override byte[] Serialize(object value)
    {
      using (var stream = MemoryStreamManager.GetStream())
      {
        Serializer.Serialize(stream, value);
        return stream.ToArray();
      }
    }
  }

  internal class GzProtobufEventSerializer : ProtobufEventSerializer
  {
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
        Serializer.Serialize(ms, value);
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

  internal class LZ4ProtobufEventSerializer : GzProtobufEventSerializer
  {
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
