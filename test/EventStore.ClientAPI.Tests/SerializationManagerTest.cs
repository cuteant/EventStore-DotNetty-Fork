using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CuteAnt;
using CuteAnt.Reflection;
using EventStore.ClientAPI.Internal;
using Xunit;
using Newtonsoft.Json;

namespace EventStore.ClientAPI.Tests
{
    public class SerializationManagerTest
    {
        [Fact]
        public void GetStreamProviderTest()
        {
            var streamId = EventManager.GetStreamId<Dog>();
            Assert.Equal(RuntimeTypeNameFormatter.Serialize(typeof(Dog)), streamId);

            streamId = EventManager.GetStreamId<Dog1>();
            Assert.Equal("test-animal1", streamId);

            streamId = EventManager.GetStreamId<IAnimal>();
            Assert.Equal("test-animal", streamId);
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

            IEventAdapter eventAdapter = DefaultEventAdapter.Instance;
            var eventData = eventAdapter.Adapt(cat, eventAdapter.ToEventMetadata(context));
            Assert.Equal("cat1", eventData.Type);
            Assert.True(eventData.IsJson);
            var metadata = eventAdapter.ToEventMetadata(eventData.Metadata);
            Assert.Equal(context["Id"].ToString(), metadata.Context["Id"].ToString());
            Assert.Equal(context["Id1"].ToString(), metadata.Context["Id1"].ToString());
            Assert.Equal(context["age"].ToString(), metadata.Context["age"].ToString());
            Assert.Equal(context["FullName"].ToString(), metadata.Context["FullName"].ToString());
            var catEvent = eventAdapter.FromEventData<Cat1>(eventData);
            Assert.Equal(cat.Name, catEvent.Value.Name);
            Assert.Equal(cat.Meow, catEvent.Value.Meow);
            Assert.Equal((CombGuid)context["Id"], catEvent.Descriptor.GetValue<CombGuid>("Id"));
            Assert.Equal((Guid)context["Id1"], catEvent.Descriptor.GetValue<Guid>("Id1"));
            // TryGetValueCamelCase
            Assert.Equal((int)context["age"], catEvent.Descriptor.GetValue<int>("Age"));
            Assert.Equal((string)context["FullName"], catEvent.Descriptor.GetValue<string>("FullName"));
        }

        [Fact]
        public void SerializeEvent_JsonConvert_Test()
        {
            var cat = new Cat1 { Name = "MyCat", Meow = "Meow testing......" };
            var context = new Dictionary<string, object>
            {
                { "Id", CombGuid.NewComb() },
                { "Id1", Guid.NewGuid() },
                { "age", 18 },
                { "FullName", "Genghis Khan" }
            };

            IEventAdapter eventAdapter = DefaultEventAdapter.Instance;
            var eventData = eventAdapter.Adapt(cat, eventAdapter.ToEventMetadata(context));
            Assert.Equal("cat1", eventData.Type);
            Assert.True(eventData.IsJson);

            var eventMetaJson = Encoding.UTF8.GetString(eventData.Metadata);
            var metadata = JsonConvert.DeserializeObject<EventMetadata>(eventMetaJson);
            Assert.Equal(context["Id"].ToString(), metadata.Context["Id"].ToString());
            Assert.Equal(context["Id1"].ToString(), metadata.Context["Id1"].ToString());
            Assert.Equal(context["age"].ToString(), metadata.Context["age"].ToString());
            Assert.Equal(context["FullName"].ToString(), metadata.Context["FullName"].ToString());
            var eventJson = Encoding.UTF8.GetString(eventData.Data);
            var catEvent = JsonConvert.DeserializeObject<Cat>(eventJson);
            Assert.Equal(cat.Name, catEvent.Name);
            Assert.Equal(cat.Meow, catEvent.Meow);
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

            IEventAdapter eventAdapter = DefaultEventAdapter.Instance;
            var eventData = eventAdapter.Adapt(cat, eventAdapter.ToEventMetadata(context));
            Assert.Equal("cat", eventData.Type);
            Assert.True(eventData.IsJson);
            var metadata = eventAdapter.ToEventMetadata(eventData.Metadata);
            Assert.Equal(context["Id"].ToString(), metadata.Context["Id"].ToString());
            Assert.Equal(context["Id1"].ToString(), metadata.Context["Id1"].ToString());
            Assert.Equal(context["age"].ToString(), metadata.Context["age"].ToString());
            Assert.Equal(context["FullName"].ToString(), metadata.Context["FullName"].ToString());
            var catEvent = eventAdapter.FromEventData<Cat>(eventData);
            Assert.Equal(cat.Name, catEvent.Value.Name);
            Assert.Equal(cat.Meow, catEvent.Value.Meow);
            Assert.Equal((CombGuid)context["Id"], catEvent.Descriptor.GetValue<CombGuid>("Id"));
            Assert.Equal((Guid)context["Id1"], catEvent.Descriptor.GetValue<Guid>("Id1"));
            // TryGetValueCamelCase
            Assert.Equal((int)context["age"], catEvent.Descriptor.GetValue<int>("Age"));
            Assert.Equal((string)context["FullName"], catEvent.Descriptor.GetValue<string>("FullName"));

            eventData = eventAdapter.Adapt(cat, null);
            Assert.Equal("cat", eventData.Type);
            Assert.True(eventData.IsJson);
            metadata = eventAdapter.ToEventMetadata(eventData.Metadata);
            catEvent = eventAdapter.FromEventData<Cat>(eventData);
            Assert.Equal(cat.Name, catEvent.Value.Name);
            Assert.Equal(cat.Meow, catEvent.Value.Meow);


            eventData = eventAdapter.Adapt(dog, null);
            Assert.Equal("dog", eventData.Type);
            Assert.True(eventData.IsJson);
            metadata = eventAdapter.ToEventMetadata(eventData.Metadata);
            var dogEvent = eventAdapter.FromEventData<Dog>(eventData);
            Assert.Equal(dog.Name, dogEvent.Value.Name);
            Assert.Equal(dog.Bark, dogEvent.Value.Bark);

            eventData = eventAdapter.Adapt(dog, null);
            Assert.Equal("dog", eventData.Type);
            Assert.True(eventData.IsJson);
            metadata = eventAdapter.ToEventMetadata(eventData.Metadata);
            dogEvent = eventAdapter.FromEventData<Dog>(eventData);
            Assert.Equal(dog.Name, dogEvent.Value.Name);
            Assert.Equal(dog.Bark, dogEvent.Value.Bark);
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
            IEventAdapter eventAdapter = DefaultEventAdapter.Instance;
            var eventDatas = EventManager.ToEventDatas(eventAdapter, events, contexts);
            var fullEvents = eventDatas.Select(_ => eventAdapter.FromEventData<TestMessage>(_)).ToArray();

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
            IEventAdapter eventAdapter = DefaultEventAdapter.Instance;
            var eventDatas = EventManager.ToEventDatas(eventAdapter, events, contexts);
            var fullEvents = eventDatas.Select(_ => eventAdapter.FromEventData(_)).ToArray();

            Assert.Equal("string......", ((StartMessage)fullEvents[0].Value).Text);
            Assert.Equal(contexts[0]["Id"], fullEvents[0].Descriptor.GetValue<CombGuid>("Id"));

            Assert.Equal("ending......", ((EndMessage)fullEvents[1].Value).Text);
            Assert.Equal(contexts[1]["Id1"], fullEvents[1].Descriptor.GetValue<Guid>("Id1"));
        }
    }
}
