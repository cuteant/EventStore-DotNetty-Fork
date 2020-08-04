using System;

namespace EventStore.Transport.Tcp
{
    internal static class Helper
    {
        public static void EatException(Action action)
        {
            try
            {
                action();
            }
            catch (Exception)
            {
            }
        }

        public static T EatException<T>(Func<T> func, T defaultValue = default(T))
        {
            if (func is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.func); }
            try
            {
                return func();
            }
            catch (Exception)
            {
                return defaultValue;
            }
        }
    }
}
