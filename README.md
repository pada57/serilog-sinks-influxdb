# Serilog.Sinks.InfluxDB.Syslog 
[![Build](https://github.com/pada57/serilog-sinks-influxdb/actions/workflows/build.yml/badge.svg)](https://github.com/pada57/serilog-sinks-influxdb/actions/workflows/build.yml)
[![nuget](https://img.shields.io/nuget/v/Serilog.Sinks.InfluxDB.Syslog.svg)](https://www.nuget.org/packages/Serilog.Sinks.InfluxDB.Syslog)

A serilog sink that writes events to [InfluxDB](https://www.influxdata.com/) in syslog message format as described on the [Influx blog](https://www.influxdata.com/blog/writing-logs-directly-to-influxdb/).
Supports platforms compatible with the [.NET Platform Standard](https://docs.microsoft.com/en-us/dotnet/standard/net-standard) `netstandard2.0`.

Compatible only with InfluxDB v2.0 and upwards

**Warning: use for InfluxDB v1.X following nuget package : [![nuget](https://img.shields.io/nuget/v/Serilog.Sinks.InfluxDBV1.Syslog.svg)](https://www.nuget.org/packages/Serilog.Sinks.InfluxDBV1.Syslog)**

**for V1 see [Get Started for V1.X](#influxdb-v1x)**

## Getting Started 

### InfluxDB v2.X

To get started install the *Serilog.Sinks.InfluxDB.Syslog* package:

```powershell
PM> Install-Package Serilog.Sinks.InfluxDB.Syslog
```

OR

```bash
$ dotnet add package Serilog.Sinks.InfluxDB.Syslog
```

If running locally for development purpose, you can use *docker-compose-v2.yml* at root of this repository and adapt volumes if needed
```
$ docker-compose -f docker-compose-v2.yml up -d
```

Point the logger to InfluxDb (quickest way using default *_internal* database):

```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.InfluxDB(applicationName: "Quick test",
                    uri: new Uri("http://127.0.0.1:8086"),
                    organizationId: "88e1f5a5ad074d9e",  // Organization Id - unique id can be found under Profile > About > Common Ids)
                    bucketName: "logs",
                    token: "bGfBKhSycNiUOia4k7peib2jHFewkz3o6Hv2uz1xAoUcdnEFRW7cHn03KICySLemA4VPZKvc0CwzSQT8GNl2DA==")
    .CreateLogger();
```

Another sample using *InfluxDBSinkOptions* for more control over periodic batching options and connection information:

```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.InfluxDB(new InfluxDBSinkOptions()
    {
        ApplicationName = "fluentSample",
        InstanceName = "fluentSampleInstance",
        ConnectionInfo = new InfluxDBConnectionInfo()
        {
            Uri = new Uri("http://127.0.0.1:8086"),
            BucketName = "logs",
            OrganizationId = "88e1f5a5ad074d9e",  // Organization Id - unique id can be found under Profile > About > Common Ids
            // To be set if bucket already created and give write permission and set CreateBucketIfNotExists to false
            Token = null,
            CreateBucketIfNotExists = true,
            //To specify if Bucket needs to be created and if token not known or without all access permissions
            AllAccessToken = "bGfBKhSycNiUOia4k7peib2jHFewkz3o6Hv2uz1xAoUcdnEFRW7cHn03KICySLemA4VPZKvc0CwzSQT8GNl2DA==",
            BucketRetentionPeriod = TimeSpan.FromDays(1)
        },
        BatchOptions = new PeriodicBatching.PeriodicBatchingSinkOptions()
        {
            BatchSizeLimit = 50,
            Period = TimeSpan.FromSeconds(10),
            EagerlyEmitFirstEvent = true,
            QueueLimit = null
        }
    })
    .CreateLogger();
```

If using `appsettings.json` for configuration the following example illustrates using InfluxDb and Console sinks.

```javascript
{
    "Serilog": {
        "Using": ["Serilog.Sinks.Console", "Serilog.Sinks.InfluxDB.Syslog"],
        "MinimumLevel": {
          "Default": "Information",
          "Override": {
            "Microsoft": "Warning",
            "System": "Warning"
          }
        },
        "WriteTo:Influx": {
          "Name": "InfluxDB",
          "Args": {
            "sinkOptions": {
              "ApplicationName": "testApp",
              "InstanceName": "testInstance",
              "ConnectionInfo": {
                "Uri": "http://localhost:8086",
                "BucketName": "logs",
                "OrganizationId": "88e1f5a5ad074d9e",
                "Token": "edBlcWgLkoPOituD_6V1ftCznpDR8niFcF46MJCSYuSxc1FM_srm9cuoc84yX5kOjOH_11Zvxk_juqr44S-57A==",
                "CreateBucketIfNotExists": false 
                //"Username": "influxdbroot",
                //"Password": "TBD"
                "BucketRetentionPeriod": "7.00:00:00",
              },
              "BatchOptions": {
                "EagerlyEmitFirstEvent": true,
                "BatchSizeLimit": 100,
                "Period": "0.00:00:30",
                "QueueLimit": 1000000
              }
            }
          }
        },
        "Properties": {
            "Application": "Serilog Sink InfluxDb Console Sample"
        }
    }
}
```

All those samples can be found under project subdirectory *samples* of this repository.

#### Optional parameters
```json
"sinkOptions": {
  "ApplicationName": "testApp",
  "InstanceName": "testInstance",
  "IncludeFullException": true,
  "IncludeHostname": false,
  "IncludeLevel": true,
  "IncludeSeverity": true,
  "IncludeFacility": true,
  "IncludeTimestamp": true,
  "IncludeVersion": true,
  "IncludeProcId": true,
  "ExtendedFields": ["ReleaseNumber"],
  "ExtendedTags": [],
  ...
}
```

### InfluxDB v1.X

**For InfluxDB v1.X use following nuget package :** [![nuget](https://img.shields.io/nuget/v/Serilog.Sinks.InfluxDBV1.Syslog.svg)](https://www.nuget.org/packages/Serilog.Sinks.InfluxDBV1.Syslog)

```powershell
PM> Install-Package Serilog.Sinks.InfluxDBV1.Syslog -Version 1.3.1
```

OR

```bash
$ dotnet add package Serilog.Sinks.InfluxDBV1.Syslog --version 1.3.1
```

If running locally for development purpose, you can use *docker-compose.yml* at root of this repository and adapt volumes if needed
```
$ docker-compose -f docker-compose.yml up -d
```

Point the logger to InfluxDb (quickest way using default *_internal* database):

```csharp
Log.Logger = new LoggerConfiguration()    
    .WriteTo.InfluxDB(
        applicationName: "Quick Test", 
        uri : new Uri("http://127.0.0.1:8086"));
```

Another sample using *InfluxDBSinkOptions* for more control over periodic batching options and connection information:

```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.InfluxDB(new InfluxDBSinkOptions()
    {
        ApplicationName = "fluentSample",               // Application Name
        InstanceName = "fluentSampleInstance",          // Instance or Environment Name
        ConnectionInfo = new InfluxDBConnectionInfo()   // Connection Details
        {
            Uri = new Uri("http://127.0.0.1:8086"),
            DbName = "_internal",
        },
        BatchOptions = new PeriodicBatching.PeriodicBatchingSinkOptions()
        {
            BatchSizeLimit = 50,
            Period = TimeSpan.FromSeconds(10),
            EagerlyEmitFirstEvent = true,
            QueueLimit = null
        }
    })
    .CreateLogger();
```

If using `appsettings.json` for configuration the following example illustrates using InfluxDb and Console sinks.

```javascript
{
    "Serilog": {
        "Using": ["Serilog.Sinks.Console", "Serilog.Sinks.InfluxDBV1.Syslog"],
        "MinimumLevel": {
          "Default": "Information",
          "Override": {
            "Microsoft": "Warning",
            "System": "Warning"
          }
        },
        "WriteTo": [
          { "Name": "Console" },
          {
            "Name": "InfluxDB",
            "Args": {
              "sinkOptions": {
                "applicationName": "testApp",
                "instanceName": "testInstance",
                "ConnectionInfo": {
                  "Uri": "http://localhost:8086",
                  "DbName": "_internal",
                  "Username": "",
                  "Password": ""
                },
                "BatchOptions": {
                  "EagerlyEmitFirstEvent": true,
                  "BatchSizeLimit": 200,
                  "Period": "0.00:00:30",
                  "QueueLimit":  null
                }
              }
            }
          }
        ],
        "Properties": {
            "Application": "Serilog Sink InfluxDb Console Sample"
        }
    }
}
```

## Build Status

[![Latest Release](https://img.shields.io/nuget/v/Serilog.Sinks.InfluxDB.Syslog?logo=nuget&label=release&style=for-the-badge)](https://www.nuget.org/packages/Serilog.Sinks.InfluxDB.Syslog)
[![Latest Pre-Release](https://img.shields.io/nuget/vpre/Serilog.Sinks.InfluxDB.Syslog?logo=nuget&color=yellow&label=pre-release&style=for-the-badge)](https://www.nuget.org/packages/Serilog.Sinks.InfluxDB.Syslog/absoluteLatest)
[![Downloads](https://img.shields.io/nuget/dt/Serilog.Sinks.InfluxDB.Syslog?style=for-the-badge&logo=data%3Aimage%2Fpng%3Bbase64%2CiVBORw0KGgoAAAANSUhEUgAAAEAAAABACAYAAACqaXHeAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAAHYcAAB2HAY%2Fl8WUAAAAZdEVYdFNvZnR3YXJlAHBhaW50Lm5ldCA0LjAuMTnU1rJkAAABrUlEQVR4XuXQQW7DMAxE0Rw1R%2BtN3XAjBOpPaptfsgkN8DazIDB8bNu2NCxXguVKsFwJlrJs6KYGS1k2dFODpSwbuqnBUpYN3dRgKcuGbmqwlGVDNzVYyrKhmxosZdnQTQ2WsmzopgZLWTZ0U4OlLBu6qcFSlg3d1GApy4ZuarCUZUM3NVjKsqGbGixl2dBNDZaybOimBktZNnRTg6UsG7qpwVKWDd3UYPnB86VKfl5owx9YflHhCbvHByz%2FcecnHBofsNzhjk84PD5gudOdnnBqfMDygDs84fT4gOVBVz4hNT5gecIVT0iPD1ieNPMJyviAZcKMJ2jjA5ZJI5%2Bgjg9YCkY8QR8fsJSYTxgyPmApMp4wbHzAUpZ5wtDxAcsBzjxh%2BPiA5SBHnjBlfMByoD1PmDY%2BYDnYtydMHR%2BwnICeMH18wHKS9ydcMj5gOVE84bLxAcuVYLkSLDvVQ5saLDvVQ5saLDvVQ5saLDvVQ5saLDvVQ5saLDvVQ5saLDvVQ5saLDvVQ5saLDvVQ5saLDvVQ5saLDvVQ5saLDvVQ5saLFeC5UqwXAmW69gev7WIMc4gs9idAAAAAElFTkSuQmCC)](https://www.nuget.org/packages/Serilog.Sinks.InfluxDB.Syslog/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg?style=for-the-badge&logo=data%3Aimage%2Fpng%3Bbase64%2CiVBORw0KGgoAAAANSUhEUgAAAEAAAABACAYAAACqaXHeAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAAHYcAAB2HAY%2Fl8WUAAAAZdEVYdFNvZnR3YXJlAHBhaW50Lm5ldCA0LjAuMTCtCgrAAAADB0lEQVR4XtWagXETMRREUwIlUAIlUAodQAl0AJ1AB9BB6AA6gA6MduKbkX%2BevKecNk525jHO3l%2Fp686xlJC70%2Bl0C942vjV%2Bn9FreVQbBc0wWujfRpW8Z78JaIb53hhJ1ygTA80w9PQ36duBMjHQHPCuoQZfutSjeqU1PAJN4E3j2pN7aVKv6pnWcgGawNfGa5N6prVcgGZBn8yvVXZXQbOgPXokXaPMNZwoc41D%2FaHZ8b7hpBrKjnCizIjD%2FaHZ8aPR6%2BeZXqqh7Agnyow43B%2BaZz40qnQ36a6rlsYgnChDLOkPzTN1z%2B9PafU0N3OAcaIMsaQ%2FNBufG1X9JyrtDMr0Y4xwokxlWX%2BPjAYdemhPrWeDvYcPJ8r0LO3v4oszNfivQQuTp2u9qJGKE2V6lvZ38UVj9q3t3oqEE2U2lvfXF4t6qPjTqDUV1fRyhw8nymws768vfOr2NtqOqFY4UUZE%2BusL6VDRX7%2FGzOHDiTIi0t9WMPsUKzNPx4kysf62gmuHir3sPXw4USbWny485ZOc2PsJ7VTro%2F3pwp5DxV7qHq2xa41TrY%2F2J7PfJkaHir3UwwdtU061PtqfTP0CUaYm2v3LxCtoDI2lMWk8p1of7Y8K0jhRJgaaYZwoE0P%2FpFUndZqtP6T4BE2zC5qtP6T4BE2zC5qtPyRN8OvhZUQae3ZBtT7anyb49PA6Ivp5wKnWR%2FvbJkncZXr6wokysf62CXRCWjmJxhqd2JwoE%2BuvTqS37JGJlB39GLzhRJmN5f31gz8XTpSJgWYYJ8rEQDOME2VioBnGiTIx0AzjRJkYaIZxokwMNMM4USYGmmGcKBMDzTBOlImBZhgnysRAM4wTZWKgGcaJMjHQDONEmRhohnGiTAw0wzhRJgaaYZwoEwPNME6UiYFmGCfKxEAzjBNlYqAZxokyMdAMoL%2FO%2BNi4bzjpT1e%2BNFb8V7gFzUXMLHqk%2BM1A8wArFj1S5GagOUly0SMtuxloTnJrUU%2B7QXOSW4t62g2ak9xa1NNu0Jzk1qKednK6%2Bw9roIB8keT%2F3QAAAABJRU5ErkJggg%3D%3D)](LICENSE.md)

| Branch   | Status         |
| -------- | -------------- |
|Main Branch|[![Build](https://github.com/pada57/serilog-sinks-influxdb/actions/workflows/build.yml/badge.svg)](https://github.com/pada57/serilog-sinks-influxdb/actions/workflows/build.yml)|

## Benchmarks

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19042

Intel Core i7-2640M CPU 2.80GHz (Sandy Bridge), 1 CPU, 4 logical and 2 physical cores

.NET Core SDK=5.0.101 
  [Host]     : .NET Core 3.1.9 (CoreCLR 4.700.20.47201, CoreFX 4.700.20.47203), X64 RyuJIT  [AttachedDebugger]   
  DefaultJob : .NET Core 3.1.9 (CoreCLR 4.700.20.47201, CoreFX 4.700.20.47203), X64 RyuJIT


|       Method |    N |     Mean |     Error |    StdDev |
|------------- |----- |---------:|----------:|----------:|
| LogSomething | 1000 | 5.781 us | 0.1832 us | 0.5315 us |

## Troubleshooting

> Nothing showed up, what can I do?

If events don't appear in InfluxDb after looking in corresponding database via Chronograf, Grafana or else. Either your application was unable to contact the InfluxDb server, or else the InfluxDb server rejected the log events for some reason.

## Server-side issues

The InfluxDb server may reject incoming events if they're missing required credentials ([check troubleshoot articles on influxdb](https://docs.influxdata.com/influxdb/v1.8/troubleshooting/), if the payload is corrupted somehow, or if the log events are too large to accept.


## Client-side issues

If there's no information in the ingestion log, the application was probably unable to reach the server because of network configuration or connectivity issues. These are reported to the application through Serilog's `SelfLog`.

Add the following line after the logger is configured to print any error information to the console:

```csharp
Serilog.Debugging.SelfLog.Enable(Console.Error);
```

If the console is not available, you can pass a delegate into `SelfLog.Enable()` that will be called with each error message:

```csharp
Serilog.Debugging.SelfLog.Enable(message => {
    // Do something with `message`
});
```

## Troubleshooting checklist

* Check InfluxDb connectivity and if _Server-side issues_ see section above
* Turn on the Serilog SelfLog as described above to check for connectivity problems and other issues on the client side.
* Make sure your application calls Log.CloseAndFlush(), or disposes the root Logger, before it exits - otherwise, buffered events may be lost.
* If your app is a Windows console application, it is also important to close the console window by exiting the app; Windows console apps are terminated "hard" if the close button in the title bar is used, so events buffered for sending to InfluxDb may be lost if you use it.
