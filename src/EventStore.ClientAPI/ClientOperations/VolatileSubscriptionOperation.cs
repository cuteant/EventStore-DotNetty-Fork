using System;
using System.Threading.Tasks;
using EventStore.ClientAPI.Internal;
using EventStore.ClientAPI.Messages;
using EventStore.ClientAPI.SystemData;
using EventStore.ClientAPI.Transport.Tcp;

namespace EventStore.ClientAPI.ClientOperations
{
  internal class VolatileSubscriptionOperation : SubscriptionOperation<EventStoreSubscription>
  {
    public VolatileSubscriptionOperation(TaskCompletionSource<EventStoreSubscription> source,
      string streamId, SubscriptionSettings settings, UserCredentials userCredentials,
      Action<EventStoreSubscription, ResolvedEvent> eventAppeared,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped,
      Func<TcpPackageConnection> getConnection)
      : base(source, streamId, settings, userCredentials, eventAppeared, subscriptionDropped, getConnection)
    {
    }
    public VolatileSubscriptionOperation(TaskCompletionSource<EventStoreSubscription> source,
      string streamId, SubscriptionSettings settings, UserCredentials userCredentials,
      Func<EventStoreSubscription, ResolvedEvent, Task> eventAppearedAsync,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped,
      Func<TcpPackageConnection> getConnection)
      : base(source, streamId, settings, userCredentials, eventAppearedAsync, subscriptionDropped, getConnection)
    {
    }

    protected override TcpPackage CreateSubscriptionPackage()
    {
      var dto = new ClientMessage.SubscribeToStream(_streamId, _resolveLinkTos);
      return new TcpPackage(
          TcpCommand.SubscribeToStream, _userCredentials != null ? TcpFlags.Authenticated : TcpFlags.None,
          _correlationId, _userCredentials != null ? _userCredentials.Username : null,
          _userCredentials != null ? _userCredentials.Password : null, dto.Serialize());
    }

    protected override bool InspectPackage(TcpPackage package, out InspectionResult result)
    {
      if (package.Command == TcpCommand.SubscriptionConfirmation)
      {
        var dto = package.Data.Deserialize<ClientMessage.SubscriptionConfirmation>();
        ConfirmSubscription(dto.LastCommitPosition, dto.LastEventNumber);
        result = new InspectionResult(InspectionDecision.Subscribed, "SubscriptionConfirmation");
        return true;
      }
      if (package.Command == TcpCommand.StreamEventAppeared)
      {
        var dto = package.Data.Deserialize<ClientMessage.StreamEventAppeared>();
        EventAppeared(dto.Event.ToRawResolvedEvent());
        result = new InspectionResult(InspectionDecision.DoNothing, "StreamEventAppeared");
        return true;
      }
      result = null;
      return false;
    }

    protected override EventStoreSubscription CreateSubscriptionObject(long lastCommitPosition, long? lastEventNumber)
    {
      return new VolatileEventStoreSubscription(this, _streamId, lastCommitPosition, lastEventNumber);
    }
  }
}