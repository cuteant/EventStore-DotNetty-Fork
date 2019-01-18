using System;
using System.Collections.Generic;
using System.Linq;
using CuteAnt;
using CuteAnt.Extensions.Serialization;
using CuteAnt.Reflection;
using EventStore.ClientAPI.Serialization;
using Xunit;

namespace EventStore.ClientAPI.Tests
{
    public class SerializationManagerTest
    {
        public SerializationManagerTest()
        {
            SerializationManager.RegisterSerializationProvider(new JsonEventSerializer());
        }

        [Fact]
        public void GetStreamProviderTest()
        {
            var streamAttr = SerializationManager.GetStreamProvider(typeof(Dog));
            Assert.Null(streamAttr);
            streamAttr = SerializationManager.GetStreamProvider(typeof(Dog1));
            Assert.NotNull(streamAttr);
            Assert.Equal("test-animal1", streamAttr.StreamId);
            Assert.Equal("animal1", streamAttr.EventType);

            streamAttr = SerializationManager.GetStreamProvider(typeof(Dog), typeof(IAnimal));
            Assert.NotNull(streamAttr);
            Assert.Equal("test-animal", streamAttr.StreamId);
            Assert.Equal("animal", streamAttr.EventType);

            streamAttr = SerializationManager.GetStreamProvider(typeof(Cat));
            Assert.NotNull(streamAttr);
            Assert.Equal("test-animal", streamAttr.StreamId);
            Assert.Equal("cat", streamAttr.EventType);

            streamAttr = SerializationManager.GetStreamProvider(typeof(Cat), typeof(IAnimal));
            Assert.NotNull(streamAttr);
            Assert.Equal("test-animal", streamAttr.StreamId);
            Assert.Equal("animal", streamAttr.EventType);
        }

        [Fact]
        public void GetSerializationTokenTest()
        {
            var token = SerializationManager.GetSerializingToken(typeof(Dog));
            Assert.Equal(SerializingToken.MessagePack, token);
            token = SerializationManager.GetSerializingToken(typeof(Dog1));
            Assert.Equal(SerializingToken.Utf8Json, token);

            token = SerializationManager.GetSerializingToken(typeof(Dog), typeof(IAnimal));
            Assert.Equal(SerializingToken.Json, token);

            token = SerializationManager.GetSerializingToken(typeof(Cat));
            Assert.Equal(SerializingToken.MessagePack, token);

            token = SerializationManager.GetSerializingToken(typeof(Cat), typeof(IAnimal));
            Assert.Equal(SerializingToken.Json, token);
        }

        [Theory]
        [InlineData(SerializingToken.Json, "test_event_type1", 101L, "this is a test.")]
        [InlineData(SerializingToken.MessagePack, "test_event_type2", 102L, "this is a test.")]
        [InlineData(SerializingToken.Lz4MessagePack, "test_event_type3", 103L, "this is a test.")]
        [InlineData(SerializingToken.Utf8Json, "test_event_type4", 104L, "this is a test4.")]
        [InlineData(SerializingToken.TypelessMessagePack, "test_event_type5", 105L, "this is a test.")]
        [InlineData(SerializingToken.Lz4TypelessMessagePack, "test_event_type6", 106L, "this is a test.")]
        public void SerializeEventTest_CallAll_SerializationToken(SerializingToken token, string eventType, long id, string text)
        {
            var testMessage = new TestMessage { Id = id, Text = text };

            var eventData = SerializationManager.SerializeEvent(token, eventType, typeof(TestMessage), testMessage, null, null);
            Assert.Equal(eventType, eventData.Type);
            Assert.Equal(token == SerializingToken.Json, eventData.IsJson);

            var metadata = SerializationManager.DeserializeMetadata(eventData.Metadata);
            Assert.Equal(token, metadata.Token);
            Assert.Equal(RuntimeTypeNameFormatter.Serialize(typeof(TestMessage)), metadata.EventType);
            var fullEvent = SerializationManager.DeserializeEvent<TestMessage>(eventData);
            Assert.Equal(testMessage.Id, fullEvent.Value.Id);
            Assert.Equal(testMessage.Text, fullEvent.Value.Text);
        }

