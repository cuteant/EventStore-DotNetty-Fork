using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using EventStore.Common.Utils;
using EventStore.Core.Util;
using EventStore.Transport.Tcp;

namespace EventStore.Core
{
    partial class VNodeBuilder
    {
        private bool _enableLibuv = Opts.EnableLibuvDefault;
        public VNodeBuilder DisableLibuv()
        {
            _enableLibuv = false;
            return this;
        }

        private bool _dnsUseIpv6 = Opts.DnsUseIpv6Default;
        public VNodeBuilder WithDnsUseIpv6()
        {
            _dnsUseIpv6 = true;
            return this;
        }

        private bool _enforceIpFamily = Opts.EnforceIpFamilyDefault;
        public VNodeBuilder WithEnforceIpFamily()
        {
            _enforceIpFamily = true;
            return this;
        }

        private TimeSpan _connectTimeout = TimeSpan.FromSeconds(15);
        public VNodeBuilder WithConnectTimeout(string connectTimeout)
        {
            _connectTimeout = GetTimeSpan(connectTimeout);
            return this;
        }

        private bool _enableBufferPooling = Opts.EnableBufferPoolingDefault;
        public VNodeBuilder DisableBufferPooling()
        {
            _enableBufferPooling = false;
            return this;
        }

        private int? _writeBufferHighWaterMark;
        public VNodeBuilder WithWriteBufferHighWaterMark(string writeBufferHighWaterMark)
        {
            _writeBufferHighWaterMark = ToNullableInt(GetByteSize(writeBufferHighWaterMark));
            return this;
        }

        private int? _writeBufferLowWaterMark;
        public VNodeBuilder WithWriteBufferLowWaterMark(string writeBufferLowWaterMark)
        {
            _writeBufferLowWaterMark = ToNullableInt(GetByteSize(writeBufferLowWaterMark));
            return this;
        }

        private int _sendBufferSize = 256 * 1024 * 1024;
        public VNodeBuilder WithSendBufferSize(string sendBufferSize)
        {
            var v = ToNullableInt(GetByteSize(sendBufferSize));
            if (v.HasValue) { _sendBufferSize = v.Value; }
            return this;
        }

        private int _receiveBufferSize = 256 * 1024 * 1024;
        public VNodeBuilder WithReceiveBufferSize(string receiveBufferSize)
        {
            var v = ToNullableInt(GetByteSize(receiveBufferSize));
            if (v.HasValue) { _receiveBufferSize = v.Value; }
            return this;
        }

        private int _maxFrameSize = 10 * 1024 * 1024;
        public VNodeBuilder WithMaxFrameSize(string maxFrameSize)
        {
            var v = ToNullableInt(GetByteSize(maxFrameSize));
            if (v.HasValue) { _maxFrameSize = v.Value; }
            return this;
        }

        private int _backlog = Opts.BacklogDefault;
        public VNodeBuilder WithBacklog(int backlog)
        {
            if (backlog > 0) { _backlog = backlog; }
            return this;
        }

        private bool _tcpNoDelay = Opts.TcpNoDelayDefault;
        public VNodeBuilder DisableTcpNoDelay()
        {
            _tcpNoDelay = false;
            return this;
        }

        private bool _tcpKeepAlive = Opts.TcpKeepAliveDefault;
        public VNodeBuilder DisableTcpKeepAlive()
        {
            _tcpKeepAlive = false;
            return this;
        }

        private int _tcpLinger = 0;
        public VNodeBuilder WithTcpLinger(int tcpLinger)
        {
            if (tcpLinger >= 0) { _tcpLinger = tcpLinger; }
            return this;
        }

        private bool _tcpReuseAddr = true;
        public VNodeBuilder WithTcpReuseAddr(string tcpReuseAddr)
        {
            if (!string.IsNullOrWhiteSpace(tcpReuseAddr))
            {
                _tcpReuseAddr = ResolveTcpReuseAddrOption(tcpReuseAddr);
            }
            return this;
        }

        private bool _tcpReusePort = Opts.TcpReusePortDefault;
        public VNodeBuilder DisableTcpReusePort()
        {
            _tcpReusePort = false;
            return this;
        }

        private int _serverSocketWorkerPoolSizeMin = Opts.ServerSocketWorkerPoolSizeMinDefault;
        public VNodeBuilder WithServerSocketWorkerPoolSizeMin(int serverSocketWorkerPoolSizeMin)
        {
            if (serverSocketWorkerPoolSizeMin > 0) { _serverSocketWorkerPoolSizeMin = serverSocketWorkerPoolSizeMin; }
            return this;
        }

        private double _serverSocketWorkerPoolSizeFactor = Opts.ServerSocketWorkerPoolSizeFactorDefault;
        public VNodeBuilder WithServerSocketWorkerPoolSizeFactor(double serverSocketWorkerPoolSizeFactor)
        {
            if (serverSocketWorkerPoolSizeFactor > 0d) { _serverSocketWorkerPoolSizeFactor = serverSocketWorkerPoolSizeFactor; }
            return this;
        }

        private int _serverSocketWorkerPoolSizeMax = Opts.ServerSocketWorkerPoolSizeMaxDefault;
        public VNodeBuilder WithServerSocketWorkerPoolSizeMax(int serverSocketWorkerPoolSizeMax)
        {
            if (serverSocketWorkerPoolSizeMax > 0) { _serverSocketWorkerPoolSizeMax = serverSocketWorkerPoolSizeMax; }
            return this;
        }

