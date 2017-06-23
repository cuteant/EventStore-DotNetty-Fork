
namespace EventStore.ClientAPI
{
  public interface IHandlerCollectionFactory
  {
    IHandlerCollection CreateHandlerCollection(string stream);

    IHandlerCollection CreateHandlerCollection<TEvent>() where TEvent : class;
  }
}