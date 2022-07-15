using Serilog.Sinks.PeriodicBatching;
using System;

namespace Serilog.Sinks.InfluxDB
{
    public class InfluxDBSinkOptions
    {
        public string ApplicationName { get; set; }

        public string InstanceName { get; set; }

        public bool? IncludeFullException { get; set; } = false;

        public bool? IncludeContextAttributes { get; set; } = false;

        public InfluxDBConnectionInfo ConnectionInfo { get; set; } = new InfluxDBConnectionInfo();

        public PeriodicBatchingSinkOptions BatchOptions { get; set; } = new PeriodicBatchingSinkOptions();

        public IFormatProvider FormatProvider { get; set; } = null;

    }
}
