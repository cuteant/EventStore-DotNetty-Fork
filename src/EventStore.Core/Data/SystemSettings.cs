using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using CuteAnt.Buffers;
using CuteAnt.Pool;
using CuteAnt.Text;
using EventStore.Common.Utils;
using EventStore.Core.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SpanJson.Serialization;

namespace EventStore.Core.Data
{
    public class SystemSettings
    {
        private const int c_initialBufferSize = 1024 * 4;
        private static readonly ArrayPool<byte> s_sharedBufferPool = BufferManager.Shared;

        public static readonly SystemSettings Default = new SystemSettings(
                new StreamAcl(SystemRoles.All, SystemRoles.All, SystemRoles.All, SystemRoles.All, SystemRoles.All),
                new StreamAcl(SystemRoles.Admins, SystemRoles.Admins, SystemRoles.Admins, SystemRoles.Admins, SystemRoles.Admins));

        public readonly StreamAcl UserStreamAcl;
        public readonly StreamAcl SystemStreamAcl;

        public SystemSettings(StreamAcl userStreamAcl, StreamAcl systemStreamAcl)
        {
            UserStreamAcl = userStreamAcl;
            SystemStreamAcl = systemStreamAcl;
        }

        public override string ToString()
        {
            return $"UserStreamAcl: ({UserStreamAcl}), SystemStreamAcl: ({SystemStreamAcl})";
        }

        public static SystemSettings FromJsonBytes(byte[] json)
        {
            using (var reader = new JsonTextReader(new StreamReader(new MemoryStream(json), Encoding.UTF8)))
            {
                reader.ArrayPool = JsonConvertX.GlobalCharacterArrayPool;
                reader.CloseInput = false;

                Check(reader.Read(), reader);
                Check(JsonToken.StartObject, reader);

                StreamAcl userStreamAcl = null;
                StreamAcl systemStreamAcl = null;

                while (true)
                {
                    Check(reader.Read(), reader);
                    if (reader.TokenType == JsonToken.EndObject) { break; }
                    Check(JsonToken.PropertyName, reader);
                    var name = (string)reader.Value;
                    switch (name)
                    {
                        case SystemMetadata.UserStreamAcl: userStreamAcl = StreamMetadata.ReadAcl(reader); break;
                        case SystemMetadata.SystemStreamAcl: systemStreamAcl = StreamMetadata.ReadAcl(reader); break;
                        default:
                            {
                                Check(reader.Read(), reader);
                                // skip
                                JToken.ReadFrom(reader);
                                break;
                            }
                    }
                }
                return new SystemSettings(userStreamAcl, systemStreamAcl);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Check(JsonToken type, JsonTextReader reader)
        {
            if (reader.TokenType != type) { ThrowHelper.ThrowException_InvalidJson(); }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Check(bool read, JsonTextReader reader)
        {
            if (!read) { ThrowHelper.ThrowException_InvalidJson(); }
        }

        public byte[] ToJsonBytes()
        {
            using (var pooledOutputStream = BufferManagerOutputStreamManager.Create())
            {
                var outputStream = pooledOutputStream.Object;
                outputStream.Reinitialize(c_initialBufferSize, s_sharedBufferPool);

                using (JsonTextWriter jsonWriter = new JsonTextWriter(new StreamWriterX(outputStream, StringHelper.UTF8NoBOM)))
                {
                    jsonWriter.ArrayPool = JsonConvertX.GlobalCharacterArrayPool;
                    jsonWriter.CloseOutput = false;

                    WriteAsJson(jsonWriter);
                    jsonWriter.Flush();
                }
                return outputStream.ToByteArray();
            }
        }

        public string ToJsonString()
        {
            using (var pooledStringWriter = StringWriterManager.Create())
            {
                var sw = pooledStringWriter.Object;

                using (JsonTextWriter jsonWriter = new JsonTextWriter(sw))
                {
                    jsonWriter.ArrayPool = JsonConvertX.GlobalCharacterArrayPool;
                    jsonWriter.CloseOutput = false;

                    WriteAsJson(jsonWriter);
                    jsonWriter.Flush();
                }
                return sw.ToString();
            }
        }

        private void WriteAsJson(JsonTextWriter jsonWriter)
        {
            jsonWriter.WriteStartObject();
            if (UserStreamAcl != null)
            {
                jsonWriter.WritePropertyName(SystemMetadata.UserStreamAcl);
                StreamMetadata.WriteAcl(jsonWriter, UserStreamAcl);
            }
            if (SystemStreamAcl != null)
            {
                jsonWriter.WritePropertyName(SystemMetadata.SystemStreamAcl);
                StreamMetadata.WriteAcl(jsonWriter, SystemStreamAcl);
            }
            jsonWriter.WriteEndObject();
        }
    }
}