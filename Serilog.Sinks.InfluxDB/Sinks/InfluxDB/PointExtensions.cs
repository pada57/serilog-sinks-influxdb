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
                if (logEvent.Properties.ContainsKey(extendedTag))
                {
                    builder.Tag(extendedTag, logEvent.Properties[extendedTag].ToString().Trim('"'));
                }
            }
            return builder;

        }

        public static PointData.Builder ExtendFields(this PointData.Builder builder, Events.LogEvent logEvent, string[] fields)
        {
            foreach (var extendedField in fields)
            {
                if (logEvent.Properties.ContainsKey(extendedField))
                {
                    var sv = logEvent.Properties[extendedField] as ScalarValue;

                    //TODO manage other types SequenceValue , StructureValue 

                    if (sv is null) continue;

                    switch (sv.Value)
                    {
                        case bool bl:
                            builder.Field(extendedField, bl);
                            break;
                        case int i:
                            builder.Field(extendedField, i);
                            break;
                        case double db:
                            builder.Field(extendedField, db);
                            break;
                        case decimal dc:
                            builder.Field(extendedField, dc);
                            break;
                        case long l:
                            builder.Field(extendedField, l);
                            break;
                        case uint ui:
                            builder.Field(extendedField, ui);
                            break;
                        case ulong u:
                            builder.Field(extendedField, u);
                            break;
                        case byte b:
                            builder.Field(extendedField, b);
                            break;
                        case null:
                        default:
                            builder.Field(extendedField, logEvent.Properties[extendedField].ToString());
                            break;
                    }
                }
            }

            return builder;
        }
    }
}