        private int _clientSocketWorkerPoolSizeMin = Opts.ClientSocketWorkerPoolSizeMinDefault;
        public VNodeBuilder WithClientSocketWorkerPoolSizeMin(int clientSocketWorkerPoolSizeMin)
        {
            if (clientSocketWorkerPoolSizeMin > 0) { _clientSocketWorkerPoolSizeMin = clientSocketWorkerPoolSizeMin; }
            return this;
        }

        private double _clientSocketWorkerPoolSizeFactor = Opts.ClientSocketWorkerPoolSizeFactorDefault;
        public VNodeBuilder WithClientSocketWorkerPoolSizeFactor(double clientSocketWorkerPoolSizeFactor)
        {
            if (clientSocketWorkerPoolSizeFactor > 0d) { _clientSocketWorkerPoolSizeFactor = clientSocketWorkerPoolSizeFactor; }
            return this;
        }

        private int _clientSocketWorkerPoolSizeMax = Opts.ClientSocketWorkerPoolSizeMaxDefault;
        public VNodeBuilder WithClientSocketWorkerPoolSizeMax(int clientSocketWorkerPoolSizeMax)
        {
            if (clientSocketWorkerPoolSizeMax > 0) { _clientSocketWorkerPoolSizeMax = clientSocketWorkerPoolSizeMax; }
            return this;
        }

        #region ** CreateDotNettyTransportSettings **

        private DotNettyTransportSettings CreateTransportSettings()
        {
            return new DotNettyTransportSettings(
                enableLibuv: _enableLibuv,
                connectTimeout: _connectTimeout,
                serverSocketWorkerPoolSize: ComputeServerWorkerPoolSize(),
                clientSocketWorkerPoolSize: ComputeClientWorkerPoolSize(),
                maxFrameSize: _maxFrameSize,
                dnsUseIpv6: _dnsUseIpv6,
                tcpReuseAddr: _tcpReuseAddr,
                tcpReusePort: _tcpReusePort,
                tcpKeepAlive: _tcpKeepAlive,
                tcpNoDelay: _tcpNoDelay,
                tcpLinger: _tcpLinger,
                backlog: _backlog,
                enforceIpFamily: Runtime.IsMono || _enforceIpFamily,
                receiveBufferSize: _receiveBufferSize,
                sendBufferSize: _sendBufferSize,
                writeBufferHighWaterMark: _writeBufferHighWaterMark,
                writeBufferLowWaterMark: _writeBufferLowWaterMark,
                enableBufferPooling: _enableBufferPooling);
        }

        #endregion

        #region ** ResolveTcpReuseAddrOption **

        internal static bool ResolveTcpReuseAddrOption(string tcpReuseAddr)
        {
            switch (tcpReuseAddr.ToLowerInvariant())
            {
                case "off-for-windows" when Runtime.IsWindows:
                    return false;
                case "off-for-windows":
                    return true;
                case "on":
                case "true":
                    return true;
                case "false":
                case "off":
                default:
                    return false;
            }
        }

        #endregion

        #region ** ToNullableInt **

        public static int? ToNullableInt(long? value) => value.HasValue && value.Value > 0L ? (int?)value.Value : null;

        #endregion

        #region ** ScaledPoolSize **

        private int ComputeServerWorkerPoolSize()
        {
            return ScaledPoolSize(
                floor: _serverSocketWorkerPoolSizeMin,
                scalar: _serverSocketWorkerPoolSizeFactor,
                ceiling: _serverSocketWorkerPoolSizeMax);
        }

        private int ComputeClientWorkerPoolSize()
        {
            return ScaledPoolSize(
                floor: _clientSocketWorkerPoolSizeMin,
                scalar: _clientSocketWorkerPoolSizeFactor,
                ceiling: _clientSocketWorkerPoolSizeMax);
        }

        private static int ScaledPoolSize(int floor, double scalar, int ceiling)
        {
            return Math.Min(Math.Max((int)(Environment.ProcessorCount * scalar), floor), ceiling);
        }

        #endregion

        #region ** GetTimeSpan **

        private static readonly Regex TimeSpanRegex =
            new Regex(@"^(?<value>([0-9]+(\.[0-9]+)?))\s*(?<unit>(nanoseconds|nanosecond|nanos|nano|ns|microseconds|microsecond|micros|micro|us|milliseconds|millisecond|millis|milli|ms|seconds|second|s|minutes|minute|m|hours|hour|h|days|day|d))$",
                RegexOptions.Compiled);

        private static TimeSpan GetTimeSpan(string res, bool allowInfinite = true)
        {
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

        #region ** GetByteSize **

        private struct ByteSize
        {
            public long Factor { get; set; }
            public string[] Suffixes { get; set; }
        }

        private static ByteSize[] ByteSizes { get; } =
            new ByteSize[]
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
                new ByteSize { Factor = 1000L, Suffixes = new string[] { "kB", "KB", "kilobyte", "kilobytes" } },
                new ByteSize { Factor = 1, Suffixes = new string[] { "b", "B", "byte", "bytes" } }
            };

        private static char[] Digits { get; } = "0123456789".ToCharArray();

        public static long? GetByteSize(string res)
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

            throw new FormatException($"{unit} is not a valid byte size suffix");
        }

        #endregion
    }
}
