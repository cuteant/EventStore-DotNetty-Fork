namespace EventStore.ClientAPI
{
    /// <summary>IEventDescriptor</summary>
    public interface IEventDescriptor
    {
        IEventMetadata Metadata { get; }

        TMeta AsMetaData<TMeta>() where TMeta : IEventMetadata;

        T GetValue<T>(string key);
        T GetValue<T>(string key, T defaultValue);
        bool TryGetValue<T>(string key, out T value);
    }
}
