using System;
using System.Runtime.CompilerServices;
using EventStore.ClientAPI.Common.MatchHandler;

namespace EventStore.ClientAPI
{
    #region -- ExceptionArgument --

    /// <summary>The convention for this enum is using the argument name as the enum name</summary>
    internal enum ExceptionArgument
    {
        code,
        array,
        arrayIndex,
        query,
        inner,
        limit,
        compiler,
        handlesType,
        @event,
        evt,
        name,
        dictionary,
        context,
        eventData,
        eventMeta,
        eventMetas,
        eventContexts,
        sleepDurationProvider,
        registerHandlers,
        serializerSettings,
        deserializerSettings,
        charPool,
        objectPoolProvider,
        serializer,
        reader,
        subscription,
        expectedType,
        esConnection,
        subscription_StreamId,
        subscription_Settings,
        registerEventHandlers,
        subscription_SubscriptionId,
        subscription_PersistentSettings,
        streamAttr_StreamId,
        consumerTypes,
        connection,
        resolvedEventAppeared,
        resolvedEventAppearedAsync,
        tcpEndPoint,
        batchSize,
        bus,
        stream,
        streamAttr,
        func,
        action,
        data,
        item,
        getConnection,
        cacheControl,
        truncateBefore,
        maxAge,
        log,
        disposables,
        metadata,
        writer,
        capacity,
        task,
        _connection,
        connectionName,
        operation,
        endPoint,
        handler,
        httpEndPoint,
        connEventHandler,
        remoteEndPoint,
        userCredentials,
        body,
        request,
        input,
        output,
        topic,
        actualType,
        concreteType,
        interfaceType,
        consumeMethodName,
        value,
        messageTimeout,
        checkPointAfter,
        maxRetries,
        maxReconnections,
        externalGossipPort,
        clusterDns,
        readBatchSize,
        maxLiveQueueSize,
        username,
        password,
        maxNoOfRetries,
        events,
        transaction,
        eventNumber,
        groupName,
        settings,
        eventAppeared,
        eventAppearedAsync,
        publisher,
        streamId,
        source,
        message,
        reason,
        processedEvents,
        maxCount,
        count,
        start,
        transactionId,
        maxConcurrentItems,
        maxQueueSize,
        clusterGossipPort,
        targetHost,
        namedConsumerStrategy,
        bufferSize,
        login,
        subscriptionId,
        subscriptionName,
        partitionId,
        fullName,
        newPassword,
        oldPassword,
        currentPassword,
        url,
        addHandlers,
        connectionSettings,
        clusterSettings,
        onSuccess,
        onError,
        onException,
        endPointDiscoverer,
        groups,
        contentType,
        key,
    }

    #endregion

    #region -- ExceptionResource --

    /// <summary>The convention for this enum is using the resource name as the enum name</summary>
    internal enum ExceptionResource
    {
        ArgumentNull_Compiler,
        ArgumentNull_Type,
        InvalidOperation_MatchBuilder_Built,
        InvalidOperation_MatchBuilder_MatchAnyAdded,
        YouDoNotHaveAccessToTheStream,
        SubscriptionNotFound,
        Write_access_denied_for_stream,
        Read_access_denied_for_stream,
        Read_access_denied_for_all,
        Write_access_denied,
        Subscription_to_failed_due_to_access_denied,
    }

    #endregion

