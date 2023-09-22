using InfluxDB.Client.Writes;
using Serilog.Events;
using System.Text;

namespace Serilog.Sinks.InfluxDB;

static class ExtensionMethods
{
    public static string EscapeSpecialCharacters(this string? text)
    {
        if (text is null)
            return string.Empty;

        static int Escape(StringBuilder text, int index, char placeholder)
        {
            text[index] = '\\';
            text.Insert(++index, placeholder);
            return index;
        }

        var builder = new StringBuilder(text, 2 * text.Length);

        for (var index = 0; index < builder.Length; index++)
        {
            var c = builder[index];

            index = c switch
            {
                '\n' => Escape(builder, index, 'n'),
                '\r' => Escape(builder, index, 'r'),
                '\\' => Escape(builder, index, '\\'),
                _ => index
            };
        }

        return builder.ToString();
    }

    public static PointData.Builder OptionalTag(this PointData.Builder builder, string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            builder.Tag(name, value);
        }
        return builder;
    }

    public static PointData.Builder OptionalTag(this PointData.Builder builder, string name, string? value, bool? include)
    {
        if (include is true)
        {
            builder.Tag(name, value);
        }
        return builder;
    }

    public static PointData.Builder ExtendTags(this PointData.Builder builder, LogEvent logEvent, string[]? tags)
    {
        if (tags == null)
            return builder;

        foreach (var extendedTag in tags)
        {
            var (sourceName, targetName) = SplitIfColumnPresent(extendedTag);

            if (!logEvent.Properties.TryGetValue(sourceName, out var propertyValue))
                continue;

            var value = (propertyValue is ScalarValue { Value: string text }) ? text : propertyValue.ToString();

            builder.Tag(targetName, value.EscapeSpecialCharacters());
        }

        return builder;
    }

    public static PointData.Builder ExtendFields(this PointData.Builder builder, LogEvent logEvent, string[]? fields)
    {
        if (fields == null)
            return builder;

        foreach (var extendedField in fields)
        {
            var (sourceName, targetName) = extendedField.SplitIfColumnPresent();

            if (logEvent.Properties.TryGetValue(sourceName, out var value))
            {
                //TODO manage other types SequenceValue , StructureValue 
                if (value is not ScalarValue sv)
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
                        builder.Field(targetName, s.EscapeSpecialCharacters());
                        break;
                    default:
                        builder.Field(targetName, value.ToString().EscapeSpecialCharacters());
                        break;
                }
            }
        }

        return builder;
    }

    public static PointData.Builder OptionalField(this PointData.Builder builder, string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            builder.Field(name, value);
        }
        return builder;
    }

    public static PointData.Builder OptionalField(this PointData.Builder builder, string name, object? value, bool? include)
    {
        if (include is true)
        {
            builder.Field(name, value);
        }
        return builder;
    }

    private static (string, string) SplitIfColumnPresent(this string value)
    {
        var i = value.IndexOf(':');

        if (i <= 0)
            return (value, value);

        return (value.Substring(0, i), value.Substring(i + 1));
    }
}
