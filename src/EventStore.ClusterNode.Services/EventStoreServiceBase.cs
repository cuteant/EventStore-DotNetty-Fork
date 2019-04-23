using System;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using CuteAnt.IO;
using CuteAnt.Pool;
using EventStore.Common.Exceptions;
using EventStore.Common.Options;
using EventStore.Common.Utils;
using EventStore.Core.Util;
using EventStore.Rags;
using Microsoft.Extensions.Logging;

namespace EventStore.ClusterNode
{
    public abstract class EventStoreServiceBase<TOptions> : IEventStoreService
      where TOptions : class, IOptions, new()
    {
        // ReSharper disable StaticFieldInGenericType
        protected static ILogger Log { get; set; }
        // ReSharper restore StaticFieldInGenericType

        protected abstract string GetLogsDirectory(TOptions options);
        protected abstract string GetComponentName(TOptions options);

        protected abstract void Create(TOptions options);
        protected abstract void PreInit(TOptions options);
        protected abstract void OnStart();
        protected abstract bool OnStop();
        protected abstract void OnProgramExit();

        public bool Start()
        {
            var success = false;

            try
            {
                //Application.RegisterExitAction(Exit);

                StringBuilderManager.DefaultPolicy.MaximumRetainedCapacity = 1024 * 32;

#if !NETCOREAPP
                MessagePack.MessagePackBinary.Shared = CuteAnt.Buffers.BufferManager.Shared;
#endif

                string[] args = null;
                var esConfigFile = ConfigurationManager.AppSettings.Get("esConfigFile");
                if (!string.IsNullOrWhiteSpace(esConfigFile))
                {
                    args = new string[] { "-config", PathHelper.ApplicationBasePathCombine(esConfigFile) };
                }
                var options = EventStoreOptions.Parse<TOptions>(args, Opts.EnvPrefix, Path.Combine(Locations.DefaultConfigurationDirectory, DefaultFiles.DefaultConfigFile));

                //if (options.Help)
                //{
                //  Console.WriteLine("Options:");
                //  Console.WriteLine(EventStoreOptions.GetUsage<TOptions>());
                //}
                //else if (options.Version)
                //{
                //  Console.WriteLine("EventStore version {0} ({1}/{2}, {3})",
                //                    VersionInfo.Version, VersionInfo.Branch, VersionInfo.Hashtag, VersionInfo.Timestamp);
                //  Application.ExitSilent(0, "Normal exit.");
                //}
                //else
                {
                    PreInit(options);
                    Init(options);
                    CommitSuicideIfInBoehmOrOnBadVersionsOfMono(options);
                    Create(options);
                    OnStart();

                    success = true;
                    //_exitEvent.Wait();
                }
            }
            catch (OptionException exc)
            {
                Log.LogError("Error while parsing options:");
                Log.LogError(FormatExceptionMessage(exc));
                Log.LogError("");
                Log.LogError("Options:");
                Log.LogError(EventStoreOptions.GetUsage<TOptions>());
            }
            catch (ApplicationInitializationException ex)
            {
                var msg = $"Application initialization error: {FormatExceptionMessage(ex)}";
                if (Log != null)
                {
                    Log.LogCritical(ex, msg);
                }
                else
                {
                    Log.LogError(msg);
                }
            }
            catch (Exception ex)
            {
                var msg = "Unhandled exception while starting application:";
                if (Log != null)
                {
                    Log.LogCritical(ex, msg);
                    Log.LogCritical(ex, "{0}", FormatExceptionMessage(ex));
                }
                else
                {
                    Log.LogError(msg);
                    Log.LogError(FormatExceptionMessage(ex));
                }
            }
            finally
            {
                //Log.Flush();
            }
            //Environment.Exit(_exitCode);

            return success;
        }

        private void CommitSuicideIfInBoehmOrOnBadVersionsOfMono(TOptions options)
        {
            if (!options.Force)
            {
                if (GC.MaxGeneration == 0)
                {
                    Application.Exit(3, "Appears that we are running in mono with boehm GC this is generally not a good idea, please run with sgen instead." +
                        "to run with sgen use mono --gc=sgen. If you really want to run with boehm GC you can use --force to override this error.");
                }
                //if (OS.IsUnix && !(OS.GetRuntimeVersion().StartsWith("4.6.2", StringComparison.Ordinal)))
                //{
                //    Log.LogWarning("You appear to be running a version of Mono which is untested and not supported. Only Mono 4.6.2 is supported at this time.");
                //}
            }
        }

        public bool Stop()
        {
            //LogManager.Finish();

            var result = false;
            try
            {
                result = OnStop();
            }
            catch (Exception exc)
            {
                Log.LogError(exc.ToString());
            }
            OnProgramExit();

            return result;
        }

        private void Init(TOptions options)
        {
            Application.AddDefines(options.Defines);

            var entryAsm = Assembly.GetEntryAssembly();
            var asmName = entryAsm.GetName().Name;
            var asmFullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, asmName +
#if DESKTOPCLR
                ".exe");
#else
                ".dll");
#endif
            var fileVersionInfo = FileVersionInfo.GetVersionInfo(asmFullPath);
            var buildTime = File.GetLastWriteTime(asmFullPath);
            var projName = asmName.Replace(".", " - ");
            var componentName = GetComponentName(options);

