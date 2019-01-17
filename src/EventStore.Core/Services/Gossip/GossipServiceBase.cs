using System;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using EventStore.Common.Utils;
using EventStore.Core.Bus;
using EventStore.Core.Cluster;
using EventStore.Core.Data;
using EventStore.Core.Messages;
using EventStore.Core.Messaging;
using EventStore.Core.Services.TimerService;

namespace EventStore.Core.Services.Gossip
{
    public abstract class GossipServiceBase :
        IHandle<SystemMessage.SystemInit>,
        IHandle<GossipMessage.RetrieveGossipSeedSources>,
        IHandle<GossipMessage.GotGossipSeedSources>,
        IHandle<GossipMessage.Gossip>,
        IHandle<GossipMessage.GossipReceived>,
        IHandle<SystemMessage.StateChangeMessage>,
        IHandle<GossipMessage.GossipSendFailed>,
        IHandle<SystemMessage.VNodeConnectionLost>,
        IHandle<SystemMessage.VNodeConnectionEstablished>
    {
        private static readonly TimeSpan DnsRetryTimeout = TimeSpan.FromMilliseconds(1000);
        private static readonly TimeSpan GossipStartupInterval = TimeSpan.FromMilliseconds(100);
        private static readonly TimeSpan DeadMemberRemovalTimeout = TimeSpan.FromMinutes(30);

        private static readonly ILogger Log = TraceLogger.GetLogger<GossipServiceBase>();

        protected readonly VNodeInfo NodeInfo;
        protected VNodeState CurrentRole = VNodeState.Initializing;
        protected VNodeInfo CurrentMaster;
        private readonly TimeSpan GossipInterval = TimeSpan.FromMilliseconds(1000);
        private readonly TimeSpan AllowedTimeDifference = TimeSpan.FromMinutes(30);

        private readonly IPublisher _bus;
        private readonly IEnvelope _publishEnvelope;
        private readonly IGossipSeedSource _gossipSeedSource;

        private GossipState _state;
        private ClusterInfo _cluster;
        private readonly Random _rnd = new Random(Math.Abs(Environment.TickCount));

        protected GossipServiceBase(IPublisher bus,
                                    IGossipSeedSource gossipSeedSource,
                                    VNodeInfo nodeInfo,
                                    TimeSpan gossipInterval,
                                    TimeSpan allowedTimeDifference)
        {
            if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }
            if (null == gossipSeedSource) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.gossipSeedSource); }
            if (null == nodeInfo) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.nodeInfo); }

            _bus = bus;
            _publishEnvelope = new PublishEnvelope(bus);
            _gossipSeedSource = gossipSeedSource;
            NodeInfo = nodeInfo;
            GossipInterval = gossipInterval;
            AllowedTimeDifference = allowedTimeDifference;
            _state = GossipState.Startup;
        }

        protected abstract MemberInfo GetInitialMe();
        protected abstract MemberInfo GetUpdatedMe(MemberInfo me);

        public void Handle(SystemMessage.SystemInit message)
        {
            if (_state != GossipState.Startup) { return; }
            _cluster = new ClusterInfo(GetInitialMe());
            Handle(new GossipMessage.RetrieveGossipSeedSources());
        }

        public void Handle(GossipMessage.RetrieveGossipSeedSources message)
        {
            _state = GossipState.RetrievingGossipSeeds;
            try
            {
                //_gossipSeedSource.BeginGetHostEndpoints(OnGotGossipSeedSources, null);
                var entries = _gossipSeedSource.GetHostEndpoints();
                _bus.Publish(new GossipMessage.GotGossipSeedSources(entries));
            }
            catch (Exception ex)
            {
                Log.ErrorWhileRetrievingClusterMembersThroughDNS(ex);
                _bus.Publish(TimerMessage.Schedule.Create(DnsRetryTimeout, _publishEnvelope, new GossipMessage.RetrieveGossipSeedSources()));
            }
        }

        //private void OnGotGossipSeedSources(IAsyncResult ar)
        //{
        //    try
        //    {
        //        var entries = _gossipSeedSource.EndGetHostEndpoints(ar);
        //        _bus.Publish(new GossipMessage.GotGossipSeedSources(entries));
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.ErrorWhileRetrievingClusterMembersThroughDNS(ex);
        //        _bus.Publish(TimerMessage.Schedule.Create(DnsRetryTimeout, _publishEnvelope, new GossipMessage.RetrieveGossipSeedSources()));
        //    }
        //}

        public void Handle(GossipMessage.GotGossipSeedSources message)
        {
            var now = DateTime.UtcNow;
            var dnsCluster = new ClusterInfo(
                message.GossipSeeds.Select(x => MemberInfo.ForManager(Guid.Empty, now, true, x, x)).ToArray());

            var oldCluster = _cluster;
            _cluster = MergeClusters(_cluster, dnsCluster, null, x => x);
            if (Log.IsTraceLevelEnabled()) LogClusterChange(oldCluster, _cluster, null);

            _state = GossipState.Working;
            Handle(new GossipMessage.Gossip(0)); // start gossiping
        }

        public void Handle(GossipMessage.Gossip message)
        {
            if (_state != GossipState.Working) { return; }

            var node = GetNodeToGossipTo(_cluster.Members);
            if (node != null)
            {
                _cluster = UpdateCluster(_cluster, x => x.InstanceId == NodeInfo.InstanceId ? GetUpdatedMe(x) : x);
                _bus.Publish(new HttpMessage.SendOverHttp(node.InternalHttpEndPoint, new GossipMessage.SendGossip(_cluster, NodeInfo.InternalHttp), DateTime.Now.Add(GossipInterval)));
            }

            var interval = message.GossipRound < 20 ? GossipStartupInterval : GossipInterval;
            var gossipRound = Math.Min(2000000000, node == null ? message.GossipRound : message.GossipRound + 1);
            _bus.Publish(TimerMessage.Schedule.Create(interval, _publishEnvelope, new GossipMessage.Gossip(gossipRound)));
        }

        private MemberInfo GetNodeToGossipTo(MemberInfo[] members)
        {
            if (members.Length == 0) { return null; }
            for (int i = 0; i < 5; ++i)
            {
                var node = members[_rnd.Next(members.Length)];
                if (node.InstanceId != NodeInfo.InstanceId) { return node; }
            }
            return null;
        }

        public void Handle(GossipMessage.GossipReceived message)
        {
            if (_state != GossipState.Working) { return; }

            var oldCluster = _cluster;
            _cluster = MergeClusters(_cluster,
                                     message.ClusterInfo,
                                     message.Server,
                                     x => x.InstanceId == NodeInfo.InstanceId ? GetUpdatedMe(x) : x);

            message.Envelope.ReplyWith(new GossipMessage.SendGossip(_cluster, NodeInfo.InternalHttp));

            if (Log.IsTraceLevelEnabled() && _cluster.HasChangedSince(oldCluster))
            {
                LogClusterChange(oldCluster, _cluster, $"gossip received from [{message.Server}]");
            }
            _bus.Publish(new GossipMessage.GossipUpdated(_cluster));
        }

        public void Handle(SystemMessage.StateChangeMessage message)
        {
            CurrentRole = message.State;
            var replicaState = message as SystemMessage.ReplicaStateMessage;
            CurrentMaster = replicaState?.Master;
            _cluster = UpdateCluster(_cluster, x => x.InstanceId == NodeInfo.InstanceId ? GetUpdatedMe(x) : x);

            //if (_cluster.HasChangedSince(oldCluster))
            //if (Log.IsTraceLevelEnabled()) LogClusterChange(oldCluster, _cluster, _nodeInfo.InternalHttp);
            _bus.Publish(new GossipMessage.GossipUpdated(_cluster));
        }

        public void Handle(GossipMessage.GossipSendFailed message)
        {
            var node = _cluster.Members.FirstOrDefault(x => x.Is(message.Recipient));
            if (node == null || !node.IsAlive) { return; }

            var traceEnabled = Log.IsTraceLevelEnabled();
            if (CurrentMaster != null && node.InstanceId == CurrentMaster.InstanceId)
            {
                if (traceEnabled) { Log.LooksLikeMasterIsDEADGossipSendFailed(message, node.InstanceId); }
                return;
            }
            if (traceEnabled) { Log.LooksLikeNodeIsDEADGossipSendFailed(message); }

            var oldCluster = _cluster;
            _cluster = UpdateCluster(_cluster, x => x.Is(message.Recipient) ? x.Updated(isAlive: false) : x);
            if (Log.IsTraceLevelEnabled() && _cluster.HasChangedSince(oldCluster))
            {
                LogClusterChange(oldCluster, _cluster, $"gossip send failed to [{message.Recipient}]");
            }

            _bus.Publish(new GossipMessage.GossipUpdated(_cluster));
        }

        public void Handle(SystemMessage.VNodeConnectionLost message)
        {
            var node = _cluster.Members.FirstOrDefault(x => x.Is(message.VNodeEndPoint));
            if (node == null || !node.IsAlive) { return; }

            if (Log.IsTraceLevelEnabled()) Log.LooksLikeNodeIsDeadTCPConnectionLost(message);

            var oldCluster = _cluster;
            _cluster = UpdateCluster(_cluster, x => x.Is(message.VNodeEndPoint) ? x.Updated(isAlive: false) : x);
            if (Log.IsTraceLevelEnabled() && _cluster.HasChangedSince(oldCluster))
            {
                LogClusterChange(oldCluster, _cluster, $"TCP connection lost to [{message.VNodeEndPoint}]");
            }
            _bus.Publish(new GossipMessage.GossipUpdated(_cluster));
        }

        public void Handle(SystemMessage.VNodeConnectionEstablished message)
        {
            var oldCluster = _cluster;
            _cluster = UpdateCluster(_cluster, x => x.Is(message.VNodeEndPoint) ? x.Updated(isAlive: true) : x);
            if (Log.IsTraceLevelEnabled() && _cluster.HasChangedSince(oldCluster))
            {
                LogClusterChange(oldCluster, _cluster, $"TCP connection established to [{message.VNodeEndPoint}]");
            }

            _bus.Publish(new GossipMessage.GossipUpdated(_cluster));
        }

        private ClusterInfo MergeClusters(ClusterInfo myCluster, ClusterInfo othersCluster,
                                          IPEndPoint peerEndPoint, Func<MemberInfo, MemberInfo> update)
        {
            var mems = myCluster.Members.ToDictionary(member => member.InternalHttpEndPoint);
            foreach (var member in othersCluster.Members)
            {
                if (member.InstanceId == NodeInfo.InstanceId || member.Is(NodeInfo.InternalHttp)) // we know about ourselves better
                { continue; }
                if (peerEndPoint != null && member.Is(peerEndPoint)) // peer knows about itself better
                {
                    if ((DateTime.UtcNow - member.TimeStamp).Duration() > AllowedTimeDifference)
                    {
                        Log.TimeDifferenceBetweenUsAndPeerendpointIsTooGreat(peerEndPoint, member.TimeStamp);
                    }
                    mems[member.InternalHttpEndPoint] = member;
                }
                else
                {
                    // if there is no data about this member or data is stale -- update
                    if (!mems.TryGetValue(member.InternalHttpEndPoint, out MemberInfo existingMem) || IsMoreUpToDate(member, existingMem))
                    {
                        // we do not trust master's alive status and state to come from outside
                        if (CurrentMaster != null && existingMem != null && member.InstanceId == CurrentMaster.InstanceId)
                            mems[member.InternalHttpEndPoint] = member.Updated(isAlive: existingMem.IsAlive, state: existingMem.State);
                        else
                            mems[member.InternalHttpEndPoint] = member;
                    }
                }
            }
            // update members and remove dead timed-out members, if there are any
            var newMembers = mems.Values.Select(update)
                                        .Where(x => x.IsAlive || DateTime.UtcNow - x.TimeStamp < DeadMemberRemovalTimeout);
            return new ClusterInfo(newMembers);
        }

        private static bool IsMoreUpToDate(MemberInfo member, MemberInfo existingMem)
        {
            if (member.EpochNumber != existingMem.EpochNumber)
            {
                return member.EpochNumber > existingMem.EpochNumber;
            }

            if (member.WriterCheckpoint != existingMem.WriterCheckpoint)
            {
                return member.WriterCheckpoint > existingMem.WriterCheckpoint;
            }

            return member.TimeStamp > existingMem.TimeStamp;
        }

        private static ClusterInfo UpdateCluster(ClusterInfo cluster, Func<MemberInfo, MemberInfo> update)
        {
            // update members and remove dead timed-out members, if there are any
            var newMembers = cluster.Members.Select(update)
                                            .Where(x => x.IsAlive || DateTime.UtcNow - x.TimeStamp < DeadMemberRemovalTimeout);
            return new ClusterInfo(newMembers);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void LogClusterChange(ClusterInfo oldCluster, ClusterInfo newCluster, string source)
        {
            if (!Log.IsTraceLevelEnabled()) { return; }
            Log.LogTrace("CLUSTER HAS CHANGED{0}", source.IsNotEmptyString() ? " (" + source + ")" : string.Empty);
            Log.LogTrace("Old:");
            var ipEndPointComparer = new IPEndPointComparer();
            foreach (var oldMember in oldCluster.Members.OrderByDescending(x => x.InternalHttpEndPoint, ipEndPointComparer))
            {
                Log.LogTrace(oldMember.ToString());
            }
            Log.LogTrace("New:");
            foreach (var newMember in newCluster.Members.OrderByDescending(x => x.InternalHttpEndPoint, ipEndPointComparer))
            {
                Log.LogTrace(newMember.ToString());
            }
            Log.LogTrace(new string('-', 80));
        }
    }
}
