using System;
using EventStore.Transport.Tcp.Messages;

namespace EventStore.Transport.Tcp
{
    /// <summary>An interface that needs to be implemented by a user of an <see cref="DotNettyConnection"/> in
    /// order to listen to association events</summary>
    public interface ITcpPackageListener
    {
        /// <summary>Notify the listener about an <see cref="TcpPackage"/>.</summary>
        /// <param name="package">The <see cref="TcpPackage"/> to notify the listener about</param>
        void Notify(TcpPackage package);

        void HandleBadRequest(in Disassociated disassociated);
    }
}
