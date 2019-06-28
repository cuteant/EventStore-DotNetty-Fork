using System;
using System.Collections.Generic;
using System.IO;
using EventStore.Common.Utils;
using EventStore.Core.TransactionLog.Chunks;
using EventStore.Transport.Tcp;
using Microsoft.Extensions.Logging;

namespace EventStore.Core.Helpers
{
    public class LengthPrefixSuffixFramer
    {
        private static readonly ILogger Log = TraceLogger.GetLogger<LengthPrefixSuffixFramer>();

        private const int PrefixLength = sizeof(int);

        public bool HasData { get { return (ulong)_memStream.Length > 0ul; } }

        private readonly int _maxPackageSize;
        private readonly Action<BinaryReader> _packageHandler;

        private readonly MemoryStream _memStream;
        private readonly BinaryReader _binaryReader;

        private int _prefixBytes;
        private int _packageLength;

        public LengthPrefixSuffixFramer(Action<BinaryReader> packageHandler, int maxPackageSize = TFConsts.MaxLogRecordSize)
        {
            if (null == packageHandler) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.packageHandler); }
            if ((uint)(maxPackageSize - 1) >= Consts.TooBigOrNegative) { ThrowHelper.ThrowArgumentOutOfRangeException_Positive(ExceptionArgument.maxPackageSize); }

            _maxPackageSize = maxPackageSize;
            _packageHandler = packageHandler;

            _memStream = new MemoryStream();
            _binaryReader = new BinaryReader(_memStream);
        }

        public void Reset()
        {
            _memStream.SetLength(0);
            _prefixBytes = 0;
            _packageLength = 0;
        }

        public void UnFrameData(IEnumerable<ArraySegment<byte>> data)
        {
            if (data == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.data);

            foreach (ArraySegment<byte> buffer in data)
            {
                Parse(buffer);
            }
        }

        public void UnFrameData(ArraySegment<byte> data)
        {
            Parse(data);
        }

        /// <summary>Parses a stream chunking based on length-prefixed-suffixed framing. Calls are re-entrant and hold state internally.</summary>
        /// <param name="bytes">A byte array of data to append.</param>
        private void Parse(ArraySegment<byte> bytes)
        {
            var data = bytes.Array;
            for (int i = bytes.Offset; i < bytes.Offset + bytes.Count;)
            {
                if (_prefixBytes < PrefixLength)
                {
                    _packageLength |= (data[i] << (_prefixBytes * 8)); // little-endian order
                    _prefixBytes += 1;
                    i += 1;
                    if (_prefixBytes == PrefixLength)
                    {
                        if ((uint)(_packageLength - 1) >= Consts.TooBigOrNegative || (uint)_packageLength > (uint)_maxPackageSize)
                        {
                            Log.FramingError(bytes);
                            ThrowHelper.ThrowPackageFramingException(_packageLength, _maxPackageSize);
                        }
                        _packageLength += PrefixLength; // we need to read suffix as well
                    }
                }
                else
                {
                    int copyCnt = Math.Min(bytes.Count + bytes.Offset - i, _packageLength - (int)_memStream.Length);
                    _memStream.Write(bytes.Array, i, copyCnt);
                    i += copyCnt;

                    if ((ulong)_memStream.Length == (ulong)_packageLength)
                    {
#if DEBUG
                        var buf = _memStream.GetBuffer();
                        int suffixLength = (buf[_packageLength - 4] << 0)
                                         | (buf[_packageLength - 3] << 8)
                                         | (buf[_packageLength - 2] << 16)
                                         | (buf[_packageLength - 1] << 24);
                        if (_packageLength - PrefixLength != suffixLength)
                        {
                            ThrowHelper.ThrowException_PrefixLengthIsNotEqualToSuffixLength(_packageLength ,PrefixLength, suffixLength);
                        }
#endif
                        _memStream.SetLength(_packageLength - PrefixLength); // remove suffix length
                        _memStream.Position = 0;

                        _packageHandler(_binaryReader);

                        _memStream.SetLength(0);
                        _prefixBytes = 0;
                        _packageLength = 0;
                    }
                }
            }
        }

        public IEnumerable<ArraySegment<byte>> FrameData(ArraySegment<byte> data)
        {
            var length = data.Count;

            var lengthArray = new ArraySegment<byte>(new[]
            {
                (byte)length, (byte)(length >> 8), (byte)(length >> 16), (byte)(length >> 24)
            });
            yield return lengthArray;
            yield return data;
            yield return lengthArray;
        }
    }
}