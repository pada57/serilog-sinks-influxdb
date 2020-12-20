# serilog-sinks-influxdb
A serilog sink that writes events to InfluxDB.


## Benchmarks

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19042

Intel Core i7-2640M CPU 2.80GHz (Sandy Bridge), 1 CPU, 4 logical and 2 physical cores

.NET Core SDK=5.0.101 
  [Host]     : .NET Core 3.1.9 (CoreCLR 4.700.20.47201, CoreFX 4.700.20.47203), X64 RyuJIT  [AttachedDebugger]   
  DefaultJob : .NET Core 3.1.9 (CoreCLR 4.700.20.47201, CoreFX 4.700.20.47203), X64 RyuJIT


|       Method |    N |     Mean |     Error |    StdDev |
|------------- |----- |---------:|----------:|----------:|
| LogSomething | 1000 | 5.781 us | 0.1832 us | 0.5315 us |
