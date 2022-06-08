using System;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Cluster.Tools.PublishSubscribe;
using Akka.Configuration;
using Akka.Event;
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

public class DistributedPubSubSpecs: TestKit.Xunit2.TestKit
{
    private readonly ITestOutputHelper _helper;
    private ActorSystem _system;
    private ILoggingAdapter _log;
    private Cluster _cluster;

    public DistributedPubSubSpecs(ITestOutputHelper helper) : base(Config.Empty, nameof(DistributedPubSubSpecs), helper)
    {
        _helper = helper;
    }
    
    private async Task<IHost> CreateHost(Action<AkkaConfigurationBuilder> specBuilder, ClusterOptions options)
    {
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        var host = new HostBuilder()
            .ConfigureLogging(builder =>
            {
                builder.AddProvider(new XUnitLoggerProvider(_helper, LogLevel.Information));
            })
            .ConfigureServices(collection =>
            {
                collection
                    .AddAkka("TestSys", (configurationBuilder, _) =>
                    {
                        configurationBuilder
                            .AddHocon(Sys.Settings.Config)
                            .WithRemoting("localhost", 0)
                            .WithClustering(options)
                            .WithActors((system, _) =>
                            {
                                InitializeLogger(system);
                                _system = system;
                                _log = Logging.GetLogger(system, this);
                                _cluster = Cluster.Get(system);
                                
                                _log.Info("Distributed pub-sub test system initialized.");
                            })
                            .WithDistributedPubSub("pub-sub-host");
                        specBuilder(configurationBuilder);
                    });
            }).Build();
        
        await host.StartAsync(cancellationTokenSource.Token);
        return host;
    }

    // Issue #55 https://github.com/akkadotnet/Akka.Hosting/issues/55
    [Fact]
    public async Task Should_launch_distributed_pub_sub_with_roles()
    {
        using var host = await CreateHost(
            _ => { },
            new ClusterOptions{ Roles = new[] { "my-host"} });

        // Lifetime should be healthy
        var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
        lifetime.ApplicationStopped.IsCancellationRequested.Should().BeFalse();
        lifetime.ApplicationStopping.IsCancellationRequested.Should().BeFalse();
        
        // Join cluster
        var myAddress = _cluster.SelfAddress;
        await _cluster.JoinAsync(myAddress); // force system to wait until we're up

        // Prepare test
        var registry = host.Services.GetRequiredService<ActorRegistry>();
        var mediator = registry.Get<DistributedPubSub>();
        var probe = CreateTestProbe(_system);

        // act
        probe.Send(mediator, new Subscribe("testSub", probe));
        var response = probe.ExpectMsg<SubscribeAck>();

        // assert
        response.Subscribe.Topic.Should().Be("testSub");
        response.Subscribe.Ref.Should().Be(probe);

        await host.StopAsync();
    }
}