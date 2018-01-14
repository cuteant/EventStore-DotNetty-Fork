using System;
using CuteAnt.Extensions.Serialization;
using Microsoft.Extensions.Logging;

namespace EventStore.Core.Services.Transport.Tcp
{
    public static class ProtobufExtensions
    {
        private static readonly ILogger Log = TraceLogger.GetLogger(typeof(ProtobufExtensions));
        private static readonly MessagePackMessageFormatter s_formatter = MessagePackMessageFormatter.DefaultInstance;

        public static T Deserialize<T>(this byte[] data)
        {
            return s_formatter.Deserialize<T>(data);
        }

        public static T Deserialize<T>(this ArraySegment<byte> data)
        {
            return s_formatter.Deserialize<T>(data.Array, data.Offset, data.Count);
        }

        public static ArraySegment<byte> Serialize<T>(this T protoContract)
        {
            return new ArraySegment<byte>(s_formatter.SerializeObject(protoContract));
        }

        public static byte[] SerializeToArray<T>(this T protoContract)
        {
            return s_formatter.SerializeObject(protoContract);
        }
    }
}
