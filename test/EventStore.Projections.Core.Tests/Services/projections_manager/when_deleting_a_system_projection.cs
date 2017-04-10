using System;
using System.Collections.Generic;
using System.Linq;
using EventStore.Core.Messages;
using EventStore.Core.Messaging;
using EventStore.Projections.Core.Messages;
using NUnit.Framework;
using EventStore.ClientAPI.Common.Utils;

namespace EventStore.Projections.Core.Tests.Services.projections_manager
{
    [TestFixture]
    public class when_deleting_a_system_projection : TestFixtureWithProjectionCoreAndManagementServices
    {
        private string _systemProjectionName;

        protected override bool GivenInitializeSystemProjections()
        {
            return true;
        }

        protected override void Given()
        {
            _systemProjectionName = "$system_projection"; //identified by the $ prefix
            AllWritesSucceed();
            NoOtherStreams();
        }

        protected override IEnumerable<WhenStep> When()
        {
            yield return new SystemMessage.BecomeMaster(Guid.NewGuid());
            yield return new SystemMessage.SystemCoreReady();
            yield return
                new ProjectionManagementMessage.Command.Disable(
                    new PublishEnvelope(_bus), _systemProjectionName, ProjectionManagementMessage.RunAs.System);
            yield return
                new ProjectionManagementMessage.Command.Delete(
                    new PublishEnvelope(_bus), _systemProjectionName,
                    ProjectionManagementMessage.RunAs.System, false, false, false);
        }

        [Test, Category("v8")]
        public void a_projection_deleted_event_is_not_written()
        {
            Assert.IsFalse(
                _consumer.HandledMessages.OfType<ClientMessage.WriteEvents>().Any(x => x.Events[0].EventType == "$ProjectionDeleted" && Helper.UTF8NoBom.GetString(x.Events[0].Data) == _systemProjectionName));
        }
    }
}
