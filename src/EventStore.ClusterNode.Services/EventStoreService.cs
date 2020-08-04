using System;
using System.Collections.Generic;
using System.Globalization;
//using System.ComponentModel.Composition;
//using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Net;
using System.Threading;
using DotNetty;
using DotNetty.Buffers;
using EventStore.Common.Exceptions;
using EventStore.Common.Options;
using EventStore.Common.Utils;
using EventStore.Core;
using EventStore.Core.Authentication;
using EventStore.Core.PluginModel;
using EventStore.Core.Services.Monitoring;
using EventStore.Core.Services.PersistentSubscription.ConsumerStrategy;
using EventStore.Core.Services.Transport.Http.Controllers;
using EventStore.Core.Util;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

// http://stackoverflow.com/questions/23791696/how-to-catch-exception-and-stop-topshelf-service

namespace EventStore.ClusterNode
{
    public class EventStoreService : EventStoreServiceBase<ClusterNodeOptions>
    {
        private ClusterVNode _node;
        private ExclusiveDbLock _dbLock = null;
        //private ClusterNodeMutex _clusterNodeMutex = null;

        protected override string GetLogsDirectory(ClusterNodeOptions options)
        {
            return options.Log;
        }

        protected override string GetComponentName(ClusterNodeOptions options)
        {
            return $"{options.ExtIp}-{options.ExtHttpPort}-cluster-node";
        }

        protected override void PreInit(ClusterNodeOptions options)
        {
            var loggerFactory = new LoggerFactory();
#pragma warning disable CS0618 // 类型或成员已过时
            loggerFactory.AddNLog();
#pragma warning restore CS0618 // 类型或成员已过时
            TraceLogger.Initialize(loggerFactory);
            Log = TraceLogger.GetLogger<EventStoreService>();

            if (options.Db.StartsWith("~", StringComparison.Ordinal) && !options.Force)
            {
                throw new ApplicationInitializationException("The given database path starts with a '~'. We don't expand '~'. You can use --force to override this error.");
            }
            if (options.Log.StartsWith("~", StringComparison.Ordinal) && !options.Force)
            {
                throw new ApplicationInitializationException("The given log path starts with a '~'. We don't expand '~'. You can use --force to override this error.");
            }
            if (options.GossipSeed.Length > 1 && options.ClusterSize == 1)
            {
                throw new ApplicationInitializationException("The given ClusterSize is set to 1 but GossipSeeds are multiple. We will never be able to sync up with this configuration.");
            }

            //0 indicates we should leave the machine defaults alone
            if (options.MonoMinThreadpoolSize == 0) { return; }

            //Change the number of worker threads to be higher if our own setting
            // is higher than the current value
            ThreadPool.GetMinThreads(out int minWorkerThreads, out int minIocpThreads);

            if (minWorkerThreads >= options.MonoMinThreadpoolSize) { return; }

            if (!ThreadPool.SetMinThreads(options.MonoMinThreadpoolSize, minIocpThreads))
                Log.LogError("Cannot override the minimum number of Threadpool threads (machine default: {0}, specified value: {1})", minWorkerThreads, options.MonoMinThreadpoolSize);
        }

        protected override void Create(ClusterNodeOptions opts)
        {
            var dbPath = opts.Db;

            if (!opts.MemDb)
            {
                //var absolutePath = Path.GetFullPath(dbPath);
                //if (Runtime.IsWindows)
                //    absolutePath = absolutePath.ToLower();

                //_dbLock = new ExclusiveDbLock(absolutePath);
                //if (!_dbLock.Acquire())
                //{
                //  throw new Exception($"Couldn't acquire exclusive lock on DB at '{dbPath}'.");
                //}
            }
            //_clusterNodeMutex = new ClusterNodeMutex();
            //if (!_clusterNodeMutex.Acquire())
            //{
            //  throw new Exception($"Couldn't acquire exclusive Cluster Node mutex '{_clusterNodeMutex.MutexName}'.");
            //}

            if (!opts.DiscoverViaDns && opts.GossipSeed.Length == 0)
            {
                if (opts.ClusterSize == 1)
                {
                    Log.LogInformation("DNS discovery is disabled, but no gossip seed endpoints have been specified. Since "
                            + "the cluster size is set to 1, this may be intentional. Gossip seeds can be specified "
                            + "using the `GossipSeed` option.");
                }
            }

            var runProjections = opts.RunProjections;
            var enabledNodeSubsystems = runProjections >= ProjectionType.System
                ? new[] { NodeSubsystems.Projections }
                : new NodeSubsystems[0];
            _node = BuildNode(opts);
            RegisterWebControllers(enabledNodeSubsystems, opts);
        }

