using System;
using System.Threading.Tasks;
using EventStore.ClientAPI.SystemData;
using EventStore.ClientAPI.Transport.Tcp;

namespace EventStore.ClientAPI.ClientOperations
{
  internal interface ISubscriptionOperation
  {
    void DropSubscription(SubscriptionDropReason reason, Exception exc, TcpPackageConnection connection = null);
    void ConnectionClosed();
    Task<InspectionResult> InspectPackageAsync(TcpPackage package);
    bool Subscribe(Guid correlationId, TcpPackageConnection connection);
  }
}