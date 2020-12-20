using InfluxData.Net.Common.Enums;
using InfluxData.Net.InfluxDb;
using InfluxData.Net.InfluxDb.Models;
using Serilog.Events;
using Serilog.Sinks.PeriodicBatching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Serilog.Sinks.InfluxDB
{
    internal class InfluxDBSink : PeriodicBatchingSink //InfluxDBSink
    {
        private readonly string _source;

        private readonly IFormatProvider _formatProvider;

        /// <summary>
        /// Connection info used to connect to InfluxDB instance.
        /// </summary>
        private readonly InfluxDBConnectionInfo _connectionInfo;

        /// <summary>
        /// Client object used to connect to InfluxDB instance.
        /// </summary>
        private readonly InfluxDbClient _influxDbClient;

        /// <summary>
        /// A reasonable default for the number of events posted in
        /// each batch.
        /// </summary>
        public const int DefaultBatchPostingLimit = 100;

        /// <summary>
        /// A reasonable default time to wait between checking for event batches.
        /// </summary>
        public static readonly TimeSpan DefaultPeriod = TimeSpan.FromSeconds(30);


        /// <inheritdoc />
        /// <summary>
        /// Construct a sink inserting into InfluxDB with the specified details.
        /// </summary>
        /// <param name="connectionInfo">Connection information used to construct InfluxDB client.</param>
        /// <param name="source">Measurement name in the InfluxDB database.</param>
        /// <param name="batchSizeLimit">The maximum number of events to post in a single batch.</param>
        /// <param name="period">The time to wait between checking for event batches.</param>
        /// <param name="formatProvider"></param>
        public InfluxDBSink(InfluxDBConnectionInfo connectionInfo, string source, int batchSizeLimit, TimeSpan period,
            IFormatProvider formatProvider)
            : base(batchSizeLimit, period)
        {
            _connectionInfo = connectionInfo ?? throw new ArgumentNullException(nameof(connectionInfo));
            _source = source;
            _influxDbClient = CreateInfluxDbClient();
            _formatProvider = formatProvider;

            Task.Run(() => CreateDatabase());
        }

        /// <inheritdoc />
        /// <summary>
        /// Emit a batch of log events, running asynchronously.
        /// </summary>
        /// <param name="events">The events to emit.</param>
        /// <remarks>Override either <see cref="M:Serilog.Sinks.PeriodicBatching.PeriodicBatchingSink.EmitBatch(System.Collections.Generic.IEnumerable{Serilog.Events.LogEvent})" /> or <see cref="M:Serilog.Sinks.PeriodicBatching.PeriodicBatchingSink.EmitBatchAsync(System.Collections.Generic.IEnumerable{Serilog.Events.LogEvent})" />,
        /// not both.</remarks>
        protected override async Task EmitBatchAsync(IEnumerable<LogEvent> events)
        {
            if (events == null){
                throw new ArgumentNullException(nameof(events));
            }

            var logEvents = events as List<LogEvent> ?? events.ToList();
            var points = new List<Point>(logEvents.Count);

            foreach (var logEvent in logEvents)
            {
                SyslogSeverity severity = SerilogSyslogSeverityConvertor.GetSyslogSeverity(logEvent.Level);

                var p = new Point
                {
                    Name = _source,
                    Fields = logEvent.Properties.ToDictionary(k => k.Key, v => (object)v.Value),
                    Timestamp = logEvent.Timestamp.UtcDateTime
                };

                // Add tags
                if (logEvent.Exception != null){
                    p.Tags.Add("exceptionType", logEvent.Exception.GetType().Name);
                }

                if (logEvent.MessageTemplate != null){
                    p.Tags.Add("messageTemplate", logEvent.MessageTemplate.Text);
                }

                p.Tags.Add("appname", "app1");
                p.Tags.Add("facility", "MyFacility");
                p.Tags.Add("host", Environment.MachineName);
                p.Tags.Add("hostname", Environment.MachineName);
                p.Tags.Add("severity", severity.Severity);

                // Add rendered message
                p.Fields["facility_code"] = 16;
                p.Fields["message"] = logEvent.RenderMessage(_formatProvider);
                p.Fields["procid"] = "1234";
                p.Fields["severity_code"] = severity.SeverityCode;
                p.Fields["timestamp"] = DateTimeOffset.Now.ToUnixTimeMilliseconds() * 1000000;
                p.Fields["version"] = 1;

                points.Add(p);
            }

            await _influxDbClient.Client.WriteAsync(points, _connectionInfo.DbName);
        }

        /// <summary>
        /// Initialize and return an InfluxDB client object.
        /// </summary>
        /// <returns></returns>
        private InfluxDbClient CreateInfluxDbClient()
        {
            return new InfluxDbClient(
                $"{_connectionInfo.Address}:{_connectionInfo.Port}",
                _connectionInfo?.Username ?? "",
                _connectionInfo?.Password ?? "",
                InfluxDbVersion.Latest);
        }

        /// <summary>
        /// Create the log database in InfluxDB if it does not exists.
        /// </summary>
        private async Task CreateDatabase()
        {
            var dbList = await _influxDbClient.Database.GetDatabasesAsync();
            if (dbList.All(db => db.Name != _connectionInfo.DbName))
            {
                var _ = await _influxDbClient.Database.CreateDatabaseAsync(_connectionInfo.DbName);
            }
        }
    }
}
