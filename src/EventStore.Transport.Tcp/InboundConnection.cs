namespace EventStore.Transport.Tcp
{
    public readonly struct InboundConnection
    {
        public readonly DotNettyConnection Connection;

        public InboundConnection(DotNettyConnection connection) => Connection = connection;
    }
}
