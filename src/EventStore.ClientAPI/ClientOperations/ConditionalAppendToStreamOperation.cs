using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventStore.ClientAPI.Exceptions;
using EventStore.ClientAPI.Messages;
using EventStore.ClientAPI.SystemData;
using EventStore.Core.Messages;
using Microsoft.Extensions.Logging;

namespace EventStore.ClientAPI.ClientOperations
{
  internal class ConditionalAppendToStreamOperation : OperationBase<ConditionalWriteResult, TcpClientMessageDto.WriteEventsCompleted>
  {
    private readonly bool _requireMaster;
    private readonly string _stream;
    private readonly long _expectedVersion;
    private readonly IEnumerable<EventData> _events;

    private bool _wasCommitTimeout;

    public ConditionalAppendToStreamOperation(TaskCompletionSource<ConditionalWriteResult> source,
                                                   bool requireMaster,
                                                   string stream,
                                                   long expectedVersion,
                                                   IEnumerable<EventData> events,
                                                   UserCredentials userCredentials)
      : base(source, TcpCommand.WriteEvents, TcpCommand.WriteEventsCompleted, userCredentials)
    {
      _requireMaster = requireMaster;
      _stream = stream;
      _expectedVersion = expectedVersion;
      _events = events;
    }

    protected override object CreateRequestDto()
    {
      var dtos = _events.Select(x => new TcpClientMessageDto.NewEvent(x.EventId.ToByteArray(), x.Type, x.IsJson ? 1 : 0, 0, x.Data, x.Metadata)).ToArray();
      return new TcpClientMessageDto.WriteEvents(_stream, _expectedVersion, dtos, _requireMaster);
    }

    protected override InspectionResult InspectResponse(TcpClientMessageDto.WriteEventsCompleted response)
    {
      switch (response.Result)
      {
        case OperationResult.Success:
          if (_wasCommitTimeout)
          {
            if (Log.IsDebugLevelEnabled()) Log.LogDebug("IDEMPOTENT WRITE SUCCEEDED FOR {0}.", this);
          }
          Succeed();
          return new InspectionResult(InspectionDecision.EndOperation, "Success");
        case OperationResult.PrepareTimeout:
          return new InspectionResult(InspectionDecision.Retry, "PrepareTimeout");
        case OperationResult.ForwardTimeout:
          return new InspectionResult(InspectionDecision.Retry, "ForwardTimeout");
        case OperationResult.CommitTimeout:
          _wasCommitTimeout = true;
          return new InspectionResult(InspectionDecision.Retry, "CommitTimeout");
        case OperationResult.WrongExpectedVersion:
          Succeed();
          return new InspectionResult(InspectionDecision.EndOperation, "ExpectedVersionMismatch");
        case OperationResult.StreamDeleted:
          Succeed();
          return new InspectionResult(InspectionDecision.EndOperation, "StreamDeleted");
        case OperationResult.InvalidTransaction:
          Fail(new InvalidTransactionException());
          return new InspectionResult(InspectionDecision.EndOperation, "InvalidTransaction");
        case OperationResult.AccessDenied:
          Fail(new AccessDeniedException(string.Format("Write access denied for stream '{0}'.", _stream)));
          return new InspectionResult(InspectionDecision.EndOperation, "AccessDenied");
        default:
          throw new Exception(string.Format("Unexpected OperationResult: {0}.", response.Result));
      }
    }

    protected override ConditionalWriteResult TransformResponse(TcpClientMessageDto.WriteEventsCompleted response)
    {
      if (response.Result == OperationResult.WrongExpectedVersion)
      {
        return new ConditionalWriteResult(ConditionalWriteStatus.VersionMismatch);
      }
      if (response.Result == OperationResult.StreamDeleted)
      {
        return new ConditionalWriteResult(ConditionalWriteStatus.StreamDeleted);
      }
      return new ConditionalWriteResult(response.LastEventNumber, new Position(response.PreparePosition ?? -1, response.CommitPosition ?? -1));
    }

    public override string ToString()
    {
      return string.Format("Stream: {0}, ExpectedVersion: {1}", _stream, _expectedVersion);
    }
  }
}