using System;
using System.Threading.Tasks;
using EventStore.ClientAPI.Exceptions;
using EventStore.ClientAPI.Internal;
using EventStore.ClientAPI.Messages;
using EventStore.Core.Data;
using EventStore.Core.Messages;
using CoreClientMessage = EventStore.Core.Messages.ClientMessage;

namespace EventStore.ClientAPI.Embedded
{
    internal static class EmbeddedResponders
    {
        internal class AppendToStream : EmbeddedResponderBase<WriteResult, CoreClientMessage.WriteEventsCompleted>
        {
            private readonly long _expectedVersion;
            private readonly string _stream;

            public AppendToStream(TaskCompletionSource<WriteResult> source, string stream, long expectedVersion)
              : base(source)
            {
                _stream = stream;
                _expectedVersion = expectedVersion;
            }

            protected override void InspectResponse(CoreClientMessage.WriteEventsCompleted response)
            {
                switch (response.Result)
                {
                    case OperationResult.Success:
                        Succeed(response);
                        break;
                    case OperationResult.PrepareTimeout:
                    case OperationResult.ForwardTimeout:
                    case OperationResult.CommitTimeout:
                        break;
                    case OperationResult.WrongExpectedVersion:
                        Fail(CoreThrowHelper.GetWrongExpectedVersionException_AppendFailed(_stream, _expectedVersion));
                        break;
                    case OperationResult.StreamDeleted:
                        Fail(CoreThrowHelper.GetStreamDeletedException(_stream));
                        break;
                    case OperationResult.InvalidTransaction:
                        Fail(CoreThrowHelper.GetInvalidTransactionException());
                        break;
                    case OperationResult.AccessDenied:
                        Fail(CoreThrowHelper.GetAccessDeniedException(ExceptionResource.Write_access_denied_for_stream, _stream));
                        break;
                    default:
                        throw new Exception($"Unexpected OperationResult: {response.Result}.");
                }
            }

            protected override WriteResult TransformResponse(CoreClientMessage.WriteEventsCompleted response)
            {
                return new WriteResult(response.LastEventNumber, new Position(response.PreparePosition, response.CommitPosition));
            }
        }

        internal class ConditionalAppendToStream : EmbeddedResponderBase<ConditionalWriteResult, CoreClientMessage.WriteEventsCompleted>
        {
            private readonly string _stream;

            public ConditionalAppendToStream(TaskCompletionSource<ConditionalWriteResult> source, string stream)
              : base(source)
            {
                _stream = stream;
            }

            protected override void InspectResponse(CoreClientMessage.WriteEventsCompleted response)
            {
                switch (response.Result)
                {
                    case OperationResult.Success:
                        Succeed(response);
                        break;
                    case OperationResult.PrepareTimeout:
                    case OperationResult.ForwardTimeout:
                    case OperationResult.CommitTimeout:
                        break;
                    case OperationResult.WrongExpectedVersion:
                        Succeed(response);
                        break;
                    case OperationResult.StreamDeleted:
                        Succeed(response);
                        break;
                    case OperationResult.InvalidTransaction:
                        Fail(CoreThrowHelper.GetInvalidTransactionException());
                        break;
                    case OperationResult.AccessDenied:
                        Fail(CoreThrowHelper.GetAccessDeniedException(ExceptionResource.Write_access_denied_for_stream, _stream));
                        break;
                    default:
                        throw new Exception($"Unexpected OperationResult: {response.Result}.");
                }
            }

            protected override ConditionalWriteResult TransformResponse(CoreClientMessage.WriteEventsCompleted response)
            {
                if (response.Result == OperationResult.WrongExpectedVersion)
                {
                    return new ConditionalWriteResult(ConditionalWriteStatus.VersionMismatch);
                }
                if (response.Result == OperationResult.StreamDeleted)
                {
                    return new ConditionalWriteResult(ConditionalWriteStatus.StreamDeleted);
                }
                return new ConditionalWriteResult(response.LastEventNumber, new Position(response.PreparePosition, response.CommitPosition));
            }
        }

        internal class DeleteStream : EmbeddedResponderBase<DeleteResult, CoreClientMessage.DeleteStreamCompleted>
        {
            private readonly long _expectedVersion;
            private readonly string _stream;

