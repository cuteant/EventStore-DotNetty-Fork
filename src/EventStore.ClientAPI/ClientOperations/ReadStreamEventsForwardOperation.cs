using System;
using System.Threading.Tasks;
using EventStore.ClientAPI.Exceptions;
using EventStore.ClientAPI.Messages;
using EventStore.ClientAPI.SystemData;

namespace EventStore.ClientAPI.ClientOperations
{
  internal class ReadStreamEventsForwardOperation : ReadStreamEventsForwardOperationBase<StreamEventsSlice<object>>
  {
    public ReadStreamEventsForwardOperation(TaskCompletionSource<StreamEventsSlice<object>> source,
                                                 string stream, long fromEventNumber, int maxCount, bool resolveLinkTos,
                                                 bool requireMaster, UserCredentials userCredentials)
      : base(source, stream, fromEventNumber, maxCount, resolveLinkTos, requireMaster, userCredentials)
    {
    }

    protected override StreamEventsSlice<object> TransformResponse(ClientMessage.ReadStreamEventsCompleted response)
    {
      return new StreamEventsSlice<object>(StatusCode.Convert(response.Result),
                                           _stream,
                                           _fromEventNumber,
                                           ReadDirection.Forward,
                                           response.Events.ToResolvedEvents(),
                                           response.NextEventNumber,
                                           response.LastEventNumber,
                                           response.IsEndOfStream);
    }
  }
  internal class ReadStreamEventsForwardOperation<T> : ReadStreamEventsForwardOperationBase<StreamEventsSlice<T>> where T : class
  {
    public ReadStreamEventsForwardOperation(TaskCompletionSource<StreamEventsSlice<T>> source,
                                                 string stream, long fromEventNumber, int maxCount, bool resolveLinkTos,
                                                 bool requireMaster, UserCredentials userCredentials)
      : base(source, stream, fromEventNumber, maxCount, resolveLinkTos, requireMaster, userCredentials)
    {
    }

    protected override StreamEventsSlice<T> TransformResponse(ClientMessage.ReadStreamEventsCompleted response)
    {
      return new StreamEventsSlice<T>(StatusCode.Convert(response.Result),
                                     _stream,
                                     _fromEventNumber,
                                     ReadDirection.Forward,
                                     response.Events.ToResolvedEvents<T>(),
                                     response.NextEventNumber,
                                     response.LastEventNumber,
                                     response.IsEndOfStream);
    }
  }

  internal class RawReadStreamEventsForwardOperation : ReadStreamEventsForwardOperationBase<StreamEventsSlice>
  {
    public RawReadStreamEventsForwardOperation(TaskCompletionSource<StreamEventsSlice> source,
                                                    string stream, long fromEventNumber, int maxCount, bool resolveLinkTos,
                                                    bool requireMaster, UserCredentials userCredentials)
      : base(source, stream, fromEventNumber, maxCount, resolveLinkTos, requireMaster, userCredentials)
    {
    }

    protected override StreamEventsSlice TransformResponse(ClientMessage.ReadStreamEventsCompleted response)
    {
      return new StreamEventsSlice(StatusCode.Convert(response.Result),
                                   _stream,
                                   _fromEventNumber,
                                   ReadDirection.Forward,
                                   response.Events.ToRawResolvedEvents(),
                                   response.NextEventNumber,
                                   response.LastEventNumber,
                                   response.IsEndOfStream);
    }
  }

  internal abstract class ReadStreamEventsForwardOperationBase<TResult> : OperationBase<TResult, ClientMessage.ReadStreamEventsCompleted>
  {
    internal readonly string _stream;
    internal readonly long _fromEventNumber;
    private readonly int _maxCount;
    private readonly bool _resolveLinkTos;
    private readonly bool _requireMaster;

    public ReadStreamEventsForwardOperationBase(TaskCompletionSource<TResult> source,
                                                     string stream, long fromEventNumber, int maxCount, bool resolveLinkTos,
                                                     bool requireMaster, UserCredentials userCredentials)
      : base(source, TcpCommand.ReadStreamEventsForward, TcpCommand.ReadStreamEventsForwardCompleted, userCredentials)
    {
      _stream = stream;
      _fromEventNumber = fromEventNumber;
      _maxCount = maxCount;
      _resolveLinkTos = resolveLinkTos;
      _requireMaster = requireMaster;
    }

    protected override object CreateRequestDto()
    {
      return new ClientMessage.ReadStreamEvents(_stream, _fromEventNumber, _maxCount, _resolveLinkTos, _requireMaster);
    }

    protected override InspectionResult InspectResponse(ClientMessage.ReadStreamEventsCompleted response)
    {
      switch (response.Result)
      {
        case ClientMessage.ReadStreamEventsCompleted.ReadStreamResult.Success:
          Succeed();
          return new InspectionResult(InspectionDecision.EndOperation, "Success");
        case ClientMessage.ReadStreamEventsCompleted.ReadStreamResult.StreamDeleted:
          Succeed();
          return new InspectionResult(InspectionDecision.EndOperation, "StreamDeleted");
        case ClientMessage.ReadStreamEventsCompleted.ReadStreamResult.NoStream:
          Succeed();
          return new InspectionResult(InspectionDecision.EndOperation, "NoStream");
        case ClientMessage.ReadStreamEventsCompleted.ReadStreamResult.Error:
          Fail(new ServerErrorException(string.IsNullOrEmpty(response.Error) ? "<no message>" : response.Error));
          return new InspectionResult(InspectionDecision.EndOperation, "Error");
        case ClientMessage.ReadStreamEventsCompleted.ReadStreamResult.AccessDenied:
          Fail(new AccessDeniedException($"Read access denied for stream '{_stream}'."));
          return new InspectionResult(InspectionDecision.EndOperation, "AccessDenied");
        default:
          throw new Exception($"Unexpected ReadStreamResult: {response.Result}.");
      }
    }

    public override string ToString()
    {
      return $"Stream: {_stream}, FromEventNumber: {_fromEventNumber}, MaxCount: {_maxCount}, ResolveLinkTos: {_resolveLinkTos}, RequireMaster: {_requireMaster}";
    }
  }
}