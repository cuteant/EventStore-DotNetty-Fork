using System.Threading.Tasks;
using EventStore.ClientAPI.Internal;
using EventStore.ClientAPI.SystemData;
using EventStore.Core.Messages;
using EventStore.Transport.Tcp.Messages;

namespace EventStore.ClientAPI.ClientOperations
{
    internal class StartTransactionOperation : OperationBase<EventStoreTransaction, TcpClientMessageDto.TransactionStartCompleted>
    {
        private readonly bool _requireMaster;
        private readonly string _stream;
        private readonly long _expectedVersion;
        private readonly IEventStoreTransactionConnection _parentConnection;

        public StartTransactionOperation(TaskCompletionSource<EventStoreTransaction> source,
                                         bool requireMaster, string stream, long expectedVersion, IEventStoreTransactionConnection parentConnection,
                                         UserCredentials userCredentials)
            : base(source, TcpCommand.TransactionStart, TcpCommand.TransactionStartCompleted, userCredentials)
        {
            _requireMaster = requireMaster;
            _stream = stream;
            _expectedVersion = expectedVersion;
            _parentConnection = parentConnection;
        }

        protected override object CreateRequestDto()
        {
            return new TcpClientMessageDto.TransactionStart(_stream, _expectedVersion, _requireMaster);
        }

        protected override InspectionResult InspectResponse(TcpClientMessageDto.TransactionStartCompleted response)
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
                    Fail(CoreThrowHelper.GetWrongExpectedVersionException_StartTransactionFailed(_stream, _expectedVersion));
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

        protected override EventStoreTransaction TransformResponse(TcpClientMessageDto.TransactionStartCompleted response)
        {
            return new EventStoreTransaction(response.TransactionId, UserCredentials, _parentConnection);
        }

        public override string ToString()
        {
            return string.Format("Stream: {0}, ExpectedVersion: {1}", _stream, _expectedVersion);
        }
    }
}
