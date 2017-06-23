using System;
using EventStore.ClientAPI.Exceptions;
using EventStore.ClientAPI.Internal;
using Xunit;

namespace EventStore.ClientAPI.Tests
{
  public class HandlerCollectionTests
  {
    private IHandlerCollection handlerCollection;

    private bool myMessageHandlerExecuted = false;
    private bool animalHandlerExecuted = false;

    public HandlerCollectionTests()
    {
      handlerCollection = new DefaultHandlerCollection();

      handlerCollection.Add<MyMessage>(message => myMessageHandlerExecuted = true);
      handlerCollection.Add<IAnimal>(message => animalHandlerExecuted = true);
    }

    [Fact]
    public void Should_return_matching_handler()
    {
      var handler = handlerCollection.GetHandler<MyMessage>();

      var recordedEvent = Create(new MyMessage());
      handler(recordedEvent);
      Assert.True(myMessageHandlerExecuted);
    }

    [Fact]
    public void Should_return_supertype_handler()
    {
      var handler = handlerCollection.GetHandler<Dog>();

      handler(Create(new Dog()));
      Assert.True(animalHandlerExecuted);
    }

    [Fact]
    public void Should_throw_if_handler_is_not_found()
    {
      Assert.Throws<EventStoreHandlerException>(() =>
      {
        handlerCollection.GetHandler<MyOtherMessage>();
      });
    }

    [Fact]
    public void Should_return_matching_handler_by_type()
    {
      var handler = handlerCollection.GetHandler(typeof(MyMessage));

      handler(Create(new MyMessage()));
      Assert.True(myMessageHandlerExecuted);
    }

    [Fact]
    public void Should_return_supertype_handler_by_type()
    {
      var handler = handlerCollection.GetHandler(typeof(Dog));

      handler(Create(new Dog()));
      Assert.True(animalHandlerExecuted);
    }

    [Fact]
    public void Should_return_a_null_logger_if_ThrowOnNoMatchingHandler_is_false()
    {
      handlerCollection.ThrowOnNoMatchingHandler = false;
      var handler = handlerCollection.GetHandler<MyOtherMessage>();

      handler(Create(new MyOtherMessage()));
      Assert.False(myMessageHandlerExecuted);
      Assert.False(animalHandlerExecuted);
    }

    [Fact]
    public void Should_not_be_able_to_register_multiple_handlers_for_the_same_type()
    {
      Assert.Throws<EventStoreHandlerException>(() =>
      {
        handlerCollection.Add<MyMessage>(message => { });
      });
    }

    private static ResolvedEvent<T> Create<T>(T body) where T : class
    {
      var fullEvent = new DefaultFullEvent<T>() { Descriptor = NullEventDescriptor.Instance, Value = body };
      var recordEvent = new RecordedEvent<T>("id", Guid.NewGuid(), 1, "eventType", null, null, fullEvent, true);
      return new ResolvedEvent<T>(recordEvent, null, null);
    }
  }
}
