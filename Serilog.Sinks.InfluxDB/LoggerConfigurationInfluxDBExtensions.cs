using Serilog.Configuration;
using Serilog.Events;
using Serilog.Sinks.InfluxDB;
using Serilog.Sinks.PeriodicBatching;

namespace Serilog;

/// <summary>
/// Adds the WriteTo.InfluxDB() extension method to <see cref="LoggerConfiguration"/>.
/// </summary>
public static class LoggerConfigurationInfluxDBExtensions
{
    public static LoggerConfiguration InfluxDB(
        this LoggerSinkConfiguration loggerConfiguration,
        string? applicationName,
        string uriString,
        string organizationId,
        string bucketName = InfluxDBDefaults.DefaultBucketName,
        string? instanceName = null,
        string? token = null,
        LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
        PeriodicBatchingSinkOptions? batchingOptions = null,
        IFormatProvider? formatProvider = null,
        bool includeFullException = false, 
        bool includeHostname = true, 
        bool includeLevel = true, 
        bool includeSeverity = true,
        bool includeFacility = true,
        bool includeTimestamp = true,
        bool includeVersion = true,
        bool includeProcId = true)
    {
        if (string.IsNullOrEmpty(uriString)) throw new ArgumentNullException(nameof(uriString));
        if (!Uri.TryCreate(uriString, UriKind.Absolute, out var _)) throw new ArgumentException($"Invalid uri : {uriString}");

        return InfluxDB(loggerConfiguration, applicationName, new Uri(uriString), organizationId, bucketName, instanceName,
            token, restrictedToMinimumLevel, batchingOptions, formatProvider, includeFullException, includeHostname, 
            includeLevel, includeSeverity, includeFacility, includeTimestamp, includeVersion, includeProcId);
    }

    /// <summary>
    /// Adds the WriteTo.InfluxDB() extension method to <see cref="LoggerConfiguration"/>.
    /// No application name and instance name will be defined as tag using this method.
    /// </summary>
    public static LoggerConfiguration InfluxDB(
        this LoggerSinkConfiguration loggerConfiguration,
        string uriString,
        string organizationId,
        string bucketName = InfluxDBDefaults.DefaultBucketName,
        string? token = null,
        LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
        PeriodicBatchingSinkOptions? batchingOptions = null,
        IFormatProvider? formatProvider = null,
        bool includeFullException = false,
        bool includeHostname = true,
        bool includeLevel = true,
        bool includeSeverity = true,
        bool includeFacility = true,
        bool includeTimestamp = true,
        bool includeVersion = true,
        bool includeProcId = true)
    {
        if (string.IsNullOrEmpty(uriString)) throw new ArgumentNullException(nameof(uriString));
        if (!Uri.TryCreate(uriString, UriKind.Absolute, out var _)) throw new ArgumentException($"Invalid uri : {uriString}");

        return InfluxDB(loggerConfiguration, null, new Uri(uriString), organizationId, bucketName, null,
            token, restrictedToMinimumLevel, batchingOptions, formatProvider, includeFullException, includeHostname, 
            includeLevel, includeSeverity, includeFacility, includeTimestamp, includeVersion, includeProcId);
    }

    /// <summary>
    /// Adds the WriteTo.InfluxDB() extension method to <see cref="LoggerConfiguration"/>.
    /// </summary>
    public static LoggerConfiguration InfluxDB(
        this LoggerSinkConfiguration loggerConfiguration,
        string? applicationName,
        Uri uri,
        string organizationId,
        string bucketName = InfluxDBDefaults.DefaultBucketName,
        string? instanceName = null,
        string? token = null,
        LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
        PeriodicBatchingSinkOptions? batchingOptions = null,
        IFormatProvider? formatProvider = null,
        bool includeFullException = false,
        bool includeHostname = true,
        bool includeLevel = true,
        bool includeSeverity = true,
        bool includeFacility = true,
        bool includeTimestamp = true,
        bool includeVersion = true,
        bool includeProcId = true)
    {
        if (uri is null) throw new ArgumentNullException(nameof(uri));
        if (loggerConfiguration is null) throw new ArgumentNullException(nameof(loggerConfiguration));
        if (string.IsNullOrEmpty(bucketName)) throw new ArgumentException("Bucket name must not be empty", nameof(bucketName));

        var sinkOptions = new InfluxDBSinkOptions()
        {
            ApplicationName = applicationName,
            InstanceName = instanceName,
            ConnectionInfo = new InfluxDBConnectionInfo
            {
                Uri = uri,
                BucketName = bucketName,
                OrganizationId = organizationId,
                Token = token
            },
            BatchOptions = batchingOptions,
            FormatProvider = formatProvider,
            IncludeFullException = includeFullException,
            IncludeHostname = includeHostname,
            IncludeLevel = includeLevel,
            IncludeSeverity = includeSeverity,
            IncludeFacility = includeFacility,
            IncludeTimestamp = includeTimestamp,
            IncludeVersion = includeVersion,
            IncludeProcId = includeProcId,
        };

        return InfluxDB(loggerConfiguration, sinkOptions, restrictedToMinimumLevel);
    }

