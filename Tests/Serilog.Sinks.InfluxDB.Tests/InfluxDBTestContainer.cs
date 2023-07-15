using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using JetBrains.Annotations;
using Serilog.Sinks.InfluxDB;

[PublicAPI]
public class InfluxDBTestContainer : IAsyncLifetime, IAsyncDisposable
{
    public const string AdminToken = "my-super-secret-admin-token";

    public IContainer TestContainer { get; private set; } = null!;
    public InfluxDBClient InfluxDBClient { get; private set; } = null!;
    public Bucket DefaultBucket { get; private set; } = null!;
    public ushort Port { get; private set; }

    public async Task<ICollection<QueryResult>> GetAllRowsAsync()
    {
        var rows = await GetAllRowsRawAsync();

        // replace volatile values with constants to simplify testing.
        foreach (var row in rows)
        {
            switch (row.Field)
            {
                case "procid":
                case "timestamp":
                    row.Value = row.Field.ToUpperInvariant();
                    break;
            }

            if (row.Tags.ContainsKey("hostname"))
                row.Tags["hostname"] = "HOSTNAME";
        }

        return rows;
    }

    public InfluxDBConnectionInfo ConnectionInfo => new()
    {
        AllAccessToken = AdminToken,
        Uri = new Uri($"http://127.0.0.1:{Port}"),
        BucketName = DefaultBucket.Name,
        OrganizationId = DefaultBucket.OrgID
    };

    private async Task<ICollection<QueryResult>> GetAllRowsRawAsync(string? bucketName = null)
    {
        bucketName ??= DefaultBucket.Name;

        var query = $"""
    from(bucket: "{bucketName}")
      |> range(start: -1h)
      |> filter(fn: (r) => r["_measurement"] == "syslog")
    """;

        var result = new List<QueryResult>();

        // First query may return nothing, retry until data is available
        for (var i = 0; i < 10; i++)
        {
            await InfluxDBClient.GetQueryApi().QueryAsync(query, record =>
            {
                result.Add(new QueryResult(record));
            });

            if (result.Any())
                break;

            await Task.Delay(200);
        }

        return result;
    }

    async Task IAsyncLifetime.InitializeAsync()
    {
        var environment = new Dictionary<string, string>
        {
            { "DOCKER_INFLUXDB_INIT_MODE", "setup" },
            { "DOCKER_INFLUXDB_INIT_USERNAME" , "User" },
            { "DOCKER_INFLUXDB_INIT_PASSWORD", "Password"},
            { "DOCKER_INFLUXDB_INIT_ORG", "Org" },
            { "DOCKER_INFLUXDB_INIT_BUCKET", "Bucket" },
            { "DOCKER_INFLUXDB_INIT_ADMIN_TOKEN", AdminToken }
        };

        TestContainer = new ContainerBuilder()
            .WithName(Guid.NewGuid().ToString())
            .WithPortBinding(8086, true)
            // .WithPortBinding(49227, 8086)
            .WithEnvironment(environment)
            .WithImage("influxdb:latest")
            .Build();

        await TestContainer.StartAsync();
        await TestContainer.ExecAsync(new[] { "influx" });

        Port = TestContainer.GetMappedPublicPort(8086);

        var address = $"http://localhost:{Port}";

        var optionsBuilder = InfluxDBClientOptions.Builder.CreateNew()
            .Url(address)
            .Org("Org")
            .AuthenticateToken(AdminToken);

        InfluxDBClient = new InfluxDBClient(optionsBuilder.Build());

        await WaitForServiceRunning(InfluxDBClient);

        var bucketsApi = InfluxDBClient.GetBucketsApi();

        DefaultBucket = await bucketsApi.FindBucketByNameAsync("Bucket");

        Assert.NotNull(DefaultBucket);
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await DisposeAsync();
    }

    private static async Task WaitForServiceRunning(InfluxDBClient client)
    {
        for (var i = 0; i < 100; i++)
        {
            if (await client.PingAsync())
                return;

            await Task.Delay(100);
        }

        Assert.Fail("Timeout waiting for service to respond.");
    }

    public async ValueTask DisposeAsync()
    {
        InfluxDBClient.Dispose();

        await TestContainer.DisposeAsync();
    }
}
