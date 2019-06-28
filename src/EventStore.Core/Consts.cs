namespace EventStore.Core
{
    internal static class Consts
    {
        public const uint TooBigOrNegative = int.MaxValue;
        public const ulong TooBigOrNegativeUL = long.MaxValue;

        public const ulong MaxEventNumber = unchecked((ulong)-1);
    }
}