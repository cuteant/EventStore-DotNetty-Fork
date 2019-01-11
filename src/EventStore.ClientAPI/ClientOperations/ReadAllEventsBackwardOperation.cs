using System.Threading.Tasks;
using EventStore.ClientAPI.Messages;
using EventStore.ClientAPI.SystemData;
using EventStore.Transport.Tcp.Messages;

namespace EventStore.ClientAPI.ClientOperations
{
    internal class ReadAllEventsBackwardOperation : OperationBase<AllEventsSlice, TcpClientMessageDto.ReadAllEventsCompleted>
    {
        private readonly Position _position;
        private readonly int _maxCount;
        private readonly bool _resolveLinkTos;
        private readonly bool _requireMaster;

        public ReadAllEventsBackwardOperation(TaskCompletionSource<AllEventsSlice> source,
                                              Position position, int maxCount, bool resolveLinkTos, bool requireMaster,
                                              UserCredentials userCredentials)
            : base(source, TcpCommand.ReadAllEventsBackward, TcpCommand.ReadAllEventsBackwardCompleted, userCredentials)
        {
            _position = position;
            _maxCount = maxCount;
            _resolveLinkTos = resolveLinkTos;
            _requireMaster = requireMaster;
        }

        protected override object CreateRequestDto()
        {
            return new TcpClientMessageDto.ReadAllEvents(_position.CommitPosition, _position.PreparePosition, _maxCount,
                                                   _resolveLinkTos, _requireMaster);
        }

        protected override InspectionResult InspectResponse(TcpClientMessageDto.ReadAllEventsCompleted response)
        {
            switch (response.Result)
            {
                case TcpClientMessageDto.ReadAllEventsCompleted.ReadAllResult.Success:
                    Succeed();
                    return new InspectionResult(InspectionDecision.EndOperation, "Success");
                case TcpClientMessageDto.ReadAllEventsCompleted.ReadAllResult.Error:
                    Fail(CoreThrowHelper.GetServerErrorException(response));
                    return new InspectionResult(InspectionDecision.EndOperation, "Error");
                case TcpClientMessageDto.ReadAllEventsCompleted.ReadAllResult.AccessDenied:
                    Fail(CoreThrowHelper.GetAccessDeniedException(ExceptionResource.Read_access_denied_for_all));
                    return new InspectionResult(InspectionDecision.EndOperation, "AccessDenied");
                default:
                    CoreThrowHelper.ThrowException_UnexpectedReadAllResult(response.Result); return null;
            }
        }

        protected override AllEventsSlice TransformResponse(TcpClientMessageDto.ReadAllEventsCompleted response)
        {
            return new AllEventsSlice(ReadDirection.Backward,
                                      new Position(response.CommitPosition, response.PreparePosition),
                                      new Position(response.NextCommitPosition, response.NextPreparePosition),
                                      response.Events.ToRawResolvedEvents());
        }

        public override string ToString()
        {
            return string.Format("Position: {0}, MaxCount: {1}, ResolveLinkTos: {2}, RequireMaster: {3}",
                                 _position, _maxCount,  _resolveLinkTos, _requireMaster);
        }
    }
}