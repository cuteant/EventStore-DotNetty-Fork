﻿using System.Threading.Tasks;
using EventStore.ClientAPI.Messages;
using EventStore.ClientAPI.SystemData;
using EventStore.Transport.Tcp.Messages;

namespace EventStore.ClientAPI.ClientOperations
{
    internal class ReadEventOperation : ReadEventOperationBase<EventReadResult<object>>
    {
        private readonly IEventAdapter _eventAdapter;

        public ReadEventOperation(TaskCompletionSource<EventReadResult<object>> source,
          string stream, long eventNumber, bool resolveLinkTo, bool requireMaster,
          UserCredentials userCredentials, IEventAdapter eventAdapter)
          : base(source, stream, eventNumber, resolveLinkTo, requireMaster, userCredentials)
        {
            if (eventAdapter is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.eventAdapter); }
            _eventAdapter = eventAdapter;
        }

        protected override EventReadResult<object> TransformResponse(TcpClientMessageDto.ReadEventCompleted response)
        {
            var readStatus = Convert(response.Result);
            return new EventReadResult<object>(readStatus, _stream, _eventNumber, response.Event.ToResolvedEvent(readStatus, _eventAdapter));
        }
    }

    internal class ReadEventOperation<T> : ReadEventOperationBase<EventReadResult<T>>
    {
        private readonly IEventAdapter _eventAdapter;

        public ReadEventOperation(TaskCompletionSource<EventReadResult<T>> source,
          string stream, long eventNumber, bool resolveLinkTo, bool requireMaster,
          UserCredentials userCredentials, IEventAdapter eventAdapter)
          : base(source, stream, eventNumber, resolveLinkTo, requireMaster, userCredentials)
        {
            if (eventAdapter is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.eventAdapter); }
            _eventAdapter = eventAdapter;
        }

        protected override EventReadResult<T> TransformResponse(TcpClientMessageDto.ReadEventCompleted response)
        {
            var readStatus = Convert(response.Result);
            return new EventReadResult<T>(readStatus, _stream, _eventNumber, response.Event.ToResolvedEvent<T>(readStatus, _eventAdapter));
        }
    }

    internal class ReadRawEventOperation : ReadEventOperationBase<EventReadResult>
    {
        public ReadRawEventOperation(TaskCompletionSource<EventReadResult> source,
          string stream, long eventNumber, bool resolveLinkTo, bool requireMaster, UserCredentials userCredentials)
          : base(source, stream, eventNumber, resolveLinkTo, requireMaster, userCredentials)
        {
        }

        protected override EventReadResult TransformResponse(TcpClientMessageDto.ReadEventCompleted response)
        {
            var readStatus = Convert(response.Result);
            return new EventReadResult(readStatus, _stream, _eventNumber, response.Event.ToRawResolvedEvent(readStatus));
        }
    }

    internal abstract class ReadEventOperationBase<TResult> : OperationBase<TResult, TcpClientMessageDto.ReadEventCompleted>
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
            return new TcpClientMessageDto.ReadEvent(_stream, _eventNumber, _resolveLinkTo, _requireMaster);
        }

        protected override InspectionResult InspectResponse(TcpClientMessageDto.ReadEventCompleted response)
        {
            switch (response.Result)
            {
                case TcpClientMessageDto.ReadEventCompleted.ReadEventResult.Success:
                    Succeed();
                    return new InspectionResult(InspectionDecision.EndOperation, "Success");
                case TcpClientMessageDto.ReadEventCompleted.ReadEventResult.NotFound:
                    Succeed();
                    return new InspectionResult(InspectionDecision.EndOperation, "NotFound");
                case TcpClientMessageDto.ReadEventCompleted.ReadEventResult.NoStream:
                    Succeed();
                    return new InspectionResult(InspectionDecision.EndOperation, "NoStream");
                case TcpClientMessageDto.ReadEventCompleted.ReadEventResult.StreamDeleted:
                    Succeed();
                    return new InspectionResult(InspectionDecision.EndOperation, "StreamDeleted");
                case TcpClientMessageDto.ReadEventCompleted.ReadEventResult.Error:
                    Fail(CoreThrowHelper.GetServerErrorException(response));
                    return new InspectionResult(InspectionDecision.EndOperation, "Error");
                case TcpClientMessageDto.ReadEventCompleted.ReadEventResult.AccessDenied:
                    Fail(CoreThrowHelper.GetAccessDeniedException(ExceptionResource.Read_access_denied_for_stream, _stream));
                    return new InspectionResult(InspectionDecision.EndOperation, "AccessDenied");
                default:
                    CoreThrowHelper.ThrowException_UnexpectedReadEventResult(response.Result); return null;
            }
        }

        internal static EventReadStatus Convert(TcpClientMessageDto.ReadEventCompleted.ReadEventResult result)
        {
            switch (result)
            {
                case TcpClientMessageDto.ReadEventCompleted.ReadEventResult.Success:
                    return EventReadStatus.Success;
                case TcpClientMessageDto.ReadEventCompleted.ReadEventResult.NotFound:
                    return EventReadStatus.NotFound;
                case TcpClientMessageDto.ReadEventCompleted.ReadEventResult.NoStream:
                    return EventReadStatus.NoStream;
                case TcpClientMessageDto.ReadEventCompleted.ReadEventResult.StreamDeleted:
                    return EventReadStatus.StreamDeleted;
                default:
                    CoreThrowHelper.ThrowException_UnexpectedReadEventResult(result); return default;
            }
        }

        public override string ToString()
        {
            return $"Stream: {_stream}, EventNumber: {_eventNumber}, ResolveLinkTo: {_resolveLinkTo}, RequireMaster: {_requireMaster}";
        }
    }
}