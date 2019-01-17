using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using EventStore.ClientAPI.Common.Utils;
using EventStore.ClientAPI.Exceptions;
using EventStore.ClientAPI.Messages;
using EventStore.ClientAPI.Transport.Http;
using Microsoft.Extensions.Logging;
using HttpStatusCode = EventStore.ClientAPI.Transport.Http.HttpStatusCode;

namespace EventStore.ClientAPI.Internal
{
    internal class ClusterDnsEndPointDiscoverer : IEndPointDiscoverer
    {
        private static readonly ILogger _log = TraceLogger.GetLogger<ClusterDnsEndPointDiscoverer>();
        private readonly string _clusterDns;
        private readonly int _maxDiscoverAttempts;
        private readonly int _managerExternalHttpPort;
        private readonly GossipSeed[] _gossipSeeds;

        private readonly HttpAsyncClient _client;
        private ClusterMessages.MemberInfoDto[] _oldGossip;
        private TimeSpan _gossipTimeout;

        private readonly NodePreference _nodePreference;

        public ClusterDnsEndPointDiscoverer(string clusterDns,
                                            int maxDiscoverAttempts,
                                            int managerExternalHttpPort,
                                            GossipSeed[] gossipSeeds,
                                            TimeSpan gossipTimeout,
                                            NodePreference nodePreference)
        {
            _clusterDns = clusterDns;
            _maxDiscoverAttempts = maxDiscoverAttempts;
            _managerExternalHttpPort = managerExternalHttpPort;
            _gossipSeeds = gossipSeeds;
            _gossipTimeout = gossipTimeout;
            _nodePreference = nodePreference;
            _client = new HttpAsyncClient(_gossipTimeout);
        }

        public async Task<NodeEndPoints> DiscoverAsync(IPEndPoint failedTcpEndPoint)
        {
            var maxDiscoverAttemptsStr = "";
            if (_maxDiscoverAttempts != Int32.MaxValue)
            {
                maxDiscoverAttemptsStr = "/" + _maxDiscoverAttempts;
            }

            var infoEnabled = _log.IsInformationLevelEnabled();
            for (int attempt = 1; attempt <= _maxDiscoverAttempts; ++attempt)
            {
                //_log.Info("Discovering cluster. Attempt {0}/{1}...", attempt, _maxDiscoverAttempts);
                try
                {
                    var endPoints = await DiscoverEndPoint(failedTcpEndPoint).ConfigureAwait(false);
                    if (endPoints != null)
                    {
                        if (infoEnabled) { _log.DiscoveringAttemptSuccessful(attempt, maxDiscoverAttemptsStr, endPoints.Value); }

                        return endPoints.Value;
                    }

                    if (infoEnabled) { _log.DiscoveringAttemptFailed(attempt, maxDiscoverAttemptsStr); }
                }
                catch (Exception exc)
                {
                    if (infoEnabled) { _log.DiscoveringAttemptFailed(attempt, maxDiscoverAttemptsStr, exc); }
                }

                //Thread.Sleep(500);
                await Task.Delay(500);
            }
            CoreThrowHelper.ThrowClusterException(_maxDiscoverAttempts); return default;
        }

        private async Task<NodeEndPoints?> DiscoverEndPoint(IPEndPoint failedEndPoint)
        {
            var oldGossip = Interlocked.Exchange(ref _oldGossip, null);
            var gossipCandidates = oldGossip != null
                                           ? GetGossipCandidatesFromOldGossip(oldGossip, failedEndPoint)
                                           : await GetGossipCandidatesFromDns();
            for (int i = 0; i < gossipCandidates.Length; ++i)
            {
                var gossip = TryGetGossipFrom(gossipCandidates[i]);
                if (gossip == null || gossip.Members == null || gossip.Members.Length == 0)
                {
                    continue;
                }

                var bestNode = TryDetermineBestNode(gossip.Members, _nodePreference);
                if (bestNode != null)
                {
                    _oldGossip = gossip.Members;
                    return bestNode;
                }
            }

            return null;
        }

        private async Task<GossipSeed[]> GetGossipCandidatesFromDns()
        {
            //_log.Debug("ClusterDnsEndPointDiscoverer: GetGossipCandidatesFromDns");
            GossipSeed[] endpoints;
            if (_gossipSeeds != null && _gossipSeeds.Length > 0)
            {
                endpoints = _gossipSeeds;
            }
            else
            {
                endpoints = (await ResolveDns(_clusterDns)).Select(x => new GossipSeed(new IPEndPoint(x, _managerExternalHttpPort))).ToArray();
            }

            RandomShuffle(endpoints, 0, endpoints.Length - 1);
            return endpoints;
        }

        private async Task<IPAddress[]> ResolveDns(string dns)
        {
            IPAddress[] addresses = null;
            try
            {
                addresses = await Dns.GetHostAddressesAsync(dns);
            }
            catch (Exception exc)
            {
                CoreThrowHelper.ThrowClusterException(_clusterDns, exc);
            }
            if (addresses == null || addresses.Length == 0)
            {
                CoreThrowHelper.ThrowClusterException(_clusterDns);
            }

            return addresses;
        }

