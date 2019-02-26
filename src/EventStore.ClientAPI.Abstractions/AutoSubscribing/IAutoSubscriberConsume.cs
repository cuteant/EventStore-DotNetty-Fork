namespace EventStore.ClientAPI.AutoSubscribing
{
    public interface IAutoSubscriberConsume<in T>
    {
        void Consume(T message);
    }
}