    partial class ThrowHelper
    {
        #region -- Exception --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_ForSuccessfulWritePassNextExpectedVersionAndLogPosition()
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception("For successful write pass next expected version and log position.");
            }
        }

        #endregion

        #region -- ArgumentException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_NotEmptyGuid(ExceptionArgument argument)
        {
            throw GetException();
            ArgumentException GetException()
            {
                var argumentName = GetArgumentName(argument);
                return new ArgumentException($"{argumentName} should be non-empty GUID.", argumentName);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_Equal(int expected, int actual, ExceptionArgument argument)
        {
            throw GetException();
            ArgumentException GetException()
            {
                var argumentName = GetArgumentName(argument);
                return new ArgumentException($"{argumentName} expected value: {expected}, actual value: {actual}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_Equal(long expected, long actual, ExceptionArgument argument)
        {
            throw GetException();
            ArgumentException GetException()
            {
                var argumentName = GetArgumentName(argument);
                return new ArgumentException($"{argumentName} expected value: {expected}, actual value: {actual}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_Equal(bool expected, bool actual, ExceptionArgument argument)
        {
            throw GetException();
            ArgumentException GetException()
            {
                var argumentName = GetArgumentName(argument);
                return new ArgumentException($"{argumentName} expected value: {expected}, actual value: {actual}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_EmptyFakeDnsEntriesCollection()
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException("Empty FakeDnsEntries collection.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_BothEndpointsAreNull()
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException("Both endpoints are null.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_TheCommitPositionCannotBeLessThanThePreparePosition()
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException("The commit position cannot be less than the prepare position", "commitPosition");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_MillisecondsMustBeLessOrEqualToThanInt32MaxValue(ExceptionArgument argument)
        {
            throw GetException();
            ArgumentException GetException()
            {
                var argumentName = GetArgumentName(argument);
                return new ArgumentException("milliseconds must be less or equal to than int32.MaxValue", argumentName);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_ReadBatchSizeShouldBeLessThanMaxReadSize()
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException($"Read batch size should be less than {ClientApiConstants.MaxReadSize}. For larger reads you should page.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_PartialActionBuilder(int MaxNumberOfArguments)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException($"Too many arguments. Max {MaxNumberOfArguments} arguments allowed.", "handlerAndArgs");
            }
        }

        #endregion

        #region -- ArgumentOutOfRangeException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentOutOfRangeException_GreaterThanOrEqualTo(long minimum, ExceptionArgument argument)
        {
            throw GetException();
            ArgumentOutOfRangeException GetException()
            {
                var argumentName = GetArgumentName(argument);
                return new ArgumentOutOfRangeException(argumentName, $"{argumentName} should be greater than or equal to {minimum}.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentOutOfRangeException_Positive(ExceptionArgument argument)
        {
            throw GetException();
            ArgumentOutOfRangeException GetException()
            {
                var argumentName = GetArgumentName(argument);
                return new ArgumentOutOfRangeException(argumentName, $"{argumentName} should be positive.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentOutOfRangeException_Nonnegative(ExceptionArgument argument)
        {
            throw GetException();
            ArgumentOutOfRangeException GetException()
            {
                var argumentName = GetArgumentName(argument);
                return new ArgumentOutOfRangeException(argumentName, $"{argumentName} should be non negative.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentOutOfRangeException_ValueIsOutOfRange(ExceptionArgument argument, int value)
        {
            throw GetException();
            ArgumentOutOfRangeException GetException()
            {
                var argumentName = GetArgumentName(argument);
                return new ArgumentOutOfRangeException(argumentName,
                    $"{argumentName} value is out of range: {value}. Allowed range: [-1, infinity].");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentOutOfRangeException_SetMaxDiscoverAttempts(int maxDiscoverAttempts)
        {
            throw GetException();
            ArgumentOutOfRangeException GetException()
            {
                return new ArgumentOutOfRangeException(nameof(maxDiscoverAttempts), $"{nameof(maxDiscoverAttempts)} value is out of range: {maxDiscoverAttempts}. Allowed range: [1, infinity].");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentOutOfRangeException_SetMaxDiscoverAttempts1(int maxDiscoverAttempts)
        {
            throw GetException();
            ArgumentOutOfRangeException GetException()
            {
                return new ArgumentOutOfRangeException(nameof(maxDiscoverAttempts), $"{nameof(maxDiscoverAttempts)} value is out of range: {maxDiscoverAttempts}. Allowed range: [-1, infinity].");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentOutOfRangeException_ExponentialBackoffMin(TimeSpan minDelay)
        {
            throw GetException();
            ArgumentOutOfRangeException GetException()
            {
                return new ArgumentOutOfRangeException(nameof(minDelay), minDelay, "ExponentialBackoff min delay must be a positive number.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentOutOfRangeException_ExponentialBackoffMax(TimeSpan maxDelay)
        {
            throw GetException();
            ArgumentOutOfRangeException GetException()
            {
                return new ArgumentOutOfRangeException(nameof(maxDelay), maxDelay, "ExponentialBackoff max delay must be a positive number.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentOutOfRangeException_ExponentialBackoffStep(TimeSpan step)
        {
            throw GetException();
            ArgumentOutOfRangeException GetException()
            {
                return new ArgumentOutOfRangeException(nameof(step), step, "ExponentialBackoff step must be a positive number.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentOutOfRangeException_ExponentialBackoffMinMax(TimeSpan minDelay)
        {
            throw GetException();
            ArgumentOutOfRangeException GetException()
            {
                return new ArgumentOutOfRangeException(nameof(minDelay), minDelay, "ExponentialBackoff min delay must be greater than max delay.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentOutOfRangeException_MatchExpressionBuilder_Add(HandlerKind kind)
        {
            throw GetException();
            ArgumentOutOfRangeException GetException()
            {
                return new ArgumentOutOfRangeException(
                    $"This should not happen. The value {typeof(HandlerKind)}.{kind} is a new enum value that has been added without updating the code in this method.");
            }
        }

        #endregion

        #region -- OverflowException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowOverflowException()
        {
            throw GetException();
            OverflowException GetException()
            {
                return new OverflowException();
            }
        }

        #endregion
    }
}
