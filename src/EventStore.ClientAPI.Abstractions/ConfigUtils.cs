using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;

namespace EventStore.ClientAPI
{
    internal static class ConfigUtils
    {
        private static readonly Regex TimeSpanRegex;
        private static readonly ByteSize[] ByteSizes;
        private static readonly char[] Digits;

        static ConfigUtils()
        {
            TimeSpanRegex =
                new Regex(@"^(?<value>([0-9]+(\.[0-9]+)?))\s*(?<unit>(nanoseconds|nanosecond|nanos|nano|ns|microseconds|microsecond|micros|micro|us|milliseconds|millisecond|millis|milli|ms|seconds|second|s|minutes|minute|m|hours|hour|h|days|day|d))$",
                    RegexOptions.Compiled);
            Digits = "0123456789".ToCharArray();
            ByteSizes = new ByteSize[]
            {
                new ByteSize { Factor = 1024L * 1024L * 1024L * 1024L * 1024 * 1024L, Suffixes = new string[] { "E", "e", "Ei", "EiB", "exbibyte", "exbibytes" } },
                new ByteSize { Factor = 1000L * 1000L * 1000L * 1000L * 1000L * 1000L, Suffixes = new string[] { "EB", "exabyte", "exabytes" } },
                new ByteSize { Factor = 1024L * 1024L * 1024L * 1024L * 1024L, Suffixes = new string[] { "P", "p", "Pi", "PiB", "pebibyte", "pebibytes" } },
                new ByteSize { Factor = 1000L * 1000L * 1000L * 1000L * 1000L, Suffixes = new string[] { "PB", "petabyte", "petabytes" } },
                new ByteSize { Factor = 1024L * 1024L * 1024L * 1024L, Suffixes = new string[] { "T", "t", "Ti", "TiB", "tebibyte", "tebibytes" } },
                new ByteSize { Factor = 1000L * 1000L * 1000L * 1000L, Suffixes = new string[] { "TB", "terabyte", "terabytes" } },
                new ByteSize { Factor = 1024L * 1024L * 1024L, Suffixes = new string[] { "G", "g", "Gi", "GiB", "gibibyte", "gibibytes" } },
                new ByteSize { Factor = 1000L * 1000L * 1000L, Suffixes = new string[] { "GB", "gigabyte", "gigabytes" } },
                new ByteSize { Factor = 1024L * 1024L, Suffixes = new string[] { "M", "m", "Mi", "MiB", "mebibyte", "mebibytes" } },
                new ByteSize { Factor = 1000L * 1000L, Suffixes = new string[] { "MB", "megabyte", "megabytes" } },
                new ByteSize { Factor = 1024L, Suffixes = new string[] { "K", "k", "Ki", "KiB", "kibibyte", "kibibytes" } },
                new ByteSize { Factor = 1000L, Suffixes = new string[] { "KB", "kilobyte", "kilobytes" } },
                new ByteSize { Factor = 1, Suffixes = new string[] { "b", "B", "byte", "bytes" } }
            };
        }

        #region -- ToNullableBool --

        public static bool? ToNullableBool(string str)
        {
            if (string.IsNullOrWhiteSpace(str)) { return null; }

            switch (str.ToLowerInvariant())
            {
                case "1":
                case "t":
                case "on":
                case "true":
                    return true;

                case "0":
                case "f":
                case "false":
                case "off":
                default:
                    return false;
            }
        }

        #endregion

        #region -- ToNullableTimeSpan --

        public static TimeSpan? ToNullableTimeSpan(string res, bool allowInfinite = true)
        {
            if (string.IsNullOrWhiteSpace(res)) { return null; }

            var match = TimeSpanRegex.Match(res);
            if (match.Success)
            {
                var u = match.Groups["unit"].Value;
                var v = ParsePositiveValue(match.Groups["value"].Value);

                switch (u)
                {
                    case "nanoseconds":
                    case "nanosecond":
                    case "nanos":
                    case "nano":
                    case "ns":
                        return TimeSpan.FromTicks((long)Math.Round(TimeSpan.TicksPerMillisecond * v / 1000000.0));
                    case "microseconds":
                    case "microsecond":
                    case "micros":
                    case "micro":
                        return TimeSpan.FromTicks((long)Math.Round(TimeSpan.TicksPerMillisecond * v / 1000.0));
                    case "milliseconds":
                    case "millisecond":
                    case "millis":
                    case "milli":
                    case "ms":
                        return TimeSpan.FromMilliseconds(v);
                    case "seconds":
                    case "second":
                    case "s":
                        return TimeSpan.FromSeconds(v);
                    case "minutes":
                    case "minute":
                    case "m":
                        return TimeSpan.FromMinutes(v);
                    case "hours":
                    case "hour":
                    case "h":
                        return TimeSpan.FromHours(v);
                    case "days":
                    case "day":
                    case "d":
                        return TimeSpan.FromDays(v);
                }
            }

            if (allowInfinite && string.Equals("infinite", res, StringComparison.OrdinalIgnoreCase))  //Not in Hocon spec
            {
                return Timeout.InfiniteTimeSpan;
            }

            return TimeSpan.FromMilliseconds(ParsePositiveValue(res));
        }

        private static double ParsePositiveValue(string v)
        {
            var value = double.Parse(v, NumberFormatInfo.InvariantInfo);
            if (value < 0)
            {
                throw new FormatException($"Expected a positive value instead of {value}");
            }
            return value;
        }

        #endregion

        #region -- ToNullableInt --

        public static int? ToNullableInt(string res)
        {
            var lv = ToNullableInt64(res);
            return lv.HasValue && lv.Value > 0L ? (int?)lv.Value : null;
        }

        #endregion

        #region -- ToNullableInt64 --

        public struct ByteSize
        {
            public long Factor { get; set; }
            public string[] Suffixes { get; set; }
        }

        public static long? ToNullableInt64(string res)
        {
            if (string.IsNullOrWhiteSpace(res)) { return null; }

            res = res.Trim();
            var index = res.LastIndexOfAny(Digits);
            if (index == -1 || index + 1 >= res.Length) { return long.Parse(res); }

            var value = res.Substring(0, index + 1);
            var unit = res.Substring(index + 1).Trim();

            for (var byteSizeIndex = 0; byteSizeIndex < ByteSizes.Length; byteSizeIndex++)
            {
                var byteSize = ByteSizes[byteSizeIndex];
                for (var suffixIndex = 0; suffixIndex < byteSize.Suffixes.Length; suffixIndex++)
                {
                    var suffix = byteSize.Suffixes[suffixIndex];
                    if (string.Equals(unit, suffix, StringComparison.Ordinal))
                    {
                        return byteSize.Factor * long.Parse(value);
                    }
                }
            }

            throw new FormatException($"{unit} is not a valid long value suffix");
        }

        #endregion
    }
}
