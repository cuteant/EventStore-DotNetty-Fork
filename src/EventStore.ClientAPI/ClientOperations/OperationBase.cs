using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using EventStore.ClientAPI.Common.Utils;
using EventStore.ClientAPI.Exceptions;
using EventStore.ClientAPI.SystemData;
using EventStore.ClientAPI.Transport.Tcp;
using EventStore.Transport.Tcp.Messages;
using Microsoft.Extensions.Logging;

namespace EventStore.ClientAPI.ClientOperations
{
    internal abstract class OperationBase<TResult, TResponse> : IClientOperation
      where TResponse : class
    {
        private readonly TcpCommand _requestCommand;
        private readonly TcpCommand _responseCommand;
        protected readonly UserCredentials UserCredentials;

        protected readonly ILogger Log;
        private readonly TaskCompletionSource<TResult> _source;
        private TResponse _response;
        private int _completed;

        protected abstract object CreateRequestDto();
        protected abstract InspectionResult InspectResponse(TResponse response);
        protected abstract TResult TransformResponse(TResponse response);

        protected OperationBase(TaskCompletionSource<TResult> source,
          TcpCommand requestCommand, TcpCommand responseCommand, UserCredentials userCredentials)
        {
            if (source is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source); }

            Log = TraceLogger.GetLogger(this.GetType());
            _source = source;
            _requestCommand = requestCommand;
            _responseCommand = responseCommand;
            UserCredentials = userCredentials;
        }

        public TcpPackage CreateNetworkPackage(Guid correlationId)
        {
            return new TcpPackage(_requestCommand,
                                  UserCredentials is object ? TcpFlags.Authenticated : TcpFlags.None,
                                  correlationId,
                                  UserCredentials?.Username,
                                  UserCredentials?.Password,
                                  CreateRequestDto().Serialize());
        }

        public virtual InspectionResult InspectPackage(TcpPackage package)
        {
            try
            {
                if (package.Command == _responseCommand)
                {
                    _response = package.Data.Deserialize<TResponse>();
                    return InspectResponse(_response);
                }
                switch (package.Command)
                {
                    case TcpCommand.NotAuthenticated: return InspectNotAuthenticated(package);
                    case TcpCommand.BadRequest: return InspectBadRequest(package);
                    case TcpCommand.NotHandled: return InspectNotHandled(package);
                    default: return InspectUnexpectedCommand(package, _responseCommand);
                }
            }
            catch (Exception e)
            {
                return InspectError(e);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private InspectionResult InspectError(Exception e)
        {
            Fail(e);
            return new InspectionResult(InspectionDecision.EndOperation, $"Exception - {e.Message}");
        }

        protected void Succeed()
        {
            if (0u >= (uint)Interlocked.CompareExchange(ref _completed, 1, 0))
            {
                if (_response is object)
                    _source.SetResult(TransformResponse(_response));
                else
                    _source.SetException(CoreThrowHelper.GetNoResultException());
            }
        }

        public void Fail(Exception exception)
        {
            if (0u >= (uint)Interlocked.CompareExchange(ref _completed, 1, 0))
            {
                _source.SetException(exception);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public InspectionResult InspectNotAuthenticated(TcpPackage package)
        {
            Fail(CoreThrowHelper.GetNotAuthenticatedException(package));
            return new InspectionResult(InspectionDecision.EndOperation, "NotAuthenticated");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public InspectionResult InspectBadRequest(TcpPackage package)
        {
            string message = string.Empty; try { message = Helper.UTF8NoBom.GetString(package.Data); } catch { }
            Fail(new ServerErrorException(string.IsNullOrEmpty(message) ? "<no message>" : message));
            return new InspectionResult(InspectionDecision.EndOperation, $"BadRequest - {message}");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public InspectionResult InspectNotHandled(TcpPackage package)
        {
            var message = package.Data.Deserialize<TcpClientMessageDto.NotHandled>();
            switch (message.Reason)
            {
                case TcpClientMessageDto.NotHandled.NotHandledReason.NotReady:
                    return new InspectionResult(InspectionDecision.Retry, "NotHandled - NotReady");

                case TcpClientMessageDto.NotHandled.NotHandledReason.TooBusy:
                    return new InspectionResult(InspectionDecision.Retry, "NotHandled - TooBusy");

                case TcpClientMessageDto.NotHandled.NotHandledReason.NotMaster:
                    var masterInfo = message.AdditionalInfo.Deserialize<TcpClientMessageDto.NotHandled.MasterInfo>();
                    return new InspectionResult(InspectionDecision.Reconnect, "NotHandled - NotMaster",
                                                masterInfo.ExternalTcpEndPoint, masterInfo.ExternalSecureTcpEndPoint);

                case TcpClientMessageDto.NotHandled.NotHandledReason.IsReadOnly:
                    Log.Cannot_perform_operation_as_this_node_is_Read_Only();
                    return new InspectionResult(InspectionDecision.NotSupported, "This node is Read Only");

                default:
                    Log.UnknownNotHandledReason(message.Reason);
                    return new InspectionResult(InspectionDecision.Retry, "NotHandled - <unknown>");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public InspectionResult InspectUnexpectedCommand(TcpPackage package, TcpCommand expectedCommand)
        {
            if (package.Command == expectedCommand)
                CoreThrowHelper.ThrowArgumentException_CommandShouldnotbe(package.Command);

            Log.LogError("Unexpected TcpCommand received.");
            Log.LogError("Expected: {0}, Actual: {1}, Flags: {2}, CorrelationId: {3}", expectedCommand, package.Command, package.Flags, package.CorrelationId);
            Log.LogError("Operation ({0}): {1}", GetType().Name, this);
            Log.LogError("TcpPackage Data Dump:");
            Log.LogError(Helper.FormatBinaryDump(package.Data));

            Fail(CoreThrowHelper.GetCommandNotExpectedException(expectedCommand, package.Command));
            return new InspectionResult(InspectionDecision.EndOperation, $"Unexpected command - {package.Command.ToString()}");
        }
    }
}