        [Fact]
        public void SerializeEvent_UsingExternalSerializer_Test()
        {
            var cat = new Cat1 { Name = "MyCat", Meow = "Meow testing......" };
            var context = new Dictionary<string, object>
            {
                { "Id", CombGuid.NewComb() },
                { "Id1", Guid.NewGuid() },
                { "age", 18 },
                { "FullName", "Genghis Khan" }
            };

            var eventData = SerializationManager.SerializeEvent(cat, context);
            Assert.Equal("animal1", eventData.Type);
            Assert.False(eventData.IsJson);
            var metadata = SerializationManager.DeserializeMetadata(eventData.Metadata);
            Assert.Equal(SerializingToken.External, metadata.Token);
            Assert.Equal(RuntimeTypeNameFormatter.Serialize(typeof(Cat1)), metadata.EventType);
            Assert.Equal(context["Id"].ToString(), metadata.Context["Id"].ToString());
            Assert.Equal(context["Id1"].ToString(), metadata.Context["Id1"].ToString());
            Assert.Equal(context["age"].ToString(), metadata.Context["age"].ToString());
            Assert.Equal(context["FullName"].ToString(), metadata.Context["FullName"].ToString());
            var catEvent = SerializationManager.DeserializeEvent<Cat1>(eventData);
            Assert.Equal(cat.Name, catEvent.Value.Name);
            Assert.Equal(cat.Meow, catEvent.Value.Meow);
            Assert.Equal((CombGuid)context["Id"], catEvent.Descriptor.GetValue<CombGuid>("Id"));
            Assert.Equal((Guid)context["Id1"], catEvent.Descriptor.GetValue<Guid>("Id1"));
            // TryGetValueCamelCase
            Assert.Equal((int)context["age"], catEvent.Descriptor.GetValue<int>("Age"));
            Assert.Equal((string)context["FullName"], catEvent.Descriptor.GetValue<string>("FullName"));
        }

        [Fact]
        public void SerializeEventTest()
        {
            var cat = new Cat { Name = "MyCat", Meow = "Meow testing......" };
            var dog = new Dog { Name = "MyDog", Bark = "Bark testing......" };

            var context = new Dictionary<string, object>
            {
                { "Id", CombGuid.NewComb() },
                { "Id1", Guid.NewGuid() },
                { "age", 18 },
                { "FullName", "Genghis Khan" }
            };

            var eventData = SerializationManager.SerializeEvent(cat, context);
            Assert.Equal("cat", eventData.Type);
            Assert.False(eventData.IsJson);
            var metadata = SerializationManager.DeserializeMetadata(eventData.Metadata);
            Assert.Equal(SerializingToken.MessagePack, metadata.Token);
            Assert.Equal(RuntimeTypeNameFormatter.Serialize(typeof(Cat)), metadata.EventType);
            Assert.Equal(context["Id"].ToString(), metadata.Context["Id"].ToString());
            Assert.Equal(context["Id1"].ToString(), metadata.Context["Id1"].ToString());
            Assert.Equal(context["age"].ToString(), metadata.Context["age"].ToString());
            Assert.Equal(context["FullName"].ToString(), metadata.Context["FullName"].ToString());
            var catEvent = SerializationManager.DeserializeEvent<Cat>(eventData);
            Assert.Equal(cat.Name, catEvent.Value.Name);
            Assert.Equal(cat.Meow, catEvent.Value.Meow);
            Assert.Equal((CombGuid)context["Id"], catEvent.Descriptor.GetValue<CombGuid>("Id"));
            Assert.Equal((Guid)context["Id1"], catEvent.Descriptor.GetValue<Guid>("Id1"));
            // TryGetValueCamelCase
            Assert.Equal((int)context["age"], catEvent.Descriptor.GetValue<int>("Age"));
            Assert.Equal((string)context["FullName"], catEvent.Descriptor.GetValue<string>("FullName"));

            eventData = SerializationManager.SerializeEvent(cat, expectedType: typeof(IAnimal));
            Assert.Equal("animal", eventData.Type);
            Assert.True(eventData.IsJson);
            metadata = SerializationManager.DeserializeMetadata(eventData.Metadata);
            Assert.Equal(SerializingToken.Json, metadata.Token);
            Assert.Equal(RuntimeTypeNameFormatter.Serialize(typeof(Cat)), metadata.EventType);
            catEvent = SerializationManager.DeserializeEvent<Cat>(eventData);
            Assert.Equal(cat.Name, catEvent.Value.Name);
            Assert.Equal(cat.Meow, catEvent.Value.Meow);


            eventData = SerializationManager.SerializeEvent(dog);
            Assert.Equal(RuntimeTypeNameFormatter.Serialize(typeof(Dog)), eventData.Type);
            Assert.False(eventData.IsJson);
            metadata = SerializationManager.DeserializeMetadata(eventData.Metadata);
            Assert.Equal(SerializingToken.MessagePack, metadata.Token);
            Assert.Equal(RuntimeTypeNameFormatter.Serialize(typeof(Dog)), metadata.EventType);
            var dogEvent = SerializationManager.DeserializeEvent<Dog>(eventData);
            Assert.Equal(dog.Name, dogEvent.Value.Name);
            Assert.Equal(dog.Bark, dogEvent.Value.Bark);

            eventData = SerializationManager.SerializeEvent(dog, expectedType: typeof(IAnimal));
            Assert.Equal("animal", eventData.Type);
            Assert.True(eventData.IsJson);
            metadata = SerializationManager.DeserializeMetadata(eventData.Metadata);
            Assert.Equal(SerializingToken.Json, metadata.Token);
            Assert.Equal(RuntimeTypeNameFormatter.Serialize(typeof(Dog)), metadata.EventType);
            dogEvent = SerializationManager.DeserializeEvent<Dog>(eventData);
            Assert.Equal(dog.Name, dogEvent.Value.Name);
            Assert.Equal(dog.Bark, dogEvent.Value.Bark);
        }

