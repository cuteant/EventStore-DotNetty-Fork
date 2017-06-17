using System;
using System.Collections.Generic;
using System.IO;
using CuteAnt;
using CuteAnt.IO;
using Hyperion;
using Hyperion.SerializerFactories;
using LZ4;

namespace EventStore.ClientAPI.Serialization
{
  internal class HyperionEventSerializer : EventSerializer
  {
    internal readonly Serializer _hyperionSerializer;

    public HyperionEventSerializer()
      : this(surrogates: null, serializerFactories: null, knownTypes: null) { }

    public HyperionEventSerializer(IEnumerable<Surrogate> surrogates,
      IEnumerable<ValueSerializerFactory> serializerFactories = null, IEnumerable<Type> knownTypes = null)
    {
      var options = new SerializerOptions(
          versionTolerance: true,
          preserveObjectReferences: true,
          surrogates: surrogates,
          serializerFactories: serializerFactories,
          knownTypes: knownTypes
      );
      _hyperionSerializer = new Serializer(options);
    }

    /// <inheritdoc/>
    public override bool IsSupportedType(Type itemType) => true;

    /// <inheritdoc/>
    public override object Deserialize(Type expectedType, byte[] data)
    {
      var ms = new MemoryStream(data);
      return _hyperionSerializer.Deserialize(ms);
    }

    /// <inheritdoc/>
    public override byte[] Serialize(object value)
    {
      if (null == value) { return EmptyArray<byte>.Instance; }

      using (var ms = MemoryStreamManager.GetStream())
      {
        _hyperionSerializer.Serialize(value, ms);
        return ms.ToArray();
      }
    }
  }

  internal class GzHyperionEventSerializer : HyperionEventSerializer
  {
    public GzHyperionEventSerializer()
      : base()
    {
    }

    public GzHyperionEventSerializer(IEnumerable<Surrogate> surrogates,
      IEnumerable<ValueSerializerFactory> serializerFactories = null, IEnumerable<Type> knownTypes = null)
      : base(surrogates, serializerFactories, knownTypes)
    {
    }

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
        _hyperionSerializer.Serialize(value, ms);
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

  internal class LZ4HyperionEventSerializer : GzHyperionEventSerializer
  {
    public LZ4HyperionEventSerializer()
      : base()
    {
    }

    public LZ4HyperionEventSerializer(IEnumerable<Surrogate> surrogates,
      IEnumerable<ValueSerializerFactory> serializerFactories = null, IEnumerable<Type> knownTypes = null)
      : base(surrogates, serializerFactories, knownTypes)
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
