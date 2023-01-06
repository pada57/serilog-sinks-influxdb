using InfluxDB.Client.Writes;
using Serilog.Events;

namespace Serilog.Sinks.InfluxDB
{
    static class PointDataExtensions
    {
        public static PointData.Builder ExtendTags(this PointData.Builder builder, Events.LogEvent logEvent, string[] tags)
        {
            foreach (var extendedTag in tags)
            {
                var (sourceName, targetName) = extendedTag.Parse();

                if (logEvent.Properties.TryGetValue(sourceName, out var propertyValue))
                {
                    var value = ((propertyValue is ScalarValue sv) && (sv.Value is string text)) ? text : propertyValue.ToString();

                    builder.Tag(targetName, value);
                }
            }
            return builder;

        }

        public static PointData.Builder ExtendFields(this PointData.Builder builder, Events.LogEvent logEvent, string[] fields)
        {
            foreach (var extendedField in fields)
            {
                var (sourceName, targetName) = extendedField.Parse();

                if (logEvent.Properties.TryGetValue(sourceName, out var value))
                {
                    //TODO manage other types SequenceValue , StructureValue 

                    if (!(value is ScalarValue sv))
                        continue;

                    switch (sv.Value)
                    {
                        case bool bl:
                            builder.Field(targetName, bl);
                            break;
                        case int i:
                            builder.Field(targetName, i);
                            break;
                        case double db:
                            builder.Field(targetName, db);
                            break;
                        case decimal dc:
                            builder.Field(targetName, dc);
                            break;
                        case long l:
                            builder.Field(targetName, l);
                            break;
                        case uint ui:
                            builder.Field(targetName, ui);
                            break;
                        case ulong u:
                            builder.Field(targetName, u);
                            break;
                        case byte b:
                            builder.Field(targetName, b);
                            break;
                        case string s:
                            builder.Field(targetName, s);
                            break;
                        case null:
                        default:
                            builder.Field(targetName, value?.ToString());
                            break;
                    }
                }
            }

            return builder;
        }

        private static (string, string) Parse(this string value)
        {
            var i = value.IndexOf(':');

            if (i <= 0)
                return (value, value);

            return (value.Substring(0, i), value.Substring(i + 1));
        }
    }
}