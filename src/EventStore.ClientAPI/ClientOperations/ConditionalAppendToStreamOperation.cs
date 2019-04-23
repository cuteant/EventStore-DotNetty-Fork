using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CuteAnt;
using DotNetty.Common;
using EventStore.ClientAPI.SystemData;
using EventStore.Core.Messages;
using EventStore.Transport.Tcp.Messages;
using Microsoft.Extensions.Logging;

namespace EventStore.ClientAPI.ClientOperations
{
    internal class ConditionalAppendToStreamOperation : OperationBase<ConditionalWriteResult, TcpClientMessageDto.WriteEventsCompleted>
    {
        private readonly bool _requireMaster;
        private readonly string _stream;
        private readonly long _expectedVersion;
        private readonly EventData _event;
        private readonly IEnumerable<EventData> _events;

        private bool _wasCommitTimeout;

        public ConditionalAppendToStreamOperation(TaskCompletionSource<ConditionalWriteResult> source,
                                                       bool requireMaster,
                                                       string stream,
                                                       long expectedVersion,
                                                       EventData evt,
                                                       UserCredentials userCredentials)
          : this(source, requireMaster, stream, expectedVersion, userCredentials)
        {
            _event = evt;
        }

        public ConditionalAppendToStreamOperation(TaskCompletionSource<ConditionalWriteResult> source,
                                                       bool requireMaster,
                                                       string stream,
                                                       long expectedVersion,
                                                       IEnumerable<EventData> events,
                                                       UserCredentials userCredentials)
          : this(source, requireMaster, stream, expectedVersion, userCredentials)
        {
            _events = events;
        }

        private ConditionalAppendToStreamOperation(TaskCompletionSource<ConditionalWriteResult> source,
                                                        bool requireMaster,
                                                        string stream,
                                                        long expectedVersion,
                                                        UserCredentials userCredentials)
          : base(source, TcpCommand.WriteEvents, TcpCommand.WriteEventsCompleted, userCredentials)
        {
            _requireMaster = requireMaster;
            _stream = stream;
            _expectedVersion = expectedVersion;
        }

        protected override object CreateRequestDto()
        {
            TcpClientMessageDto.NewEvent[] dtos;
            if (_event != null)
            {
                dtos = new[] { new TcpClientMessageDto.NewEvent(_event.EventId.ToByteArray(), _event.Type, _event.IsJson ? 1 : 0, 0, _event.Data, _event.Metadata) };
            }
            else
            {
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
            }
            return new TcpClientMessageDto.WriteEvents(_stream, _expectedVersion, dtos, _requireMaster);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static TcpClientMessageDto.NewEvent[] Convert(IEnumerable<EventData> events)
        {
            var list = ThreadLocalList<TcpClientMessageDto.NewEvent>.NewInstance(16);
            try
            {
                foreach (var x in events)
                {
                    list.Add(new TcpClientMessageDto.NewEvent(x.EventId.ToByteArray(), x.Type, x.IsJson ? 1 : 0, 0, x.Data, x.Metadata));
                }
                return list.ToArray();
            }
            finally
            {
                list.Return();
            }
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
                    Succeed();
                    return new InspectionResult(InspectionDecision.EndOperation, "ExpectedVersionMismatch");
                case OperationResult.StreamDeleted:
                    Succeed();
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