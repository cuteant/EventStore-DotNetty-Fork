namespace EventStore.Core.Index
{
    public readonly struct IndexKey
    {
        public readonly string StreamId;
        public readonly long Version;
        public readonly long Position;
        public readonly ulong Hash;
        public IndexKey(string streamId, long version, long position) : this(streamId, version, position, 0) { }
        public IndexKey(string streamId, long version, long position, ulong hash)
        {
            StreamId = streamId;
            Version = version;
            Position = position;

            Hash = hash;
        }
    }
}
