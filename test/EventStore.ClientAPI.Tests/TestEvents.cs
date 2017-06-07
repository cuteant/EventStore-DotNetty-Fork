using System;
using EventStore.ClientAPI.Serialization;
using ProtoBuf;

namespace EventStore.ClientAPI.Tests
{
  [SerializationToken(SerializationToken.Json)]
  [Stream("test-animal", "animal")]
  public interface IAnimal
  {
    string Name { get; set; }
  }

  [SerializationToken(SerializationToken.Hyperion)]
  [ProtoContract]
  [Stream("test-animal", "cat")]
  public class Cat : IAnimal
  {
    [ProtoMember(1)]
    public string Name { get; set; }
    [ProtoMember(2)]
    public string Meow { get; set; }
  }

  [ProtoContract]
  public class Dog : IAnimal
  {
    [ProtoMember(1)]
    public string Name { get; set; }
    [ProtoMember(2)]
    public string Bark { get; set; }
  }

  [SerializationToken(SerializationToken.Lz4Json)]
  [Stream("test-animal1", "animal1")]
  public abstract class Animal : IAnimal
  {
    public virtual string Name { get; set; }
  }

  [SerializationToken(SerializationToken.External)]
  public class Cat1 : Animal
  {
    public string Meow { get; set; }
  }

  public class Dog1 : Animal
  {
    public string Bark { get; set; }
  }

  [SerializationToken(SerializationToken.GzJson)]
  [Stream("test-message")]
  [ProtoContract]
  public class StartMessage
  {
    [ProtoMember(1)]
    public string Text { get; set; }
  }

  [Stream("test-message")]
  [SerializationToken(SerializationToken.Lz4Json)]
  [ProtoContract]
  public class EndMessage
  {
    [ProtoMember(1)]
    public string Text { get; set; }
  }

  [SerializationToken(SerializationToken.Json)]
  [ProtoContract]
  public class TestMessage
  {
    [ProtoMember(1)]
    public long Id { get; set; }
    [ProtoMember(2)]
    public string Text { get; set; }
  }
}
