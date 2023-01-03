using InfluxDB.Client.Writes;
using Serilog.Events;
using Serilog.Parsing;
using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace Serilog.Sinks.InfluxDB.Tests;

public class PointExtensionsTest
{
    [Fact]
    public void ValuesAreAddedToFields()
    {
        //Arrange
        var testProperty = "Test Property";

        var p = PointData.Builder.Measurement("test");

        LogEventProperty[] properties = { new LogEventProperty(testProperty, new ScalarValue("Stored Value")) };

        var le = new LogEvent(DateTime.Now, LogEventLevel.Error, new Exception(), new MessageTemplate("", Array.Empty<MessageTemplateToken>()), properties);

        string[] fieldNames = { testProperty };

        //Act
        p.ExtendFields(le, fieldNames);

        //Assert
        FieldInfo fi = p.GetType().GetField("_fields", BindingFlags.NonPublic | BindingFlags.Instance);

        var fields = (IDictionary<string, object>)fi.GetValue(p);

        Assert.Single(fields);

        fields.TryGetValue(testProperty, out var storedValue);

        Assert.Equal("\"Stored Value\"", storedValue);
    }

    [Fact]
    public void ValuesAreAddedToTags()
    {
        //Arrange
        var testProperty = "Test Property";

        var p = PointData.Builder.Measurement("test");

        LogEventProperty[] properties = { new LogEventProperty(testProperty, new ScalarValue("Stored Value")) };

        var le = new LogEvent(DateTime.Now, LogEventLevel.Error, new Exception(), new MessageTemplate("", Array.Empty<MessageTemplateToken>()), properties);

        string[] tagNames = { testProperty };

        //Act
        p.ExtendTags(le, tagNames);

        //Assert
        FieldInfo fi = p.GetType().GetField("_tags", BindingFlags.NonPublic | BindingFlags.Instance);

        var tags = (IDictionary<string, string>)fi.GetValue(p);

        Assert.Single(tags);

        tags.TryGetValue(testProperty, out var storedValue);

        Assert.Equal("Stored Value", storedValue);
    }
}