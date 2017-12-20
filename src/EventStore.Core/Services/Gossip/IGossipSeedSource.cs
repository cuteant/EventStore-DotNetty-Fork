using System;
using System.Net;
using System.Threading.Tasks;

namespace EventStore.Core.Services.Gossip
{
    public interface IGossipSeedSource
    {
        //IAsyncResult BeginGetHostEndpoints(AsyncCallback requestCallback, object state);
        //IPEndPoint[] EndGetHostEndpoints(IAsyncResult asyncResult);
        IPEndPoint[] GetHostEndpoints();
    }
}