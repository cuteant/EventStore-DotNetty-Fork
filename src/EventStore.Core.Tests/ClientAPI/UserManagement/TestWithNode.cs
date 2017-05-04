using System;
using EventStore.ClientAPI;
using EventStore.ClientAPI.UserManagement;
using EventStore.Core.Tests.ClientAPI.Helpers;
using EventStore.Core.Tests.Helpers;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace EventStore.Core.Tests.ClientAPI.UserManagement
{
    [Category("LongRunning"), Category("ClientAPI")]
    public class TestWithNode : SpecificationWithDirectoryPerTestFixture
    {
        protected MiniNode _node;
        protected UsersManager _manager;

        [OneTimeSetUp]
        public override void TestFixtureSetUp()
        {
            base.TestFixtureSetUp();
            _node = new MiniNode(PathName);
            _node.Start();
            _manager = new UsersManager(_node.ExtHttpEndPoint, TimeSpan.FromSeconds(5));
        }

        [OneTimeTearDown]
        public override void TestFixtureTearDown()
        {
            _node.Shutdown();
            base.TestFixtureTearDown();
        }


        protected virtual IEventStoreConnection BuildConnection(MiniNode node)
        {
            return TestConnection.Create(node.TcpEndPoint);
        }

    }
}