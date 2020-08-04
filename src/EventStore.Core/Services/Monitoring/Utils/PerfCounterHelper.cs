using System;
using System.Diagnostics;
using EventStore.Common.Utils;
using EventStore.Core.Services.Monitoring.Stats;
using Microsoft.Extensions.Logging;

namespace EventStore.Core.Services.Monitoring.Utils
{
    internal class PerfCounterHelper : IDisposable
    {
        private const string DotNetProcessIdCounterName = "Process Id";
        private const string ProcessIdCounterName = "ID Process";
        private const string DotNetMemoryCategory = ".NET CLR Memory";
        private const string ProcessCategory = "Process";
        private const int InvalidCounterResult = -1;

        private readonly ILogger _log;

        private readonly PerformanceCounter _totalCpuCounter;
        private readonly PerformanceCounter _totalMemCounter; //doesn't work on mono
        private readonly PerformanceCounter _procCpuCounter;
        private readonly PerformanceCounter _procThreadsCounter;

        private readonly PerformanceCounter _thrownExceptionsRateCounter;
        private readonly PerformanceCounter _contentionsRateCounter;

        private readonly PerformanceCounter _gcGen0ItemsCounter;
        private readonly PerformanceCounter _gcGen1ItemsCounter;
        private readonly PerformanceCounter _gcGen2ItemsCounter;
        private readonly PerformanceCounter _gcGen0SizeCounter;
        private readonly PerformanceCounter _gcGen1SizeCounter;
        private readonly PerformanceCounter _gcGen2SizeCounter;
        private readonly PerformanceCounter _gcLargeHeapSizeCounter;
        private readonly PerformanceCounter _gcAllocationSpeedCounter;
        private readonly PerformanceCounter _gcTimeInGcCounter;
        private readonly PerformanceCounter _gcTotalBytesInHeapsCounter;
        private readonly int _pid;

        public PerfCounterHelper(ILogger log)
        {
            _log = log;

            var currentProcess = Process.GetCurrentProcess();
            _pid = currentProcess.Id;

            _totalCpuCounter = CreatePerfCounter("Processor", "% Processor Time", "_Total");
            _totalMemCounter = CreatePerfCounter("Memory", "Available Bytes");

            var processInstanceName = GetProcessInstanceName(ProcessCategory, ProcessIdCounterName);

            if (processInstanceName is object)
            {
                _procCpuCounter = CreatePerfCounter(ProcessCategory, "% Processor Time", processInstanceName);
                _procThreadsCounter = CreatePerfCounter(ProcessCategory, "Thread Count", processInstanceName);
            }

            var netInstanceName = GetProcessInstanceName(DotNetMemoryCategory, DotNetProcessIdCounterName);

            if (netInstanceName is object)
            {
                _thrownExceptionsRateCounter =
                    CreatePerfCounter(".NET CLR Exceptions", "# of Exceps Thrown / sec", netInstanceName);
                _contentionsRateCounter =
                    CreatePerfCounter(".NET CLR LocksAndThreads", "Contention Rate / sec", netInstanceName);
                _gcGen0ItemsCounter = CreatePerfCounter(DotNetMemoryCategory, "# Gen 0 Collections", netInstanceName);
                _gcGen1ItemsCounter = CreatePerfCounter(DotNetMemoryCategory, "# Gen 1 Collections", netInstanceName);
                _gcGen2ItemsCounter = CreatePerfCounter(DotNetMemoryCategory, "# Gen 2 Collections", netInstanceName);
                _gcGen0SizeCounter = CreatePerfCounter(DotNetMemoryCategory, "Gen 0 heap size", netInstanceName);
                _gcGen1SizeCounter = CreatePerfCounter(DotNetMemoryCategory, "Gen 1 heap size", netInstanceName);
                _gcGen2SizeCounter = CreatePerfCounter(DotNetMemoryCategory, "Gen 2 heap size", netInstanceName);
                _gcLargeHeapSizeCounter = CreatePerfCounter(DotNetMemoryCategory, "Large Object Heap size",
                    netInstanceName);
                _gcAllocationSpeedCounter =
                    CreatePerfCounter(DotNetMemoryCategory, "Allocated Bytes/sec", netInstanceName);
                _gcTimeInGcCounter = CreatePerfCounter(DotNetMemoryCategory, "% Time in GC", netInstanceName);
                _gcTotalBytesInHeapsCounter =
                    CreatePerfCounter(DotNetMemoryCategory, "# Bytes in all Heaps", netInstanceName);
            }
        }

