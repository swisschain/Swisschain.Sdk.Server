using System;
using System.Diagnostics;
using BenchmarkDotNet.Running;
using Swisschain.Sdk.Server.Benchmark.Grpc.Streaming;

namespace Swisschain.Sdk.Server.Benchmark
{
    class Program
    {
        public static void Main(string[] args)
        {
            //var config = new BenchmarkDotNet.Configs.DebugBuildConfig();

            //var x = new StreamServiceBenchmark();
            //x._totalCount = 100;
            //x.Setup();
            //var sw = new Stopwatch();
            //for (int i = 0; i < 100_000; i++)
            //{
            //    sw.Start();
            //    x.Base();
            //    sw.Stop();
            //    Console.WriteLine($"{sw.ElapsedMilliseconds} ms.");
            //    sw.Reset();
            //}
            //x.CleanUp();

            //var x = new StreamServiceMultipleClientsBenchmark();
            //x.TotalCount = 100;
            //x.ConnectedStreams = 10;
            //x.Setup();
            //var sw = new Stopwatch();
            //for (int i = 0; i < 10; i++)
            //{
            //    sw.Start();
            //    x.Base();
            //    sw.Stop();
            //    Console.WriteLine($"{sw.ElapsedMilliseconds} ms.");
            //    sw.Reset();
            //}
            //x.CleanUp();

            //BenchmarkDotNet.Running.BenchmarkRunner.Run<StreamServiceBenchmark>();
            //config.With(ConfigOptions.DisableOptimizationsValidator)
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        }
    }
}
