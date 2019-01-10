using System;
using System.Globalization;
using System.Threading.Tasks;
using EventStore.ClientAPI.Exceptions;
using EventStore.ClientAPI.Internal;
using EventStore.ClientAPI.SystemData;
using EventStore.Transport.Tcp.Messages;

namespace EventStore.ClientAPI.ClientOperations
{
  internal class DeletePersistentSubscriptionOperation : OperationBase<PersistentSubscriptionDeleteResult, TcpClientMessageDto.DeletePersistentSubscriptionCompleted>
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
      return new TcpClientMessageDto.DeletePersistentSubscription(_groupName, _stream);
    }

    protected override InspectionResult InspectResponse(TcpClientMessageDto.DeletePersistentSubscriptionCompleted response)
    {
      switch (response.Result)
      {
        case TcpClientMessageDto.DeletePersistentSubscriptionCompleted.DeletePersistentSubscriptionResult.Success:
          Succeed();
          return new InspectionResult(InspectionDecision.EndOperation, "Success");
        case TcpClientMessageDto.DeletePersistentSubscriptionCompleted.DeletePersistentSubscriptionResult.Fail:
          Fail(new InvalidOperationException($"Subscription group {_groupName} on stream {_stream} failed '{response.Reason}'"));
          return new InspectionResult(InspectionDecision.EndOperation, "Fail");
        case TcpClientMessageDto.DeletePersistentSubscriptionCompleted.DeletePersistentSubscriptionResult.AccessDenied:
          Fail(new AccessDeniedException($"Write access denied for stream '{_stream}'."));
          return new InspectionResult(InspectionDecision.EndOperation, "AccessDenied");
        case TcpClientMessageDto.DeletePersistentSubscriptionCompleted.DeletePersistentSubscriptionResult.DoesNotExist:
          Fail(new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, Consts.PersistentSubscriptionDoesNotExist, _groupName, _stream)));
          return new InspectionResult(InspectionDecision.EndOperation, "DoesNotExist");
        default:
          throw new Exception($"Unexpected OperationResult: {response.Result}.");
      }
    }

    protected override PersistentSubscriptionDeleteResult TransformResponse(TcpClientMessageDto.DeletePersistentSubscriptionCompleted response)
    {
      return new PersistentSubscriptionDeleteResult(PersistentSubscriptionDeleteStatus.Success);
    }


    public override string ToString()
    {
      return $"Stream: {_stream}, Group Name: {_groupName}";
    }
  }
}