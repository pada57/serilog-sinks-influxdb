using Serilog.Events;
using System.Diagnostics.CodeAnalysis;

namespace Serilog.Sinks.InfluxDB
{

#pragma warning disable S101 // Types should be named in PascalCase
    [SuppressMessage(
 "not my naming convention",
 "S101:Types should be named in PascalCase"
)]
    internal static class SerilogSyslogSeverityConvertor
#pragma warning restore S101 // Types should be named in PascalCase
    {
        internal static SyslogSeverity GetSyslogSeverity(LogEventLevel level)
        {
            switch (level)
            {
                case LogEventLevel.Fatal:
                    return new SyslogSeverity() { Severity = "emerg", SeverityCode = 0 };

                case LogEventLevel.Error:
                    return new SyslogSeverity() { Severity = "err", SeverityCode = 3 };

                case LogEventLevel.Warning:
                    return new SyslogSeverity() { Severity = "warning", SeverityCode = 4 };

                case LogEventLevel.Information:
                    return new SyslogSeverity() { Severity = "info", SeverityCode = 4 };

                case LogEventLevel.Debug:
                    return new SyslogSeverity() { Severity = "debug", SeverityCode = 6 };

                default:
                    return new SyslogSeverity() { Severity = "notice", SeverityCode = 7 };
            }
        }
    }
}