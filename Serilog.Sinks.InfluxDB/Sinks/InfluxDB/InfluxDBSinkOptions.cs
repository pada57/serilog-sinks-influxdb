using Serilog.Sinks.PeriodicBatching;

namespace Serilog.Sinks.InfluxDB;

public class InfluxDBSinkOptions
{
    public string? ApplicationName { get; set; }

    public string? InstanceName { get; set; }

    public bool? IncludeFullException { get; set; } = false;
    
    public bool? IncludeHostname { get; set; } = true; 

    public InfluxDBConnectionInfo? ConnectionInfo { get; set; } = new();

    public PeriodicBatchingSinkOptions? BatchOptions { get; set; } = new();

    public IFormatProvider? FormatProvider { get; set; }

    public string[]? ExtendedTags { get; set; }

    public string[]? ExtendedFields { get; set; }
}