            public DeleteStream(TaskCompletionSource<DeleteResult> source, string stream, long expectedVersion) : base(source)
            {
                _stream = stream;
                _expectedVersion = expectedVersion;
            }

            protected override void InspectResponse(CoreClientMessage.DeleteStreamCompleted response)
            {
                switch (response.Result)
                {
                    case OperationResult.Success:
                        Succeed(response);
                        break;
                    case OperationResult.PrepareTimeout:
                    case OperationResult.CommitTimeout:
                    case OperationResult.ForwardTimeout:
                        break;
                    case OperationResult.WrongExpectedVersion:
                        Fail(CoreThrowHelper.GetWrongExpectedVersionException_DeleteStreamFailed(_stream, _expectedVersion));
                        break;
                    case OperationResult.StreamDeleted:
                        Fail(CoreThrowHelper.GetStreamDeletedException(_stream));
                        break;
                    case OperationResult.InvalidTransaction:
                        Fail(CoreThrowHelper.GetInvalidTransactionException());
                        break;
                    case OperationResult.AccessDenied:
                        Fail(CoreThrowHelper.GetAccessDeniedException(ExceptionResource.Write_access_denied_for_stream, _stream));
                        break;
                    default:
                        throw new Exception($"Unexpected OperationResult: {response.Result}.");

                }
            }

            protected override DeleteResult TransformResponse(CoreClientMessage.DeleteStreamCompleted response)
            {
                return new DeleteResult(new Position(response.PreparePosition, response.CommitPosition));

            }
        }

        internal class ReadAllEventsBackward : EmbeddedResponderBase<AllEventsSlice, CoreClientMessage.ReadAllEventsBackwardCompleted>
        {
            public ReadAllEventsBackward(TaskCompletionSource<AllEventsSlice> source)
              : base(source)
            {
            }

            protected override void InspectResponse(CoreClientMessage.ReadAllEventsBackwardCompleted response)
            {
                switch (response.Result)
                {
                    case ReadAllResult.Success:
                        Succeed(response);
                        break;
                    case ReadAllResult.Error:
                        Fail(new ServerErrorException(string.IsNullOrEmpty(response.Error) ? "<no message>" : response.Error));
                        break;
                    case ReadAllResult.AccessDenied:
                        Fail(CoreThrowHelper.GetAccessDeniedException(ExceptionResource.Read_access_denied_for_all));
                        break;
                    default:
                        throw new Exception($"Unexpected ReadAllResult: {response.Result}.");
                }

            }

            protected override AllEventsSlice TransformResponse(CoreClientMessage.ReadAllEventsBackwardCompleted response)
            {
                return new AllEventsSlice(ReadDirection.Backward,
                    new Position(response.CurrentPos.CommitPosition, response.CurrentPos.PreparePosition),
                    new Position(response.NextPos.CommitPosition, response.NextPos.PreparePosition),
                    response.Events.ConvertToClientResolvedEvents().ToRawResolvedEvents());

            }
        }

        internal class ReadAllEventsForward : EmbeddedResponderBase<AllEventsSlice, CoreClientMessage.ReadAllEventsForwardCompleted>
        {
            public ReadAllEventsForward(TaskCompletionSource<AllEventsSlice> source) : base(source)
            {
            }

            protected override void InspectResponse(CoreClientMessage.ReadAllEventsForwardCompleted response)
            {
                switch (response.Result)
                {
                    case ReadAllResult.Success:
                        Succeed(response);
                        break;
                    case ReadAllResult.Error:
                        Fail(new ServerErrorException(string.IsNullOrEmpty(response.Error) ? "<no message>" : response.Error));
                        break;
                    case ReadAllResult.AccessDenied:
                        Fail(CoreThrowHelper.GetAccessDeniedException(ExceptionResource.Read_access_denied_for_all));
                        break;
                    default:
                        throw new Exception($"Unexpected ReadAllResult: {response.Result}.");
                }

            }

            protected override AllEventsSlice TransformResponse(CoreClientMessage.ReadAllEventsForwardCompleted response)
            {
                return new AllEventsSlice(ReadDirection.Forward,
                    new Position(response.CurrentPos.CommitPosition, response.CurrentPos.PreparePosition),
                    new Position(response.NextPos.CommitPosition, response.NextPos.PreparePosition),
                    response.Events.ConvertToClientResolvedEvents().ToRawResolvedEvents());

            }
        }

