using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Core.Exceptions;
using InfluxDB.Client.Writes;
using Serilog.Debugging;
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
        private readonly string[] _extendedTags;
        private readonly string[] _extendedFields;

        private readonly bool _includeFullException;

        private readonly IFormatProvider _formatProvider;

        /// <summary>
        /// Connection info used to connect to InfluxDB instance.
        /// </summary>
        private readonly InfluxDBConnectionInfo _connectionInfo;
        private readonly AuthMethods _authMethod;

        /// <summary>
        /// Client object used to connect to InfluxDB instance.
        /// </summary>
        private InfluxDBClient _influxDbClient;

        private bool _disposed = false;

        /// <inheritdoc />
        /// <summary>
        /// Construct a sink inserting into InfluxDB with the specified details.
        /// </summary>
        /// <param name="connectionInfo">Connection information used to construct InfluxDB client.</param>
        /// <param name="applicationName">Application name in the InfluxDB bucket.</param>
        /// <param name="instanceName">Facility name in the InfluxDB bucket.</param>
        /// <param name="batchSizeLimit">The maximum number of events to post in a single batch.</param>
        /// <param name="period">The time to wait between checking for event batches.</param>
        /// <param name="formatProvider"></param>
        public InfluxDBSink(InfluxDBSinkOptions options)
        {
            if (options is null) throw new ArgumentNullException(nameof(options));

            _connectionInfo = options.ConnectionInfo ?? throw new ArgumentNullException(nameof(options.ConnectionInfo));
            _applicationName = options.ApplicationName ?? throw new ArgumentNullException(nameof(options.ApplicationName));

            if (_connectionInfo.Uri is null) throw new ArgumentNullException(nameof(_connectionInfo.Uri));
            if (_connectionInfo.BucketName is null) throw new ArgumentNullException(nameof(_connectionInfo.BucketName));
            if (_connectionInfo.OrganizationId is null) throw new ArgumentNullException(nameof(_connectionInfo.OrganizationId));
            if (string.IsNullOrWhiteSpace(_connectionInfo.Token) && string.IsNullOrWhiteSpace(_connectionInfo.AllAccessToken) && string.IsNullOrWhiteSpace(_connectionInfo.Username))
                throw new ArgumentNullException($"At least one authentication field should be provided: {nameof(_connectionInfo.Username)}/{nameof(_connectionInfo.Password)} or {nameof(_connectionInfo.Token)}({nameof(_connectionInfo.AllAccessToken)} If Buckets has to be check and created if needed)");

            if (string.IsNullOrWhiteSpace(_connectionInfo.Username))
                _authMethod = AuthMethods.Token;
            else
                _authMethod = AuthMethods.Credentials;

            if (_authMethod == AuthMethods.Token && string.IsNullOrWhiteSpace(_connectionInfo.Token) && string.IsNullOrWhiteSpace(_connectionInfo.AllAccessToken) && string.IsNullOrWhiteSpace(_connectionInfo.Username))
                throw new ArgumentNullException(nameof(_connectionInfo.Token), $"At least one Token should be given either {nameof(_connectionInfo.Token)} if already created with write permissions or {nameof(_connectionInfo.AllAccessToken)}");

            _instanceName = options.InstanceName ?? _applicationName;
            _formatProvider = options.FormatProvider;

            _includeFullException = options.IncludeFullException ?? false;

            CreateBucketIfNotExists();

            _influxDbClient = CreateInfluxDbClientWithWriteAccess();

            _extendedFields = options.ExtendedFields ?? Array.Empty<string>();

            _extendedTags = options.ExtendedTags ?? Array.Empty<string>();
        }

        /// <inheritdoc />
        /// <summary>
        /// Emit a batch of log events, running asynchronously.
        /// </summary>
        /// <param name="events">The events to emit.</param>
        public Task EmitBatchAsync(IEnumerable<Events.LogEvent> batch)
        {
            if (batch is null) throw new ArgumentNullException(nameof(batch));

            var logEvents = batch as List<Events.LogEvent> ?? batch.ToList();
            var points = new List<PointData>(logEvents.Count);

            foreach (var logEvent in logEvents)
            {
                var severity = logEvent.Level.ToSeverity();

                var p = PointData.Builder.Measurement(PointName)
                    .Tag(Tags.Level, logEvent.Level.ToString())
                    .Tag(Tags.AppName, _applicationName)
                    .Tag(Tags.Facility, _instanceName)
                    .Tag(Tags.Hostname, Environment.MachineName)
                    .Tag(Tags.Severity, severity.ToString())
                    .Field(Fields.Message, StripSpecialCharacter(logEvent.RenderMessage(_formatProvider)))
                    .Field(Fields.Facility, Fields.Values.Facility)
                    .Field(Fields.ProcId, Process.GetCurrentProcess().Id.ToString())
                    .Field(Fields.Severity, severity.ToString())
                    .Field(Fields.Timestamp, logEvent.Timestamp.ToUnixTimeMilliseconds() * 1000000)
                    .Field(Fields.Version, Fields.Values.Version)
                    .Timestamp(logEvent.Timestamp.UtcDateTime, WritePrecision.Ms)
                    .ExtendTags(logEvent, _extendedTags)
                    .ExtendFields(logEvent, _extendedFields);

                if (logEvent.Exception != null) p.Tag(Tags.ExceptionType, logEvent.Exception.GetType().Name);

                if (_includeFullException && logEvent.Exception != null) p.Field(Fields.Exception, logEvent.Exception.ToString());

                points.Add(p.ToPointData());
            }

            //Not handling exceptions and let handle by wrapping class PeriodicBatchingSink            
            // -> no async and let base class do the work for waiting
            return _influxDbClient
                    .GetWriteApiAsync()
                    .WritePointsAsync(points, _connectionInfo.BucketName, _connectionInfo.OrganizationId);
        }

        public Task OnEmptyBatchAsync() => Task.CompletedTask;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose() => Dispose(true);

        /// <summary>
        /// Free resources held by the sink.
        /// </summary>
        /// <param name="disposing">If true, called because the object is being disposed; if false,
        /// the object is being disposed from the finalizer.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _influxDbClient?.Dispose();
                _influxDbClient = null;
            }

            _disposed = true;
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
        private InfluxDBClient CreateInfluxDbClientWithWriteAccess()
        {
            var builder = InfluxDBClientOptions
                    .Builder
                    .CreateNew()
                    .Url(_connectionInfo.Uri.ToString())
                    .Bucket(_connectionInfo.BucketName)
                    .Org(_connectionInfo.OrganizationId);

            if (_authMethod == AuthMethods.Token)
                builder.AuthenticateToken((_connectionInfo.Token ?? _connectionInfo.AllAccessToken).ToCharArray());
            else
                builder.Authenticate(_connectionInfo.Username, _connectionInfo.Password.ToCharArray());

            return InfluxDBClientFactory.Create(builder.Build());
        }

        /// <summary>
        /// Create the log Bucket in InfluxDB if it does not exists. 
        /// Synchronous as should be done prior any emit done and also as don't want to move check on each emit if db exists
        /// </summary>
        private void CreateBucketIfNotExists()
        {
            if (!_connectionInfo.CreateBucketIfNotExists) return;

            using (var createBucketClient = CreateInfluxClientWithAllAccessIfGiven())
            {
                var bucket = GetBucketOrDefault(createBucketClient, _connectionInfo.BucketName);

                if (bucket is null)
                {
                    var newBucket = CreateBucket(createBucketClient);

                    if (_authMethod == AuthMethods.Token)
                        _connectionInfo.Token = GenerateWriteToken(createBucketClient, newBucket);
                }
            }
        }

        private InfluxDBClient CreateInfluxClientWithAllAccessIfGiven()
        {
            var builder = InfluxDBClientOptions
                    .Builder
                    .CreateNew()
                    .Url(_connectionInfo.Uri.ToString())
                    .Org(_connectionInfo.OrganizationId);

            if (_authMethod == AuthMethods.Token)
                builder.AuthenticateToken((_connectionInfo.AllAccessToken ?? _connectionInfo.Token).ToCharArray());
            else
                builder.Authenticate(_connectionInfo.Username, _connectionInfo.Password.ToCharArray());

            return InfluxDBClientFactory.Create(builder.Build());
        }

        private Bucket GetBucketOrDefault(InfluxDBClient createBucketClient, string bucketName)
        {
            //TODO use Maybe monad ?
            Bucket bucket;
            try
            {
                bucket = createBucketClient.GetBucketsApi()
                    .FindBucketByNameAsync(bucketName)
                    .GetAwaiter().GetResult();
            }
            catch (HttpException ex)
            {
                SelfLog.WriteLine($"Error while trying to get {_connectionInfo.BucketName} (Org: {_connectionInfo.OrganizationId}), Message : {ex.Message}. Check if Token has enough access to read (if only write to bucket then set to False parameter {_connectionInfo.CreateBucketIfNotExists}) or set AllAccessToken or is active token");
                throw new InfluxDbClientCreateBucketException($"Cannot create bucket {_connectionInfo.BucketName}. Check if Token has enough access or set AllAccessToken or is active", ex);
            }

            return bucket;
        }

        private string GenerateWriteToken(InfluxDBClient createBucketClient, Bucket bucket)
        {
            var resource = new PermissionResource { Id = bucket.Id, OrgID = _connectionInfo.OrganizationId, Type = PermissionResource.TypeBuckets };

            var write = new Permission(Permission.ActionEnum.Write, resource);
            var authorizationRequest = new AuthorizationPostRequest(_connectionInfo.OrganizationId, permissions: new List<Permission> { write }, description: $"{nameof(Permission.ActionEnum.Write)} Token for Bucket '{bucket.Name}' (Serilog)");
            string token;

            try
            {
                var authorizationCreated = createBucketClient.GetAuthorizationsApi()
                    .CreateAuthorizationAsync(authorizationRequest)
                    .GetAwaiter().GetResult();
                token = authorizationCreated?.Token;
            }
            catch (HttpException ex)
            {
                SelfLog.WriteLine($"Error while trying to get {_connectionInfo.BucketName} (Org: {_connectionInfo.OrganizationId}), Message : {ex.Message}. Check if Token has enough access to read (if only write to bucket then set to False parameter {_connectionInfo.CreateBucketIfNotExists}) or set AllAccessToken or is active token");
                throw new InfluxDbClientCreateBucketException($"Cannot create token for bucket {_connectionInfo.BucketName} with write permissions. Check if Token has enough access or set AllAccessToken or is active", ex);
            }

            SelfLog.WriteLine($"Token generated successfully for bucket {bucket.Name}, using it for write operations");

            return token;
        }

        private Bucket CreateBucket(InfluxDBClient createBucketClient)
        {
            var retention = new BucketRetentionRules(BucketRetentionRules.TypeEnum.Expire, (int)_connectionInfo.BucketRetentionPeriod.TotalSeconds);

            Bucket bucket;
            try
            {
                bucket = createBucketClient
                    .GetBucketsApi()
                    .CreateBucketAsync(_connectionInfo.BucketName, retention, _connectionInfo.OrganizationId)
                    .GetAwaiter().GetResult();
            }
            catch (HttpException ex)
            {
                SelfLog.WriteLine($"Error while creating {_connectionInfo.BucketName} (Org: {_connectionInfo.OrganizationId}), Message : {ex.Message}. Check if Token has enough access or set AllAccessToken or is active");
                throw new InfluxDbClientCreateBucketException($"Cannot create bucket {_connectionInfo.BucketName}. Check if Token has enough access or set AllAccessToken or is active", ex);
            }

            SelfLog.WriteLine($"Bucket {bucket.Name} ({bucket.Id} / Org: {bucket.OrgID}) created at {bucket.CreatedAt}");

            return bucket;
        }

        private enum AuthMethods
        {
            Token,
            Credentials
        }
    }
}