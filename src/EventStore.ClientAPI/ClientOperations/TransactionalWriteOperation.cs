using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CuteAnt;
using EventStore.ClientAPI.SystemData;
using EventStore.Core.Messages;
using EventStore.Transport.Tcp.Messages;

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
            TcpClientMessageDto.NewEvent[] dtos;
            switch (_events)
            {
                case IList<EventData> evts:
                    var evtCount = evts.Count;
                    if (evtCount == 0) { dtos = EmptyArray<TcpClientMessageDto.NewEvent>.Instance; break; }
                    dtos = new TcpClientMessageDto.NewEvent[evtCount];
                    for (var idx = 0; idx < evtCount; idx++)
                    {
                        var x = evts[idx];
                        dtos[idx] = new TcpClientMessageDto.NewEvent(x.EventId.ToByteArray(), x.Type, x.IsJson ? 1 : 0, 0, x.Data, x.Metadata);
                    }
                    break;
                case null:
                    dtos = EmptyArray<TcpClientMessageDto.NewEvent>.Instance;
                    break;
                default:
                    dtos = Convert(_events);
                    break;
            }
            return new TcpClientMessageDto.TransactionWrite(_transactionId, dtos, _requireMaster);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static TcpClientMessageDto.NewEvent[] Convert(IEnumerable<EventData> events)
        {
            var list = new List<TcpClientMessageDto.NewEvent>(16);
            foreach (var x in events)
            {
                list.Add(new TcpClientMessageDto.NewEvent(x.EventId.ToByteArray(), x.Type, x.IsJson ? 1 : 0, 0, x.Data, x.Metadata));
            }
            return list.ToArray();
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
                    Fail(CoreThrowHelper.GetAccessDeniedException(ExceptionResource.Write_access_denied));
                    return new InspectionResult(InspectionDecision.EndOperation, "AccessDenied");
                default:
                    CoreThrowHelper.ThrowException_UnexpectedOperationResult(response.Result); return null;
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
