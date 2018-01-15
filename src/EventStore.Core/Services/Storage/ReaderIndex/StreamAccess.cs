namespace EventStore.Core.Services.Storage.ReaderIndex
{
    public readonly struct StreamAccess
    {
        public readonly bool Granted;
        public readonly bool Public;

        public StreamAccess(bool granted)
        {
            Granted = granted;
            Public = false;
        }

        public StreamAccess(bool granted, bool @public)
        {
            Granted = granted;
            Public = @public;
        }
    }
}