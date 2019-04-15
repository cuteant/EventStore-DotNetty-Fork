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
        public static T Deserialize<T>(this ArraySegment<byte> data)
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
        public static TcpPackage AsTcpPackage(this ArraySegment<byte> data)
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
        private static readonly IMessagePackFormatter<Guid> s_guidFormatter = ExtBinaryGuidFormatter.Instance;

        public TcpPackage Deserialize(ref MessagePackReader reader, IFormatterResolver formatterResolver)
        {
            if (reader.IsNil()) { return null; }

            var command = (TcpCommand)reader.ReadByte();
            var flags = (TcpFlags)reader.ReadByte();
            var correlationId = s_guidFormatter.Deserialize(ref reader, null);
            string login = null;
            string pass = null;
            if ((flags & TcpFlags.Authenticated) != 0)
            {
                login = MessagePackBinary.ResolveString(reader.ReadUtf8Span());
                pass = MessagePackBinary.ResolveString(reader.ReadUtf8Span());
            }
            var data = reader.ReadBytes();
            return new TcpPackage(command,
                                  flags,
                                  correlationId,
                                  login,
                                  pass,
                                  data);
        }

        public void Serialize(ref MessagePackWriter writer, ref int idx, TcpPackage value, IFormatterResolver formatterResolver)
        {
            if (value == null) { writer.WriteNil(ref idx); return; }

            var flags = value.Flags;
            writer.WriteByte((byte)value.Command, ref idx);
            writer.WriteByte((byte)flags, ref idx);
            s_guidFormatter.Serialize(ref writer, ref idx, value.CorrelationId, null);
            if ((flags & TcpFlags.Authenticated) != 0)
            {
                var login = s_loginCache.GetOrAdd(value.Login, s_getBytesFunc);
                writer.WriteRawBytes(login, ref idx);
                var password = s_loginCache.GetOrAdd(value.Password, s_getBytesFunc);
                writer.WriteRawBytes(password, ref idx);
            }
            writer.WriteBytes(value.Data, ref idx);
        }

        private static readonly Func<string, byte[]> s_getBytesFunc = s => GetBytesInternal(s);
        private static byte[] GetBytesInternal(string v)
        {
            var len = Helper.UTF8NoBom.GetByteCount(v);
            if (len > 255) ThrowArgumentException(len);

            return MessagePackBinary.GetEncodedStringBytes(v);
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
    }
}
