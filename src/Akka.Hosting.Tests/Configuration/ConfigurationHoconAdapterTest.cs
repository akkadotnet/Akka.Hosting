// -----------------------------------------------------------------------
//  <copyright file="ConfigurationHoconAdapterTest.cs" company="Akka.NET Project">
//      Copyright (C) 2009-2022 Lightbend Inc. <http://www.lightbend.com>
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Akka.Configuration;
using Akka.Hosting.Configuration;
using FluentAssertions;
using FluentAssertions.Extensions;
using Microsoft.Extensions.Configuration;
using Xunit;
using static FluentAssertions.FluentActions;

namespace Akka.Hosting.Tests.Configuration;

public class ConfigurationHoconAdapterTest
{
    private const string ConfigSource = @"{
  ""akka"": {
    ""cluster"": {
      ""roles"": [ ""front-end"", ""back-end"" ],
      ""role"" : {
        ""back-end"" : 5
      },
      ""app-version"": ""1.0.0"",
      ""min-nr-of-members"": 99,
      ""seed-nodes"": [ ""akka.tcp://system@somewhere.com:9999"" ],
      ""log-info"": false,
      ""log-info-verbose"": true
    }
  },
  ""test1"": ""test1 content"",
  ""test2.a"": ""on"",
  ""test2.b.c"": ""2s"",
  ""test2.b.d"": ""test2.b.d content"",
  ""test2.d"": ""test2.d content"",
  ""test3"": ""3"",
  ""test4"": 4
}";

    private readonly Config _config;

    public ConfigurationHoconAdapterTest()
    {
        Environment.SetEnvironmentVariable("AKKA__TEST_VALUE_1__A", "A VALUE");
        Environment.SetEnvironmentVariable("AKKA__TEST_VALUE_1__B", "B VALUE");
        Environment.SetEnvironmentVariable("AKKA__TEST_VALUE_1__C__D", "D");
        Environment.SetEnvironmentVariable("AKKA__TEST_VALUE_2__0", "ZERO");
        Environment.SetEnvironmentVariable("AKKA__TEST_VALUE_2__22", "TWO");
        Environment.SetEnvironmentVariable("AKKA__TEST_VALUE_2__1", "ONE");
        
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(ConfigSource));
        var configuration = new ConfigurationBuilder()
            .AddJsonStream(stream)
            .AddEnvironmentVariables()
            .Build();
        _config = configuration.ToHocon();
    }

    #region Adapter unit tests

    [Fact(DisplayName = "Adaptor should read environment variable sourced configuration correctly")]
    public void EnvironmentVariableTest()
    {
        _config.GetString("akka.test-value-1.a").Should().Be("A VALUE");
        _config.GetString("akka.test-value-1.b").Should().Be("B VALUE");
        _config.GetString("akka.test-value-1.c.d").Should().Be("D");
        var array = _config.GetStringList("akka.test-value-2");
        array[0].Should().Be("ZERO");
        array[1].Should().Be("ONE");
        array[2].Should().Be("TWO");
    }
    
    [Fact(DisplayName = "Adaptor should expand keys")]
    public void EncodedKeyTest()
    {
        var test2 = _config.GetConfig("test2");
        test2.Should().NotBeNull();
        test2.GetBoolean("a").Should().BeTrue();
        test2.GetTimeSpan("b.c").Should().Be(2.Seconds());
        test2.GetString("b.d").Should().Be("test2.b.d content");
        test2.GetString("d").Should().Be("test2.d content");
    }

    [Fact(DisplayName = "Adaptor should convert correctly")]
    public void ArrayTest()
    {
        _config.GetString("test1").Should().Be("test1 content");
        _config.GetInt("test3").Should().Be(3);
        _config.GetInt("test4").Should().Be(4);
        
        _config.GetStringList("akka.cluster.roles").Should().BeEquivalentTo("front-end", "back-end");
        _config.GetInt("akka.cluster.role.back-end").Should().Be(5);
        _config.GetString("akka.cluster.app-version").Should().Be("1.0.0");
        _config.GetInt("akka.cluster.min-nr-of-members").Should().Be(99);
        _config.GetStringList("akka.cluster.seed-nodes").Should()
            .BeEquivalentTo("akka.tcp://system@somewhere.com:9999");
        _config.GetBoolean("akka.cluster.log-info").Should().BeFalse();
        _config.GetBoolean("akka.cluster.log-info-verbose").Should().BeTrue();
    }

    #endregion

    #region ToHocon unit tests

    [Fact(DisplayName = "Regression test: connection-string should convert to hocon properly")]
    public void ConnectionStringShouldConvertToHoconProperly()
    {
        const string connectionString = "Server=COMPUTER1\\TEST;Database=BV_PROD_1;uid=**;pwd=--;TransparentNetworkIPResolution=False;Connection Timeout=180;Max Pool Size=120;Column Encryption Setting=Enabled;";
        var hoconString = $"connection-string = {connectionString.ToHocon()}";
        Config? cfg = null;
        Invoking(() => cfg = ConfigurationFactory.ParseString(hoconString))
            .Should().NotThrow();
        
        cfg!.GetString("connection-string").Should().Be(connectionString);
    }
    
    [MemberData(nameof(IllegalCharacterGenerator))]
    [Theory(DisplayName = "ToHocon(string) should add quotes to string with illegal characters")]
    public void StringToHoconQuote(string c)
    {
        var testString = $"this is {c} a test";
        var hoconized = testString.ToHocon();

        switch (c)
        {
            case "\\":
                // backslash is a special case
                hoconized.Should().Be("\"this is \\\\ a test\"");
                break;
            case "\"":
                // quote is a special case
                hoconized.Should().Be("\"this is \\\" a test\"");
                break;
            default:
                hoconized.Should().Be($"\"{testString}\"");
                break;
        }
        
        var hoconString = $"test-string = {hoconized}";
        Config? cfg = null;
        Invoking(() => cfg = ConfigurationFactory.ParseString(hoconString))
            .Should().NotThrow();
        
        cfg!.GetString("test-string").Should().Be(testString);
    }

    [MemberData(nameof(EscapeCharacterGenerator))]
    [Theory(DisplayName = "ToHocon(string) should add backslash escape character on escapable characters")]
    public void StringToHoconEscape(string escape, string expected)
    {
        var hoconSafe = escape.ToHocon();
        hoconSafe.Should().Be(expected);
        
        var hoconString = $"test-string = {$"a test {escape} string".ToHocon()}";
        Config? cfg = null;
        Invoking(() => cfg = ConfigurationFactory.ParseString(hoconString))
            .Should().NotThrow();
        cfg!.GetString("test-string").Should().Be($"a test {escape} string");
    }

    public static IEnumerable<object[]> IllegalCharacterGenerator()
    {
        const string illegals = "$\"{}[]:=,#`^?!@*&\\";
        foreach (var c in illegals)
        {
            yield return new [] { (object) $"{c}" };
        }
    }
    
    public static IEnumerable<object[]> EscapeCharacterGenerator()
    {
        yield return new[] { (object)"\\", "\"\\\\\"" };
        yield return new[] { (object)"\"", "\"\\\"\"" };
        yield return new[] { (object)"/", "\"\\/\"" };
        yield return new[] { (object)"\b", "\"\\b\"" };
        yield return new[] { (object)"\f", "\"\\f\"" };
        yield return new[] { (object)"\n", "\"\\n\"" };
        yield return new[] { (object)"\r", "\"\\r\"" };
        yield return new[] { (object)"\t", "\"\\t\"" };
        yield return new[] { (object)"\uFB2F", "\"\\ufb2f\"" };
    }
    
    #endregion
}