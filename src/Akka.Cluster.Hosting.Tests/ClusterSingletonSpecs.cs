using System;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Hosting;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
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
        public static Props MyProps => Props.Create(() => new ClusterSingletonSpecs.MySingletonActor());

        public MySingletonActor()
        {
            ReceiveAny(_ => Sender.Tell(_));
        }
    }

    [Fact]
    public async Task Should_launch_ClusterSingletonAndProxy()
    {
        // arrange
        using var host = await TestHelper.CreateHost(
            builder => { builder.WithSingleton<ClusterSingletonSpecs.MySingletonActor>("my-singleton", MySingletonActor.MyProps); },
            new ClusterOptions(){ Roles = new[] { "my-host"}}, Output);

        var registry = host.Services.GetRequiredService<ActorRegistry>();
        var singletonProxy = registry.Get<ClusterSingletonSpecs.MySingletonActor>();

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

        var singletonOptions = new ClusterSingletonOptions() { Role = "my-host" };
        using var singletonHost = await TestHelper.CreateHost(
            builder => { builder.WithSingleton<ClusterSingletonSpecs.MySingletonActor>("my-singleton", MySingletonActor.MyProps, singletonOptions, createProxyToo:false); },
            new ClusterOptions(){ Roles = new[] { "my-host"}}, Output);

        var singletonSystem = singletonHost.Services.GetRequiredService<ActorSystem>();
        var address = Cluster.Get(singletonSystem).SelfAddress;
        
        using var singletonProxyHost =  await TestHelper.CreateHost(
            builder => { builder.WithSingletonProxy<ClusterSingletonSpecs.MySingletonActor>("my-singleton", singletonOptions); },
            new ClusterOptions(){ Roles = new[] { "proxy" }, SeedNodes = new Address[]{ address } }, Output);
        
        var registry = singletonProxyHost.Services.GetRequiredService<ActorRegistry>();
        var singletonProxy = registry.Get<ClusterSingletonSpecs.MySingletonActor>();
        
        // act
        
        // verify round-trip to the singleton proxy and back
        var respond = await singletonProxy.Ask<string>("hit", TimeSpan.FromSeconds(3));

        // assert
        respond.Should().Be("hit");

        await Task.WhenAll(singletonHost.StopAsync(), singletonProxyHost.StopAsync());
    }
}