        internal class ReadEvent : EmbeddedResponderBase<EventReadResult, CoreClientMessage.ReadEventCompleted>
        {
            private readonly long _eventNumber;
            private readonly string _stream;

            public ReadEvent(TaskCompletionSource<EventReadResult> source, string stream, long eventNumber)
              : base(source)
            {
                _stream = stream;
                _eventNumber = eventNumber;
            }

            protected override void InspectResponse(CoreClientMessage.ReadEventCompleted response)
            {
                switch (response.Result)
                {
                    case ReadEventResult.Success:
                    case ReadEventResult.NotFound:
                    case ReadEventResult.NoStream:
                    case ReadEventResult.StreamDeleted:
                        Succeed(response);
                        return;
                    case ReadEventResult.Error:
                        Fail(new ServerErrorException(string.IsNullOrEmpty(response.Error) ? "<no message>" : response.Error));
                        return;
                    case ReadEventResult.AccessDenied:
                        Fail(CoreThrowHelper.GetAccessDeniedException( ExceptionResource.Read_access_denied_for_stream, _stream));
                        return;
                    default:
                        EmbeddedThrowHelper.ThrowException_UnexpectedReadEventResult(response.Result); return;
                }
            }

            protected override EventReadResult TransformResponse(CoreClientMessage.ReadEventCompleted response)
            {
                var readStatus = Convert(response.Result);
                return new EventReadResult(readStatus, _stream, _eventNumber, response.Record.ConvertToClientResolvedIndexEvent().ToRawResolvedEvent(readStatus));
            }


            private static EventReadStatus Convert(ReadEventResult result)
            {
                switch (result)
                {
                    case ReadEventResult.Success:
                        return EventReadStatus.Success;
                    case ReadEventResult.NotFound:
                        return EventReadStatus.NotFound;
                    case ReadEventResult.NoStream:
                        return EventReadStatus.NoStream;
                    case ReadEventResult.StreamDeleted:
                        return EventReadStatus.StreamDeleted;
                    default:
                        EmbeddedThrowHelper.ThrowException_UnexpectedReadEventResult(result); return default;
                }
            }
        }

        internal class ReadStreamEventsBackward : EmbeddedResponderBase<StreamEventsSlice, CoreClientMessage.ReadStreamEventsBackwardCompleted>
        {
            private readonly long _fromEventNumber;
            private readonly string _stream;

            public ReadStreamEventsBackward(TaskCompletionSource<StreamEventsSlice> source, string stream, long fromEventNumber)
              : base(source)
            {
                _stream = stream;
                _fromEventNumber = fromEventNumber;
            }

            protected override void InspectResponse(CoreClientMessage.ReadStreamEventsBackwardCompleted response)
            {
                switch (response.Result)
                {
                    case ReadStreamResult.Success:
                    case ReadStreamResult.StreamDeleted:
                    case ReadStreamResult.NoStream:
                        Succeed(response);
                        break;
                    case ReadStreamResult.Error:
                        Fail(new ServerErrorException(string.IsNullOrEmpty(response.Error) ? "<no message>" : response.Error));
                        break;
                    case ReadStreamResult.AccessDenied:
                        Fail(CoreThrowHelper.GetAccessDeniedException( ExceptionResource.Read_access_denied_for_stream, _stream));
                        break;
                    default:
                        throw new Exception($"Unexpected ReadStreamResult: {response.Result}.");
                }
            }

            protected override StreamEventsSlice TransformResponse(CoreClientMessage.ReadStreamEventsBackwardCompleted response)
            {
                return new StreamEventsSlice(Convert(response.Result),
                    _stream,
                    _fromEventNumber,
                    ReadDirection.Backward,
                    response.Events.ConvertToClientResolvedIndexEvents().ToRawResolvedEvents(),
                    response.NextEventNumber,
                    response.LastEventNumber,
                    response.IsEndOfStream);

            }

            SliceReadStatus Convert(ReadStreamResult result)
            {
                switch (result)
                {
                    case ReadStreamResult.Success:
                        return SliceReadStatus.Success;
                    case ReadStreamResult.NoStream:
                        return SliceReadStatus.StreamNotFound;
                    case ReadStreamResult.StreamDeleted:
                        return SliceReadStatus.StreamDeleted;
                    default:
                        throw new Exception($"Unexpected ReadStreamResult: {result}.");
                }
            }
        }

