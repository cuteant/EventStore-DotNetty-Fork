using System;

namespace EventStore.Transport.Tcp
{
    public interface IIConnectionEventListener
    {
        void Notify(in InboundConnection connection);
    }
}