        private GossipSeed[] GetGossipCandidatesFromOldGossip(IEnumerable<ClusterMessages.MemberInfoDto> oldGossip, IPEndPoint failedTcpEndPoint)
        {
            //_log.Debug("ClusterDnsEndPointDiscoverer: GetGossipCandidatesFromOldGossip, failedTcpEndPoint: {0}.", failedTcpEndPoint);
            var gossipCandidates = failedTcpEndPoint == null
                    ? oldGossip.ToArray()
                    : oldGossip.Where(x => !(x.ExternalTcpPort == failedTcpEndPoint.Port
                                             && IPAddress.Parse(x.ExternalTcpIp).Equals(failedTcpEndPoint.Address)))
                               .ToArray();
            return ArrangeGossipCandidates(gossipCandidates);
        }

        private GossipSeed[] ArrangeGossipCandidates(ClusterMessages.MemberInfoDto[] members)
        {
            var result = new GossipSeed[members.Length];
            int i = -1;
            int j = members.Length;
            for (int k = 0; k < members.Length; ++k)
            {
                if (members[k].State == ClusterMessages.VNodeState.Manager)
                {
                    result[--j] = new GossipSeed(new IPEndPoint(IPAddress.Parse(members[k].ExternalHttpIp), members[k].ExternalHttpPort));
                }
                else
                {
                    result[++i] = new GossipSeed(new IPEndPoint(IPAddress.Parse(members[k].ExternalHttpIp), members[k].ExternalHttpPort));
                }
            }
            RandomShuffle(result, 0, i); // shuffle nodes
            RandomShuffle(result, j, members.Length - 1); // shuffle managers

            return result;
        }

        private void RandomShuffle<T>(T[] arr, int i, int j)
        {
            if (i >= j) return;
            var rnd = new Random(Guid.NewGuid().GetHashCode());
            for (int k = i; k <= j; ++k)
            {
                var index = rnd.Next(k, j + 1);
                var tmp = arr[index];
                arr[index] = arr[k];
                arr[k] = tmp;
            }
        }

        private ClusterMessages.ClusterInfoDto TryGetGossipFrom(GossipSeed endPoint)
        {
            //_log.Debug("ClusterDnsEndPointDiscoverer: Trying to get gossip from [{0}].", endPoint);

            ClusterMessages.ClusterInfoDto result = null;
            var completed = new ManualResetEventSlim(false);

            var url = endPoint.EndPoint.ToHttpUrl(EndpointExtensions.HTTP_SCHEMA, "/gossip?format=json");
            _client.Get(
                url,
                null,
                response =>
                {
                    if (response.HttpStatusCode != HttpStatusCode.OK)
                    {
                        //_log.Info("[{0}] responded with {1} ({2})", endPoint, response.HttpStatusCode, response.StatusDescription);
                        completed.Set();
                        return;
                    }
                    try
                    {
                        result = response.Body.ParseJson<ClusterMessages.ClusterInfoDto>();
                        //_log.Debug("ClusterDnsEndPointDiscoverer: Got gossip from [{0}]:\n{1}.", endPoint, string.Join("\n", result.Members.Select(x => x.ToString())));
                    }
                    catch (Exception)
                    {
                        //_log.Info("Failed to get cluster info from [{0}]: deserialization error: {1}.", endPoint, e.Message);
                    }
                    completed.Set();
                },
                e =>
                {
                    //_log.Info("Failed to get cluster info from [{0}]: request failed, error: {1}.", endPoint, e.Message);
                    completed.Set();
                }, endPoint.HostHeader);

            completed.Wait();
            return result;
        }

        private NodeEndPoints? TryDetermineBestNode(IEnumerable<ClusterMessages.MemberInfoDto> members, NodePreference nodePreference)
        {
            var notAllowedStates = new[]
            {
                ClusterMessages.VNodeState.Manager,
                ClusterMessages.VNodeState.ShuttingDown,
                ClusterMessages.VNodeState.Shutdown
            };

            var nodes = members.Where(x => x.IsAlive)
                               .Where(x => !notAllowedStates.Contains(x.State))
                               .OrderByDescending(x => x.State)
                               .ToArray();

            switch (nodePreference)
            {
                case NodePreference.Random:
                    RandomShuffle(nodes, 0, nodes.Length - 1);
                    break;
                case NodePreference.Slave:
                    nodes = nodes.OrderBy(nodeEntry => nodeEntry.State != ClusterMessages.VNodeState.Slave).ToArray(); // OrderBy is a stable sort and only affects order of matching entries
                    RandomShuffle(nodes, 0, nodes.Count(nodeEntry => nodeEntry.State == ClusterMessages.VNodeState.Slave) - 1);
                    break;
            }

            var node = nodes.FirstOrDefault();

            if (node == default(ClusterMessages.MemberInfoDto))
            {
                //_log.Info("Unable to locate suitable node. Gossip info:\n{0}.", string.Join("\n", members.Select(x => x.ToString())));
                return null;
            }

            var normTcp = new IPEndPoint(IPAddress.Parse(node.ExternalTcpIp), node.ExternalTcpPort);
            var secTcp = node.ExternalSecureTcpPort > 0
                                 ? new IPEndPoint(IPAddress.Parse(node.ExternalTcpIp), node.ExternalSecureTcpPort)
                                 : null;
            if (_log.IsInformationLevelEnabled())
            {
                _log.DiscoveringFoundBestChoice(normTcp, secTcp, node.State);
            }
            return new NodeEndPoints(normTcp, secTcp);
        }
    }
}