        private void RegisterWebControllers(NodeSubsystems[] enabledNodeSubsystems, ClusterNodeOptions options)
        {
            if (_node.InternalHttpService is object)
            {
                _node.InternalHttpService.SetupController(new ClusterWebUiController(_node.MainQueue, enabledNodeSubsystems));
            }
            if (options.AdminOnExt)
            {
                _node.ExternalHttpService.SetupController(new ClusterWebUiController(_node.MainQueue, enabledNodeSubsystems));
            }
        }

        private static int GetQuorumSize(int clusterSize)
        {
            if (clusterSize == 1) { return 1; }
            return clusterSize / 2 + 1;
        }

        private static ClusterVNode BuildNode(ClusterNodeOptions options)
        {
            var quorumSize = GetQuorumSize(options.ClusterSize);

            var intHttp = new IPEndPoint(options.IntIp, options.IntHttpPort);
            var extHttp = new IPEndPoint(options.ExtIp, options.ExtHttpPort);
            var intTcp = new IPEndPoint(options.IntIp, options.IntTcpPort);
            var intSecTcp = options.IntSecureTcpPort > 0 ? new IPEndPoint(options.IntIp, options.IntSecureTcpPort) : null;
            var extTcp = new IPEndPoint(options.ExtIp, options.ExtTcpPort);
            var extSecTcp = options.ExtSecureTcpPort > 0 ? new IPEndPoint(options.ExtIp, options.ExtSecureTcpPort) : null;

            var prepareCount = options.PrepareCount > quorumSize ? options.PrepareCount : quorumSize;
            var commitCount = options.CommitCount > quorumSize ? options.CommitCount : quorumSize;
            Log.LogInformation($"Quorum size set to {prepareCount}");
            if (options.DisableInsecureTCP)
            {
                if (!options.UseInternalSsl)
                {
                    throw new Exception("You have chosen to disable the insecure TCP ports and haven't set 'UseInternalSsl'. The nodes in the cluster will not be able to communicate properly.");
                }
                if (extSecTcp is null || intSecTcp is null)
                {
                    throw new Exception("You have chosen to disable the insecure TCP ports and haven't setup the External or Internal Secure TCP Ports.");
                }
            }
            if (options.UseInternalSsl)
            {
                if (ReferenceEquals(options.SslTargetHost, Opts.SslTargetHostDefault)) throw new Exception("No SSL target host specified.");
                if (intSecTcp is null) throw new Exception("Usage of internal secure communication is specified, but no internal secure endpoint is specified!");
            }

            VNodeBuilder builder;
            if (options.ClusterSize > 1)
            {
                builder = ClusterVNodeBuilder.AsClusterMember(options.ClusterSize);
            }
            else
            {
                builder = ClusterVNodeBuilder.AsSingleNode();
            }
            if (options.MemDb)
            {
                builder = builder.RunInMemory();
            }
            else
            {
                builder = builder.RunOnDisk(options.Db);
            }

            if (options.WriteStatsToDb)
            {
                builder = builder.WithStatsStorage(StatsStorage.StreamAndFile);
            }
            else
            {
                builder = builder.WithStatsStorage(StatsStorage.File);
            }

            builder.WithInternalTcpOn(intTcp)
                   .WithInternalSecureTcpOn(intSecTcp)
                   .WithExternalTcpOn(extTcp)
                   .WithExternalSecureTcpOn(extSecTcp)
                   .WithInternalHttpOn(intHttp)
                   .WithExternalHttpOn(extHttp)
                   .WithWorkerThreads(options.WorkerThreads)
                   .WithInternalHeartbeatTimeout(TimeSpan.FromMilliseconds(options.IntTcpHeartbeatTimeout))
                   .WithInternalHeartbeatInterval(TimeSpan.FromMilliseconds(options.IntTcpHeartbeatInterval))
                   .WithExternalHeartbeatTimeout(TimeSpan.FromMilliseconds(options.ExtTcpHeartbeatTimeout))
                   .WithExternalHeartbeatInterval(TimeSpan.FromMilliseconds(options.ExtTcpHeartbeatInterval))
                   .MaximumMemoryTableSizeOf(options.MaxMemTableSize)
                   .WithHashCollisionReadLimitOf(options.HashCollisionReadLimit)
                   .WithGossipInterval(TimeSpan.FromMilliseconds(options.GossipIntervalMs))
                   .WithGossipAllowedTimeDifference(TimeSpan.FromMilliseconds(options.GossipAllowedDifferenceMs))
                   .WithGossipTimeout(TimeSpan.FromMilliseconds(options.GossipTimeoutMs))
                   .WithClusterGossipPort(options.ClusterGossipPort)
                   .WithMinFlushDelay(TimeSpan.FromMilliseconds(options.MinFlushDelayMs))
                   .WithPrepareTimeout(TimeSpan.FromMilliseconds(options.PrepareTimeoutMs))
                   .WithCommitTimeout(TimeSpan.FromMilliseconds(options.CommitTimeoutMs))
                   .WithStatsPeriod(TimeSpan.FromSeconds(options.StatsPeriodSec))
                   .WithPrepareCount(prepareCount)
                   .WithCommitCount(commitCount)
                   .WithNodePriority(options.NodePriority)
                   .WithScavengeHistoryMaxAge(options.ScavengeHistoryMaxAge)
                   .WithIndexPath(options.Index)
                   .WithIndexVerification(options.SkipIndexVerify)
                   .WithIndexCacheDepth(options.IndexCacheDepth)
                   .WithSslTargetHost(options.SslTargetHost)
                   .RunProjections(options.RunProjections, options.ProjectionThreads, options.FaultOutOfOrderProjections)
                   .WithProjectionQueryExpirationOf(TimeSpan.FromMinutes(options.ProjectionsQueryExpiry))
                   .WithTfCachedChunks(options.CachedChunks)
                   .WithTfChunksCacheSize(options.ChunksCacheSize)
                   .AdvertiseInternalIPAs(options.IntIpAdvertiseAs)
                   .AdvertiseExternalIPAs(options.ExtIpAdvertiseAs)
                   .AdvertiseInternalHttpPortAs(options.IntHttpPortAdvertiseAs)
                   .AdvertiseExternalHttpPortAs(options.ExtHttpPortAdvertiseAs)
                   .AdvertiseInternalTCPPortAs(options.IntTcpPortAdvertiseAs)
                   .AdvertiseExternalTCPPortAs(options.ExtTcpPortAdvertiseAs)
                   .AdvertiseInternalSecureTCPPortAs(options.IntSecureTcpPortAdvertiseAs)
                   .AdvertiseExternalSecureTCPPortAs(options.ExtSecureTcpPortAdvertiseAs)
                   .HavingReaderThreads(options.ReaderThreadsCount)
                   .WithConnectionPendingSendBytesThreshold(options.ConnectionPendingSendBytesThreshold)
                   .WithConnectionQueueSizeThreshold(options.ConnectionQueueSizeThreshold)
                   .WithChunkInitialReaderCount(options.ChunkInitialReaderCount)
                   .WithInitializationThreads(options.InitializationThreads)
                   .WithWriteBufferHighWaterMark(options.WriteBufferHighWaterMark)
                   .WithWriteBufferLowWaterMark(options.WriteBufferLowWaterMark)
                   .WithSendBufferSize(options.SendBufferSize)
                   .WithReceiveBufferSize(options.ReceiveBufferSize)
                   .WithMaxFrameSize(options.MaxFrameSize)
                   .WithBacklog(options.Backlog)
                   .WithTcpLinger(options.TcpLinger)
                   .WithTcpReuseAddr(options.TcpReuseAddr)
                   .WithServerSocketWorkerPoolSizeMin(options.ServerSocketWorkerPoolSizeMin)
                   .WithServerSocketWorkerPoolSizeFactor(options.ServerSocketWorkerPoolSizeFactor)
                   .WithServerSocketWorkerPoolSizeMax(options.ServerSocketWorkerPoolSizeMax)
                   .WithClientSocketWorkerPoolSizeMin(options.ClientSocketWorkerPoolSizeMin)
                   .WithClientSocketWorkerPoolSizeFactor(options.ClientSocketWorkerPoolSizeFactor)
                   .WithClientSocketWorkerPoolSizeMax(options.ClientSocketWorkerPoolSizeMax)
                   ;

            if (options.GossipSeed.Length > 0) { builder.WithGossipSeeds(options.GossipSeed); }

            if (options.DiscoverViaDns)
            {
                builder.WithClusterDnsName(options.ClusterDns);
            }
            else
            {
                builder.DisableDnsDiscovery();
            }

            if (!options.AddInterfacePrefixes) { builder.DontAddInterfacePrefixes(); }
            if (options.GossipOnSingleNode) { builder.GossipAsSingleNode(); }
            foreach (var prefix in options.IntHttpPrefixes)
            {
                builder.AddInternalHttpPrefix(prefix);
            }
            foreach (var prefix in options.ExtHttpPrefixes)
            {
                builder.AddExternalHttpPrefix(prefix);
            }

            if (options.EnableTrustedAuth) { builder.EnableTrustedAuth(); }
            if (options.StartStandardProjections) { builder.StartStandardProjections(); }
            if (options.DisableHTTPCaching) { builder.DisableHTTPCaching(); }
            if (options.DisableScavengeMerging) { builder.DisableScavengeMerging(); }
            if (options.LogHttpRequests) { builder.EnableLoggingOfHttpRequests(); }
            if (options.LogFailedAuthenticationAttempts) { builder.EnableLoggingOfFailedAuthenticationAttempts(); }
            if (options.EnableHistograms) { builder.EnableHistograms(); }
            if (options.UnsafeIgnoreHardDelete) { builder.WithUnsafeIgnoreHardDelete(); }
            if (options.UnsafeDisableFlushToDisk) { builder.WithUnsafeDisableFlushToDisk(); }
            if (options.BetterOrdering) { builder.WithBetterOrdering(); }
            if (options.SslValidateServer) { builder.ValidateSslServer(); }
            if (options.UseInternalSsl) { builder.EnableSsl(); }
            if (options.DisableInsecureTCP) { builder.DisableInsecureTCP(); }
            if (!options.AdminOnExt) { builder.NoAdminOnPublicInterface(); }
            if (!options.StatsOnExt) { builder.NoStatsOnPublicInterface(); }
            if (!options.GossipOnExt) { builder.NoGossipOnPublicInterface(); }
            if (options.SkipDbVerify) { builder.DoNotVerifyDbHashes(); }
            if (options.AlwaysKeepScavenged) { builder.AlwaysKeepScavenged(); }
            if (options.Unbuffered) { builder.EnableUnbuffered(); }
            if (options.WriteThrough) { builder.EnableWriteThrough(); }
            if (options.SkipIndexScanOnReads) { builder.SkipIndexScanOnReads(); }
            if (options.ReduceFileCachePressure) { builder.ReduceFileCachePressure(); }
            if (options.StructuredLog) { builder.WithStructuredLogging(options.StructuredLog); }
            if (options.DisableFirstLevelHttpAuthorization) { builder.DisableFirstLevelHttpAuthorization(); }

            if (!options.EnableLibuv) { builder.DisableLibuv(); }
            if (options.DnsUseIpv6) { builder.WithDnsUseIpv6(); }
            if (options.EnforceIpFamily) { builder.WithEnforceIpFamily(); }
            if (!string.IsNullOrWhiteSpace(options.ConnectTimeout)) { builder.WithConnectTimeout(options.ConnectTimeout); }
            if (!options.EnableBufferPooling) { builder.DisableBufferPooling(); }
            if (!options.TcpNoDelay) { builder.DisableTcpNoDelay(); }
            if (!options.TcpKeepAlive) { builder.DisableTcpKeepAlive(); }
            if (!options.TcpReusePort) { builder.DisableTcpReusePort(); }

            if (options.IntSecureTcpPort > 0 || options.ExtSecureTcpPort > 0)
            {
                if (!string.IsNullOrWhiteSpace(options.CertificateStoreLocation))
                {
                    var location = GetCertificateStoreLocation(options.CertificateStoreLocation);
                    var name = GetCertificateStoreName(options.CertificateStoreName);
                    builder.WithServerCertificateFromStore(location, name, options.CertificateSubjectName, options.CertificateThumbprint);
                }
                else if (!string.IsNullOrWhiteSpace(options.CertificateStoreName))
                {
                    var name = GetCertificateStoreName(options.CertificateStoreName);
                    builder.WithServerCertificateFromStore(name, options.CertificateSubjectName, options.CertificateThumbprint);
                }
                else if (options.CertificateFile.IsNotEmptyString())
                {
                    builder.WithServerCertificateFromFile(options.CertificateFile, options.CertificatePassword);
                }
                else
                {
                    throw new Exception("No server certificate specified.");
                }
            }

            // DotNetty buffer options
            if (!options.DirectBufferPreferred)
            {
                Environment.SetEnvironmentVariable("io.netty.noPreferDirect", "true");
            }
            if (!options.CheckBufferAccessible)
            {
                Environment.SetEnvironmentVariable("io.netty.buffer.checkAccessible", "false");
            }
            if (!options.CheckBufferBounds)
            {
                Environment.SetEnvironmentVariable("io.netty.buffer.checkBounds", "false");
            }
            var numHeapArena = VNodeBuilder.ToNullableInt(VNodeBuilder.GetByteSize(options.AllocatorHeapArenas));
            if (numHeapArena.HasValue)
            {
                Environment.SetEnvironmentVariable("io.netty.allocator.numHeapArenas",
                    numHeapArena.Value.ToString(CultureInfo.InvariantCulture));
            }
            var numDirectArena = VNodeBuilder.ToNullableInt(VNodeBuilder.GetByteSize(options.AllocatorDirectArenas));
            if (numDirectArena.HasValue)
            {
                Environment.SetEnvironmentVariable("io.netty.allocator.numDirectArenas",
                    numDirectArena.Value.ToString(CultureInfo.InvariantCulture));
            }
            var pageSize = VNodeBuilder.ToNullableInt(VNodeBuilder.GetByteSize(options.AllocatorPageSize));
            if (pageSize.HasValue)
            {
                Environment.SetEnvironmentVariable("io.netty.allocator.pageSize",
                    pageSize.Value.ToString(CultureInfo.InvariantCulture));
            }
            var maxOrder = VNodeBuilder.ToNullableInt(VNodeBuilder.GetByteSize(options.AllocatorMaxOrder));
            if (maxOrder.HasValue)
            {
                Environment.SetEnvironmentVariable("io.netty.allocator.maxOrder",
                    maxOrder.Value.ToString(CultureInfo.InvariantCulture));
            }
            var tinyCacheSize = VNodeBuilder.ToNullableInt(VNodeBuilder.GetByteSize(options.AllocatorTinyCacheSize));
            if (tinyCacheSize.HasValue)
            {
                Environment.SetEnvironmentVariable("io.netty.allocator.tinyCacheSize",
                    tinyCacheSize.Value.ToString(CultureInfo.InvariantCulture));
            }
            var smallCacheSize = VNodeBuilder.ToNullableInt(VNodeBuilder.GetByteSize(options.AllocatorSmallCacheSize));
            if (smallCacheSize.HasValue)
            {
                Environment.SetEnvironmentVariable("io.netty.allocator.smallCacheSize",
                    smallCacheSize.Value.ToString(CultureInfo.InvariantCulture));
            }
            var normalCacheSize = VNodeBuilder.ToNullableInt(VNodeBuilder.GetByteSize(options.AllocatorNormalCacheSize));
            if (normalCacheSize.HasValue)
            {
                Environment.SetEnvironmentVariable("io.netty.allocator.normalCacheSize",
                    normalCacheSize.Value.ToString(CultureInfo.InvariantCulture));
            }
            var maxCachedBufferCapacity = VNodeBuilder.ToNullableInt(VNodeBuilder.GetByteSize(options.AllocatorCacheBufferMaxCapacity));
            if (maxCachedBufferCapacity.HasValue)
            {
                Environment.SetEnvironmentVariable("io.netty.allocator.maxCachedBufferCapacity",
                    maxCachedBufferCapacity.Value.ToString(CultureInfo.InvariantCulture));
            }
            var cacheTrimInterval = VNodeBuilder.ToNullableInt(VNodeBuilder.GetByteSize(options.AllocatorCacheTrimInterval));
            if (cacheTrimInterval.HasValue)
            {
                Environment.SetEnvironmentVariable("io.netty.allocator.cacheTrimInterval",
                    cacheTrimInterval.Value.ToString(CultureInfo.InvariantCulture));
            }


            var authenticationConfig = String.IsNullOrEmpty(options.AuthenticationConfig) ? options.Config : options.AuthenticationConfig;
            //var plugInContainer = FindPlugins();
            //var authenticationProviderFactory = GetAuthenticationProviderFactory(options.AuthenticationType, authenticationConfig, plugInContainer);
            //var consumerStrategyFactories = GetPlugInConsumerStrategyFactories(plugInContainer);
            //builder.WithAuthenticationProvider(authenticationProviderFactory);

            return builder.Build(options);//, consumerStrategyFactories);
        }

