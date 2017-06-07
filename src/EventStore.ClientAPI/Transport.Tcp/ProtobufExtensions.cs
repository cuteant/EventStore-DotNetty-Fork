using System;
using System.IO;
using CuteAnt.IO;
using Microsoft.Extensions.Logging;
using ProtoBuf;

namespace EventStore.ClientAPI.Transport.Tcp
{
  internal static class ProtobufExtensions
  {
    private static readonly ILogger Log = TraceLogger.GetLogger(typeof(ProtobufExtensions));

    public static T Deserialize<T>(this byte[] data)
    {
      return Deserialize<T>(new ArraySegment<byte>(data));
    }

    public static T Deserialize<T>(this ArraySegment<byte> data)
    {
      try
      {
        using (var memory = new MemoryStream(data.Array, data.Offset, data.Count))
        {
          var res = Serializer.Deserialize<T>(memory);
          return res;
        }
      }
      catch (Exception e)
      {
        if (Log.IsDebugLevelEnabled())
        {
          Log.LogDebug("Deserialization to {0} failed : {1}", typeof(T).FullName, e);
        }
        return default(T);
      }
    }

    public static ArraySegment<byte> Serialize<T>(this T protoContract)
    {
      using (var memory = MemoryStreamManager.GetStream())
      {
        Serializer.Serialize(memory, protoContract);
        var bytes = memory.ToArray();
        return new ArraySegment<byte>(bytes, 0, bytes.Length);
      }
    }

    public static byte[] SerializeToArray<T>(this T protoContract)
    {
      using (var memory = MemoryStreamManager.GetStream())
      {
        Serializer.Serialize(memory, protoContract);
        return memory.ToArray();
      }
    }
  }
}