            Log.LogInformation($"{projName}, {componentName}");

            //string logsDirectory = Path.GetFullPath(options.Log.IsNotEmptyString() ? options.Log : GetLogsDirectory(options));
            //LogManager.Init(componentName, logsDirectory, Locations.DefaultConfigurationDirectory);

            Log.LogInformation("\n{0,-25} {1} ({2}, {3})", "ES VERSION:", fileVersionInfo.FileVersion, fileVersionInfo.ProductVersion.Replace(". Commit Hash: ", @"/"), buildTime.ToString("ddd, d MMMM yyyy HH:mm:ss zzz", CultureInfo.InvariantCulture));
            Log.LogInformation("{0,-25} {1} ({2})", "OS:", OS.OsFlavor, RuntimeInformation.OSDescription);
            Log.LogInformation("{0,-25} {1} ({2}-bit)", "RUNTIME:", RuntimeInformation.FrameworkDescription, Marshal.SizeOf(typeof(IntPtr)) * 8);
            Log.LogInformation("{0,-25} {1}", "GC:", GC.MaxGeneration == 0 ? "NON-GENERATION (PROBABLY BOEHM)" : string.Format("{0} GENERATIONS", GC.MaxGeneration + 1));
            Log.LogInformation("{0,-25} {1}", "LOGS:", ""); // LogManager.LogsDirectory
            Log.LogInformation("{0}", EventStoreOptions.DumpOptions());

            //if (options.WhatIf)
            //  Application.Exit(ExitCode.Success, "WhatIf option specified");
        }

        private string FormatExceptionMessage(Exception ex)
        {
            string msg = ex.Message;
            var exc = ex.InnerException;
            int cnt = 0;
            while (exc != null)
            {
                cnt += 1;
                msg += "\n" + new string(' ', 2 * cnt) + exc.Message;
                exc = exc.InnerException;
            }
            return msg;
        }

        protected static StoreLocation GetCertificateStoreLocation(string certificateStoreLocation)
        {
            if (!Enum.TryParse(certificateStoreLocation, out StoreLocation location))
            {
                throw new Exception($"Could not find certificate store location '{certificateStoreLocation}'");
            }
            return location;
        }

        protected static StoreName GetCertificateStoreName(string certificateStoreName)
        {
            if (!Enum.TryParse(certificateStoreName, out StoreName name))
            {
                throw new Exception($"Could not find certificate store name '{certificateStoreName}'");
            }
            return name;
        }
    }
}
