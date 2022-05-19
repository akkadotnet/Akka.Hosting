using System;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Event;
using Akka.Hosting;
using Akka.Remote.Hosting;
using Akka.TestKit.Xunit2.Internals;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;
using Xunit.Abstractions;

namespace Akka.Cluster.Hosting.Tests;

public class ClusterSingletonSpecs
{
    public ClusterSingletonSpecs(ITestOutputHelper output)
    {
        Output = output;
    }

    public ITestOutputHelper Output { get; }
    
    private class MySingletonActor : ReceiveActor
    {
        public static Props MyProps => Props.Create(() => new MySingletonActor());

        public MySingletonActor()
        {
            ReceiveAny(_ => Sender.Tell(_));
        }
    }

    private async Task<IHost> CreateHost(Action<AkkaConfigurationBuilder> specBuilder, ClusterOptions options)
    {
        var tcs = new TaskCompletionSource();
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        var host = new HostBuilder()
            .ConfigureServices(collection =>
            {
                collection.AddAkka("TestSys", (configurationBuilder, provider) =>
                {
                    configurationBuilder
                        .WithRemoting("localhost", 0)
                        .WithClustering(options)
                        .WithActors((system, registry) =>
                        {
                            var extSystem = (ExtendedActorSystem)system;
                            var logger = extSystem.SystemActorOf(Props.Create(() => new TestOutputLogger(Output)), "log-test");
                            logger.Tell(new InitializeLogger(system.EventStream));
                        })
                        .WithActors(async (system, registry) =>
                        {
                            var cluster = Cluster.Get(system);
                            cluster.RegisterOnMemberUp(() =>
                            {
                                tcs.SetResult();
                            });  
                            if (options.SeedNodes == null || options.SeedNodes.Length == 0)
                            {
                                var myAddress = cluster.SelfAddress;
                                await cluster.JoinAsync(myAddress); // force system to wait until we're up
                            }
                        });
                    specBuilder(configurationBuilder);
                });
            }).Build();

        await host.StartAsync(cancellationTokenSource.Token);
        await (tcs.Task.WaitAsync(cancellationTokenSource.Token));

        return host;
    }

    [Fact]
    public async Task Should_launch_ClusterSingletonAndProxy()
    {
        // arrange
        using var host = await CreateHost(
            builder => { builder.WithSingleton<MySingletonActor>("my-singleton", MySingletonActor.MyProps); },
            new ClusterOptions(){ Roles = new[] { "my-host"}});

        var registry = host.Services.GetRequiredService<ActorRegistry>();
        var singletonProxy = registry.Get<MySingletonActor>();

        // act
        
        // verify round-trip to the singleton proxy and back
        var respond = await singletonProxy.Ask<string>("hit", TimeSpan.FromSeconds(3));

        // assert
        respond.Should().Be("hit");

        await host.StopAsync();
    }

    [Fact]
    public async Task Should_launch_ClusterSingleton_and_Proxy_separately()
    {
        // arrange
        using var singletonHost = await CreateHost(
            builder => { builder.WithSingleton<MySingletonActor>("my-singleton", MySingletonActor.MyProps); },
            new ClusterOptions(){ Roles = new[] { "my-host"}});
        
        // act
        
        // assert
    }
}