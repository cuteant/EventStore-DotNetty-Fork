using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventStore.ClientAPI.Exceptions;
using EventStore.ClientAPI.Messages;
using EventStore.ClientAPI.SystemData;
using EventStore.Core.Messages;

namespace EventStore.ClientAPI.ClientOperations
{
    internal class TransactionalWriteOperation : OperationBase<object, TcpClientMessageDto.TransactionWriteCompleted>
    {
        private readonly bool _requireMaster;
        private readonly long _transactionId;
        private readonly IEnumerable<EventData> _events;

        public TransactionalWriteOperation(TaskCompletionSource<object> source,
                                           bool requireMaster, long transactionId, IEnumerable<EventData> events,
                                           UserCredentials userCredentials)
                : base(source, TcpCommand.TransactionWrite, TcpCommand.TransactionWriteCompleted, userCredentials)
        {
            _requireMaster = requireMaster;
            _transactionId = transactionId;
            _events = events;
        }

        protected override object CreateRequestDto()
        {
            var dtos = _events.Select(x => new TcpClientMessageDto.NewEvent(x.EventId.ToByteArray(), x.Type, x.IsJson ? 1 : 0, 0, x.Data, x.Metadata)).ToArray();
            return new TcpClientMessageDto.TransactionWrite(_transactionId, dtos, _requireMaster);
        }

        protected override InspectionResult InspectResponse(TcpClientMessageDto.TransactionWriteCompleted response)
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
                case OperationResult.AccessDenied:
                    Fail(new AccessDeniedException("Write access denied."));
                    return new InspectionResult(InspectionDecision.EndOperation, "AccessDenied");
                default:
                    throw new Exception(string.Format("Unexpected OperationResult: {0}.", response.Result));
            }
        }

        protected override object TransformResponse(TcpClientMessageDto.TransactionWriteCompleted response)
        {
            return null;
        }

        public override string ToString()
        {
            return string.Format("TransactionId: {0}", _transactionId);
        }
    }
}
