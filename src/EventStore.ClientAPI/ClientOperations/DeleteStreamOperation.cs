using System;
using System.Threading.Tasks;
using EventStore.ClientAPI.Exceptions;
using EventStore.ClientAPI.Messages;
using EventStore.ClientAPI.SystemData;
using EventStore.Core.Messages;

namespace EventStore.ClientAPI.ClientOperations
{
    internal class DeleteStreamOperation : OperationBase<DeleteResult, TcpClientMessageDto.DeleteStreamCompleted>
    {
        private readonly bool _requireMaster;
        private readonly string _stream;
        private readonly long _expectedVersion;
        private readonly bool _hardDelete;

        public DeleteStreamOperation(TaskCompletionSource<DeleteResult> source,
                                     bool requireMaster, string stream, long expectedVersion, bool hardDelete,
                                     UserCredentials userCredentials)
            :base(source, TcpCommand.DeleteStream, TcpCommand.DeleteStreamCompleted, userCredentials)
        {
            _requireMaster = requireMaster;
            _stream = stream;
            _expectedVersion = expectedVersion;
            _hardDelete = hardDelete;
        }

        protected override object CreateRequestDto()
        {
            return new TcpClientMessageDto.DeleteStream(_stream, _expectedVersion, _requireMaster, _hardDelete);
        }

        protected override InspectionResult InspectResponse(TcpClientMessageDto.DeleteStreamCompleted response)
        {
            switch (response.Result)
            {
                case OperationResult.Success:
                    Succeed();
                    return new InspectionResult(InspectionDecision.EndOperation, "Success");
                case OperationResult.PrepareTimeout:
                    return new InspectionResult(InspectionDecision.Retry, "PrepareTimeout");
                case OperationResult.CommitTimeout:
                    return new InspectionResult(InspectionDecision.Retry, "CommitTimeout");
                case OperationResult.ForwardTimeout:
                    return new InspectionResult(InspectionDecision.Retry, "ForwardTimeout");
                case OperationResult.WrongExpectedVersion:
                    var err = string.Format("Delete stream failed due to WrongExpectedVersion. Stream: {0}, Expected version: {1}.", _stream, _expectedVersion);
                    Fail(new WrongExpectedVersionException(err));
                    return new InspectionResult(InspectionDecision.EndOperation, "WrongExpectedVersion");
                case OperationResult.StreamDeleted:
                    Fail(new StreamDeletedException(_stream));
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

        protected override DeleteResult TransformResponse(TcpClientMessageDto.DeleteStreamCompleted response)
        {
            return new DeleteResult(new Position(response.PreparePosition ?? -1, response.CommitPosition ?? -1));
        }

        public override string ToString()
        {
            return string.Format("Stream: {0}, ExpectedVersion: {1}.", _stream, _expectedVersion);
        }
    }
}