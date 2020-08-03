// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using BenchmarkDotNet.Running;

namespace EventStore.ClientAPI.Benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args == null || args.Length == 0)
            {   // if no args, we're probably using Ctrl+F5 in the IDE; enlargen thyself!
                try
                {
                    Console.WindowWidth = Console.LargestWindowWidth - 20;
                }
                catch { }
            }

            //BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);

            var mb = new MatchHandlerBenchmark();
            mb.GlobalSetup();
            var sw = new Stopwatch();
            sw.Start();
            for (int i = 0; i < 100000; i++)
            {
                mb.EventStoreHandler();
            }
            sw.Stop();
            Console.WriteLine("  Time used: {0,9} ticks", sw.ElapsedTicks);

            sw.Restart();
            for (int i = 0; i < 100000; i++)
            {
                mb.SimpleMatcher();
            }
            sw.Stop();
            Console.WriteLine("  Time used: {0,9} ticks", sw.ElapsedTicks);

            sw.Restart();
            for (int i = 0; i < 100000; i++)
            {
                mb.EsPackageHandler();
            }
            sw.Stop();
            Console.WriteLine("  Time used: {0,9} ticks", sw.ElapsedTicks);

            sw.Restart();
            for (int i = 0; i < 100000; i++)
            {
                mb.EsPackageHandler2();
            }
            sw.Stop();
            Console.WriteLine("  Time used: {0,9} ticks", sw.ElapsedTicks);

            sw.Restart();
            for (int i = 0; i < 100000; i++)
            {
                mb.ActionMatcher();
            }
            sw.Stop();
            Console.WriteLine("  Time used: {0,9} ticks", sw.ElapsedTicks);

            Console.WriteLine("按任意键退出");
            Console.ReadKey();
        }
    }
}