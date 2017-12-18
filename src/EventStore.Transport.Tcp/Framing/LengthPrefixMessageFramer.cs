using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using EventStore.Common.Utils;

namespace EventStore.Transport.Tcp.Framing
{

    /// <summary>
    /// Uses length-prefixed framing to encode outgoing messages and decode
    /// incoming messages, using internal state and raising a callback once 
    /// full message arrives.
    /// </summary>
    public class LengthPrefixMessageFramer : IMessageFramer
    {
        private static readonly ILogger Log = TraceLogger.GetLogger<LengthPrefixMessageFramer>();

        public const int HeaderLength = sizeof(Int32);

        private byte[] _messageBuffer;
        private int _bufferIndex = 0;
        private Action<ArraySegment<byte>> _receivedHandler;
        private readonly int _maxPackageSize;

        private int _headerBytes = 0;
        private int _packageLength = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="LengthPrefixMessageFramer"/> class.
        /// </summary>
        public LengthPrefixMessageFramer(int maxPackageSize = 64 * 1024 * 1024)
        {
            Ensure.Positive(maxPackageSize, nameof(maxPackageSize));
            _maxPackageSize = maxPackageSize;
        }

        public void Reset()
        {
            _messageBuffer = null;
            _headerBytes = 0;
            _packageLength = 0;
            _bufferIndex = 0;
        }

        public void UnFrameData(IEnumerable<ArraySegment<byte>> data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            foreach (ArraySegment<byte> buffer in data)
            {
                Parse(buffer);
            }
        }

        public void UnFrameData(ArraySegment<byte> data)
        {
            Parse(data);
        }

        /// <summary>Parses a stream chunking based on length-prefixed framing. 
        /// Calls are re-entrant and hold state internally. Once full message arrives,
        /// callback is raised (it is registered via <see cref="RegisterMessageArrivedCallback"/>.</summary>
        /// <param name="bytes">A byte array of data to append</param>
        private void Parse(ArraySegment<byte> bytes)
        {
            byte[] data = bytes.Array;
            for (int i = bytes.Offset, n = bytes.Offset + bytes.Count; i < n; i++)
            {
                if (_headerBytes < HeaderLength)
                {
                    _packageLength |= (data[i] << (_headerBytes * 8)); // little-endian order
                    ++_headerBytes;
                    if (_headerBytes == HeaderLength)
                    {
                        if (_packageLength <= 0 || _packageLength > _maxPackageSize)
                        {
                            Log.LogError("FRAMING ERROR! Data:");
                            Log.LogError(Common.Utils.Helper.FormatBinaryDump(bytes));
                            throw new PackageFramingException($"Package size is out of bounds: {_packageLength} (max: {_maxPackageSize}).");
                        }

                        _messageBuffer = new byte[_packageLength];
                    }
                }
                else
                {
                    int copyCnt = Math.Min(bytes.Count + bytes.Offset - i, _packageLength - _bufferIndex);
                    Buffer.BlockCopy(bytes.Array, i, _messageBuffer, _bufferIndex, copyCnt);
                    _bufferIndex += copyCnt;
                    i += copyCnt - 1;

                    if (_bufferIndex == _packageLength)
                    {
                        _receivedHandler?.Invoke(new ArraySegment<byte>(_messageBuffer, 0, _bufferIndex));
                        _messageBuffer = null;
                        _headerBytes = 0;
                        _packageLength = 0;
                        _bufferIndex = 0;
                    }
                }
            }
        }

        public IEnumerable<ArraySegment<byte>> FrameData(ArraySegment<byte> data)
        {
            var length = data.Count;

            yield return new ArraySegment<byte>(
                new[] { (byte)length, (byte)(length >> 8), (byte)(length >> 16), (byte)(length >> 24) });
            yield return data;
        }

        public void RegisterMessageArrivedCallback(Action<ArraySegment<byte>> handler)
        {
            _receivedHandler = handler ?? throw new ArgumentNullException(nameof(handler));
        }
    }
}