using System;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.DistributedData;
using Akka.Hosting;
using Akka.Remote.Hosting;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Akka.Cluster.Hosting.Tests;

public class ClusterShardingDistributedDataSpecs: Akka.Hosting.TestKit.TestKit
{
    private const string ReplicatorName = "dDataReplicator";
    
    public ClusterShardingDistributedDataSpecs(ITestOutputHelper output): base(output: output)
    {
    }
    
    protected override void ConfigureAkka(AkkaConfigurationBuilder builder, IServiceProvider provider)
    {
        builder
            .WithRemoting()
            .WithClustering()
            .WithDistributedData(opt =>
            {
                opt.Name = ReplicatorName;
            });
    }

    [Fact(DisplayName = "WithDistributedData should start DistributedData extension automatically")]
    public async Task WithDistributedDataStartsAutomaticallyTest()
    {
        var cluster = Cluster.Get(Sys);
        await cluster.JoinAsync(cluster.SelfAddress);
        await AwaitAssertAsync(() => 
                cluster.State.Members.Count(m => m.Status == MemberStatus.Up).Should().Be(1),
            interval: TimeSpan.FromMilliseconds(200),
            duration: TimeSpan.FromSeconds(10));
        
        var settings = ReplicatorSettings.Create(Sys);
        var coordinatorName = settings.RestartReplicatorOnFailure ? $"{ReplicatorName}Supervisor" : ReplicatorName;
        
        var actorSelection = Sys.ActorSelection(new RootActorPath(cluster.SelfAddress) / "user" / coordinatorName);
        
        await AwaitAssertAsync(async () =>
        {
            actorSelection.Tell(new Identify("coordinator"), TestActor);
            var identity = await ExpectMsgAsync<ActorIdentity>(TimeSpan.FromSeconds(3));
            
            // The DData replicator should be running
            // * This actor is created inside DistributedData extension .ctor
            // * Marks that the extension is running
            // * We can't use DistributedData.Get() because that defeats the purpose.
            identity.Subject.Should().NotBeNull();
        });
    }
}