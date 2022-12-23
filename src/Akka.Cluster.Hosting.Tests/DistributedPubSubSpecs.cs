﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Cluster.Tools.PublishSubscribe;
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

public class DistributedPubSubSpecs : IAsyncLifetime
{
    private readonly ITestOutputHelper _helper;
    private readonly Action<AkkaConfigurationBuilder> _specBuilder;
    private readonly ClusterOptions _clusterOptions;    
    private IHost? _host;
    private ActorSystem? _system;
    private ILoggingAdapter? _log;
    private Cluster? _cluster;
    private TestKit.Xunit2.TestKit? _testKit;

    private IActorRef? _mediator;

    public DistributedPubSubSpecs(ITestOutputHelper helper)
    {
        _helper = helper;
        _specBuilder = _ => { };
        _clusterOptions = new ClusterOptions { Roles = new[] { "my-host" } };
    }
    
    // Issue #55 https://github.com/akkadotnet/Akka.Hosting/issues/55
    [Fact]
    public Task Should_launch_distributed_pub_sub_with_roles()
    {
        var testProbe = _testKit!.CreateTestProbe(_system);

        // act
        testProbe.Send(_mediator, new Subscribe("testSub", testProbe));
        var response = testProbe.ExpectMsg<SubscribeAck>();

        // assert
        _system!.Settings.Config.GetString("akka.cluster.pub-sub.role").Should().Be("my-host");
        response.Subscribe.Topic.Should().Be("testSub");
        response.Subscribe.Ref.Should().Be(testProbe);

        return Task.CompletedTask;
    }
    
    [Fact]
    public Task Distributed_pub_sub_should_work()
    {
        const string topic = "testSub";
        
        var subscriber = _testKit!.CreateTestProbe(_system);
        var publisher = _testKit.CreateTestProbe(_system);

        subscriber.Send(_mediator, new Subscribe(topic, subscriber));
        subscriber.ExpectMsg<SubscribeAck>();

        publisher.Send(_mediator, new Publish(topic, "test message"));
        subscriber.ExpectMsg("test message");

        return Task.CompletedTask;
    }

    public async Task InitializeAsync()
    {
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        _host = new HostBuilder()
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
                            .AddHocon(TestKit.Xunit2.TestKit.DefaultConfig, HoconAddMode.Append)
                            .WithRemoting("localhost", 0)
                            .WithClustering(_clusterOptions)
                            .WithActors((system, _) =>
                            {
                                _testKit = new TestKit.Xunit2.TestKit(system, _helper);
                                _system = system;
                                _log = Logging.GetLogger(system, this);
                                _cluster = Cluster.Get(system);
                                
                                _log.Info("Distributed pub-sub test system initialized.");
                            })
                            .WithDistributedPubSub("my-host");
                        _specBuilder(configurationBuilder);
                    });
            }).Build();
        
        await _host.StartAsync(cancellationTokenSource.Token);

        // Lifetime should be healthy
        var lifetime = _host.Services.GetRequiredService<IHostApplicationLifetime>();
        lifetime.ApplicationStopped.IsCancellationRequested.Should().BeFalse();
        lifetime.ApplicationStopping.IsCancellationRequested.Should().BeFalse();
        
        // Join cluster
        var myAddress = _cluster!.SelfAddress;
        await _cluster.JoinAsync(myAddress, cancellationTokenSource.Token); // force system to wait until we're up

        // Prepare test
        var registry = _host.Services.GetRequiredService<ActorRegistry>();
        _mediator = registry.Get<DistributedPubSub>();
    }

    public async Task DisposeAsync()
    {
        if (_host != null) 
            await _host.StopAsync();
    }
}