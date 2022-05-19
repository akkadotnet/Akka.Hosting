using System;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Hosting;
using Akka.Remote.Hosting;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Akka.Cluster.Hosting.Tests;

public class ClusterSingletonSpecs
{
    private class MySingletonActor : ReceiveActor
    {
        public static Props MyProps => Props.Create(() => new MySingletonActor());
        
        public MySingletonActor()
        {
            ReceiveAny(_ => Sender.Tell(_));
        }
    }
    
    private static async Task<IHost> CreateHost(Action<AkkaConfigurationBuilder> specBuilder, string clusterRole)
    {
        var tcs = new TaskCompletionSource();
        
        var host = new HostBuilder()
            .ConfigureServices(collection =>
            {
                collection.AddAkka("TestSys", (configurationBuilder, provider) =>
                {
                    configurationBuilder.WithRemoting("localhost", 0)
                        .WithClustering(new ClusterOptions(){ Roles = new []{ clusterRole }})
                        .WithActors(async (system, registry) =>
                        {
                            var cluster = Cluster.Get(system);
                            var myAddress = cluster.SelfAddress;
                            await cluster.JoinAsync(myAddress); // force system to wait until we're up
                            tcs.SetResult();
                        });
                    specBuilder(configurationBuilder);
                });
            }).Build();
        
        
    }

    [Fact]
    public async Task Should_launch_ClusterSingletonAndProxy()
    {
        // arrange
        var host = CreateHost(builder =>
        {
            builder.WithSingleton<MySingletonActor>("my-singleton", MySingletonActor.MyProps);
        }, "my-host");

        // act

        // assert
    }

    public Task InitializeAsync()
    {
        throw new System.NotImplementedException();
    }

    public Task DisposeAsync()
    {
        throw new System.NotImplementedException();
    }
}