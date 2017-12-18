using System;
using System.IO;
using CuteAnt.Buffers;
using Microsoft.Extensions.Logging;
using ProtoBuf;

namespace EventStore.Core.Services.Transport.Tcp
{
    public static class ProtobufExtensions
    {
        private const int c_bufferSize = 1024 * 2;
        private static readonly ILogger Log = TraceLogger.GetLogger(typeof(ProtobufExtensions));

        public static T Deserialize<T>(this byte[] data)
        {
            return Deserialize<T>(new ArraySegment<byte>(data));
        }

        public static T Deserialize<T>(this ArraySegment<byte> data)
        {
            try
            {
                using (var memory = new MemoryStream(data.Array, data.Offset, data.Count)) //uses original buffer as memory
                {
                    var res = Serializer.Deserialize<T>(memory);
                    return res;
                }
            }
            catch (Exception e)
            {
                if (Log.IsInformationLevelEnabled()) Log.LogInformation(e, "Deserialization to {0} failed", typeof(T).FullName);
                return default(T);
            }
        }

        public static ArraySegment<byte> Serialize<T>(this T protoContract)
        {
            using (var pooledOutputStream = BufferManagerOutputStreamManager.Create())
            {
                var outputStream = pooledOutputStream.Object;
                outputStream.Reinitialize(c_bufferSize);
                Serializer.Serialize(outputStream, protoContract);
                var bytes = outputStream.ToByteArray();
                return new ArraySegment<byte>(bytes);
            }
        }

        public static byte[] SerializeToArray<T>(this T protoContract)
        {
            using (var pooledOutputStream = BufferManagerOutputStreamManager.Create())
            {
                var outputStream = pooledOutputStream.Object;
                outputStream.Reinitialize(c_bufferSize);
                Serializer.Serialize(outputStream, protoContract);
                return outputStream.ToByteArray();
            }
        }
    }
}
