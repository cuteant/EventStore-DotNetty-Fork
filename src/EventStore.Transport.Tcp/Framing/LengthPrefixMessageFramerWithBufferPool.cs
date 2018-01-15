using System;
using System.Collections.Generic;
using EventStore.BufferManagement;
using Microsoft.Extensions.Logging;
using EventStore.Common.Utils;

namespace EventStore.Transport.Tcp.Framing
{
    public class LengthPrefixMessageFramerWithBufferPool
    {
        private static readonly ILogger Log = TraceLogger.GetLogger<LengthPrefixMessageFramerWithBufferPool>();

        private const int PrefixLength = sizeof(int);

        private readonly int _maxPackageSize;
        private readonly BufferManager _bufferManager;
        private BufferPool _messageBuffer;
        private Action<BufferPool> _receivedHandler;

        private int _headerBytes;
        private int _packageLength;

        /// <summary>Initializes a new instance of the <see cref="LengthPrefixMessageFramerWithBufferPool"/> class.</summary>
        public LengthPrefixMessageFramerWithBufferPool(BufferManager bufferManager, int maxPackageSize = 16 * 1024 * 1024)
        {
            Ensure.Positive(maxPackageSize, nameof(maxPackageSize));
            _bufferManager = bufferManager ?? throw new ArgumentNullException(nameof(bufferManager));
            _maxPackageSize = maxPackageSize;
        }

        public void Reset()
        {
            _messageBuffer = null;
            _headerBytes = 0;
            _packageLength = 0;
        }

        public void UnFrameData(IEnumerable<ArraySegment<byte>> data)
        {
            if (data == null) { throw new ArgumentNullException(nameof(data)); }

            foreach (ArraySegment<byte> buffer in data)
            {
                Parse(buffer);
            }
        }

        public void UnFrameData(in ArraySegment<byte> data)
        {
            Parse(data);
        }

        /// <summary>Parses a stream chunking based on length-prefixed framing. Calls are re-entrant and hold state internally.</summary>
        /// <param name="bytes">A byte array of data to append</param>
        private void Parse(in ArraySegment<byte> bytes)
        {
            byte[] data = bytes.Array;
            for (int i = bytes.Offset; i < bytes.Offset + bytes.Count;)
            {
                if (_headerBytes < PrefixLength)
                {
                    _packageLength |= (data[i] << (_headerBytes * 8)); // little-endian order
                    ++_headerBytes;
                    i += 1;
                    if (_headerBytes == PrefixLength)
                    {
                        if (_packageLength <= 0 || _packageLength > _maxPackageSize)
                        {
                            Log.LogError("FRAMING ERROR! Data:");
                            Log.LogError(Common.Utils.Helper.FormatBinaryDump(bytes));
                            throw new PackageFramingException($"Package size is out of bounds: {_packageLength} (max: {_maxPackageSize}).");
                        }

                        _messageBuffer = new BufferPool(_bufferManager);
                    }
                }
                else
                {
                    int copyCnt = Math.Min(bytes.Count + bytes.Offset - i, _packageLength - _messageBuffer.Length);
                    _messageBuffer.Append(bytes.Array, i, copyCnt);
                    i += copyCnt;

                    if (_messageBuffer.Length == _packageLength)
                    {
                        _receivedHandler?.Invoke(_messageBuffer);
                        _messageBuffer = null;
                        _headerBytes = 0;
                        _packageLength = 0;
                    }
                }
            }
        }

        public IEnumerable<ArraySegment<byte>> FrameData(ArraySegment<byte> data)
        {
            var length = data.Count;

            yield return new ArraySegment<byte>(new[] { (byte)length, (byte)(length >> 8), (byte)(length >> 16), (byte)(length >> 24) });
            yield return data;
        }

        public void RegisterMessageArrivedCallback(Action<BufferPool> handler)
        {
            _receivedHandler = handler ?? throw new ArgumentNullException(nameof(handler));
        }
    }
}