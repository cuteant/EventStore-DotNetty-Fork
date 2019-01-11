using System;
using System.Runtime.CompilerServices;
using EventStore.ClientAPI.Exceptions;
using EventStore.Core.Data;
using EventStore.Core.Messages;
using EventStore.Core.Messaging;

namespace EventStore.ClientAPI.Embedded
{
    internal static class EmbeddedThrowHelper
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static NoResultException GetNoResultException<TResponse>(Message message) where TResponse : Message
        {
            return new NoResultException(String.Format("Expected response of {0}, received {1} instead.", typeof(TResponse), message.GetType()));
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
        internal static void ThrowException_DoubleConfirmationOfSubscription()
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception("Double confirmation of subscription.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_UnexpectedReadEventResult(ReadEventResult result)
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception($"Unexpected ReadEventResult: {result}.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_UnexpectedOperationResult(ClientMessage.UpdatePersistentSubscriptionCompleted.UpdatePersistentSubscriptionResult result)
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception($"Unexpected OperationResult: {result}.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_UnexpectedOperationResult(ClientMessage.DeletePersistentSubscriptionCompleted.DeletePersistentSubscriptionResult result)
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception($"Unexpected OperationResult: {result}.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_NoExceptionProvidedForSubscriptionDropReason(SubscriptionDropReason reason)
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception($"No exception provided for subscription drop reason '{reason}");
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
        internal static void ThrowArgumentException_SettingMetadataForMetastreamIsNotSupported(string stream)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException($"Setting metadata for metastream '{stream}' is not supported.", nameof(stream));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentOutOfRangeException_InvalidLastCommitPosition(long lastCommitPosition)
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
    }
}
