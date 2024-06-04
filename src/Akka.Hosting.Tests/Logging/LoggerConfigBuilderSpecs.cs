// -----------------------------------------------------------------------
//  <copyright file="LoggerConfigBuilderSpecs.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq;
using Akka.Configuration;
using Akka.Event;
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

        config.GetConfig("akka.actor.debug").Should().BeNull();
        config.GetString("akka.log-dead-letters").Should().BeNull();
        config.GetString("akka.log-dead-letters-during-shutdown").Should().BeNull();
        config.GetString("akka.log-dead-letters-suspend-duration").Should().BeNull();
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
                setup.DebugOptions = new DebugOptions
                {
                    Receive = true,
                    AutoReceive = true,
                    LifeCycle = true,
                    EventStream = true,
                    FiniteStateMachine = true,
                    Unhandled = true,
                    RouterMisconfiguration = true
                };
                setup.DeadLetterOptions = new DeadLetterOptions
                {
                    LogCount = 99,
                    LogDuringShutdown = false,
                    LogSuspendDuration = TimeSpan.Zero
                };
            });

        builder.Configuration.HasValue.Should().BeTrue();
        var config = builder.Configuration.Value;
        config.GetString("akka.loglevel").Should().Be("Warning");
        config.GetString("akka.log-config-on-start").Should().Be("true");
        var loggers = config.GetStringList("akka.loggers");
        loggers.Count.Should().Be(0);
        
        var debug = config.GetConfig("akka.actor.debug");
        debug.Should().NotBeNull();
        debug.GetBoolean("receive").Should().BeTrue();
        debug.GetBoolean("autoreceive").Should().BeTrue();
        debug.GetBoolean("lifecycle").Should().BeTrue();
        debug.GetBoolean("fsm").Should().BeTrue();
        debug.GetBoolean("event-stream").Should().BeTrue();
        debug.GetBoolean("unhandled").Should().BeTrue();
        debug.GetBoolean("router-misconfiguration").Should().BeTrue();
        
        config.GetInt("akka.log-dead-letters").Should().Be(99);
        config.GetBoolean("akka.log-dead-letters-during-shutdown").Should().BeFalse();
        config.GetString("akka.log-dead-letters-suspend-duration").Should().Be("infinite");
    }

    [Fact(DisplayName = "DeadLetterOptions should override log-dead-letters properly")]
    public void DeadLetterOptionsTest()
    {
        var cfg = (Config)new DeadLetterOptions
        {
            ShouldLog = TriStateValue.All
        }.ToString();
        cfg.GetBoolean("akka.log-dead-letters").Should().BeTrue();
        
        cfg = new DeadLetterOptions
        {
            ShouldLog = TriStateValue.None
        }.ToString();
        cfg.GetBoolean("akka.log-dead-letters").Should().BeFalse();
        
        cfg = new DeadLetterOptions
        {
            ShouldLog = TriStateValue.Some
        }.ToString();
        cfg.GetInt("akka.log-dead-letters").Should().Be(10);
    }

    [Fact(DisplayName = "WithLogFilter should populate the LogFilterBuilder property")]
    public void WithLogFilterPropertyTest()
    {
        var akkaBuilder = new AkkaConfigurationBuilder(new ServiceCollection(), "test");
        var loggerConfigBuilder = new LoggerConfigBuilder(akkaBuilder)
                .WithLogFilter(filterBuilder =>
                {
                    filterBuilder.ExcludeMessageContaining("Test");
                });
        loggerConfigBuilder.LogFilterBuilder.Should().NotBeNull();
        var filterSetup = loggerConfigBuilder.LogFilterBuilder!.Build();
        filterSetup.Filters.Length.Should().Be(1);
        filterSetup.Filters.Any(f => f is RegexLogMessageFilter).Should().BeTrue();
    }
    
    [Fact(DisplayName = "WithLogFilter should append existing LogFilterBuilder property")]
    public void WithLogFilterConcatTest()
    {
        var akkaBuilder = new AkkaConfigurationBuilder(new ServiceCollection(), "test");
        var loggerConfigBuilder = new LoggerConfigBuilder(akkaBuilder)
        {
            LogFilterBuilder = new LogFilterBuilder()
                .ExcludeSourceContaining("Test")
        };
        loggerConfigBuilder
            .WithLogFilter(filterBuilder =>
            {
                filterBuilder.ExcludeMessageContaining("Test");
            });
        
        loggerConfigBuilder.LogFilterBuilder.Should().NotBeNull();
        var filterSetup = loggerConfigBuilder.LogFilterBuilder.Build();
        filterSetup.Filters.Length.Should().Be(2);
        filterSetup.Filters.Any(f => f is RegexLogMessageFilter).Should().BeTrue();
        filterSetup.Filters.Any(f => f is RegexLogSourceFilter).Should().BeTrue();
    }
    
}