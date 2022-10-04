// -----------------------------------------------------------------------
//  <copyright file="LoggerConfigBuilderSpecs.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using Akka.Hosting.Logging;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using LogLevel = Akka.Event.LogLevel;

namespace Akka.Hosting.Tests.Logging;

public class LoggerConfigBuilderSpecs
{
    [Fact(DisplayName = "LoggerConfigBuilder should contain proper default configuration")]
    public void LoggerSetupDefaultValues()
    {
        var builder = new AkkaConfigurationBuilder(new ServiceCollection(), "test")
            .ConfigureLoggers(_ => { });

        builder.Configuration.HasValue.Should().BeTrue();
        var config = builder.Configuration.Value;
        config.GetString("akka.loglevel").Should().Be("Info");
        config.GetString("akka.log-config-on-start").Should().Be("false");
        var loggers = config.GetStringList("akka.loggers");
        loggers.Count.Should().Be(1);
        loggers[0].Should().Contain("Akka.Event.DefaultLogger");
    }
    
    [Fact(DisplayName = "LoggerConfigBuilder should override config values")]
    public void LoggerSetupOverrideValues()
    {
        var builder = new AkkaConfigurationBuilder(new ServiceCollection(), "test")
            .ConfigureLoggers(setup =>
            {
                setup.LogLevel = LogLevel.WarningLevel;
                setup.LogConfigOnStart = true;
                setup.ClearLoggers();
            });

        builder.Configuration.HasValue.Should().BeTrue();
        var config = builder.Configuration.Value;
        config.GetString("akka.loglevel").Should().Be("Warning");
        config.GetString("akka.log-config-on-start").Should().Be("true");
        var loggers = config.GetStringList("akka.loggers");
        loggers.Count.Should().Be(0);
    }
}