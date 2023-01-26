using InfluxData.Net.Common.Enums;
using InfluxData.Net.InfluxDb;
using InfluxData.Net.InfluxDb.Models;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Sinks.PeriodicBatching;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using static Serilog.Sinks.InfluxDB.SyslogConst;

namespace Serilog.Sinks.InfluxDB
{
    internal class InfluxDBSink : IBatchedLogEventSink, IDisposable
    {
        private readonly string _applicationName;
        private readonly string _instanceName;
        private readonly bool _includeFullException;

        private readonly IFormatProvider _formatProvider;

        /// <summary>
        /// Connection info used to connect to InfluxDB instance.
        /// </summary>
        private readonly InfluxDBConnectionInfo _connectionInfo;

        /// <summary>
        /// Client object used to connect to InfluxDB instance.
        /// </summary>
        private InfluxDbClient _influxDbClient;

        /// <inheritdoc />
        /// <summary>
        /// Construct a sink inserting into InfluxDB with the specified details.
        /// </summary>
        /// <param name="connectionInfo">Connection information used to construct InfluxDB client.</param>
        /// <param name="applicationName">Application name in the InfluxDB database.</param>
        /// <param name="instanceName">Facility name in the InfluxDB database.</param>
        /// <param name="batchSizeLimit">The maximum number of events to post in a single batch.</param>
        /// <param name="period">The time to wait between checking for event batches.</param>
        /// <param name="formatProvider"></param>
        public InfluxDBSink(InfluxDBSinkOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            _connectionInfo = options.ConnectionInfo ?? throw new ArgumentNullException(nameof(options.ConnectionInfo));

            if (options.ApplicationName == null) throw new ArgumentNullException(nameof(options.ApplicationName));
            if (_connectionInfo.Uri == null) throw new ArgumentNullException(nameof(_connectionInfo.Uri));
            if (_connectionInfo.DbName == null) throw new ArgumentNullException(nameof(_connectionInfo.DbName));
            if (_connectionInfo.Username == null) _connectionInfo.Username = string.Empty;
            if (_connectionInfo.Password == null) _connectionInfo.Password = string.Empty;

            _applicationName = options.ApplicationName;
            _instanceName = options.InstanceName ?? _applicationName;
            _formatProvider = options.FormatProvider;
            _includeFullException = options.IncludeFullException;

            _influxDbClient = CreateInfluxDbClient();

            CreateDatabaseIfNotExists();
        }

        /// <inheritdoc />
        /// <summary>
        /// Emit a batch of log events, running asynchronously.
        /// </summary>
        /// <param name="events">The events to emit.</param>
        public async Task EmitBatchAsync(IEnumerable<LogEvent> batch)
        {
            if (batch == null) throw new ArgumentNullException(nameof(batch));

            var logEvents = batch as List<LogEvent> ?? batch.ToList();
            var points = new List<Point>(logEvents.Count);

            foreach (var logEvent in logEvents)
            {
                var p = new Point
                {
                    Name = PointName,
                    Fields = new Dictionary<string, object>(),
                    Timestamp = logEvent.Timestamp.UtcDateTime
                };

                // Add tags
                if (logEvent.Exception != null) p.Tags.Add("exceptionType", logEvent.Exception.GetType().Name);

                var severity = logEvent.Level.ToSeverity();

                p.Tags.Add(Tags.Level, logEvent.Level.ToString());
                p.Tags.Add(Tags.AppName, _applicationName);
                p.Tags.Add(Tags.Facility, _instanceName);
                p.Tags.Add(Tags.Hostname, Environment.MachineName);
                p.Tags.Add(Tags.Severity, severity.ToString());

                // Add Fields - rendered message
                p.Fields[Fields.Message] = StripSpecialCharacter(logEvent.RenderMessage(_formatProvider));
                p.Fields[Fields.Facility] = 16;
                p.Fields[Fields.ProcId] = Process.GetCurrentProcess().Id.ToString();
                p.Fields[Fields.Severity] = ((int)severity);
                p.Fields[Fields.Timestamp] = logEvent.Timestamp.ToUnixTimeMilliseconds() * 1000000;
                p.Fields[Fields.Version] = 1;
                
                if (_includeFullException && logEvent.Exception != null) p.Fields[Fields.Exception] = logEvent.Exception.ToString();

                points.Add(p);
            }


            var response = await _influxDbClient.Client.WriteAsync(points, _connectionInfo.DbName);

            if (!response.Success)
            {
                // in case InfluxDbClient not throwing exception, throw new Exception to be handle by base class PeriodicBatchingSink

                var message = $@"A status code of {response.StatusCode} was received when attempting to send to {_connectionInfo?.Uri}.
The event has been discarded and will not be placed back in the queue.
Response body: {response.Body}";

                SelfLog.WriteLine(message);

                throw new InfluxDbClientWriteException(message);
            }
        }

        public Task OnEmptyBatchAsync()
        {
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _influxDbClient = null;
        }


        private string StripSpecialCharacter(string text)
        {
            return text != null ?
                    HttpUtility.JavaScriptStringEncode(text)
                : string.Empty;
        }

        /// <summary>
        /// Initialize and return an InfluxDB client object.
        /// </summary>
        /// <returns></returns>
        private InfluxDbClient CreateInfluxDbClient()
        {
            return new InfluxDbClient(
                _connectionInfo.Uri.ToString(),
                _connectionInfo.Username,
                _connectionInfo.Password,
                InfluxDbVersion.Latest);
        }

        /// <summary>
        /// Create the log database in InfluxDB if it does not exists. 
        /// Synchronous as should be done prior any emit done and also as don't want to move check on each emit if db exists
        /// </summary>
        private void CreateDatabaseIfNotExists()
        {
            var dbList = _influxDbClient.Database.GetDatabasesAsync().GetAwaiter().GetResult();
            if (dbList.All(db => db.Name != _connectionInfo.DbName))
            {
                var _ = _influxDbClient.Database.CreateDatabaseAsync(_connectionInfo.DbName).GetAwaiter().GetResult();
            }
        }

    }
}
