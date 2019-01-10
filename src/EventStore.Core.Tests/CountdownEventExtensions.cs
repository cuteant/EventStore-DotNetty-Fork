
namespace System.Threading
{
    static class CountdownEventExtensions
    {
        public static bool SafeSignal(this CountdownEvent @event)
        {
#if NETCOREAPP
            if (@event.IsSet) { return true; }
#endif
            return @event.Signal();
        }
    }
}