        [Fact]
        public void SerializeEvents_SingleType_SingleContext()
        {
            var events = new List<TestMessage>
            {
                new TestMessage { Id = 10L, Text = "One testing......" },
                new TestMessage { Id = 11L, Text = "Two testing......" }
            };
            var context = new Dictionary<string, object> { { "Id", CombGuid.NewComb() } };
            var eventDatas = SerializationManager.SerializeEvents(events, context);
            var fullEvents = eventDatas.Select(_ => SerializationManager.DeserializeEvent<TestMessage>(_)).ToArray();

            Assert.Equal(10L, fullEvents[0].Value.Id);
            Assert.Equal("One testing......", fullEvents[0].Value.Text);
            Assert.Equal(context["Id"], fullEvents[0].Descriptor.GetValue<CombGuid>("Id"));

            Assert.Equal(11L, fullEvents[1].Value.Id);
            Assert.Equal("Two testing......", fullEvents[1].Value.Text);
            Assert.Equal(context["Id"], fullEvents[1].Descriptor.GetValue<CombGuid>("Id"));
        }

        [Fact]
        public void SerializeEvents_MultiType_SingleContext()
        {
            var events = new List<object>
            {
                new StartMessage { Text = "string......" },
                new EndMessage { Text = "ending......" }
            };
            var context = new Dictionary<string, object> { { "Id", CombGuid.NewComb() } };
            var eventDatas = SerializationManager.SerializeEvents(events, context);
            var fullEvents = eventDatas.Select(_ => SerializationManager.DeserializeEvent(_)).ToArray();

            Assert.Equal("string......", ((StartMessage)fullEvents[0].Value).Text);
            Assert.Equal(context["Id"], fullEvents[0].Descriptor.GetValue<CombGuid>("Id"));

            Assert.Equal("ending......", ((EndMessage)fullEvents[1].Value).Text);
            Assert.Equal(context["Id"], fullEvents[1].Descriptor.GetValue<CombGuid>("Id"));
        }

        [Fact]
        public void SerializeEvents_SingleType_MultiContext()
        {
            var events = new List<TestMessage>
            {
                new TestMessage { Id = 10L, Text = "One testing......" },
                new TestMessage { Id = 11L, Text = "Two testing......" }
            };
            var contexts = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object> { { "Id", CombGuid.NewComb() } },
                new Dictionary<string, object> { { "Id1", Guid.NewGuid() } }
            };
            var eventDatas = SerializationManager.SerializeEvents(events, contexts);
            var fullEvents = eventDatas.Select(_ => SerializationManager.DeserializeEvent<TestMessage>(_)).ToArray();

            Assert.Equal(10L, fullEvents[0].Value.Id);
            Assert.Equal("One testing......", fullEvents[0].Value.Text);
            Assert.Equal(contexts[0]["Id"], fullEvents[0].Descriptor.GetValue<CombGuid>("Id"));

            Assert.Equal(11L, fullEvents[1].Value.Id);
            Assert.Equal("Two testing......", fullEvents[1].Value.Text);
            Assert.Equal(contexts[1]["Id1"], fullEvents[1].Descriptor.GetValue<Guid>("Id1"));
        }

        [Fact]
        public void SerializeEvents_MultiType_MultiContext()
        {
            var events = new List<object>
            {
                new StartMessage { Text = "string......" },
                new EndMessage { Text = "ending......" }
            };
            var contexts = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object> { { "Id", CombGuid.NewComb() } },
                new Dictionary<string, object> { { "Id1", Guid.NewGuid() } }
            };
            var eventDatas = SerializationManager.SerializeEvents(events, contexts);
            var fullEvents = eventDatas.Select(_ => SerializationManager.DeserializeEvent(_)).ToArray();

            Assert.Equal("string......", ((StartMessage)fullEvents[0].Value).Text);
            Assert.Equal(contexts[0]["Id"], fullEvents[0].Descriptor.GetValue<CombGuid>("Id"));

            Assert.Equal("ending......", ((EndMessage)fullEvents[1].Value).Text);
            Assert.Equal(contexts[1]["Id1"], fullEvents[1].Descriptor.GetValue<Guid>("Id1"));
        }
    }
}