        internal class ReadStreamForwardEvents : EmbeddedResponderBase<StreamEventsSlice, CoreClientMessage.ReadStreamEventsForwardCompleted>
        {
            private readonly long _fromEventNumber;
            private readonly string _stream;

            public ReadStreamForwardEvents(TaskCompletionSource<StreamEventsSlice> source, string stream, long fromEventNumber) : base(source)
            {
                _stream = stream;
                _fromEventNumber = fromEventNumber;
            }

            protected override void InspectResponse(CoreClientMessage.ReadStreamEventsForwardCompleted response)
            {
                switch (response.Result)
                {
                    case ReadStreamResult.Success:
                    case ReadStreamResult.StreamDeleted:
                    case ReadStreamResult.NoStream:
                        Succeed(response);
                        break;
                    case ReadStreamResult.Error:
                        Fail(new ServerErrorException(string.IsNullOrEmpty(response.Error) ? "<no message>" : response.Error));
                        break;
                    case ReadStreamResult.AccessDenied:
                        Fail(CoreThrowHelper.GetAccessDeniedException( ExceptionResource.Read_access_denied_for_stream, _stream));
                        break;
                    default:
                        throw new Exception($"Unexpected ReadStreamResult: {response.Result}.");
                }
            }

            protected override StreamEventsSlice TransformResponse(CoreClientMessage.ReadStreamEventsForwardCompleted response)
            {
                return new StreamEventsSlice(Convert(response.Result),
                    _stream,
                    _fromEventNumber,
                    ReadDirection.Forward,
                    response.Events.ConvertToClientResolvedIndexEvents().ToRawResolvedEvents(),
                    response.NextEventNumber,
                    response.LastEventNumber,
                    response.IsEndOfStream);

            }

            SliceReadStatus Convert(ReadStreamResult result)
            {
                switch (result)
                {
                    case ReadStreamResult.Success:
                        return SliceReadStatus.Success;
                    case ReadStreamResult.NoStream:
                        return SliceReadStatus.StreamNotFound;
                    case ReadStreamResult.StreamDeleted:
                        return SliceReadStatus.StreamDeleted;
                    default:
                        throw new Exception($"Unexpected ReadStreamResult: {result}.");
                }
            }
        }

        internal class TransactionCommit : EmbeddedResponderBase<WriteResult, CoreClientMessage.TransactionCommitCompleted>
        {
            public TransactionCommit(TaskCompletionSource<WriteResult> source)
              : base(source)
            {
            }

            protected override void InspectResponse(CoreClientMessage.TransactionCommitCompleted response)
            {
                switch (response.Result)
                {
                    case OperationResult.Success:
                        Succeed(response);
                        break;
                    case OperationResult.PrepareTimeout:
                    case OperationResult.CommitTimeout:
                    case OperationResult.ForwardTimeout:
                        break;
                    case OperationResult.WrongExpectedVersion:
                        Fail(CoreThrowHelper.GetWrongExpectedVersionException_CommitTransactionFailed(response.TransactionId));
                        break;
                    case OperationResult.StreamDeleted:
                        Fail(CoreThrowHelper.GetStreamDeletedException());
                        break;
                    case OperationResult.InvalidTransaction:
                        Fail(CoreThrowHelper.GetInvalidTransactionException());
                        break;
                    case OperationResult.AccessDenied:
                        Fail(CoreThrowHelper.GetAccessDeniedException(ExceptionResource.Write_access_denied));
                        break;
                    default:
                        throw new Exception($"Unexpected OperationResult: {response.Result}.");
                }

            }

            protected override WriteResult TransformResponse(CoreClientMessage.TransactionCommitCompleted response)
            {
                return new WriteResult(response.LastEventNumber, new Position(response.PreparePosition, response.CommitPosition));
            }
        }

        internal class TransactionStart : EmbeddedResponderBase<EventStoreTransaction, CoreClientMessage.TransactionStartCompleted>
        {
            private readonly long _expectedVersion;
            private readonly IEventStoreTransactionConnection _parentConnection;
            private readonly string _stream;

            public TransactionStart(TaskCompletionSource<EventStoreTransaction> source, IEventStoreTransactionConnection parentConnection, string stream, long expectedVersion) : base(source)
            {
                _parentConnection = parentConnection;
                _stream = stream;
                _expectedVersion = expectedVersion;
            }

