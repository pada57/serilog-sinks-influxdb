using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Core.Exceptions;
using InfluxDB.Client.Writes;
using Serilog.Debugging;
using Serilog.Sinks.PeriodicBatching;
using System.Diagnostics;
using System.Globalization;
using static Serilog.Sinks.InfluxDB.SyslogConst;
using LogEvent = Serilog.Events.LogEvent;

namespace Serilog.Sinks.InfluxDB;

internal class InfluxDBSink : IBatchedLogEventSink, IDisposable
{
    private readonly string? _applicationName;
    private readonly string? _instanceName;
    private readonly string[]? _extendedTags;
    private readonly string[]? _extendedFields;

    private readonly bool _includeFullException;
    private readonly bool _includeHostname;
    private readonly bool _includeLevel;
    private readonly bool _includeSeverity;
    private readonly bool _includeDefaultFields;

    private readonly IFormatProvider? _formatProvider;

    /// <summary>
    /// Connection info used to connect to InfluxDB instance.
    /// </summary>
    private readonly InfluxDBConnectionInfo _connectionInfo;
    private readonly AuthMethods _authMethod;

    /// <summary>
    /// Client object used to connect to InfluxDB instance.
    /// </summary>
    private readonly InfluxDBClient _influxDbClient;

    private bool _disposed;

    /// <summary>
    /// Construct a sink inserting into InfluxDB with the specified details.
    /// </summary>
    public InfluxDBSink(InfluxDBSinkOptions options)
    {
        if (options is null) throw new ArgumentNullException(nameof(options));

        _connectionInfo = options.ConnectionInfo ?? throw new ArgumentException("ConnectionInfo info is required.", nameof(options));

        if (_connectionInfo.Uri is null) throw new ArgumentException("ConnectionInfo.Uri is required", nameof(options));
        if (_connectionInfo.BucketName is null) throw new ArgumentException("ConnectionInfo.BucketName is required", nameof(options));
        if (_connectionInfo.OrganizationId is null) throw new ArgumentException("ConnectionInfo.OrganizationId is required", nameof(options));

        if (string.IsNullOrWhiteSpace(_connectionInfo.Token) && string.IsNullOrWhiteSpace(_connectionInfo.AllAccessToken) && string.IsNullOrWhiteSpace(_connectionInfo.Username))
            throw new ArgumentException($"At least one authentication field should be provided: {nameof(_connectionInfo.Username)}/{nameof(_connectionInfo.Password)} or {nameof(_connectionInfo.Token)}({nameof(_connectionInfo.AllAccessToken)} If Buckets has to be check and created if needed)");

        _authMethod = string.IsNullOrWhiteSpace(_connectionInfo.Username) ? AuthMethods.Token : AuthMethods.Credentials;

        if (_authMethod == AuthMethods.Token && string.IsNullOrWhiteSpace(_connectionInfo.Token) && string.IsNullOrWhiteSpace(_connectionInfo.AllAccessToken) && string.IsNullOrWhiteSpace(_connectionInfo.Username))
            throw new ArgumentNullException(nameof(_connectionInfo.Token), $"At least one Token should be given either {nameof(_connectionInfo.Token)} if already created with write permissions or {nameof(_connectionInfo.AllAccessToken)}");

        _applicationName = options.ApplicationName;
        _instanceName = options.InstanceName ?? _applicationName;
        _formatProvider = options.FormatProvider;

        _includeDefaultFields = options.IncludeDefaultFields ?? true;
        _includeFullException = options.IncludeFullException ?? false;
        
        _includeHostname = options.IncludeHostname ?? _includeDefaultFields;

        _includeLevel = options.IncludeLevel ?? _includeDefaultFields;

        _includeSeverity = options.IncludeSeverity ?? _includeDefaultFields;
        
        CreateBucketIfNotExists();

        _influxDbClient = CreateInfluxDbClientWithWriteAccess();

        _extendedFields = options.ExtendedFields;

        _extendedTags = options.ExtendedTags;
    }

