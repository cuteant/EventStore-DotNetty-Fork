using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EventStore.Core
{
    #region -- ExceptionArgument --

    /// <summary>The convention for this enum is using the argument name as the enum name</summary>
    internal enum ExceptionArgument
    {
        array,
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

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_InvalidHandle()
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception("Invalid handle");
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

        #region -- Win32Exception --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowWin32Exception()
        {
            throw GetException();
            Win32Exception GetException()
            {
                return new Win32Exception();
            }
        }

        #endregion
    }
}
