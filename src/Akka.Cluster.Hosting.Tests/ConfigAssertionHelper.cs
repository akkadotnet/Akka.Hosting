using Akka.Configuration;
using FluentAssertions;

namespace Akka.Cluster.Hosting.Tests;

public static class ConfigAssertionHelper
{
    public static void AssertSameString(this Config first, Config second, string key)
        => first.GetString(key).Should().Be(second.GetString(key));

    public static void AssertSameInt(this Config first, Config second, string key)
        => first.GetInt(key).Should().Be(second.GetInt(key));
    
    public static void AssertSameTimeSpan(this Config first, Config second, string key)
        => first.GetTimeSpan(key).Should().Be(second.GetTimeSpan(key));
}