        //  private static IPersistentSubscriptionConsumerStrategyFactory[] GetPlugInConsumerStrategyFactories(CompositionContainer plugInContainer)
        //  {
        //      var allPlugins = plugInContainer.GetExports<IPersistentSubscriptionConsumerStrategyPlugin>();

        //      var strategyFactories = new List<IPersistentSubscriptionConsumerStrategyFactory>();

        //      foreach (var potentialPlugin in allPlugins)
        //      {
        //          try
        //          {
        //              var plugin = potentialPlugin.Value;
        //              Log.LogInformation("Loaded consumer strategy plugin: {0} version {1}.", plugin.Name, plugin.Version);
        //              strategyFactories.Add(plugin.GetConsumerStrategyFactory());
        //          }
        //          catch (CompositionException ex)
        //          {
        //              Log.LogError(ex, "Error loading consumer strategy plugin.");
        //          }
        //      }

        //      return strategyFactories.ToArray();
        //  }

        //  private static IAuthenticationProviderFactory GetAuthenticationProviderFactory(string authenticationType, string authenticationConfigFile, CompositionContainer plugInContainer)
        //  {
        //      var potentialPlugins = plugInContainer.GetExports<IAuthenticationPlugin>();

        //      var authenticationTypeToPlugin = new Dictionary<string, Func<IAuthenticationProviderFactory>>
        //{
        //  { "internal", () => new InternalAuthenticationProviderFactory() }
        //};

