using System;
using EventStore.ClientAPI.Serialization;
using MessagePack;

namespace EventStore.ClientAPI.Tests
{
  [EventSerializingToken(EventSerializingToken.Json)]
  [Stream("test-animal", "animal")]
  [Union(0, typeof(Cat))]
  [Union(1, typeof(Dog))]
  [Union(2, typeof(Cat1))]
  [Union(3, typeof(Dog1))]
  public interface IAnimal
  {
    string Name { get; set; }
  }

  [EventSerializingToken(EventSerializingToken.MessagePack)]
  [MessagePackObject]
  [Stream("test-animal", "cat")]
  public class Cat : IAnimal
  {
    [Key(0)]
    public string Name { get; set; }
    [Key(1)]
    public string Meow { get; set; }
  }

  [MessagePackObject]
  public class Dog : IAnimal
  {
    [Key(0)]
    public string Name { get; set; }
    [Key(1)]
    public string Bark { get; set; }
  }

  [EventSerializingToken(EventSerializingToken.Utf8Json)]
  [Stream("test-animal1", "animal1")]
  [MessagePackObject]
  [Union(0, typeof(Cat1))]
  [Union(1, typeof(Dog1))]
  public abstract class Animal : IAnimal
  {
    [Key(0)]
    public virtual string Name { get; set; }
  }

  [EventSerializingToken(EventSerializingToken.External)]
  [MessagePackObject]
  public class Cat1 : Animal
  {
    [Key(1)]
    public string Meow { get; set; }
  }

  [MessagePackObject]
  public class Dog1 : Animal
  {
    [Key(1)]
    public string Bark { get; set; }
  }

  [EventSerializingToken(EventSerializingToken.MessagePack)]
  [Stream("test-message")]
  [MessagePackObject]
  public class StartMessage
  {
    [Key(0)]
    public string Text { get; set; }
  }

  [Stream("test-message")]
  [EventSerializingToken(EventSerializingToken.Lz4MessagePack)]
  [MessagePackObject]
  public class EndMessage
  {
    [Key(0)]
    public string Text { get; set; }
  }

  [EventSerializingToken(EventSerializingToken.Json)]
  [MessagePackObject]
  public class TestMessage
  {
    [Key(0)]
    public long Id { get; set; }
    [Key(1)]
    public string Text { get; set; }
  }

  public class MyMessage
  {
    public string Text { get; set; }
  }

  public class MyOtherMessage
  {
    public string Text { get; set; }
  }
}