    /// <inheritdoc />
    /// <summary>
    /// Emit a batch of log events, running asynchronously.
    /// </summary>
    /// <param name="events">The events to emit.</param>
    public Task EmitBatchAsync(IEnumerable<LogEvent> batch)
    {
        if (batch is null) throw new ArgumentNullException(nameof(batch));

        var logEvents = batch as List<LogEvent> ?? batch.ToList();
        var points = new List<PointData>(logEvents.Count);

        foreach (var logEvent in logEvents)
        {
            var severity = logEvent.Level.ToSeverity();
            var message = logEvent.RenderMessage(_formatProvider).EscapeSpecialCharacters();
            // Skip the message if default fields are disabled and the message is blank
            var shouldLogMessage = _includeDefaultFields || !string.IsNullOrWhiteSpace(message);

            var p = PointData.Builder.Measurement(PointName)
                .OptionalTag(Tags.AppName, _applicationName)
                .OptionalTag(Tags.Facility, _instanceName)
                .OptionalTag(Tags.Hostname, Environment.MachineName, _includeHostname)
                .OptionalTag(Tags.Level, logEvent.Level.ToString(), _includeLevel)
                .OptionalTag(Tags.Severity, severity.ToString(), _includeSeverity)
                .OptionalField(Fields.Message, message, shouldLogMessage)
                .OptionalField(Fields.Facility, Values.Facility, _includeDefaultFields)
                .OptionalField(Fields.ProcId, Process.GetCurrentProcess().Id.ToString(CultureInfo.InvariantCulture), _includeDefaultFields)
                .OptionalField(Fields.Severity, severity.ToString(), _includeDefaultFields)
                .OptionalField(Fields.Timestamp, logEvent.Timestamp.ToUnixTimeMilliseconds() * 1000000, _includeDefaultFields)
                .OptionalField(Fields.Version, Values.Version, _includeDefaultFields)
                .Timestamp(logEvent.Timestamp.UtcDateTime, WritePrecision.Ms)
                .ExtendTags(logEvent, _extendedTags)
                .ExtendFields(logEvent, _extendedFields);

            if (logEvent.Exception != null)
            {
                p.Tag(Tags.ExceptionType, logEvent.Exception.GetType().Name);

                if (_includeFullException)
                {
                    p.Field(Fields.Exception, logEvent.Exception.ToString().EscapeSpecialCharacters());
                }
            }
            
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
        }

        _disposed = true;
    }

    /// <summary>
    /// Initialize and return an InfluxDB client object.
    /// </summary>
    /// <returns></returns>
    private InfluxDBClient CreateInfluxDbClientWithWriteAccess()
    {
#nullable disable // parameters are already validated
        var builder = InfluxDBClientOptions.Builder
            .CreateNew()
            .Url(_connectionInfo.Uri.ToString())
            .Bucket(_connectionInfo.BucketName)
            .Org(_connectionInfo.OrganizationId);

        if (_authMethod == AuthMethods.Token)
        {
            builder.AuthenticateToken((_connectionInfo.Token ?? _connectionInfo.AllAccessToken).ToCharArray());
        }
        else
        {
            builder.Authenticate(_connectionInfo.Username, _connectionInfo.Password.ToCharArray());
        }
#nullable restore

        return new InfluxDBClient(builder.Build());
    }

    /// <summary>
    /// Create the log Bucket in InfluxDB if it does not exists. 
    /// Synchronous as should be done prior any emit done and also as don't want to move check on each emit if db exists
    /// </summary>
    private void CreateBucketIfNotExists()
    {
        if (!_connectionInfo.CreateBucketIfNotExists)
            return;

        using var createBucketClient = CreateInfluxClientWithAllAccessIfGiven();

        var bucket = GetBucketOrDefault(createBucketClient, _connectionInfo.BucketName);

        if (bucket is not null)
            return;

        var newBucket = CreateBucket(createBucketClient);

        if (_authMethod == AuthMethods.Token)
        {
            _connectionInfo.Token = GenerateWriteToken(createBucketClient, newBucket);
        }
    }

    private InfluxDBClient CreateInfluxClientWithAllAccessIfGiven()
    {
#nullable disable // parameters are already validated
        var builder = InfluxDBClientOptions.Builder
            .CreateNew()
            .Url(_connectionInfo.Uri.ToString())
            .Org(_connectionInfo.OrganizationId);

        if (_authMethod == AuthMethods.Token)
            builder.AuthenticateToken((_connectionInfo.AllAccessToken ?? _connectionInfo.Token).ToCharArray());
        else
            builder.Authenticate(_connectionInfo.Username, _connectionInfo.Password.ToCharArray());
#nullable restore

        return new InfluxDBClient(builder.Build());
    }

    private Bucket? GetBucketOrDefault(InfluxDBClient createBucketClient, string bucketName)
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

    private string? GenerateWriteToken(InfluxDBClient createBucketClient, Bucket bucket)
    {
        var resource = new PermissionResource { Id = bucket.Id, OrgID = _connectionInfo.OrganizationId, Type = PermissionResource.TypeBuckets };

        var write = new Permission(Permission.ActionEnum.Write, resource);
        var authorizationRequest = new AuthorizationPostRequest(_connectionInfo.OrganizationId, permissions: new List<Permission> { write }, description: $"{nameof(Permission.ActionEnum.Write)} Token for Bucket '{bucket.Name}' (Serilog)");
        string? token;

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
