// -----------------------------------------------------------------------
//  <copyright file="StrictSerializationSpecs.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2026 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Akka.Actor;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Akka.Hosting.Tests;

public class StrictSerializationSpecs
{
    private readonly ITestOutputHelper _helper;

    public StrictSerializationSpecs(ITestOutputHelper helper)
    {
        _helper = helper;
    }

    private async Task<IHost> StartHost(Action<AkkaConfigurationBuilder, IServiceProvider> testSetup)
    {
        var host = new HostBuilder()
            .ConfigureLogging(builder =>
            {
                builder.AddProvider(new XUnitLoggerProvider(_helper, LogLevel.Information));
            })
            .ConfigureServices(service =>
            {
                service.AddAkka("TestActorSystem", testSetup);
            }).Build();

        await host.StartAsync();
        return host;
    }

    [Fact(DisplayName = "WithStrictSerialization should set allow-unregistered-types to off")]
    public async Task StrictSerializationEnabled_ShouldSetAllowUnregisteredTypesOff()
    {
        using var host = await StartHost((builder, _) =>
        {
            builder.WithStrictSerialization();
        });

        var system = host.Services.GetRequiredService<ActorSystem>();
        var hocon = system.Settings.Config.GetConfig("akka.actor.serialization-settings");
        var value = hocon.GetString("allow-unregistered-types");

        value.Should().Be("off");
    }

    [Fact(DisplayName = "WithStrictSerialization(false) should set allow-unregistered-types to on")]
    public async Task StrictSerializationDisabled_ShouldSetAllowUnregisteredTypesOn()
    {
        using var host = await StartHost((builder, _) =>
        {
            builder.WithStrictSerialization(false);
        });

        var system = host.Services.GetRequiredService<ActorSystem>();
        var hocon = system.Settings.Config.GetConfig("akka.actor.serialization-settings");
        var value = hocon.GetString("allow-unregistered-types");

        value.Should().Be("on");
    }

    [Fact(DisplayName = "WithStrictSerialization should throw for unregistered types")]
    public async Task StrictSerializationEnabled_ShouldThrowForUnregisteredType()
    {
        using var host = await StartHost((builder, _) =>
        {
            builder.WithStrictSerialization();
        });

        var system = host.Services.GetRequiredService<ActorSystem>();

        // typeof(object) is not explicitly registered, so strict mode should reject it
        Assert.Throws<Exception>(() => system.Serialization.FindSerializerForType(typeof(object)));
    }
}
