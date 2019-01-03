﻿using System;
using EventStore.Core.Data;
using EventStore.Projections.Core.Services.Processing;
using EventStore.Projections.Core.Standard;
using NUnit.Framework;
using ResolvedEvent = EventStore.Projections.Core.Services.Processing.ResolvedEvent;

namespace EventStore.Projections.Core.Tests.Services.handlers
{
    public static class categorize_events_by_correlation_id
    {
        [TestFixture]
        public class when_handling_simple_event
        {
            private ByCorrelationId _handler;
            private string _state;
            private EmittedEventEnvelope[] _emittedEvents;
            private bool _result;

            [SetUp]
            public void when()
            {
                _handler = new ByCorrelationId("", Console.WriteLine);
                _handler.Initialize();
                string sharedState;
                _result = _handler.ProcessEvent(
                    "", CheckpointTag.FromPosition(0, 200, 150), null,
                    new ResolvedEvent(
                        "cat1-stream1", 10, "cat1-stream1", 10, false, new TFPos(200, 150), Guid.NewGuid(),
                        "event_type", true, "{}", "{\"$correlationId\":\"testing1\"}"), out _state, out sharedState, out _emittedEvents);
            }

            [Test]
            public void result_is_true()
            {
                Assert.IsTrue(_result);
            }

            [Test]
            public void state_stays_null()
            {
                Assert.IsNull(_state);
            }

            [Test]
            public void emits_correct_link()
            {
                Assert.NotNull(_emittedEvents);
                Assert.AreEqual(1, _emittedEvents.Length);
                var @event = _emittedEvents[0].Event;
                Assert.AreEqual("$>", @event.EventType);
                Assert.AreEqual("$bc-testing1", @event.StreamId);
                Assert.AreEqual("10@cat1-stream1", @event.Data);
            }

        }

        [TestFixture]
        public class when_handling_link_to_event
        {
            private ByCorrelationId _handler;
            private string _state;
            private EmittedEventEnvelope[] _emittedEvents;
            private bool _result;

            [SetUp]
            public void when()
            {
                _handler = new ByCorrelationId("", Console.WriteLine);
                _handler.Initialize();
                string sharedState;
                _result = _handler.ProcessEvent(
                    "", CheckpointTag.FromPosition(0, 200, 150), null,
                    new ResolvedEvent(
                        "cat2-stream2", 20, "cat2-stream2", 20, true, new TFPos(200, 150), Guid.NewGuid(),
                        "$>", true, "10@cat1-stream1", "{\"$correlationId\":\"testing2\"}"), out _state, out sharedState, out _emittedEvents);
            }

            [Test]
            public void result_is_true()
            {
                Assert.IsTrue(_result);
            }

            [Test]
            public void state_stays_null()
            {
                Assert.IsNull(_state);
            }

            [Test]
            public void emits_correct_link()
            {
                Assert.NotNull(_emittedEvents);
                Assert.AreEqual(1, _emittedEvents.Length);
                var @event = _emittedEvents[0].Event;
                Assert.AreEqual("$>", @event.EventType);
                Assert.AreEqual("$bc-testing2", @event.StreamId);
                Assert.AreEqual("10@cat1-stream1", @event.Data);
            }
        }

        [TestFixture]
        public class when_handling_non_json_event
        {
            private ByCorrelationId _handler;
            private string _state;
            private EmittedEventEnvelope[] _emittedEvents;
            private bool _result;

            [SetUp]
            public void when()
            {
                _handler = new ByCorrelationId("", Console.WriteLine);
                _handler.Initialize();
                string sharedState;
                _result = _handler.ProcessEvent(
                    "", CheckpointTag.FromPosition(0, 200, 150), null,
                    new ResolvedEvent(
                        "cat1-stream1", 10, "cat1-stream1", 10, false, new TFPos(200, 150), Guid.NewGuid(),
                        "event_type", false, "non_json_data", "non_json_metadata"), out _state, out sharedState, out _emittedEvents);
            }

            [Test]
            public void result_is_false()
            {
                Assert.IsFalse(_result);
            }

            [Test]
            public void state_stays_null()
            {
                Assert.IsNull(_state);
            }

            [Test]
            public void does_not_emit_link()
            {
                Assert.IsNull(_emittedEvents);
            }
        }

        [TestFixture]
        public class when_handling_json_event_with_no_correlation_id
        {
            private ByCorrelationId _handler;
            private string _state;
            private EmittedEventEnvelope[] _emittedEvents;
            private bool _result;

            [SetUp]
            public void when()
            {
                _handler = new ByCorrelationId("", Console.WriteLine);
                _handler.Initialize();
                string sharedState;
                _result = _handler.ProcessEvent(
                    "", CheckpointTag.FromPosition(0, 200, 150), null,
                    new ResolvedEvent(
                        "cat1-stream1", 10, "cat1-stream1", 10, false, new TFPos(200, 150), Guid.NewGuid(),
                        "event_type", true, "{}", "{}"), out _state, out sharedState, out _emittedEvents);
            }

            [Test]
            public void result_is_false()
            {
                Assert.IsFalse(_result);
            }

            [Test]
            public void state_stays_null()
            {
                Assert.IsNull(_state);
            }

            [Test]
            public void does_not_emit_link()
            {
                Assert.IsNull(_emittedEvents);
            }
        }

        [TestFixture]
        public class when_handling_json_event_with_non_json_metadata
        {
            private ByCorrelationId _handler;
            private string _state;
            private EmittedEventEnvelope[] _emittedEvents;
            private bool _result;

            [SetUp]
            public void when()
            {
                _handler = new ByCorrelationId("", Console.WriteLine);
                _handler.Initialize();
                string sharedState;
                _result = _handler.ProcessEvent(
                    "", CheckpointTag.FromPosition(0, 200, 150), null,
                    new ResolvedEvent(
                        "cat1-stream1", 10, "cat1-stream1", 10, false, new TFPos(200, 150), Guid.NewGuid(),
                        "event_type", true, "{}", "non_json_metadata"), out _state, out sharedState, out _emittedEvents);
            }

            [Test]
            public void result_is_false()
            {
                Assert.IsFalse(_result);
            }

            [Test]
            public void state_stays_null()
            {
                Assert.IsNull(_state);
            }

            [Test]
            public void does_not_emit_link()
            {
                Assert.IsNull(_emittedEvents);
            }
        }        
    }
}
