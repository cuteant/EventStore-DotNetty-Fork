namespace EventStore.ClientAPI
{
    public interface IEventAdapterProvider
    {
        IEventAdapter Create();
    }
}
