using System;
using System.Runtime.CompilerServices;
#if DESKTOPCLR
using System.Runtime.InteropServices;
#endif
namespace EventStore.Common
{
    #region -- ExceptionArgument --

    /// <summary>The convention for this enum is using the argument name as the enum name</summary>
    internal enum ExceptionArgument
    {
        exitAction,
        reason,
        define,
    }

    #endregion

    #region -- ExceptionResource --

    /// <summary>The convention for this enum is using the resource name as the enum name</summary>
    internal enum ExceptionResource
    {
    }

    #endregion

    partial class ThrowHelper
    {
        #region -- Exception --

#if DESKTOPCLR
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_FlushFileBuffersFailed()
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception($"FlushFileBuffers failed with err: {Marshal.GetLastWin32Error()}");
            }
        }
#endif

        #endregion

        #region -- ArgumentException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_NotEmptyGuid(ExceptionArgument argument)
        {
            throw GetException();
            ArgumentException GetException()
            {
                var argumentName = GetArgumentName(argument);
                return new ArgumentException(argumentName, $"{argumentName} should be non-empty GUID.");
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

        #endregion

        #region -- ArgumentOutOfRangeException --

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

        #endregion
    }
}
