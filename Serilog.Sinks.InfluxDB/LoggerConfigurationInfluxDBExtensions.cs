using Serilog.Configuration;
using Serilog.Events;
using Serilog.Sinks.InfluxDB;
using Serilog.Sinks.PeriodicBatching;
using System;

namespace Serilog
{
    /// <summary>
    /// Adds the WriteTo.InfluxDB() extension method to <see cref="LoggerConfiguration"/>.
    /// </summary>
    public static class LoggerConfigurationInfluxDBExtensions
    {
        public static LoggerConfiguration InfluxDB(
            this LoggerSinkConfiguration loggerConfiguration,
            string applicationName,
            string uriString,
            string dbName = InfluxDBDefaults.DefaultDbName,
            string instanceName = null,
            string username = null,
            string password = null,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
            PeriodicBatchingSinkOptions batchingOptions = null,
            IFormatProvider formatProvider = null,
            bool includeFullException = false)
        {
            if (string.IsNullOrEmpty(uriString)) throw new ArgumentNullException(nameof(uriString));
            if (!Uri.TryCreate(uriString, UriKind.Absolute, out var _)) throw new ArgumentException($"Invalid uri : {uriString}");

            return InfluxDB(loggerConfiguration, applicationName, new Uri(uriString), dbName, instanceName,
                username, password, restrictedToMinimumLevel, batchingOptions, formatProvider);
        }

        /// <summary>
        /// Adds the WriteTo.InfluxDB() extension method to <see cref="LoggerConfiguration"/>.
        /// </summary>
        public static LoggerConfiguration InfluxDB(
            this LoggerSinkConfiguration loggerConfiguration,
            string applicationName,
            Uri uri,
            string dbName = InfluxDBDefaults.DefaultDbName,
            string instanceName = null,
            string username = null,
            string password = null,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
            PeriodicBatchingSinkOptions batchingOptions = null,
            IFormatProvider formatProvider = null,
            bool includeFullException = false)
        {
            if (uri == null) throw new ArgumentNullException(nameof(uri));
            if (loggerConfiguration == null) throw new ArgumentNullException(nameof(loggerConfiguration));
            if (string.IsNullOrEmpty(dbName)) throw new ArgumentException(nameof(dbName));

            var sinkOptions = new InfluxDBSinkOptions()
            {
                ApplicationName = applicationName,
                InstanceName = instanceName,
                ConnectionInfo = new InfluxDBConnectionInfo
                {
                    Uri = uri,
                    DbName = dbName,
                    Username = username,
                    Password = password
                },
                BatchOptions = batchingOptions,
                FormatProvider = formatProvider,
                IncludeFullException = includeFullException
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
            if (sinkOptions == null) throw new ArgumentNullException(nameof(sinkOptions));

            var defaultOptions = new PeriodicBatchingSinkOptions();

            if (sinkOptions.BatchOptions == null)
            {
                sinkOptions.BatchOptions = defaultOptions; // initialized with default from lib
            }

            if (sinkOptions.BatchOptions.QueueLimit == defaultOptions.QueueLimit)
            {
                // set back to null as don't want to have queue limit if was read null from settings file
                sinkOptions.BatchOptions.QueueLimit = null;
            }

            var influxDbSink = new InfluxDBSink(sinkOptions);
            var batchingSink = new PeriodicBatchingSink(influxDbSink, sinkOptions.BatchOptions);

            return loggerConfiguration.Sink(batchingSink, restrictedToMinimumLevel);
        }
    }
}