            protected override void InspectResponse(CoreClientMessage.TransactionStartCompleted response)
            {
                switch (response.Result)
                {
                    case OperationResult.Success:
                        Succeed(response);
                        break;
                    case OperationResult.PrepareTimeout:
                    case OperationResult.CommitTimeout:
                    case OperationResult.ForwardTimeout:
                        break;
                    case OperationResult.WrongExpectedVersion:
                        Fail(CoreThrowHelper.GetWrongExpectedVersionException_StartTransactionFailed(_stream, _expectedVersion));
                        break;
                    case OperationResult.StreamDeleted:
                        Fail(CoreThrowHelper.GetStreamDeletedException(_stream));
                        break;
                    case OperationResult.InvalidTransaction:
                        Fail(CoreThrowHelper.GetInvalidTransactionException());
                        break;
                    case OperationResult.AccessDenied:
                        Fail(CoreThrowHelper.GetAccessDeniedException(ExceptionResource.Write_access_denied_for_stream, _stream));
                        break;
                    default:
                        throw new Exception($"Unexpected OperationResult: {response.Result}.");
                }

            }

            protected override EventStoreTransaction TransformResponse(CoreClientMessage.TransactionStartCompleted response)
            {
                return new EventStoreTransaction(response.TransactionId, null, _parentConnection);
            }
        }

        internal class TransactionWrite : EmbeddedResponderBase<EventStoreTransaction, CoreClientMessage.TransactionWriteCompleted>
        {
            private readonly IEventStoreTransactionConnection _parentConnection;

            public TransactionWrite(TaskCompletionSource<EventStoreTransaction> source, IEventStoreTransactionConnection parentConnection)
              : base(source)
            {
                _parentConnection = parentConnection;
            }

            protected override void InspectResponse(CoreClientMessage.TransactionWriteCompleted response)
            {
                switch (response.Result)
                {
                    case OperationResult.Success:
                        Succeed(response);
                        break;
                    case OperationResult.PrepareTimeout:
                    case OperationResult.CommitTimeout:
                    case OperationResult.ForwardTimeout:
                        break;
                    case OperationResult.AccessDenied:
                        Fail(CoreThrowHelper.GetAccessDeniedException(ExceptionResource.Write_access_denied));
                        break;
                    default:
                        throw new Exception($"Unexpected OperationResult: {response.Result}.");
                }

            }

            protected override EventStoreTransaction TransformResponse(CoreClientMessage.TransactionWriteCompleted response)
            {
                return new EventStoreTransaction(response.TransactionId, null, _parentConnection);
            }
        }

        internal class CreatePersistentSubscription : EmbeddedResponderBase<PersistentSubscriptionCreateResult, CoreClientMessage.CreatePersistentSubscriptionCompleted>
        {
            private readonly string _stream;
            private readonly string _groupName;

            public CreatePersistentSubscription(TaskCompletionSource<PersistentSubscriptionCreateResult> source, string stream, string groupName)
              : base(source)
            {
                _groupName = groupName;
                _stream = stream;
            }

            protected override void InspectResponse(CoreClientMessage.CreatePersistentSubscriptionCompleted response)
            {
                switch (response.Result)
                {
                    case CoreClientMessage.CreatePersistentSubscriptionCompleted.CreatePersistentSubscriptionResult.Success:
                        Succeed(response);
                        break;
                    case CoreClientMessage.CreatePersistentSubscriptionCompleted.CreatePersistentSubscriptionResult.Fail:
                        Fail(new InvalidOperationException($"Subscription group {_groupName} on stream {_stream} failed '{response.Reason}'"));
                        break;
                    case CoreClientMessage.CreatePersistentSubscriptionCompleted.CreatePersistentSubscriptionResult.AccessDenied:
                        Fail(CoreThrowHelper.GetAccessDeniedException(ExceptionResource.Write_access_denied_for_stream, _stream));
                        break;
                    case CoreClientMessage.CreatePersistentSubscriptionCompleted.CreatePersistentSubscriptionResult.AlreadyExists:
                        Fail(new InvalidOperationException($"Subscription group {_groupName} on stream {_stream} already exists"));
                        break;
                    default:
                        throw new Exception($"Unexpected OperationResult: {response.Result}.");
                }
            }

