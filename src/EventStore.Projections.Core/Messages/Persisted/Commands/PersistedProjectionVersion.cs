using EventStore.Projections.Core.Services.Processing;

namespace EventStore.Projections.Core.Messages.Persisted.Commands
{
    public readonly struct PersistedProjectionVersion
    {
        public readonly long Id;
        public readonly long Epoch;
        public readonly long Version;

        public PersistedProjectionVersion(long id, long epoch, long version)
        {
            Id = id;
            Epoch = epoch;
            Version = version;
        }

        public static implicit operator ProjectionVersion(PersistedProjectionVersion source)
        {
            return new ProjectionVersion(source.Id, source.Epoch, source.Version);
        }

        public static implicit operator PersistedProjectionVersion(ProjectionVersion source)
        {
            return new PersistedProjectionVersion(source.ProjectionId, source.Epoch, source.Version);
        }
    }
}