        private PerformanceCounter CreatePerfCounter(string category, string counter, string instance = null)
        {
            try
            {
                return string.IsNullOrEmpty(instance)
                    ? new PerformanceCounter(category, counter)
                    : new PerformanceCounter(category, counter, instance);
            }
            catch (Exception ex)
            {
                if (_log.IsTraceLevelEnabled()) _log.CouldNotCreatePerformanceCounter(category, counter, instance, ex);
                return null;
            }
        }

        private string GetProcessInstanceName(string categoryName, string counterName)
        {
            // On Unix or MacOS, use the PID as the instance name
            if (Runtime.IsUnixOrMac)
            {
                return _pid.ToString();
            }

            try
            {
                if (PerformanceCounterCategory.Exists(categoryName))
                {
                    var category = new PerformanceCounterCategory(categoryName).ReadCategory();

                    if (category.Contains(counterName))
                    {
                        var instanceDataCollection = category[counterName];

                        if (instanceDataCollection.Values is object)
                        {
                            foreach (InstanceData item in instanceDataCollection.Values)

                            {
                                var instancePid = (int)item.RawValue;
                                if (_pid.Equals(instancePid))
                                {
                                    return item.InstanceName;
                                }
                            }
                        }
                    }
                }
            }
            catch (InvalidOperationException)
            {
                if (_log.IsTraceLevelEnabled()) _log.UnableToGetPerformanceCounterCategoryInstances(categoryName);
            }

            return null;
        }


        /// <summary>
        /// Re-examines the performance counter instances for the correct instance name for this process.
        /// </summary>
        /// <remarks>
        /// The performance counter instance on .NET Framework can change at any time
        /// due to creation or destruction of processes with the same image name. This method should be called before using the Get methods.
        ///
        /// The correct instance name must be found by dereferencing via a look up counter, e.g. .Net CLR Memory/Process Id
        /// </remarks>
        public void RefreshInstanceName()
        {
            if (!Runtime.IsWindows)
            {
                return;
            }

            if (_procCpuCounter is object)
            {
                var processInstanceName = GetProcessInstanceName(ProcessCategory, ProcessIdCounterName);

                if (processInstanceName is object)
                {
                    if (_procCpuCounter is object) _procCpuCounter.InstanceName = processInstanceName;
                    if (_procThreadsCounter is object) _procThreadsCounter.InstanceName = processInstanceName;
                }
            }

            if (_gcLargeHeapSizeCounter is object)
            {
                var netInstanceName = GetProcessInstanceName(DotNetMemoryCategory, DotNetProcessIdCounterName);

                if (netInstanceName is object)
                {
                    if (_thrownExceptionsRateCounter is object)
                        _thrownExceptionsRateCounter.InstanceName = netInstanceName;
                    if (_contentionsRateCounter is object) _contentionsRateCounter.InstanceName = netInstanceName;
                    if (_gcGen0ItemsCounter is object) _gcGen0ItemsCounter.InstanceName = netInstanceName;
                    if (_gcGen1ItemsCounter is object) _gcGen1ItemsCounter.InstanceName = netInstanceName;
                    if (_gcGen2ItemsCounter is object) _gcGen2ItemsCounter.InstanceName = netInstanceName;
                    if (_gcGen0SizeCounter is object) _gcGen0SizeCounter.InstanceName = netInstanceName;
                    if (_gcGen1SizeCounter is object) _gcGen1SizeCounter.InstanceName = netInstanceName;
                    if (_gcGen2SizeCounter is object) _gcGen2SizeCounter.InstanceName = netInstanceName;
                    if (_gcLargeHeapSizeCounter is object) _gcLargeHeapSizeCounter.InstanceName = netInstanceName;
                    if (_gcAllocationSpeedCounter is object) _gcAllocationSpeedCounter.InstanceName = netInstanceName;
                    if (_gcTimeInGcCounter is object) _gcTimeInGcCounter.InstanceName = netInstanceName;
                    if (_gcTotalBytesInHeapsCounter is object) _gcTotalBytesInHeapsCounter.InstanceName = netInstanceName;
                }
            }
        }

