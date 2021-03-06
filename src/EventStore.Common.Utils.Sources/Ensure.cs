﻿#if !CLIENTAPI
using System;
using System.Runtime.CompilerServices;

namespace EventStore.Common.Utils
{
  internal static class Ensure
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void NotNull<T>(T argument, string argumentName) where T : class
    {
      if (argument == null)
        throw new ArgumentNullException(argumentName);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void NotNullOrEmpty(string argument, string argumentName)
    {
      if (string.IsNullOrEmpty(argument))
        throw new ArgumentNullException(argument, argumentName);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Positive(int number, string argumentName)
    {
      if (number <= 0)
        throw new ArgumentOutOfRangeException(argumentName, argumentName + " should be positive.");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Positive(long number, string argumentName)
    {
      if (number <= 0)
        throw new ArgumentOutOfRangeException(argumentName, argumentName + " should be positive.");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Nonnegative(long number, string argumentName)
    {
      if (number < 0)
        throw new ArgumentOutOfRangeException(argumentName, argumentName + " should be non negative.");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Nonnegative(int number, string argumentName)
    {
      if (number < 0)
        throw new ArgumentOutOfRangeException(argumentName, argumentName + " should be non negative.");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void NotEmptyGuid(Guid guid, string argumentName)
    {
      if (Guid.Empty == guid)
        throw new ArgumentException(argumentName, argumentName + " should be non-empty GUID.");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Equal(int expected, int actual, string argumentName)
    {
      if (expected != actual)
        throw new ArgumentException($"{argumentName} expected value: {expected}, actual value: {actual}");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Equal(long expected, long actual, string argumentName)
    {
      if (expected != actual)
        throw new ArgumentException($"{argumentName} expected value: {expected}, actual value: {actual}");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Equal(bool expected, bool actual, string argumentName)
    {
      if (expected != actual)
        throw new ArgumentException($"{argumentName} expected value: {expected}, actual value: {actual}");
    }
  }
}
#endif