using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Toolchains.CsProj;
using BenchmarkDotNet.Toolchains.DotNetCli;
using BenchmarkDotNet.Validators;

namespace EventStore.ClientAPI.Benchmarks
{
    public class CoreConfig : ManualConfig
    {
        public CoreConfig()
        {
            Add(ConsoleLogger.Default);
            Add(MarkdownExporter.GitHub);

            Add(MemoryDiagnoser.Default);
            Add(StatisticColumn.OperationsPerSecond);
            Add(DefaultColumnProviders.Instance);

            Add(JitOptimizationsValidator.FailOnError);

#if NETCOREAPP
            Add(Job.Core
                .With(CsProjCoreToolchain.From(NetCoreAppSettings.NetCoreApp22))
                .With(RunStrategy.Throughput));
#else
            Add(Job.Clr
                .With(RunStrategy.Throughput));
#endif
        }
    }
}