        ///<summary>
        ///Total CPU usage in percentage
        ///</summary>
        public float GetTotalCpuUsage()
        {
            return _totalCpuCounter?.NextValue() ?? InvalidCounterResult;
        }

        ///<summary>
        ///Free memory in bytes
        ///</summary>
        public long GetFreeMemory()
        {
            return _totalMemCounter?.NextSample().RawValue ?? InvalidCounterResult;
        }

        ///<summary>
        ///Total process CPU usage
        ///</summary>
        public float GetProcCpuUsage()
        {
            return _procCpuCounter?.NextValue() ?? InvalidCounterResult;
        }

        ///<summary>
        ///Current thread count
        ///</summary>
        public int GetProcThreadsCount()
        {
            return (int)(_procThreadsCounter?.NextValue() ?? InvalidCounterResult);
        }

        ///<summary>
        ///Number of exceptions thrown per second
        ///</summary>
        public float GetThrownExceptionsRate()
        {
            return _thrownExceptionsRateCounter?.NextValue() ?? InvalidCounterResult;
        }

        ///<summary>
        ///The rate at which threads in the runtime attempt to acquire a managed lock unsuccessfully
        ///</summary>
        public float GetContentionsRateCount()
        {
            return _contentionsRateCounter?.NextValue() ?? InvalidCounterResult;
        }

        public GcStats GetGcStats()
        {
            return new GcStats(
                gcGen0Items: _gcGen0ItemsCounter?.NextSample().RawValue ?? InvalidCounterResult,
                gcGen1Items: _gcGen1ItemsCounter?.NextSample().RawValue ?? InvalidCounterResult,
                gcGen2Items: _gcGen2ItemsCounter?.NextSample().RawValue ?? InvalidCounterResult,
                gcGen0Size: _gcGen0SizeCounter?.NextSample().RawValue ?? InvalidCounterResult,
                gcGen1Size: _gcGen1SizeCounter?.NextSample().RawValue ?? InvalidCounterResult,
                gcGen2Size: _gcGen2SizeCounter?.NextSample().RawValue ?? InvalidCounterResult,
                gcLargeHeapSize: _gcLargeHeapSizeCounter?.NextSample().RawValue ?? InvalidCounterResult,
                gcAllocationSpeed: _gcAllocationSpeedCounter?.NextValue() ?? InvalidCounterResult,
                gcTimeInGc: _gcTimeInGcCounter?.NextValue() ?? InvalidCounterResult,
                gcTotalBytesInHeaps: _gcTotalBytesInHeapsCounter?.NextSample().RawValue ?? InvalidCounterResult);
        }

        public void Dispose()
        {
            _totalCpuCounter?.Dispose();
            _totalMemCounter?.Dispose();
            _procCpuCounter?.Dispose();
            _procThreadsCounter?.Dispose();

            _thrownExceptionsRateCounter?.Dispose();
            _contentionsRateCounter?.Dispose();

            _gcGen0ItemsCounter?.Dispose();
            _gcGen1ItemsCounter?.Dispose();
            _gcGen2ItemsCounter?.Dispose();
            _gcGen0SizeCounter?.Dispose();
            _gcGen1SizeCounter?.Dispose();
            _gcGen2SizeCounter?.Dispose();
            _gcLargeHeapSizeCounter?.Dispose();
            _gcAllocationSpeedCounter?.Dispose();
            _gcTimeInGcCounter?.Dispose();
            _gcTotalBytesInHeapsCounter?.Dispose();
        }

    }
}