    /// <summary>
    /// Adds the WriteTo.InfluxDB() extension method to <see cref="LoggerConfiguration"/>.
    /// No application name and instance name will be defined as tag using this method.
    /// </summary>
    public static LoggerConfiguration InfluxDB(
        this LoggerSinkConfiguration loggerConfiguration,
        Uri uri,
        string organizationId,
        string bucketName = InfluxDBDefaults.DefaultBucketName,
        string? token = null,
        LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
        PeriodicBatchingSinkOptions? batchingOptions = null,
        IFormatProvider? formatProvider = null,
        bool includeFullException = false,
        bool includeHostname = true,
        bool includeLevel = true,
        bool includeSeverity = true,
        bool includeFacility = true,
        bool includeTimestamp = true,
        bool includeVersion = true,
        bool includeProcId = true)
    {
        if (uri is null) throw new ArgumentNullException(nameof(uri));
        if (loggerConfiguration is null) throw new ArgumentNullException(nameof(loggerConfiguration));
        if (string.IsNullOrEmpty(bucketName)) throw new ArgumentException("Bucket name must not be empty", nameof(bucketName));

        var sinkOptions = new InfluxDBSinkOptions()
        {
            ConnectionInfo = new InfluxDBConnectionInfo
            {
                Uri = uri,
                BucketName = bucketName,
                OrganizationId = organizationId,
                Token = token
            },
            BatchOptions = batchingOptions,
            FormatProvider = formatProvider,
            IncludeFullException = includeFullException,
            IncludeHostname = includeHostname,
            IncludeLevel = includeLevel,
            IncludeSeverity = includeSeverity,
            IncludeFacility = includeFacility,
            IncludeTimestamp = includeTimestamp,
            IncludeVersion = includeVersion,
            IncludeProcId = includeProcId,
    };

        return InfluxDB(loggerConfiguration, sinkOptions, restrictedToMinimumLevel);
    }

    /// <summary>
    /// Adds the WriteTo.InfluxDB() extension method to <see cref="LoggerConfiguration"/>.
    /// </summary>
    public static LoggerConfiguration InfluxDB(
        this LoggerSinkConfiguration loggerConfiguration,
        InfluxDBSinkOptions sinkOptions,
        LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum)
    {
        if (sinkOptions is null) throw new ArgumentNullException(nameof(sinkOptions));

        var defaultOptions = new PeriodicBatchingSinkOptions();

        if (sinkOptions.BatchOptions is null)
        {
            sinkOptions.BatchOptions = defaultOptions; // initialized with default from lib
        }

        if (sinkOptions.BatchOptions.QueueLimit == defaultOptions.QueueLimit)
        {
            // set back to null as don't want to have queue limit if was read null from settings file
            sinkOptions.BatchOptions.QueueLimit = null;
        }

#pragma warning disable CA2000 // Dispose objects before losing scope => serilog disposes them
        var influxDbSink = new InfluxDBSink(sinkOptions);
        var batchingSink = new PeriodicBatchingSink(influxDbSink, sinkOptions.BatchOptions);

        return loggerConfiguration.Sink(batchingSink, restrictedToMinimumLevel);
#pragma warning restore CA2000 // Dispose objects before losing scope
    }
}
