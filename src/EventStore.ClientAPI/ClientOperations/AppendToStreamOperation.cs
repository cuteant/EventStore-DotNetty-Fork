using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventStore.ClientAPI.SystemData;
using EventStore.Core.Messages;
using EventStore.Transport.Tcp.Messages;
using Microsoft.Extensions.Logging;

namespace EventStore.ClientAPI.ClientOperations
{
    internal class AppendToStreamOperation : OperationBase<WriteResult, TcpClientMessageDto.WriteEventsCompleted>
    {
        private readonly bool _requireMaster;
        private readonly string _stream;
        private readonly long _expectedVersion;
        private readonly IEnumerable<EventData> _events;

        private bool _wasCommitTimeout;

        public AppendToStreamOperation(TaskCompletionSource<WriteResult> source,
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
                        if (Log.IsDebugLevelEnabled()) Log.IdempotentWriteSucceededFor(this);
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
                    Fail(CoreThrowHelper.GetWrongExpectedVersionException_AppendFailed(_stream, _expectedVersion, response.CurrentVersion));
                    return new InspectionResult(InspectionDecision.EndOperation, "WrongExpectedVersion");
                case OperationResult.StreamDeleted:
                    Fail(CoreThrowHelper.GetStreamDeletedException(_stream));
                    return new InspectionResult(InspectionDecision.EndOperation, "StreamDeleted");
                case OperationResult.InvalidTransaction:
                    Fail(CoreThrowHelper.GetInvalidTransactionException());
                    return new InspectionResult(InspectionDecision.EndOperation, "InvalidTransaction");
                case OperationResult.AccessDenied:
                    Fail(CoreThrowHelper.GetAccessDeniedException(ExceptionResource.Write_access_denied_for_stream, _stream));
                    return new InspectionResult(InspectionDecision.EndOperation, "AccessDenied");
                default:
                    CoreThrowHelper.ThrowException_UnexpectedOperationResult(response.Result); return null;
            }
        }

        protected override WriteResult TransformResponse(TcpClientMessageDto.WriteEventsCompleted response)
        {
            return new WriteResult(response.LastEventNumber, new Position(response.PreparePosition ?? -1, response.CommitPosition ?? -1));
        }

        public override string ToString()
        {
            return string.Format("Stream: {0}, ExpectedVersion: {1}", _stream, _expectedVersion);
        }
    }
}