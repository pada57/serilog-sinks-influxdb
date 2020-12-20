using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using Xunit;

namespace Serilog.Sinks.InfluxDB.Tests
{
    public class InfluxAppenderBenchmarks
    {
        [Params(1000)]
        public int N;

        [GlobalSetup(Targets = new[] { nameof(LogSomethingInfluxWithLayout), nameof(LogSomethingInfluxWithLayoutInterp) })]
        public void SetupWithLayout()
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.InfluxDB("source", new InfluxDBConnectionInfo())
                .CreateLogger();


        }

        [Benchmark]
        public void LogSomethingInfluxWithLayout() => Log.Error("Error Console");

        [Benchmark]
        public void LogSomethingInfluxWithLayoutInterp() => Log.Error($"Error Console{N}");
    }

    [ExcludeFromCodeCoverage]
    public class InfluxAppenderBenchmarkTests : IDisposable
    {
        private bool disposedValue;

        [Fact]
        public void BenchmarkTest()
        {
            var summary = BenchmarkRunner.Run<InfluxAppenderBenchmarks>();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~InfluxAppenderBenchmarkTests()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public class SetupSerilog
        {
            public const string IndexPrefix = "logs-6x-default-";
            public const string TemplateName = "serilog-logs-6x";

            public SetupSerilog()
            {
                var loggerConfig = new LoggerConfiguration()
                    .MinimumLevel.Information()
                    .WriteTo.InfluxDB("source", new InfluxDBConnectionInfo());
                var logger = loggerConfig.CreateLogger();

                logger.Information("Hello Information");
                logger.Debug("Hello Debug");
                logger.Warning("Hello Warning");
                logger.Error("Hello Error");
                logger.Fatal("Hello Fatal");

                logger.Dispose();
            }
        }
    }
}