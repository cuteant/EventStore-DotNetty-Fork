using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using DotNetty.Codecs;
using DotNetty.Common;
using DotNetty.Transport.Channels;
using Microsoft.Extensions.Logging;

namespace EventStore.Transport.Tcp
{
    public abstract class DotNettyTransport
    {
        internal readonly ConcurrentHashSet<IChannel> ConnectionGroup;

        protected readonly ILogger Logger;

        protected readonly DotNettyTransportSettings Settings;

        public DotNettyTransport(DotNettyTransportSettings settings)
        {
            if (settings is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.settings); }
            Logger = TraceLogger.GetLogger(this.GetType());
            ConnectionGroup = new ConcurrentHashSet<IChannel>(ChannelComparer.Default);

            ResourceLeakDetector.Level = ResourceLeakDetector.DetectionLevel.Disabled;
            Settings = settings;
        }

        public long MaximumPayloadBytes => Settings.MaxFrameSize;

        public abstract Task<bool> Shutdown();

        protected void SetInitialChannelPipeline(IChannel channel)
        {
            var pipeline = channel.Pipeline;

            pipeline.AddLast("FrameDecoder", new LengthFieldBasedFrameDecoder2((int)MaximumPayloadBytes, 0, 4, 0, 4, true));
            pipeline.AddLast("FrameEncoder", new LengthFieldPrepender2(4, 0, false));
        }

        protected async Task<IPEndPoint> DnsToIPEndpoint(DnsEndPoint dns)
        {
            var addressFamily = Settings.DnsUseIpv6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork;
            var endpoint = await ResolveNameAsync(dns, addressFamily).ConfigureAwait(false);
            return endpoint;
        }

        protected static async Task<IPEndPoint> ResolveNameAsync(DnsEndPoint address)
        {
            var resolved = await Dns.GetHostEntryAsync(address.Host).ConfigureAwait(false);
            //NOTE: for some reason while Helios takes first element from resolved address list
            // on the DotNetty side we need to take the last one in order to be compatible
            return new IPEndPoint(resolved.AddressList[resolved.AddressList.Length - 1], address.Port);
        }

        protected static async Task<IPEndPoint> ResolveNameAsync(DnsEndPoint address, AddressFamily addressFamily)
        {
            var resolved = await Dns.GetHostEntryAsync(address.Host).ConfigureAwait(false);
            var found = resolved.AddressList.LastOrDefault(a => a.AddressFamily == addressFamily);
            if (found is null)
            {
                throw new KeyNotFoundException($"Couldn't resolve IP endpoint from provided DNS name '{address}' with address family of '{addressFamily}'");
            }

            return new IPEndPoint(found, address.Port);
        }
    }
}
