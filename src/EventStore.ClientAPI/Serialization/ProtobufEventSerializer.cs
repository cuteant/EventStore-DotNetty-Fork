using System;
using System.IO;
using CuteAnt;
using CuteAnt.IO;
using LZ4;
using ProtoBuf.Meta;

namespace EventStore.ClientAPI.Serialization
{
  internal class ProtobufEventSerializer : EventSerializer
  {
    internal static readonly RuntimeTypeModel RuntimeModel = RuntimeTypeModel.Default;
    /// <inheritdoc/>
    public override bool IsSupportedType(Type itemType) => true;

    /// <inheritdoc/>
    public override object Deserialize(Type expectedType, byte[] data)
    {
      var stream = new MemoryStream(data);
      return RuntimeModel.Deserialize(stream, null, expectedType);
    }

    /// <inheritdoc/>
    public override byte[] Serialize(object value)
    {
      if (null == value) { return EmptyArray<byte>.Instance; }

      using (var stream = MemoryStreamManager.GetStream())
      {
        RuntimeModel.Serialize(stream, value, null);
        return stream.ToArray();
      }
    }
  }

  internal class GzProtobufEventSerializer : ProtobufEventSerializer
  {
    /// <inheritdoc/>
    public override object Deserialize(Type expectedType, byte[] data)
    {
      var compressedData = this.Decompression(data);

      return base.Deserialize(expectedType, compressedData);
    }

    /// <inheritdoc/>
    public override byte[] Serialize(object value)
    {
      if (null == value) { return EmptyArray<byte>.Instance; }

      using (var ms = MemoryStreamManager.GetStream())
      {
        RuntimeModel.Serialize(ms, value, null);
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
