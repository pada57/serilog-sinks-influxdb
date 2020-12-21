using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Serilog.Sinks.InfluxDB;

namespace Serilog.Sinks.InfluxDb.Benchmarks
{
    public class SerilogInfluxDBBenchmarks
    {
        private ILogger log { get; set; }
        [Params(1000)]
        public int N;

        [GlobalSetup]
        public void Setup()
        {
            var loggerConfig = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.InfluxDB("benchmark", "benchmarkInstance", new InfluxDBConnectionInfo()
                {
                    Uri = new Uri("http://127.0.0.1:8086"),
                    DbName = "_internal",
                });
            log = loggerConfig.CreateLogger();
        }

        public SerilogInfluxDBBenchmarks()
        {

        }

        [Benchmark]
        public void LogSomething() => log.Error($"Error {N}");
    }
    class Program
    {
        protected Program()
        { }
        static void Main(string[] args)
        {
            var loggerConfig = new LoggerConfiguration()
                    .MinimumLevel.Information()
                    .WriteTo.InfluxDB("benchmark", "local", new InfluxDBConnectionInfo()
                    {
                        Uri = new Uri("http://127.0.0.1:8086"),
                        DbName = "_internal",
                    });
            var log = loggerConfig.CreateLogger();

            Console.WriteLine("Hello World!");
            log.Error("Error Console");
            log.Debug("Debug Console");

#if RELEASE
            var summary = BenchmarkRunner.Run<SerilogInfluxDBBenchmarks>();
#endif

            Console.WriteLine("Hello World!");
            log.Error("Error Console");
            log.Debug("Debug Console");
            log.Warning("Warn Console");
            log.Information("Info Console");

            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }
    }
}