        //      foreach (var potentialPlugin in potentialPlugins)
        //      {
        //          try
        //          {
        //              var plugin = potentialPlugin.Value;
        //              var commandLine = plugin.CommandLineName.ToLowerInvariant();
        //              Log.LogInformation("Loaded authentication plugin: {0} version {1} (Command Line: {2})", plugin.Name, plugin.Version, commandLine);
        //              authenticationTypeToPlugin.Add(commandLine, () => plugin.GetAuthenticationProviderFactory(authenticationConfigFile));
        //          }
        //          catch (CompositionException ex)
        //          {
        //              Log.LogError(ex, "Error loading authentication plugin.");
        //          }
        //      }

        //      if (!authenticationTypeToPlugin.TryGetValue(authenticationType.ToLowerInvariant(), out Func<IAuthenticationProviderFactory> factory))
        //      {
        //          throw new ApplicationInitializationException(string.Format("The authentication type {0} is not recognised. If this is supposed " +
        //                      "to be provided by an authentication plugin, confirm the plugin DLL is located in {1}.\n" +
        //                      "Valid options for authentication are: {2}.", authenticationType, Locations.PluginsDirectory, string.Join(", ", authenticationTypeToPlugin.Keys)));
        //      }

        //      return factory();
        //  }

        //private static CompositionContainer FindPlugins()
        //{
        //    var catalog = new AggregateCatalog();

