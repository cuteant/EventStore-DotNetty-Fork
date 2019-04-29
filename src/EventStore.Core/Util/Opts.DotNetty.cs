namespace EventStore.Core.Util
{
    partial class Opts
    {
        /*
         * OPTIONS GROUPS
         */
        public const string DotNettyGroup = "DotNetty Options";

        public const string EnableLibuvDescr = "Whether to use Libuv-Transport";
        public const bool EnableLibuvDefault = true;

        public const string DnsUseIpv6Descr = "If set to true, we will use IPV6 addresses upon DNS resolution for host names. Otherwise, we will use IPV4.";
        public const bool DnsUseIpv6Default = false;

        public const string EnforceIpFamilyDescr = "If set to true, we will enforce usage of IPV4 or IPV6 addresses upon DNS resolution for host names.";
        public const bool EnforceIpFamilyDefault = false;

        public const string ConnectTimeoutDescr = "Sets the connectTimeoutMillis of all outbound connections, i.e. how long a connect may take until it is timed out";
        public const string ConnectTimeoutDefault = "15 s";

        public const string EnableBufferPoolingDescr = "Toggles buffer pooling on and off inside DotNetty.";
        public const bool EnableBufferPoolingDefault = true;

        public const string WriteBufferHighWaterMarkDescr = "Sets the high water mark for the in and outbound sockets, set to 0b for platform default.";
        public const string WriteBufferHighWaterMarkDefault = "0b";

        public const string WriteBufferLowWaterMarkDescr = "Sets the low water mark for the in and outbound sockets, set to 0b for platform default.";
        public const string WriteBufferLowWaterMarkDefault = "0b";

        public const string SendBufferSizeDescr = "Sets the send buffer size of the Sockets, set to 0b for platform default.";
        public const string SendBufferSizeDefault = "256K";

        public const string ReceiveBufferSizeDescr = "Sets the receive buffer size of the Sockets, set to 0b for platform default.";
        public const string ReceiveBufferSizeDefault = "256K";

        public const string MaxFrameSizeDescr = "Maximum message size the transport will accept, but at least 32000 bytes.";
        public const string MaxFrameSizeDefault = "10M";

        public const string BacklogDescr = "Sets the size of the connection backlog";
        public const int BacklogDefault = 4096;

        public const string TcpNoDelayDescr = "Enables the TCP_NODELAY flag, i.e. disables Nagleâ€™s algorithm";
        public const bool TcpNoDelayDefault = true;

        public const string TcpKeepAliveDescr = "Enables TCP Keepalive, subject to the O/S kernelâ€™s configuration";
        public const bool TcpKeepAliveDefault = true;

        public const string TcpLingerDescr = "TcpLinger";
        public const int TcpLingerDefault = 0;

        public const string TcpReuseAddrDescr = "Enables SO_REUSEADDR, which determines when an ActorSystem can open the specified listen port (the meaning differs between *nix and Windows)";
        public const string TcpReuseAddrDefault = "on";

        public const string TcpReusePortDescr = "Enables SO_REUSEPORT";
        public const bool TcpReusePortDefault = true;

        public const string ServerSocketWorkerPoolSizeMinDescr = "Min number of threads to cap factor-based number to";
        public const int ServerSocketWorkerPoolSizeMinDefault = 2;

        public const string ServerSocketWorkerPoolSizeFactorDescr = "The pool size factor is used to determine thread pool size using the following formula: ceil(available processors * factor).";
        public const double ServerSocketWorkerPoolSizeFactorDefault = 1.0d;

        public const string ServerSocketWorkerPoolSizeMaxDescr = "Max number of threads to cap factor-based number to";
        public const int ServerSocketWorkerPoolSizeMaxDefault = 2;

        public const string ClientSocketWorkerPoolSizeMinDescr = "Min number of threads to cap factor-based number to";
        public const int ClientSocketWorkerPoolSizeMinDefault = 2;

        public const string ClientSocketWorkerPoolSizeFactorDescr = "The pool size factor is used to determine thread pool size using the following formula: ceil(available processors * factor).";
        public const double ClientSocketWorkerPoolSizeFactorDefault = 1.0d;

        public const string ClientSocketWorkerPoolSizeMaxDescr = "Max number of threads to cap factor-based number to";
        public const int ClientSocketWorkerPoolSizeMaxDefault = 2;




        public const string DirectBufferPreferredDescr = "DirectBufferPreferred";
        public const bool DirectBufferPreferredDefault = true;

        public const string CheckBufferAccessibleDescr = "CheckBufferAccessible";
        public const bool CheckBufferAccessibleDefault = true;

        public const string CheckBufferBoundsDescr = "CheckBufferBounds";
        public const bool CheckBufferBoundsDefault = true;

        public const string AllocatorHeapArenasDescr = "AllocatorHeapArenas";
        public const string AllocatorHeapArenasDefault = "0";

        public const string AllocatorDirectArenasDescr = "AllocatorDirectArenas";
        public const string AllocatorDirectArenasDefault = "0";

        public const string AllocatorPageSizeDescr = "AllocatorPageSize";
        public const string AllocatorPageSizeDefault = "8K";

        public const string AllocatorMaxOrderDescr = "AllocatorMaxOrder";
        public const string AllocatorMaxOrderDefault = "11";

        public const string AllocatorTinyCacheSizeDescr = "AllocatorTinyCacheSize";
        public const string AllocatorTinyCacheSizeDefault = "512";

        public const string AllocatorSmallCacheSizeDescr = "AllocatorSmallCacheSize";
        public const string AllocatorSmallCacheSizeDefault = "256";

        public const string AllocatorNormalCacheSizeDescr = "AllocatorNormalCacheSize";
        public const string AllocatorNormalCacheSizeDefault = "64";

        public const string AllocatorCacheBufferMaxCapacityDescr = "AllocatorCacheBufferMaxCapacity";
        public const string AllocatorCacheBufferMaxCapacityDefault = "32K";

        public const string AllocatorCacheTrimIntervalDescr = "AllocatorCacheTrimInterval";
        public const string AllocatorCacheTrimIntervalDefault = "8K";
    }
}
