using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using EventStore.ClientAPI.Common;
using EventStore.ClientAPI.Common.Utils;
using EventStore.ClientAPI.Exceptions;
using EventStore.ClientAPI.Internal;
using EventStore.ClientAPI.SystemData;
using EventStore.ClientAPI.Transport.Http;
using EventStore.Core.Messages;
using EventStore.Transport.Tcp.Messages;

namespace EventStore.ClientAPI
{
    internal static class CoreThrowHelper
    {
        #region -- Exception --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException()
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_InvalidJson()
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception("Invalid JSON");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_DropReasonNotSpecified()
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception("Drop reason not specified.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_DoubleConfirmationOfSubscription()
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception("Double confirmation of subscription.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_SubscriptionNotConfirmedButEventAppeared()
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception("Subscription not confirmed, but event appeared!");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_EventIsNullWhileOperationResultIsSuccess()
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception("Event is null while operation result is Success.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_NotHandledCommandAppeared()
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception("NotHandled command appeared while we were already subscribed.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_SubscriptionEventCameUpWithNoOriginalPosition(string subscriptionName)
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception($"Subscription {subscriptionName} event came up with no OriginalPosition.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_StreamDisappearedInTheMiddleOfCatchingUpSubscription(string streamId, string subscriptionName)
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception($"Impossible: stream {streamId} disappeared in the middle of catching up subscription {subscriptionName}.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_UnableToParseIPAddress(Uri uri)
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception($"Unable to parse IP address or lookup DNS host for '{uri.Host}'");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_CouldnotGetAnIPv4Address(Uri uri)
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception($"Could not get an IPv4 address for host '{uri.Host}'");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_NoHandlerRegisteredForMessage(Message message)
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception($"No handler registered for message {message.GetType().Name}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_DidnotFindConnectToOrGossipSeeds(string connectionString)
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception($"Did not find ConnectTo or GossipSeeds in the connection string.\n'{connectionString}'");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_UnexpectedOperationResult(OperationResult result)
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception(string.Format("Unexpected OperationResult: {0}.", result));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_UnexpectedReadEventResult(TcpClientMessageDto.ReadEventCompleted.ReadEventResult result)
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception(string.Format("Unexpected ReadEventResult: {0}.", result));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_UnexpectedReadStreamResult(TcpClientMessageDto.ReadStreamEventsCompleted.ReadStreamResult result)
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception($"Unexpected ReadStreamResult: {result}.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_UnexpectedOperationResult(TcpClientMessageDto.UpdatePersistentSubscriptionCompleted.UpdatePersistentSubscriptionResult result)
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception(string.Format("Unexpected OperationResult: {0}.", result));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_UnexpectedOperationResult(TcpClientMessageDto.CreatePersistentSubscriptionCompleted.CreatePersistentSubscriptionResult result)
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception(string.Format("Unexpected OperationResult: {0}.", result));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_UnexpectedOperationResult(TcpClientMessageDto.DeletePersistentSubscriptionCompleted.DeletePersistentSubscriptionResult result)
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception(string.Format("Unexpected OperationResult: {0}.", result));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_UnexpectedReadAllResult(TcpClientMessageDto.ReadAllEventsCompleted.ReadAllResult result)
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception(string.Format("Unexpected ReadAllResult: {0}.", result));
            }
        }

        #endregion

        #region -- ArgumentException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static ArgumentException GetArgumentException_All(string streamId)
        {
            return new ArgumentException($"Subscription to '{(streamId == string.Empty ? "<all>" : streamId)}' failed due to not found.");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_TcpEndPointIsNotNullForDecision(InspectionDecision decision)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException(string.Format("tcpEndPoint is not null for decision {0}.", decision));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_SettingMetadataForMetastreamIsNotSupported(string stream)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException($"Setting metadata for metastream '{stream}' is not supported.", nameof(stream));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_DisposablesCollectionCanNotContainNullValues()
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException("Disposables collection can not contain null values.", "disposables");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_NoKeyFoundInCustomMetadata(string key)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException($"Key '{key}' not found in custom metadata.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_UnderlyingStreamACLHasMultipleRoles()
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException("Underlying stream ACL has multiple roles, which is not supported in old version of this API.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_NoGossipSeedsSpecified()
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException("No gossip seeds specified", "connectionSettings");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_CountShouldBeLessThanMaxReadSize()
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException($"Count should be less than {ClientApiConstants.MaxReadSize}. For larger reads you should page.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_CommandShouldnotbe(TcpCommand command)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException($"Command should not be {command}.");
            }
        }

        #endregion

        #region -- ArgumentOutOfRangeException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentOutOfRangeException_StreamMetadata_MaxCount()
        {
            throw GetException();
            ArgumentOutOfRangeException GetException()
            {
                return new ArgumentOutOfRangeException("maxCount", $"{SystemMetadata.MaxCount} should be positive value.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentOutOfRangeException_StreamMetadata_MaxAge()
        {
            throw GetException();
            ArgumentOutOfRangeException GetException()
            {
                return new ArgumentOutOfRangeException("maxAge", $"{SystemMetadata.MaxAge} should be positive time span.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentOutOfRangeException_StreamMetadata_TruncateBefore()
        {
            throw GetException();
            ArgumentOutOfRangeException GetException()
            {
                return new ArgumentOutOfRangeException("truncateBefore", $"{SystemMetadata.TruncateBefore} should be non-negative value.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentOutOfRangeException_StreamMetadata_CacheControl()
        {
            throw GetException();
            ArgumentOutOfRangeException GetException()
            {
                return new ArgumentOutOfRangeException("cacheControl", $"{SystemMetadata.CacheControl} should be positive time span.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentOutOfRangeException_EventsIsLimitedTo2000ToAckAtATime()
        {
            throw GetException();
            ArgumentOutOfRangeException GetException()
            {
                return new ArgumentOutOfRangeException("events", "events is limited to 2000 to ack at a time");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentOutOfRangeException_TheNumConnectionsMustBeAtLeastTwoConnections()
        {
            throw GetException();
            ArgumentOutOfRangeException GetException()
            {
                return new ArgumentOutOfRangeException("numConnections", $"The numConnections must be at least two connections.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentOutOfRangeException_InvalidLastCommitPositionOnSubscriptionConfirmation(long lastCommitPosition)
        {
            throw GetException();
            ArgumentOutOfRangeException GetException()
            {
                return new ArgumentOutOfRangeException(nameof(lastCommitPosition), $"Invalid lastCommitPosition {lastCommitPosition} on subscription confirmation.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentOutOfRangeException_UnexpectedReadEventResult(EventReadStatus status)
        {
            throw GetException();
            ArgumentOutOfRangeException GetException()
            {
                return new ArgumentOutOfRangeException($"Unexpected ReadEventResult: {status}.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentOutOfRangeException_UnexpectedStreamEventsSliceStatus(string subscriptionName)
        {
            throw GetException();
            ArgumentOutOfRangeException GetException()
            {
                return new ArgumentOutOfRangeException($"Subscription {subscriptionName} unexpected StreamEventsSlice.Status: {subscriptionName}.");
            }
        }

        #endregion

        #region -- InvalidOperationException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidOperationException_FailedConnection()
        {
            throw GetException();
            InvalidOperationException GetException()
            {
                return new InvalidOperationException("Failed connection.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidOperationException_TransactionIsAlreadyCommitted()
        {
            throw GetException();
            InvalidOperationException GetException()
            {
                return new InvalidOperationException("Transaction is already committed");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidOperationException_CannotCommitARolledbackTransaction()
        {
            throw GetException();
            InvalidOperationException GetException()
            {
                return new InvalidOperationException("Cannot commit a rolledback transaction");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidOperationException_CannotWriteToARolledbackTransaction()
        {
            throw GetException();
            InvalidOperationException GetException()
            {
                return new InvalidOperationException("Cannot write to a rolled-back transaction");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidOperationException_NonSerializableExceptionOfType(Type type)
        {
            throw GetException();
            InvalidOperationException GetException()
            {
                return new InvalidOperationException($"Non-serializable exception of type {type.AssemblyQualifiedName}");
            }
        }

        #endregion

        #region -- TimeoutException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowTimeoutException_CouldNotStopSubscriptionsInTime(Type type)
        {
            throw GetException();
            TimeoutException GetException()
            {
                return new TimeoutException($"Could not stop {type.Name} in time.");
            }
        }

        #endregion

        #region -- NotSupportedException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowNotSupportedException_SettingConnectToAsWellAsGossipSeeds(string connectionString)
        {
            throw GetException();
            NotSupportedException GetException()
            {
                return new NotSupportedException($"Setting ConnectTo as well as GossipSeeds on the connection string is currently not supported.\n{connectionString}");
            }
        }

        #endregion

        #region -- AccessDeniedException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static AccessDeniedException GetAccessDeniedException(ExceptionResource resource)
        {
            return new AccessDeniedException(ThrowHelper.GetResourceString(resource));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static AccessDeniedException GetAccessDeniedException(ExceptionResource resource, string stream)
        {
            return new AccessDeniedException(string.Format(ThrowHelper.GetResourceString(resource), stream));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static AccessDeniedException GetAccessDeniedException_All(string streamId)
        {
            return new AccessDeniedException(string.Format("Subscription to '{0}' failed due to access denied.", streamId == string.Empty ? "<all>" : streamId));
        }

        #endregion

        #region -- CannotEstablishConnectionException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static CannotEstablishConnectionException GetCannotEstablishConnectionException(AggregateException exc)
        {
            return new CannotEstablishConnectionException("Cannot resolve target end point.", exc);
        }

        #endregion

        #region -- CommandNotExpectedException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static CommandNotExpectedException GetCommandNotExpectedException(TcpCommand cmd)
        {
            return new CommandNotExpectedException(cmd.ToString());
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static CommandNotExpectedException GetCommandNotExpectedException(TcpCommand expectedCmd, TcpCommand cmd)
        {
            return new CommandNotExpectedException(expectedCmd.ToString(), cmd.ToString());
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static CommandNotExpectedException GetCommandNotExpectedException(TcpClientMessageDto.SubscriptionDropped.SubscriptionDropReason reason)
        {
            return new CommandNotExpectedException($"Unsubscribe reason: '{reason}'.");
        }

        #endregion

        #region -- ConnectionClosedException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static ConnectionClosedException GetConnectionClosedException()
        {
            return new ConnectionClosedException("Connection was closed.");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static ConnectionClosedException GetConnectionClosedException(string connName)
        {
            return new ConnectionClosedException($"Connection {connName} was closed.");
        }

        #endregion

        #region -- EventStoreConnectionException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static EventStoreConnectionException GetEventStoreConnectionException(TcpPackage package)
        {
            string message = Helper.EatException(() => Helper.UTF8NoBom.GetString(package.Data));
            return new EventStoreConnectionException($"Bad request received from server. Error: {(string.IsNullOrEmpty(message) ? "<no message>" : message)}");
        }

        #endregion

        #region -- InvalidTransactionException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static InvalidTransactionException GetInvalidTransactionException()
        {
            return new InvalidTransactionException();
        }

        #endregion

        #region -- NoResultException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static NoResultException GetNoResultException()
        {
            return new NoResultException();
        }

        #endregion

        #region -- NotAuthenticatedException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static NotAuthenticatedException GetNotAuthenticatedException(TcpPackage package)
        {
            string message = Helper.EatException(() => Helper.UTF8NoBom.GetString(package.Data), string.Empty);
            return new NotAuthenticatedException(string.IsNullOrEmpty(message) ? "Authentication error" : message);
        }

        #endregion

        #region -- OperationExpiredException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static OperationExpiredException GetOperationExpiredException(string connName, OperationItem operation)
        {
            var err = $"EventStoreConnection '{connName}': request expired.\nUTC now: {DateTime.UtcNow:HH:mm:ss.fff}, operation: {operation}.";
            return new OperationExpiredException(err);
        }

        #endregion

        #region -- PersistentSubscriptionCommandFailedException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static PersistentSubscriptionCommandFailedException GetPersistentSubscriptionCommandFailedException_Get(HttpResponse response, string url)
        {
            return new PersistentSubscriptionCommandFailedException(response.HttpStatusCode,
                string.Format("Server returned {0} ({1}) for GET on {2}", response.HttpStatusCode, response.StatusDescription, url));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static PersistentSubscriptionCommandFailedException GetPersistentSubscriptionCommandFailedException_Post(HttpResponse response, string url)
        {
            return new PersistentSubscriptionCommandFailedException(response.HttpStatusCode,
                string.Format("Server returned {0} ({1}) for POST on {2}", response.HttpStatusCode, response.StatusDescription, url));
        }

        #endregion

        #region -- ProjectionCommandConflictException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static ProjectionCommandConflictException GetProjectionCommandConflictException(HttpResponse response)
        {
            return new ProjectionCommandConflictException(response.HttpStatusCode, response.StatusDescription);
        }

        #endregion

        #region -- ProjectionCommandFailedException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static ProjectionCommandFailedException GetProjectionCommandFailedException_Get(HttpResponse response, string url)
        {
            return new ProjectionCommandFailedException(response.HttpStatusCode,
                string.Format("Server returned {0} ({1}) for GET on {2}", response.HttpStatusCode, response.StatusDescription, url));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static ProjectionCommandFailedException GetProjectionCommandFailedException_Delete(HttpResponse response, string url)
        {
            return new ProjectionCommandFailedException(response.HttpStatusCode,
                string.Format("Server returned {0} ({1}) for DELETE on {2}", response.HttpStatusCode, response.StatusDescription, url));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static ProjectionCommandFailedException GetProjectionCommandFailedException_Put(HttpResponse response, string url)
        {
            return new ProjectionCommandFailedException(response.HttpStatusCode,
                string.Format("Server returned {0} ({1}) for PUT on {2}", response.HttpStatusCode, response.StatusDescription, url));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static ProjectionCommandFailedException GetProjectionCommandFailedException_Post(HttpResponse response, string url)
        {
            return new ProjectionCommandFailedException(response.HttpStatusCode,
                string.Format("Server returned {0} ({1}) for POST on {2}", response.HttpStatusCode, response.StatusDescription, url));
        }

        #endregion

        #region -- UserCommandConflictException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static UserCommandConflictException GetUserCommandConflictException(HttpResponse response)
        {
            return new UserCommandConflictException(response.HttpStatusCode, response.StatusDescription);
        }

        #endregion

        #region -- UserCommandFailedException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static UserCommandFailedException GetUserCommandFailedException_Get(HttpResponse response, string url)
        {
            return new UserCommandFailedException(response.HttpStatusCode,
                string.Format("Server returned {0} ({1}) for GET on {2}", response.HttpStatusCode, response.StatusDescription, url));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static UserCommandFailedException GetUserCommandFailedException_Delete(HttpResponse response, string url)
        {
            return new UserCommandFailedException(response.HttpStatusCode,
                string.Format("Server returned {0} ({1}) for DELETE on {2}", response.HttpStatusCode, response.StatusDescription, url));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static UserCommandFailedException GetUserCommandFailedException_Put(HttpResponse response, string url)
        {
            return new UserCommandFailedException(response.HttpStatusCode,
                string.Format("Server returned {0} ({1}) for PUT on {2}", response.HttpStatusCode, response.StatusDescription, url));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static UserCommandFailedException GetUserCommandFailedException_Post(HttpResponse response, string url)
        {
            return new UserCommandFailedException(response.HttpStatusCode,
                string.Format("Server returned {0} ({1}) for POST on {2}", response.HttpStatusCode, response.StatusDescription, url));
        }

        #endregion

        #region -- RetriesLimitReachedException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static RetriesLimitReachedException GetRetriesLimitReachedException(SubscriptionItem subscription)
        {
            return new RetriesLimitReachedException(subscription.ToString(), subscription.RetryCount);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static RetriesLimitReachedException GetRetriesLimitReachedException(OperationItem operation)
        {
            return new RetriesLimitReachedException(operation.ToString(), operation.RetryCount);
        }

        #endregion

        #region -- ServerErrorException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static ServerErrorException GetServerErrorException(TcpClientMessageDto.ReadAllEventsCompleted response)
        {
            return new ServerErrorException(string.IsNullOrEmpty(response.Error) ? "<no message>" : response.Error);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static ServerErrorException GetServerErrorException(TcpClientMessageDto.ReadStreamEventsCompleted response)
        {
            return new ServerErrorException(string.IsNullOrEmpty(response.Error) ? "<no message>" : response.Error);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static ServerErrorException GetServerErrorException(TcpClientMessageDto.ReadEventCompleted response)
        {
            return new ServerErrorException(string.IsNullOrEmpty(response.Error) ? "<no message>" : response.Error);
        }

        #endregion

        #region -- StreamDeletedException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static StreamDeletedException GetStreamDeletedException()
        {
            return new StreamDeletedException();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static StreamDeletedException GetStreamDeletedException(string streamId)
        {
            return new StreamDeletedException(streamId);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowStreamDeletedException(string streamId)
        {
            throw GetStreamDeletedException(streamId);
        }

        #endregion

        #region -- WrongExpectedVersionException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static WrongExpectedVersionException GetWrongExpectedVersionException_AppendFailed(string stream, long expectedVersion)
        {
            var err = string.Format("Append failed due to WrongExpectedVersion. Stream: {0}, Expected version: {1}", stream, expectedVersion);
            return new WrongExpectedVersionException(err);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static WrongExpectedVersionException GetWrongExpectedVersionException_AppendFailed(string stream, long expectedVersion, long? currentVersion)
        {
            var err = string.Format("Append failed due to WrongExpectedVersion. Stream: {0}, Expected version: {1}, Current version: {2}", stream, expectedVersion, currentVersion);
            return new WrongExpectedVersionException(err);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static WrongExpectedVersionException GetWrongExpectedVersionException_CommitTransactionFailed(long transactionId)
        {
            var err = string.Format("Commit transaction failed due to WrongExpectedVersion. TransactionID: {0}.", transactionId);
            return new WrongExpectedVersionException(err);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static WrongExpectedVersionException GetWrongExpectedVersionException_DeleteStreamFailed(string stream, long expectedVersion)
        {
            var err = string.Format("Delete stream failed due to WrongExpectedVersion. Stream: {0}, Expected version: {1}.", stream, expectedVersion);
            return new WrongExpectedVersionException(err);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static WrongExpectedVersionException GetWrongExpectedVersionException_StartTransactionFailed(string stream, long expectedVersion)
        {
            var err = string.Format("Start transaction failed due to WrongExpectedVersion. Stream: {0}, Expected version: {1}.", stream, expectedVersion);
            return new WrongExpectedVersionException(err);
        }

        #endregion

        #region -- OperationTimedOutException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static OperationTimedOutException GetOperationTimedOutException(string connName, SubscriptionItem subscription)
        {
            var err = $"EventStoreConnection '{connName}': subscription never got confirmation from server.\n UTC now: {DateTime.UtcNow:HH:mm:ss.fff}, operation: {subscription}.";
            return new OperationTimedOutException(err);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static OperationTimedOutException GetOperationTimedOutException(string connName, OperationItem operation)
        {
            var err = $"EventStoreConnection '{connName}': operation never got response from server.\n UTC now: {DateTime.UtcNow:HH:mm:ss.fff}, operation: {operation}.";
            return new OperationTimedOutException(err);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowOperationTimedOutException(TimeSpan timeout)
        {
            throw GetException();
            OperationTimedOutException GetException()
            {
                return new OperationTimedOutException(string.Format("The operation did not complete within the specified time of {0}", timeout));
            }
        }

        #endregion

        #region -- ClusterException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowClusterException(int maxDiscoverAttempts)
        {
            throw GetException();
            ClusterException GetException()
            {
                return new ClusterException($"Failed to discover candidate in {maxDiscoverAttempts} attempts.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowClusterException(string clusterDns)
        {
            throw GetException();
            ClusterException GetException()
            {
                return new ClusterException($"DNS entry '{clusterDns}' resolved into empty list.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowClusterException(string clusterDns, Exception exc)
        {
            throw GetException();
            ClusterException GetException()
            {
                return new ClusterException($"Error while resolving DNS entry '{clusterDns}'.", exc);
            }
        }

        #endregion

        #region -- EventDataDeserializationException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowEventDataDeserializationException(Exception exc)
        {
            throw GetException();
            EventDataDeserializationException GetException()
            {
                return new EventDataDeserializationException(exc.Message, exc);
            }
        }

        #endregion

        #region -- EventMetadataDeserializationException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowEventMetadataDeserializationException()
        {
            throw GetException();
            EventMetadataDeserializationException GetException()
            {
                const string _metadataEmpty = "The meta-data of EventRecord is not available.";
                return new EventMetadataDeserializationException(_metadataEmpty);
            }
        }

        #endregion

        #region -- EventStoreHandlerException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowEventStoreHandlerException<TEvent>() where TEvent : class
        {
            throw GetException();
            EventStoreHandlerException GetException()
            {
                return new EventStoreHandlerException($"There is already a handler for event type '{typeof(TEvent).Name}'");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowEventStoreHandlerException(Type eventType)
        {
            throw GetException();
            EventStoreHandlerException GetException()
            {
                return new EventStoreHandlerException($"No handler found for event type {eventType.Name}");
            }
        }

        #endregion

        #region -- KeyNotFoundException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowKeyNotFoundException(string key)
        {
            throw GetException();
            KeyNotFoundException GetException()
            {
                return new KeyNotFoundException($"The key was not present: {key}");
            }
        }

        #endregion
    }
}
