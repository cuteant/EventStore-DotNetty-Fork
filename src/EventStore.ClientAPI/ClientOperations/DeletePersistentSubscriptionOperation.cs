using System;
using System.Globalization;
using System.Threading.Tasks;
using EventStore.ClientAPI.Exceptions;
using EventStore.ClientAPI.Internal;
using EventStore.ClientAPI.Messages;
using EventStore.ClientAPI.SystemData;

namespace EventStore.ClientAPI.ClientOperations
{
  internal class DeletePersistentSubscriptionOperation : OperationBase<PersistentSubscriptionDeleteResult, ClientMessage.DeletePersistentSubscriptionCompleted>
  {
    private readonly string _stream;
    private readonly string _groupName;

    public DeletePersistentSubscriptionOperation(TaskCompletionSource<PersistentSubscriptionDeleteResult> source,
      string stream, string groupName, UserCredentials userCredentials)
      : base(source, TcpCommand.DeletePersistentSubscription, TcpCommand.DeletePersistentSubscriptionCompleted, userCredentials)
    {
      _stream = stream;
      _groupName = groupName;
    }

    protected override object CreateRequestDto()
    {
      return new ClientMessage.DeletePersistentSubscription(_groupName, _stream);
    }

    protected override InspectionResult InspectResponse(ClientMessage.DeletePersistentSubscriptionCompleted response)
    {
      switch (response.Result)
      {
        case ClientMessage.DeletePersistentSubscriptionCompleted.DeletePersistentSubscriptionResult.Success:
          Succeed();
          return new InspectionResult(InspectionDecision.EndOperation, "Success");
        case ClientMessage.DeletePersistentSubscriptionCompleted.DeletePersistentSubscriptionResult.Fail:
          Fail(new InvalidOperationException($"Subscription group {_groupName} on stream {_stream} failed '{response.Reason}'"));
          return new InspectionResult(InspectionDecision.EndOperation, "Fail");
        case ClientMessage.DeletePersistentSubscriptionCompleted.DeletePersistentSubscriptionResult.AccessDenied:
          Fail(new AccessDeniedException($"Write access denied for stream '{_stream}'."));
          return new InspectionResult(InspectionDecision.EndOperation, "AccessDenied");
        case ClientMessage.DeletePersistentSubscriptionCompleted.DeletePersistentSubscriptionResult.DoesNotExist:
          Fail(new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, Consts.PersistentSubscriptionDoesNotExist, _groupName, _stream)));
          return new InspectionResult(InspectionDecision.EndOperation, "DoesNotExist");
        default:
          throw new Exception($"Unexpected OperationResult: {response.Result}.");
      }
    }

    protected override PersistentSubscriptionDeleteResult TransformResponse(ClientMessage.DeletePersistentSubscriptionCompleted response)
    {
      return new PersistentSubscriptionDeleteResult(PersistentSubscriptionDeleteStatus.Success);
    }


    public override string ToString()
    {
      return $"Stream: {_stream}, Group Name: {_groupName}";
    }
  }
}