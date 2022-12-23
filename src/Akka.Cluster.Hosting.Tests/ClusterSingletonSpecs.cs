using System;
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

public class ClusterSingletonWithDISpecs : Akka.Hosting.TestKit.TestKit
{
    #region Actor and DI impls
    
    
    public interface IMyThing
    {
        string ThingId { get; }
    }

    public sealed class ThingImpl : IMyThing
    {
        public ThingImpl(string thingId)
        {
            ThingId = thingId;
        }

        public string ThingId { get; }
    }
    
    private class MySingletonDiActor : ReceiveActor
    {
        private readonly IMyThing _thing;
        
        public MySingletonDiActor(IMyThing thing)
        {
            _thing = thing;
            ReceiveAny(_ => Sender.Tell(_thing.ThingId));
        }
    }
    
    #endregion

    private readonly TaskCompletionSource _tcs = new(TimeSpan.FromSeconds(3));
    
    public ClusterSingletonWithDISpecs(ITestOutputHelper output) : base(output: output)
    {
    }

    protected override void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
        services.AddSingleton<IMyThing>(new ThingImpl("foo1"));
        base.ConfigureServices(context, services);
    }

    protected override void ConfigureAkka(AkkaConfigurationBuilder builder, IServiceProvider provider)
    {
        builder.ConfigureHost(configurationBuilder =>
        {
            configurationBuilder.WithSingleton<MySingletonDiActor>("my-singleton",
                (_, _, dependencyResolver) => dependencyResolver.Props<MySingletonDiActor>());
        },  new ClusterOptions(){ Roles = new[] { "my-host" }}, _tcs, Output!);
    }

    [Fact]
    public async Task Should_launch_ClusterSingletonAndProxy_with_DI_delegate()
    {
        // arrange
        await _tcs.Task; // wait for cluster to start

        var registry = Host.Services.GetRequiredService<ActorRegistry>();
        var singletonProxy = registry.Get<MySingletonDiActor>();
        var thing = Host.Services.GetRequiredService<IMyThing>();

        // act
        
        // verify round-trip to the singleton proxy and back
        var respond = await singletonProxy.Ask<string>("hit", TimeSpan.FromSeconds(3));

        // assert
        respond.Should().Be(thing.ThingId);
    }
}

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
            new ClusterOptions(){ Roles = new[] { "my-host" }}, Output);

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
            new ClusterOptions(){ Roles = new[] { "my-host" }}, Output);

        var singletonSystem = singletonHost.Services.GetRequiredService<ActorSystem>();
        var address = Cluster.Get(singletonSystem).SelfAddress;
        
        using var singletonProxyHost =  await TestHelper.CreateHost(
            builder => { builder.WithSingletonProxy<ClusterSingletonSpecs.MySingletonActor>("my-singleton", singletonOptions); },
            new ClusterOptions(){ Roles = new[] { "proxy" }, SeedNodes = new []{ address.ToString() } }, Output);
        
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