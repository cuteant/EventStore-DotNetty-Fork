using System.Threading.Tasks;
using EventStore.ClientAPI.Messages;
using EventStore.ClientAPI.SystemData;
using EventStore.Transport.Tcp.Messages;

namespace EventStore.ClientAPI.ClientOperations
{
    internal class ReadStreamEventsBackwardOperation : ReadStreamEventsBackwardOperationBase<StreamEventsSlice<object>>
    {
        public ReadStreamEventsBackwardOperation(TaskCompletionSource<StreamEventsSlice<object>> source,
                                                      string stream, long fromEventNumber, int maxCount, bool resolveLinkTos,
                                                      bool requireMaster, UserCredentials userCredentials)
          : base(source, stream, fromEventNumber, maxCount, resolveLinkTos, requireMaster, userCredentials)
        {
        }

        protected override StreamEventsSlice<object> TransformResponse(TcpClientMessageDto.ReadStreamEventsCompleted response)
        {
            return new StreamEventsSlice<object>(StatusCode.Convert(response.Result),
                                                 _stream,
                                                 _fromEventNumber,
                                                 ReadDirection.Backward,
                                                 response.Events.ToResolvedEvents(),
                                                 response.NextEventNumber,
                                                 response.LastEventNumber,
                                                 response.IsEndOfStream);
        }
    }

    internal class ReadStreamEventsBackwardOperation<T> : ReadStreamEventsBackwardOperationBase<StreamEventsSlice<T>> where T : class
    {
        public ReadStreamEventsBackwardOperation(TaskCompletionSource<StreamEventsSlice<T>> source,
                                                      string stream, long fromEventNumber, int maxCount, bool resolveLinkTos,
                                                      bool requireMaster, UserCredentials userCredentials)
          : base(source, stream, fromEventNumber, maxCount, resolveLinkTos, requireMaster, userCredentials)
        {
        }

        protected override StreamEventsSlice<T> TransformResponse(TcpClientMessageDto.ReadStreamEventsCompleted response)
        {
            return new StreamEventsSlice<T>(StatusCode.Convert(response.Result),
                                            _stream,
                                            _fromEventNumber,
                                            ReadDirection.Backward,
                                            response.Events.ToResolvedEvents<T>(),
                                            response.NextEventNumber,
                                            response.LastEventNumber,
                                            response.IsEndOfStream);
        }
    }

    internal class ReadRawStreamEventsBackwardOperation : ReadStreamEventsBackwardOperationBase<StreamEventsSlice>
    {
        public ReadRawStreamEventsBackwardOperation(TaskCompletionSource<StreamEventsSlice> source,
                                                         string stream, long fromEventNumber, int maxCount, bool resolveLinkTos,
                                                         bool requireMaster, UserCredentials userCredentials)
          : base(source, stream, fromEventNumber, maxCount, resolveLinkTos, requireMaster, userCredentials)
        {
        }

        protected override StreamEventsSlice TransformResponse(TcpClientMessageDto.ReadStreamEventsCompleted response)
        {
            return new StreamEventsSlice(StatusCode.Convert(response.Result),
                                         _stream,
                                         _fromEventNumber,
                                         ReadDirection.Backward,
                                         response.Events.ToRawResolvedEvents(),
                                         response.NextEventNumber,
                                         response.LastEventNumber,
                                         response.IsEndOfStream);
        }
    }

    internal abstract class ReadStreamEventsBackwardOperationBase<TResult> : OperationBase<TResult, TcpClientMessageDto.ReadStreamEventsCompleted>
    {
        internal readonly string _stream;
        internal readonly long _fromEventNumber;
        private readonly int _maxCount;
        private readonly bool _resolveLinkTos;
        private readonly bool _requireMaster;

        public ReadStreamEventsBackwardOperationBase(TaskCompletionSource<TResult> source,
                                                          string stream, long fromEventNumber, int maxCount, bool resolveLinkTos,
                                                          bool requireMaster, UserCredentials userCredentials)
          : base(source, TcpCommand.ReadStreamEventsBackward, TcpCommand.ReadStreamEventsBackwardCompleted, userCredentials)
        {
            _stream = stream;
            _fromEventNumber = fromEventNumber;
            _maxCount = maxCount;
            _resolveLinkTos = resolveLinkTos;
            _requireMaster = requireMaster;
        }

        protected override object CreateRequestDto()
        {
            return new TcpClientMessageDto.ReadStreamEvents(_stream, _fromEventNumber, _maxCount, _resolveLinkTos, _requireMaster);
        }

        protected override InspectionResult InspectResponse(TcpClientMessageDto.ReadStreamEventsCompleted response)
        {
            switch (response.Result)
            {
                case TcpClientMessageDto.ReadStreamEventsCompleted.ReadStreamResult.Success:
                    Succeed();
                    return new InspectionResult(InspectionDecision.EndOperation, "Success");
                case TcpClientMessageDto.ReadStreamEventsCompleted.ReadStreamResult.StreamDeleted:
                    Succeed();
                    return new InspectionResult(InspectionDecision.EndOperation, "StreamDeleted");
                case TcpClientMessageDto.ReadStreamEventsCompleted.ReadStreamResult.NoStream:
                    Succeed();
                    return new InspectionResult(InspectionDecision.EndOperation, "NoStream");
                case TcpClientMessageDto.ReadStreamEventsCompleted.ReadStreamResult.Error:
                    Fail(CoreThrowHelper.GetServerErrorException(response));
                    return new InspectionResult(InspectionDecision.EndOperation, "Error");
                case TcpClientMessageDto.ReadStreamEventsCompleted.ReadStreamResult.AccessDenied:
                    Fail(CoreThrowHelper.GetAccessDeniedException(ExceptionResource.Read_access_denied_for_stream, _stream));
                    return new InspectionResult(InspectionDecision.EndOperation, "AccessDenied");
                default:
                    CoreThrowHelper.ThrowException_UnexpectedReadStreamResult(response.Result); return null;
            }
        }

        public override string ToString()
        {
            return $"Stream: {_stream}, FromEventNumber: {_fromEventNumber}, MaxCount: {_maxCount}, ResolveLinkTos: {_resolveLinkTos}, RequireMaster: {_requireMaster}";
        }
    }
}