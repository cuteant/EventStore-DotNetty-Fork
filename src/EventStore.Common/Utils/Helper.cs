using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace EventStore.Common.Utils
{
    public static class Helper
    {
        public static readonly UTF8Encoding UTF8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        public static void EatException(Action action)
        {
            try
            {
                action();
            }
            catch (Exception)
            {
            }
        }

        public static T EatException<T>(Func<T> action, T defaultValue = default(T))
        {
            try
            {
                return action();
            }
            catch (Exception)
            {
                return defaultValue;
            }
        }

        public static string GetDefaultLogsDir()
        {
            return Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "es-logs");
        }

        public static string FormatBinaryDump(byte[] logBulk)
        {
            return FormatBinaryDump(new ArraySegment<byte>(logBulk ?? Empty.ByteArray));
        }

        public static string FormatBinaryDump(ArraySegment<byte> logBulk)
        {
            if (logBulk.Count == 0)
                return "--- NO DATA ---";

            var sb = new StringBuilder();
            int cur = 0;
            int len = logBulk.Count;
            for (int row = 0, rows = (logBulk.Count + 15) / 16; row < rows; ++row)
            {
                sb.AppendFormat("{0:000000}:", row * 16);
                for (int i = 0; i < 16; ++i, ++cur)
                {
                    if (cur >= len)
                        sb.Append("   ");
                    else
                        sb.AppendFormat(" {0:X2}", logBulk.Array[logBulk.Offset + cur]);
                }
                sb.Append("  | ");
                cur -= 16;
                for (int i = 0; i < 16; ++i, ++cur)
                {
                    if (cur < len)
                    {
                        var b = (char)logBulk.Array[logBulk.Offset + cur];
                        sb.Append(char.IsControl(b) ? '.' : b);
                    }
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }

        /// <summary>
        /// Returns <see langword="true"/> if <paramref name="value"/> is between
        /// <paramref name="lowerBound"/> and <paramref name="upperBound"/>, inclusive.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsInRangeInclusive(byte value, byte lowerBound, byte upperBound)
            => ((byte)(value - lowerBound) <= (byte)(upperBound - lowerBound));

        /// <summary>
        /// Returns <see langword="true"/> if <paramref name="value"/> is between
        /// <paramref name="lowerBound"/> and <see cref="int.MaxValue"/>, inclusive.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsInRangeInclusive(int value, int lowerBound)
            => (uint)(value - lowerBound) <= (uint)(int.MaxValue - lowerBound);

        /// <summary>
        /// Returns <see langword="true"/> if <paramref name="value"/> is between
        /// <paramref name="lowerBound"/> and <paramref name="upperBound"/>, inclusive.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsInRangeInclusive(int value, int lowerBound, int upperBound)
            => (uint)(value - lowerBound) <= (uint)(upperBound - lowerBound);

        /// <summary>
        /// Returns <see langword="true"/> if <paramref name="value"/> is between
        /// <paramref name="lowerBound"/> and <see cref="uint.MaxValue"/>, inclusive.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsInRangeInclusive(uint value, uint lowerBound)
            => (value - lowerBound) <= (uint.MaxValue - lowerBound);

        /// <summary>
        /// Returns <see langword="true"/> if <paramref name="value"/> is between
        /// <paramref name="lowerBound"/> and <paramref name="upperBound"/>, inclusive.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsInRangeInclusive(uint value, uint lowerBound, uint upperBound)
            => (value - lowerBound) <= (upperBound - lowerBound);

        /// <summary>
        /// Returns <see langword="true"/> if <paramref name="value"/> is between
        /// <paramref name="lowerBound"/> and <see cref="long.MaxValue"/>, inclusive.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsInRangeInclusive(long value, long lowerBound)
            => (ulong)(value - lowerBound) <= (ulong)(long.MaxValue - lowerBound);

        /// <summary>
        /// Returns <see langword="true"/> if <paramref name="value"/> is between
        /// <paramref name="lowerBound"/> and <paramref name="upperBound"/>, inclusive.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsInRangeInclusive(long value, long lowerBound, long upperBound)
            => (ulong)(value - lowerBound) <= (ulong)(upperBound - lowerBound);

        /// <summary>
        /// Returns <see langword="true"/> if <paramref name="value"/> is in the range [0..9].
        /// Otherwise, returns <see langword="false"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsDigit(char value) => (uint)(value - '0') <= '9' - '0';

        /// <summary>
        /// Returns <see langword="true"/> if <paramref name="value"/> is in the range [A..F].
        /// Otherwise, returns <see langword="false"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsUpper(char value) => (uint)(value - 'A') <= 'F' - 'A';
    }
}