            protected override PersistentSubscriptionCreateResult TransformResponse(CoreClientMessage.CreatePersistentSubscriptionCompleted response)
            {
                return new PersistentSubscriptionCreateResult((PersistentSubscriptionCreateStatus)response.Result);
            }
        }

        internal class UpdatePersistentSubscription : EmbeddedResponderBase<PersistentSubscriptionUpdateResult, CoreClientMessage.UpdatePersistentSubscriptionCompleted>
        {
            private readonly string _stream;
            private readonly string _groupName;

            public UpdatePersistentSubscription(TaskCompletionSource<PersistentSubscriptionUpdateResult> source, string stream, string groupName)
              : base(source)
            {
                _groupName = groupName;
                _stream = stream;
            }

            protected override void InspectResponse(CoreClientMessage.UpdatePersistentSubscriptionCompleted response)
            {
                switch (response.Result)
                {
                    case CoreClientMessage.UpdatePersistentSubscriptionCompleted.UpdatePersistentSubscriptionResult.Success:
                        Succeed(response);
                        break;
                    case CoreClientMessage.UpdatePersistentSubscriptionCompleted.UpdatePersistentSubscriptionResult.Fail:
                        Fail(new InvalidOperationException($"Subscription group {_groupName} on stream {_stream} failed '{response.Reason}'"));
                        break;
                    case CoreClientMessage.UpdatePersistentSubscriptionCompleted.UpdatePersistentSubscriptionResult.AccessDenied:
                        Fail(CoreThrowHelper.GetAccessDeniedException(ExceptionResource.Write_access_denied_for_stream, _stream));
                        break;
                    case CoreClientMessage.UpdatePersistentSubscriptionCompleted.UpdatePersistentSubscriptionResult.DoesNotExist:
                        Fail(new InvalidOperationException($"Subscription group {_groupName} on stream {_stream} does not exist"));
                        break;
                    default:
                        EmbeddedThrowHelper.ThrowException_UnexpectedOperationResult(response.Result); break; ;
                }
            }

            protected override PersistentSubscriptionUpdateResult TransformResponse(CoreClientMessage.UpdatePersistentSubscriptionCompleted response)
            {
                return new PersistentSubscriptionUpdateResult((PersistentSubscriptionUpdateStatus)response.Result);
            }
        }

        internal class DeletePersistentSubscription : EmbeddedResponderBase<PersistentSubscriptionDeleteResult, CoreClientMessage.DeletePersistentSubscriptionCompleted>
        {
            private readonly string _stream;
            private readonly string _groupName;

            public DeletePersistentSubscription(TaskCompletionSource<PersistentSubscriptionDeleteResult> source, string stream, string groupName)
              : base(source)
            {
                _groupName = groupName;
                _stream = stream;
            }

            protected override void InspectResponse(CoreClientMessage.DeletePersistentSubscriptionCompleted response)
            {
                switch (response.Result)
                {
                    case CoreClientMessage.DeletePersistentSubscriptionCompleted.DeletePersistentSubscriptionResult.Success:
                        Succeed(response);
                        break;
                    case CoreClientMessage.DeletePersistentSubscriptionCompleted.DeletePersistentSubscriptionResult.Fail:
                        Fail(new InvalidOperationException($"Subscription group {_groupName} on stream {_stream} failed '{response.Reason}'"));
                        break;
                    case CoreClientMessage.DeletePersistentSubscriptionCompleted.DeletePersistentSubscriptionResult.AccessDenied:
                        Fail(CoreThrowHelper.GetAccessDeniedException(ExceptionResource.Write_access_denied_for_stream, _stream));
                        break;
                    case CoreClientMessage.DeletePersistentSubscriptionCompleted.DeletePersistentSubscriptionResult.DoesNotExist:
                        Fail(new InvalidOperationException($"Subscription group {_groupName} on stream {_stream} does not exist"));
                        break;
                    default:
                        EmbeddedThrowHelper.ThrowException_UnexpectedOperationResult(response.Result); break;
                }
            }

            protected override PersistentSubscriptionDeleteResult TransformResponse(CoreClientMessage.DeletePersistentSubscriptionCompleted response)
            {
                return new PersistentSubscriptionDeleteResult((PersistentSubscriptionDeleteStatus)response.Result);
            }
        }
    }

}
