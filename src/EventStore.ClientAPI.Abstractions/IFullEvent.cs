namespace EventStore.ClientAPI
{
  public interface IFullEvent : IFullEvent<object> { }

  public interface IFullEvent<T> where T : class
  {
    IEventDescriptor Descriptor { get; }

    T Value { get; }
  }
}