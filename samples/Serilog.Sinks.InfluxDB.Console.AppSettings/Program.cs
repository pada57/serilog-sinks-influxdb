using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog.Debugging;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Invocation;
using System.CommandLine.NamingConventionBinder;
using System.CommandLine.Parsing;
using System.Diagnostics;

namespace Serilog.Sinks.InfluxDB.Console.AppSettings;

class Program
{
    public static IConfiguration Configuration { get; } = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
        .AddUserSecrets<Program>()
        .AddEnvironmentVariables()
        .Build();


    static async Task Main(string[] args)
    {
        await BuildCommandLine()
        .UseHost(_ => Host.CreateDefaultBuilder(),
        host =>
        {
            SelfLog.Enable(System.Console.Out);
            host.UseSerilog((hostingContext, loggerConfiguration) =>
            {
                loggerConfiguration.ReadFrom.Configuration(hostingContext.Configuration)
                 .Enrich.WithReleaseNumber();
            })
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.Sources.Clear();
                config.AddConfiguration(Configuration);
                var configuration = config.Build();

                if (args is not null)
                {
                    config.AddCommandLine(args);
                }

                if (args is null)
                {
                    //add some defaults from config
                    var number = configuration.GetSection("Sample").GetValue<int>("number");
                    args = Array.Empty<string>();
                    args = args.Append($"-n {number}").ToArray();
                }

                configuration = config.Build();
            })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddOptions<SampleOptions>()
                 .Bind(hostContext.Configuration.GetSection(SampleOptions.Sample));

                })
                .ConfigureLogging((hostContext, logging) =>
                {
                    // nothing to do here as UseSerilog configures everything
                }
                );
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

        for (var i = 0; i < options.Number; ++i)
        {
            Log.Information($"Hello, InfluxDB logger! i => {i}");
            Log.Warning("Warning, what is it ?");
            Log.Error($"Error, when ? {DateTime.Now}");
            Log.Debug($"var i => {i}");
        }

        Log.CloseAndFlush();

        sw.Stop();

        System.Console.WriteLine($"Elapsed: {sw.ElapsedMilliseconds} ms");

        System.Console.WriteLine("Press any key to exit...");
        System.Console.ReadKey(true);
    }
}
