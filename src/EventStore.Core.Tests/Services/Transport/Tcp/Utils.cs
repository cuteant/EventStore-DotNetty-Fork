using System;
using EventStore.Transport.Tcp;

namespace EventStore.Core.Tests.Services.Transport.Tcp
{
    public static class Utils
    {
        public static DotNettyTransportSettings Create()
        {
            return new DotNettyTransportSettings(
                enableLibuv: true,
                connectTimeout: TimeSpan.FromSeconds(5),
                serverSocketWorkerPoolSize: 2,
                clientSocketWorkerPoolSize: 2,
                maxFrameSize: 10 * 1024 * 1024,
                dnsUseIpv6: false,
                tcpReuseAddr: true,
                tcpReusePort: true,
                tcpKeepAlive: true,
                tcpNoDelay: true,
                tcpLinger: 0,
                backlog: 200,
                enforceIpFamily: false,
                receiveBufferSize: 256 * 1024,
                sendBufferSize: 256 * 1024,
                writeBufferHighWaterMark: null,
                writeBufferLowWaterMark: null,
                enableBufferPooling: true);
        }
    }
}
