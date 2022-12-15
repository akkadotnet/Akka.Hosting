// -----------------------------------------------------------------------
//  <copyright file="SplitBrainResolverSpecs.cs" company="Akka.NET Project">
//      Copyright (C) 2009-2022 Lightbend Inc. <http://www.lightbend.com>
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Cluster.Hosting.SBR;
using Akka.Cluster.Hosting.Tests.Lease;
using Akka.Cluster.SBR;
using Akka.Hosting;
using Akka.Remote.Hosting;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Akka.Cluster.Hosting.Tests;

public class SplitBrainResolverSpecs
{
    private readonly ITestOutputHelper _output;
    
    public SplitBrainResolverSpecs(ITestOutputHelper output)
    {
        _output = output;
    }

    private async Task<IHost> StartHost(Action<AkkaConfigurationBuilder> specBuilder)
    {
        var tcs = new TaskCompletionSource();
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        
        var host = new HostBuilder()
            .ConfigureLogging(logger =>
            {
                logger.ClearProviders();
                logger.AddProvider(new XUnitLoggerProvider(_output, LogLevel.Information));
            })
            .ConfigureServices(collection =>
            {
                collection.AddAkka("TestSys", (configurationBuilder, provider) =>
                {
                    configurationBuilder
                        .ConfigureLoggers(logger =>
                        {
                            logger.ClearLoggers();
                            logger.AddLoggerFactory();
                        })
                        .WithRemoting("localhost", 0)
                        .AddStartup((system, registry) =>
                        {
                            var cluster = Cluster.Get(system);
                            cluster.RegisterOnMemberUp(() =>
                            {
                                tcs.SetResult();
                            });
                            cluster.Join(cluster.SelfAddress);
                        });
                    specBuilder(configurationBuilder);
                });
            }).Build();

        await host.StartAsync(cancellationTokenSource.Token);
        await tcs.Task.WaitAsync(cancellationTokenSource.Token);

        return host;
    }
    
    [Fact(DisplayName = "Default SBR set from Akka.Hosting should load")]
    public async Task HostingSbrTest()
    {
        var host = await StartHost(builder =>
        {
            builder.WithClustering( new ClusterOptions{ SplitBrainResolver = SplitBrainResolverOption.Default });
        });

        var system = host.Services.GetRequiredService<ActorSystem>();
        
        Cluster.Get(system).DowningProvider.Should().BeOfType<SplitBrainResolverProvider>();
        var settings = new SplitBrainResolverSettings(system.Settings.Config);
        settings.DowningStrategy.Should().Be(SplitBrainResolverSettings.KeepMajorityName);
        settings.KeepMajorityRole.Should().BeNull();
    }
    
    [Fact(DisplayName = "Static quorum SBR set from Akka.Hosting should load")]
    public async Task StaticQuorumTest()
    {
        var host = await StartHost(builder =>
        {
            builder.WithClustering( new ClusterOptions
            {
                SplitBrainResolver = new StaticQuorumOption
                {
                    QuorumSize = 1,
                    Role = "myRole"
                }
            });
        });

        var system = host.Services.GetRequiredService<ActorSystem>();
        Cluster.Get(system).DowningProvider.Should().BeOfType<SplitBrainResolverProvider>();
        
        var settings = new SplitBrainResolverSettings(system.Settings.Config);
        settings.DowningStrategy.Should().Be(SplitBrainResolverSettings.StaticQuorumName);
        settings.StaticQuorumSettings.Size.Should().Be(1);
        settings.StaticQuorumSettings.Role.Should().Be("myRole");
    }
    
    [Fact(DisplayName = "Keep majority SBR set from Akka.Hosting should load")]
    public async Task KeepMajorityTest()
    {
        var host = await StartHost(builder =>
        {
            builder.WithClustering(new ClusterOptions
            {
                SplitBrainResolver = new KeepMajorityOption
                {
                    Role = "myRole"
                }
            });
        });

        var system = host.Services.GetRequiredService<ActorSystem>();
        Cluster.Get(system).DowningProvider.Should().BeOfType<SplitBrainResolverProvider>();
        
        var settings = new SplitBrainResolverSettings(system.Settings.Config);
        settings.DowningStrategy.Should().Be(SplitBrainResolverSettings.KeepMajorityName);
        settings.KeepMajorityRole.Should().Be("myRole");
    }
    
    [Fact(DisplayName = "Keep oldest SBR set from Akka.Hosting should load")]
    public async Task KeepOldestTest()
    {
        var host = await StartHost(builder =>
        {
            builder.WithClustering(new ClusterOptions
            {
                SplitBrainResolver = new KeepOldestOption
                {
                    DownIfAlone = false,
                    Role = "myRole"
                }
            });
        });

        var system = host.Services.GetRequiredService<ActorSystem>();
        Cluster.Get(system).DowningProvider.Should().BeOfType<SplitBrainResolverProvider>();
        
        var settings = new SplitBrainResolverSettings(system.Settings.Config);
        settings.DowningStrategy.Should().Be(SplitBrainResolverSettings.KeepOldestName);
        settings.KeepOldestSettings.DownIfAlone.Should().BeFalse();
        settings.KeepOldestSettings.Role.Should().Be("myRole");
    }
    
    [Fact(DisplayName = "Lease Majority SBR set from Akka.Hosting should load")]
    public async Task LeaseMajorityTest()
    {
        var host = await StartHost(builder =>
        {
            builder.AddHocon(TestLease.Configuration, HoconAddMode.Prepend);
            builder.WithClustering(new ClusterOptions
            {
                SplitBrainResolver = new LeaseMajorityOption
                {
                    LeaseImplementation = new TestLeaseOption(),
                    LeaseName = "myService-akka-sbr",
                    Role = "myRole"
                }
            });
        });

        var system = host.Services.GetRequiredService<ActorSystem>();
        Cluster.Get(system).DowningProvider.Should().BeOfType<SplitBrainResolverProvider>();
        
        var settings = new SplitBrainResolverSettings(system.Settings.Config);
        settings.DowningStrategy.Should().Be(SplitBrainResolverSettings.LeaseMajorityName);
        settings.LeaseMajoritySettings.LeaseImplementation.Should().Be("test-lease");
        settings.LeaseMajoritySettings.LeaseName.Should().Be("myService-akka-sbr");
        settings.LeaseMajoritySettings.Role.Should().Be("myRole");
    }
    
}