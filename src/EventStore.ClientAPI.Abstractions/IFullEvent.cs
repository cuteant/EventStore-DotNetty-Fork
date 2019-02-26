namespace EventStore.ClientAPI
{
    public interface IFullEvent : IFullEvent<object> { }

    public interface IFullEvent<out T>
    {
        IEventDescriptor Descriptor { get; }

        T Value { get; }
    }
}