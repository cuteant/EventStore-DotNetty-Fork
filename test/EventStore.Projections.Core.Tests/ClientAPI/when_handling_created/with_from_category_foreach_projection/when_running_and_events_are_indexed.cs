﻿using NUnit.Framework;

namespace EventStore.Projections.Core.Tests.ClientAPI.when_handling_created.with_from_category_foreach_projection
{
    [TestFixture]
    public class when_running_and_events_are_indexed : specification_with_standard_projections_runnning
    {
        protected override bool GivenStandardProjectionsRunning()
        {
            return false;
        }

        protected override void Given()
        {
            base.Given();
            PostEvent("stream-1", "type1", "{}");
            PostEvent("stream-1", "type2", "{}");
            PostEvent("stream-2", "type1", "{}");
            PostEvent("stream-2", "type2", "{}");
            WaitIdle();
            EnableStandardProjections();
        }

        protected override void When()
        {
            base.When();
            PostProjection(@"
fromCategory('stream').foreachStream().when({
    $init: function(){return {a:0}},
    type1: function(s,e){s.a++;},
    type2: function(s,e){s.a++;},
    $created: function(s,e){s.a++;},
}).outputState();
");
            WaitIdle();
        }

        [Test, Category("Network")]
        public void receives_deleted_notification()
        {
            AssertStreamTail("$projections-test-projection-stream-1-result", "Result:{\"a\":3}");
            AssertStreamTail("$projections-test-projection-stream-2-result", "Result:{\"a\":3}");
        }
    }
}
