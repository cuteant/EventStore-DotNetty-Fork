using EventStore.Core.Util;
using EventStore.Rags;

namespace EventStore.ClusterNode
{
    partial class ClusterNodeOptions
    {
        [ArgDescription(Opts.EnableLibuvDescr, Opts.DotNettyGroup)]
        public bool EnableLibuv { get; set; }

        [ArgDescription(Opts.DnsUseIpv6Descr, Opts.DotNettyGroup)]
        public bool DnsUseIpv6 { get; set; }

        [ArgDescription(Opts.EnforceIpFamilyDescr, Opts.DotNettyGroup)]
        public bool EnforceIpFamily { get; set; }

        [ArgDescription(Opts.ConnectTimeoutDescr, Opts.DotNettyGroup)]
        public string ConnectTimeout { get; set; }

        [ArgDescription(Opts.EnableBufferPoolingDescr, Opts.DotNettyGroup)]
        public bool EnableBufferPooling { get; set; }

        [ArgDescription(Opts.WriteBufferHighWaterMarkDescr, Opts.DotNettyGroup)]
        public string WriteBufferHighWaterMark { get; set; }

        [ArgDescription(Opts.WriteBufferLowWaterMarkDescr, Opts.DotNettyGroup)]
        public string WriteBufferLowWaterMark { get; set; }

        [ArgDescription(Opts.SendBufferSizeDescr, Opts.DotNettyGroup)]
        public string SendBufferSize { get; set; }

        [ArgDescription(Opts.ReceiveBufferSizeDescr, Opts.DotNettyGroup)]
        public string ReceiveBufferSize { get; set; }

        [ArgDescription(Opts.MaxFrameSizeDescr, Opts.DotNettyGroup)]
        public string MaxFrameSize { get; set; }

        [ArgDescription(Opts.BacklogDescr, Opts.DotNettyGroup)]
        public int Backlog { get; set; }

        [ArgDescription(Opts.TcpNoDelayDescr, Opts.DotNettyGroup)]
        public bool TcpNoDelay { get; set; }

        [ArgDescription(Opts.TcpKeepAliveDescr, Opts.DotNettyGroup)]
        public bool TcpKeepAlive { get; set; }

        [ArgDescription(Opts.TcpLingerDescr, Opts.DotNettyGroup)]
        public int TcpLinger { get; set; }

        [ArgDescription(Opts.TcpReuseAddrDescr, Opts.DotNettyGroup)]
        public string TcpReuseAddr { get; set; }

        [ArgDescription(Opts.TcpReusePortDescr, Opts.DotNettyGroup)]
        public bool TcpReusePort { get; set; }

        [ArgDescription(Opts.ServerSocketWorkerPoolSizeMinDescr, Opts.DotNettyGroup)]
        public int ServerSocketWorkerPoolSizeMin { get; set; }

        [ArgDescription(Opts.ServerSocketWorkerPoolSizeFactorDescr, Opts.DotNettyGroup)]
        public double ServerSocketWorkerPoolSizeFactor { get; set; }

        [ArgDescription(Opts.ServerSocketWorkerPoolSizeMaxDescr, Opts.DotNettyGroup)]
        public int ServerSocketWorkerPoolSizeMax { get; set; }

        [ArgDescription(Opts.ClientSocketWorkerPoolSizeMinDescr, Opts.DotNettyGroup)]
        public int ClientSocketWorkerPoolSizeMin { get; set; }

        [ArgDescription(Opts.ClientSocketWorkerPoolSizeFactorDescr, Opts.DotNettyGroup)]
        public double ClientSocketWorkerPoolSizeFactor { get; set; }

        [ArgDescription(Opts.ClientSocketWorkerPoolSizeMaxDescr, Opts.DotNettyGroup)]
        public int ClientSocketWorkerPoolSizeMax { get; set; }
    }
}