        //    catalog.Catalogs.Add(new AssemblyCatalog(typeof(Program).Assembly));

        //    if (Directory.Exists(Locations.PluginsDirectory))
        //    {
        //        Log.LogInformation("Plugins path: {0}", Locations.PluginsDirectory);
        //        catalog.Catalogs.Add(new DirectoryCatalog(Locations.PluginsDirectory));
        //    }
        //    else
        //    {
        //        Log.LogInformation("Cannot find plugins path: {0}", Locations.PluginsDirectory);
        //    }

        //    return new CompositionContainer(catalog);
        //}

        protected override void OnStart()
        {
            _node.Start();
        }

        protected override bool OnStop()
        {
            //_node.StopNonblocking(true, true);
            return _node.Stop(TimeSpan.FromSeconds(20), true, true);
        }

        protected override void OnProgramExit()
        {
            try
            {
                if (_dbLock is object && _dbLock.IsAcquired) { _dbLock.Release(); }
            }
            catch (Exception exc)
            {
                Log.LogError(exc.ToString());
            }
            //try
            //{
            //    if (_clusterNodeMutex is object && _clusterNodeMutex.IsAcquired) { _clusterNodeMutex.Release(); }
            //}
            //catch (Exception exc)
            //{
            //    Log.LogError(exc.ToString());
            //}
        }

        //protected override bool GetIsStructuredLog(ClusterNodeOptions options)
        //{
        //    return options.StructuredLog;
        //}
    }
}
