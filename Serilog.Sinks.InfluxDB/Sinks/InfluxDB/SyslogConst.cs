namespace Serilog.Sinks.InfluxDB;

internal static class SyslogConst
{
    public const string MeasurementName = "syslog";

    public static class Tags
    {
        public const string Level = "level";
        public const string AppName = "appname";
        public const string Facility = "facility";
        public const string Hostname = "hostname";
        public const string Severity = "severity";
        public const string ExceptionType = "exceptionType";
    }

    public static class Fields
    {
        public const string Message = "message";
        public const string Facility = "facility_code";
        public const string ProcId = "procid";
        public const string Severity = "severity_code";
        public const string Timestamp = "timestamp";
        public const string Version = "version";
        public const string Exception = "exception";
    }

    public static class Values
    {
        public const int Facility = 16;
        public const int Version = 1;
    }
}