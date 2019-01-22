using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using CuteAnt.Collections;
using EventStore.Transport.Tcp.Messages;
using MessagePack;
using MessagePack.Formatters;

#if CLIENTAPI
using EventStore.ClientAPI.Common.Utils;
namespace EventStore.ClientAPI.Transport.Tcp
#else
using EventStore.Common.Utils;
namespace EventStore.Core.Services.Transport.Tcp
#endif
{
    public static class MessagePackExtensions
    {
        private static readonly IFormatterResolver DefaultResolver;

        static MessagePackExtensions()
        {
            MessagePackStandardResolver.TryRegister(TcpPackageFormatter.Instance);
            DefaultResolver = MessagePackStandardResolver.Default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Deserialize<T>(this byte[] data)
        {
            return MessagePackSerializer.Deserialize<T>(data, DefaultResolver);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Deserialize<T>(this in ArraySegment<byte> data)
        {
            return MessagePackSerializer.Deserialize<T>(data, DefaultResolver);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] Serialize<T>(this T protoContract)
        {
            return MessagePackSerializer.Serialize(protoContract, DefaultResolver);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] SerializeToArray<T>(this T protoContract)
        {
            return MessagePackSerializer.Serialize(protoContract, DefaultResolver);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TcpPackage AsTcpPackage(this byte[] data)
        {
            return MessagePackSerializer.Deserialize<TcpPackage>(data, DefaultResolver);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TcpPackage AsTcpPackage(this in ArraySegment<byte> data)
        {
            return MessagePackSerializer.Deserialize<TcpPackage>(data, DefaultResolver);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] AsByteArray(this TcpPackage data)
        {
            return MessagePackSerializer.Serialize(data, DefaultResolver);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ArraySegment<byte> AsArraySegment(this TcpPackage data)
        {
            return new ArraySegment<byte>(MessagePackSerializer.Serialize(data, DefaultResolver));
        }
    }

    public sealed class TcpPackageFormatter : IMessagePackFormatter<TcpPackage>
    {
        public static readonly TcpPackageFormatter Instance = new TcpPackageFormatter();

        private static readonly CachedReadConcurrentDictionary<string, byte[]> s_loginCache =
            new CachedReadConcurrentDictionary<string, byte[]>(StringComparer.Ordinal);
        private static readonly CachedReadConcurrentDictionary<byte[], string> s_loginCache2 =
            new CachedReadConcurrentDictionary<byte[], string>(ByteArrayComparer.Instance);

        public TcpPackage Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset)) { readSize = 1; return null; }

            var startOffset = offset;

            var command = (TcpCommand)MessagePackBinary.ReadByte(bytes, offset, out readSize);
            offset += readSize;
            var flags = (TcpFlags)MessagePackBinary.ReadByte(bytes, offset, out readSize);
            offset += readSize;
            var correlationId = new Guid(MessagePackBinary.ReadBytes(bytes, offset, out readSize));
            offset += readSize;
            string login = null;
            string pass = null;
            if ((flags & TcpFlags.Authenticated) != 0)
            {
                var bts = MessagePackBinary.ReadBytes(bytes, offset, out readSize);
                offset += readSize;
                login = s_loginCache2.GetOrAdd(bts, s_getStringFunc);
                bts = MessagePackBinary.ReadBytes(bytes, offset, out readSize);
                offset += readSize;
                pass = s_loginCache2.GetOrAdd(bts, s_getStringFunc);
            }
            var data = MessagePackBinary.ReadBytes(bytes, offset, out readSize);
            readSize += offset - startOffset;
            return new TcpPackage(command,
                                  flags,
                                  correlationId,
                                  login,
                                  pass,
                                  data);
        }

        public int Serialize(ref byte[] bytes, int offset, TcpPackage value, IFormatterResolver formatterResolver)
        {
            if (value == null) { return MessagePackBinary.WriteNil(ref bytes, offset); }

            var startOffset = offset;

            var flags = value.Flags;
            offset += MessagePackBinary.WriteByte(ref bytes, offset, (byte)value.Command);
            offset += MessagePackBinary.WriteByte(ref bytes, offset, (byte)flags);
            offset += MessagePackBinary.WriteBytes(ref bytes, offset, value.CorrelationId.ToByteArray());
            if ((flags & TcpFlags.Authenticated) != 0)
            {
                offset += MessagePackBinary.WriteBytes(ref bytes, offset, s_loginCache.GetOrAdd(value.Login, s_getBytesFunc));
                offset += MessagePackBinary.WriteBytes(ref bytes, offset, s_loginCache.GetOrAdd(value.Password, s_getBytesFunc));
            }
            offset += MessagePackBinary.WriteBytes(ref bytes, offset, value.Data);

            return offset - startOffset;
        }

        private static Func<string, byte[]> s_getBytesFunc = GetBytesInternal;
        private static byte[] GetBytesInternal(string v)
        {
            var len = Helper.UTF8NoBom.GetByteCount(v);
            if (len > 255) ThrowArgumentException(len);

            return Helper.UTF8NoBom.GetBytes(v);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowArgumentException(int len)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException($"Login or password serialized length should be less than 256 bytes (but is {len}).");
            }
        }

        private static Func<byte[], string> s_getStringFunc = GetStringInternal;
        private static string GetStringInternal(byte[] v) => Encoding.UTF8.GetString(v);

        internal sealed class ByteArrayComparer : IEqualityComparer<byte[]>
        {
            public static readonly ByteArrayComparer Instance = new ByteArrayComparer();

            public bool Equals(byte[] a, byte[] b)
            {
                if (ReferenceEquals(a, b)) { return true; }
                if (a.Length != b.Length) { return false; }
                var length = a.Length;
                for (var i = 0; i < length; i++)
                {
                    if (a[i] != b[i]) { return false; }
                }
                return true;
            }

            public int GetHashCode(byte[] obj) => obj.GetHashCode();
        }
    }
}
