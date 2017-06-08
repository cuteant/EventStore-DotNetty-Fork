using System;
using System.Threading.Tasks;
using EventStore.ClientAPI.Exceptions;
using EventStore.ClientAPI.Messages;
using EventStore.ClientAPI.SystemData;

namespace EventStore.ClientAPI.ClientOperations
{
  internal class ReadEventOperation : ReadEventOperationBase<EventReadResult<object>>
  {
    public ReadEventOperation(TaskCompletionSource<EventReadResult<object>> source,
      string stream, long eventNumber, bool resolveLinkTo, bool requireMaster, UserCredentials userCredentials)
      : base(source, stream, eventNumber, resolveLinkTo, requireMaster, userCredentials)
    {
    }

    protected override EventReadResult<object> TransformResponse(ClientMessage.ReadEventCompleted response)
    {
      var readStatus = Convert(response.Result);
      return new EventReadResult<object>(readStatus, _stream, _eventNumber, response.Event.ToResolvedEvent(readStatus));
    }
  }

  internal class ReadEventOperation<T> : ReadEventOperationBase<EventReadResult<T>> where T : class
  {
    public ReadEventOperation(TaskCompletionSource<EventReadResult<T>> source,
      string stream, long eventNumber, bool resolveLinkTo, bool requireMaster, UserCredentials userCredentials)
      : base(source, stream, eventNumber, resolveLinkTo, requireMaster, userCredentials)
    {
    }

    protected override EventReadResult<T> TransformResponse(ClientMessage.ReadEventCompleted response)
    {
      var readStatus = Convert(response.Result);
      return new EventReadResult<T>(readStatus, _stream, _eventNumber, response.Event.ToResolvedEvent<T>(readStatus));
    }
  }

  internal class RawReadEventOperation : ReadEventOperationBase<EventReadResult>
  {
    public RawReadEventOperation(TaskCompletionSource<EventReadResult> source,
      string stream, long eventNumber, bool resolveLinkTo, bool requireMaster, UserCredentials userCredentials)
      : base(source, stream, eventNumber, resolveLinkTo, requireMaster, userCredentials)
    {
    }

    protected override EventReadResult TransformResponse(ClientMessage.ReadEventCompleted response)
    {
      var readStatus = Convert(response.Result);
      return new EventReadResult(readStatus, _stream, _eventNumber, response.Event.ToRawResolvedEvent(readStatus));
    }
  }

  internal abstract class ReadEventOperationBase<TResult> : OperationBase<TResult, ClientMessage.ReadEventCompleted>
  {
    internal readonly string _stream;
    internal readonly long _eventNumber;
    private readonly bool _resolveLinkTo;
    private readonly bool _requireMaster;

    public ReadEventOperationBase(TaskCompletionSource<TResult> source,
      string stream, long eventNumber, bool resolveLinkTo, bool requireMaster, UserCredentials userCredentials)
      : base(source, TcpCommand.ReadEvent, TcpCommand.ReadEventCompleted, userCredentials)
    {
      _stream = stream;
      _eventNumber = eventNumber;
      _resolveLinkTo = resolveLinkTo;
      _requireMaster = requireMaster;
    }

    protected override object CreateRequestDto()
    {
      return new ClientMessage.ReadEvent(_stream, _eventNumber, _resolveLinkTo, _requireMaster);
    }

    protected override InspectionResult InspectResponse(ClientMessage.ReadEventCompleted response)
    {
      switch (response.Result)
      {
        case ClientMessage.ReadEventCompleted.ReadEventResult.Success:
          Succeed();
          return new InspectionResult(InspectionDecision.EndOperation, "Success");
        case ClientMessage.ReadEventCompleted.ReadEventResult.NotFound:
          Succeed();
          return new InspectionResult(InspectionDecision.EndOperation, "NotFound");
        case ClientMessage.ReadEventCompleted.ReadEventResult.NoStream:
          Succeed();
          return new InspectionResult(InspectionDecision.EndOperation, "NoStream");
        case ClientMessage.ReadEventCompleted.ReadEventResult.StreamDeleted:
          Succeed();
          return new InspectionResult(InspectionDecision.EndOperation, "StreamDeleted");
        case ClientMessage.ReadEventCompleted.ReadEventResult.Error:
          Fail(new ServerErrorException(string.IsNullOrEmpty(response.Error) ? "<no message>" : response.Error));
          return new InspectionResult(InspectionDecision.EndOperation, "Error");
        case ClientMessage.ReadEventCompleted.ReadEventResult.AccessDenied:
          Fail(new AccessDeniedException($"Read access denied for stream '{_stream}'."));
          return new InspectionResult(InspectionDecision.EndOperation, "AccessDenied");
        default:
          throw new Exception($"Unexpected ReadEventResult: {response.Result}.");
      }
    }

    internal static EventReadStatus Convert(ClientMessage.ReadEventCompleted.ReadEventResult result)
    {
      switch (result)
      {
        case ClientMessage.ReadEventCompleted.ReadEventResult.Success:
          return EventReadStatus.Success;
        case ClientMessage.ReadEventCompleted.ReadEventResult.NotFound:
          return EventReadStatus.NotFound;
        case ClientMessage.ReadEventCompleted.ReadEventResult.NoStream:
          return EventReadStatus.NoStream;
        case ClientMessage.ReadEventCompleted.ReadEventResult.StreamDeleted:
          return EventReadStatus.StreamDeleted;
        default:
          throw new Exception($"Unexpected ReadEventResult: {result}.");
      }
    }

    public override string ToString()
    {
      return $"Stream: {_stream}, EventNumber: {_eventNumber}, ResolveLinkTo: {_resolveLinkTo}, RequireMaster: {_requireMaster}";
    }
  }
}