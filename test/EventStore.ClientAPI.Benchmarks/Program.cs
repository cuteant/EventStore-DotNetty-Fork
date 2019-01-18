// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Reflection;
using BenchmarkDotNet.Running;

namespace EventStore.ClientAPI.Benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            //BenchmarkSwitcher.FromAssembly(typeof(Program).GetTypeInfo().Assembly).Run(args);

            Stopwatch sw = new Stopwatch();
            var benchmark = new MatchHandlerBenchmark();
            benchmark.GlobalSetup();

            sw.Start();
            for (int i = 0; i < 100000; i++)
            {
                benchmark.EventStoreHandler();
            }
            sw.Stop();
            Console.WriteLine("  Time used: {0,9} ticks", sw.ElapsedTicks);

            sw.Restart();
            for (int i = 0; i < 100000; i++)
            {
                benchmark.AkkaMatchHandler();
            }
            sw.Stop();
            Console.WriteLine("  Time used: {0,9} ticks", sw.ElapsedTicks);

            Console.WriteLine("按任意键");
            Console.ReadKey();
        }
    }
}