using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Sinks.InfluxDB.Console.Console.FluentConfig;
using Serilog.Sinks.PeriodicBatching;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Diagnostics;

namespace Serilog.Sinks.InfluxDB.Console.FluentConfig;

class Program
{
    static async Task Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
           .MinimumLevel.Information()
           .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
           .WriteTo.Console()
           .Enrich.FromLogContext()
           //.WriteTo.InfluxDB("Test App"                     // Application Name
           //     , "http://localhost:8086"                   // InfluxDb Address
           //     , organizationId: "88e1f5a5ad074d9e"        // Organization Id - unique id can be found under Profile > About > Common Ids
           //     , instanceName: "Test Instance"            // Instance or Environment Name
           //     , bucketName: "logs"                       // InfluxDb Bucket Name
           //     , token: "3KRZfuLM5xkJucRbudvMJIwaU-UAlqR5E18YFG-DMjgcKs9LQgZTAKLAEhj2poF-Ap6lLP0h7G7DwRfS33Wjkw=="
           //)
           .CreateLogger();

        Log.Information("Started sample Fluent Config :)");

        await BuildCommandLine()
        .UseHost(_ => Host.CreateDefaultBuilder(),
        host =>
        {
            SelfLog.Enable(System.Console.Out);
            host.UseSerilog((hostingContext, loggerConfiguration) =>
            {
                // could be configured here, but prefer earlier configuration
            })
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                // The next 2 lines commented out as they are added by default in the correct order
                // for more control first call config.Sources.Clear();
                //config.AddJsonFile("appsettings.json", optional: true);
                //config.AddEnvironmentVariables();
                config.AddUserSecrets<Program>();
                var configuration = config.Build();

                if (args is null)
                {
                    //add some defaults from config
                    var number = configuration.GetSection("Sample").GetValue<int>("number");
                    args = Array.Empty<string>();
                    args = args.Append($"-n {number}").ToArray();
                }

                config.AddCommandLine(args);
            })
            .ConfigureServices((hostContext, services) =>
            {
                services.AddOptions<SampleOptions>()
             .Bind(hostContext.Configuration.GetSection(SampleOptions.Sample));

            });
        })
        .UseDefaults()
        .Build()
        .InvokeAsync(args);
    }

    private static CommandLineBuilder BuildCommandLine()
    {
        var root = new RootCommand(@"$ TestConsole.exe --number 10"){
            new Option<int>(aliases: new string[] { "--number", "-n" }){
                Description = "Number of log entries to create",
                IsRequired = false
            },
        };
        root.Handler = CommandHandler.Create<SampleOptions, IHost>(Run);
        return new CommandLineBuilder(root);
    }

    private static void Run(SampleOptions options, IHost host)
    {
        var sw = Stopwatch.StartNew();

        Log.Logger = new LoggerConfiguration()
            .WriteTo.InfluxDB(new InfluxDBSinkOptions()
            {
                ApplicationName = "fluentSample",
                InstanceName = "fluentSampleInstance",
                ConnectionInfo = new InfluxDBConnectionInfo()
                {
                    Uri = new Uri("http://localhost:8086"),
                    BucketName = "logs",
                    OrganizationId = "76698537685b4527",  // Organization Id - unique id can be found under Profile > About > Common Ids
                    // To be set if bucket already created and give write permission and set CreateBucketIfNotExists to false
                    Token = null,
                    CreateBucketIfNotExists = true,
                    //To specify if Bucket needs to be created and if token not known or without all access permissions
                    AllAccessToken = "fGXiRVaWmwUO0pbT9CClG3qWHJa4Sio1IQPuyEsUOTq9T0hKDVabrgDzq_Np70y1UnmbzkRddvfU65eb7z0pJQ==",
                    BucketRetentionPeriod = TimeSpan.FromDays(1)
                },
                BatchOptions = new PeriodicBatchingSinkOptions()
                {
                    BatchSizeLimit = 50,
                    Period = TimeSpan.FromSeconds(10),
                    EagerlyEmitFirstEvent = true,
                    QueueLimit = null
                },
                IncludeFullException = true,
                IncludeHostname = false
            })
            .CreateLogger();

        // Simple config
        //Log.Logger = new LoggerConfiguration()
        //    .WriteTo.InfluxDB(applicationName: "Quick test",
        //                    uri: new Uri("http://127.0.0.1:8086"),
        //                    organizationId: "88e1f5a5ad074d9e",  // Organization Id - unique id can be found under Profile > About > Common Ids)
        //                    bucketName: "logs",
        //                    token: "bGfBKhSycNiUOia4k7peib2jHFewkz3o6Hv2uz1xAoUcdnEFRW7cHn03KICySLemA4VPZKvc0CwzSQT8GNl2DA==")
        //    .CreateLogger();

        for (var i = 0; i < options.Number; ++i)
        {
            Log.Information("Hello, InfluxDB logger!");
            Log.Warning("Warning, could be worse");
            Log.Error("Error, Oops what could have happened");
            Log.Error(new NullReferenceException("Something was null damn it"), "Error, with exception details see exception field");
            Log.Debug($"Debug: i -> {i}");
        }

        Log.CloseAndFlush();

        sw.Stop();

        System.Console.WriteLine($"Elapsed: {sw.ElapsedMilliseconds} ms");
        System.Console.WriteLine("Press any key to exit...");

        System.Console.ReadKey(true);
    }
}
