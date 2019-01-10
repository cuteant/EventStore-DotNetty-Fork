using System;
using System.Threading.Tasks;
using EventStore.ClientAPI.Exceptions;
using EventStore.ClientAPI.SystemData;
using EventStore.Core.Messages;
using EventStore.Transport.Tcp.Messages;

namespace EventStore.ClientAPI.ClientOperations
{
    internal class CommitTransactionOperation : OperationBase<WriteResult, TcpClientMessageDto.TransactionCommitCompleted>
    {
        private readonly bool _requireMaster;
        private readonly long _transactionId;

        public CommitTransactionOperation(TaskCompletionSource<WriteResult> source,
                                          bool requireMaster, long transactionId, UserCredentials userCredentials)
            : base(source, TcpCommand.TransactionCommit, TcpCommand.TransactionCommitCompleted, userCredentials)
        {
            _requireMaster = requireMaster;
            _transactionId = transactionId;
        }

        protected override object CreateRequestDto()
        {
            return new TcpClientMessageDto.TransactionCommit(_transactionId, _requireMaster);
        }

        protected override InspectionResult InspectResponse(TcpClientMessageDto.TransactionCommitCompleted response)
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
                    var err = string.Format("Commit transaction failed due to WrongExpectedVersion. TransactionID: {0}.", _transactionId);
                    Fail(new WrongExpectedVersionException(err));
                    return new InspectionResult(InspectionDecision.EndOperation, "WrongExpectedVersion");
                case OperationResult.StreamDeleted:
                    Fail(new StreamDeletedException());
                    return new InspectionResult(InspectionDecision.EndOperation, "StreamDeleted");
                case OperationResult.InvalidTransaction:
                    Fail(new InvalidTransactionException());
                    return new InspectionResult(InspectionDecision.EndOperation, "InvalidTransaction");
                case OperationResult.AccessDenied:
                    Fail(new AccessDeniedException("Write access denied."));
                    return new InspectionResult(InspectionDecision.EndOperation, "AccessDenied");
                default:
                    throw new Exception(string.Format("Unexpected OperationResult: {0}.", response.Result));
            }
        }

        protected override WriteResult TransformResponse(TcpClientMessageDto.TransactionCommitCompleted response)
        {
            return new WriteResult(response.LastEventNumber, new Position(response.PreparePosition ?? -1, response.CommitPosition ?? -1));
        }

        public override string ToString()
        {
            return string.Format("TransactionId: {0}", _transactionId);
        }
    }
}
