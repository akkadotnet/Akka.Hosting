using System;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Event;
using Akka.Hosting;
using Akka.Remote.Hosting;
using Akka.TestKit.Xunit2.Internals;
using Microsoft.Extensions.Hosting;
using Xunit.Abstractions;

namespace Akka.Cluster.Hosting.Tests;

public static class TestHelper
{
    
    public static void ConfigureHost(this AkkaConfigurationBuilder builder, 
        Action<AkkaConfigurationBuilder> specBuilder, 
        ClusterOptions options, TaskCompletionSource tcs, ITestOutputHelper output)
    {
        builder
            .WithRemoting("localhost", 0)
            .WithClustering(options)
            .WithActors((system, registry) =>
            {
                var extSystem = (ExtendedActorSystem)system;
                var logger = extSystem.SystemActorOf(Props.Create(() => new TestOutputLogger(output)));
                logger.Tell(new InitializeLogger(system.EventStream));
            })
            .AddStartup(async (system, registry) =>
            {
                var cluster = Cluster.Get(system);
                cluster.RegisterOnMemberUp(tcs.SetResult);
                if (options.SeedNodes == null || options.SeedNodes.Length == 0)
                {
                    var myAddress = cluster.SelfAddress;
                    await cluster.JoinAsync(myAddress); // force system to wait until we're up
                }
            });
        specBuilder(builder);
    }

    public static async Task<IHost> CreateHost(Action<AkkaConfigurationBuilder> specBuilder, ClusterOptions options, ITestOutputHelper output)
    {
        var tcs = new TaskCompletionSource();
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        var host = new HostBuilder()
            .ConfigureServices(collection =>
            {
                collection.AddAkka("TestSys", (configurationBuilder, provider) =>
                {
                   configurationBuilder.ConfigureHost(specBuilder, options, tcs, output);
                });
            }).Build();

        await host.StartAsync(cancellationTokenSource.Token);
        await (tcs.Task.WaitAsync(cancellationTokenSource.Token));

        return host;
    }
}