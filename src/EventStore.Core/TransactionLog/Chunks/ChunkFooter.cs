using System;
using System.IO;
using EventStore.Core.TransactionLog.Chunks.TFChunk;

namespace EventStore.Core.TransactionLog.Chunks
{
    public class ChunkFooter
    {
        public const int Size = 128;
        public const int ChecksumSize = 16;

        // flags within single byte
        public readonly bool IsCompleted;
        public readonly bool IsMap12Bytes;

        public readonly int PhysicalDataSize; // the size of a section of data in chunk
        public readonly long LogicalDataSize;  // the size of a logical data size (after scavenge LogicalDataSize can be > physicalDataSize)
        public readonly int MapSize;
        public readonly byte[] MD5Hash;

        public readonly int MapCount; // calculated, not stored

        public ChunkFooter(bool isCompleted, bool isMap12Bytes, int physicalDataSize, long logicalDataSize, int mapSize, byte[] md5Hash)
        {
            if ((uint)physicalDataSize > Consts.TooBigOrNegative) { ThrowHelper.ThrowArgumentOutOfRangeException_Nonnegative(ExceptionArgument.physicalDataSize); }
            if ((ulong)logicalDataSize > Consts.TooBigOrNegativeUL) { ThrowHelper.ThrowArgumentOutOfRangeException_Nonnegative(ExceptionArgument.logicalDataSize); }
            if ((ulong)logicalDataSize < (ulong)physicalDataSize)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException_LogicalDataSizeIsLessThanPhysicalDataSize(logicalDataSize, physicalDataSize);
            }
            if ((uint)mapSize > Consts.TooBigOrNegative) { ThrowHelper.ThrowArgumentOutOfRangeException_Nonnegative(ExceptionArgument.mapSize); }
            if (md5Hash is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.md5Hash); }
            if ((uint)md5Hash.Length != (uint)ChecksumSize)
            {
                ThrowHelper.ThrowArgumentException_MD5HashIsOfWrongLength();
            }

            IsCompleted = isCompleted;
            IsMap12Bytes = isMap12Bytes;

            PhysicalDataSize = physicalDataSize;
            LogicalDataSize = logicalDataSize;
            MapSize = mapSize;
            MD5Hash = md5Hash;

            var posMapSize = isMap12Bytes ? PosMap.FullSize : PosMap.DeprecatedSize;
            if (MapSize % posMapSize != 0)
            {
                ThrowHelper.ThrowException_WrongMapSize(MapSize, posMapSize);
            }

            MapCount = mapSize / posMapSize;
        }

        public byte[] AsByteArray()
        {
            var array = new byte[Size];
            using (var memStream = new MemoryStream(array))
            using (var writer = new BinaryWriter(memStream))
            {
                var flags = (byte)((IsCompleted ? 1 : 0) | (IsMap12Bytes ? 2 : 0));
                writer.Write(flags);
                writer.Write(PhysicalDataSize);
                if (IsMap12Bytes)
                {
                    writer.Write(LogicalDataSize);
                }
                else
                {
                    writer.Write((int)LogicalDataSize);
                }

                writer.Write(MapSize);

                memStream.Position = Size - ChecksumSize;
                writer.Write(MD5Hash);
            }
            return array;
        }

        public static ChunkFooter FromStream(Stream stream)
        {
            var reader = new BinaryReader(stream);
            var flags = reader.ReadByte();
            var isCompleted = (flags & 1) != 0;
            var isMap12Bytes = (flags & 2) != 0;
            var physicalDataSize = reader.ReadInt32();
            var logicalDataSize = isMap12Bytes ? reader.ReadInt64() : reader.ReadInt32();
            var mapSize = reader.ReadInt32();
            stream.Seek(-ChecksumSize, SeekOrigin.End);
            var hash = reader.ReadBytes(ChecksumSize);

            return new ChunkFooter(isCompleted, isMap12Bytes, physicalDataSize, logicalDataSize, mapSize, hash);
        }
    }
}