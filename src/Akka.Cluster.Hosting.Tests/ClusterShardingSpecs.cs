using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Cluster.Sharding;
using Akka.Hosting;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Akka.Cluster.Hosting.Tests;

public class ClusterShardingSpecs
{
    public sealed class MyTopLevelActor : ReceiveActor
    {
    }

    public sealed class MyEntityActor : ReceiveActor
    {
        public MyEntityActor(string entityId, IActorRef sourceRef)
        {
            EntityId = entityId;
            SourceRef = sourceRef;

            Receive<GetId>(g => { Sender.Tell(EntityId); });
            Receive<GetSourceRef>(g => Sender.Tell(SourceRef));
        }

        public string EntityId { get; }

        public IActorRef SourceRef { get; }

        public sealed class GetId : IWithId
        {
            public GetId(string id)
            {
                Id = id;
            }

            public string Id { get; }
        }

        public sealed class GetSourceRef : IWithId
        {
            public GetSourceRef(string id)
            {
                Id = id;
            }

            public string Id { get; }
        }
    }

    public interface IWithId
    {
        string Id { get; }
    }

    public sealed class Extractor : HashCodeMessageExtractor
    {
        public Extractor() : base(30)
        {
        }

        public override string EntityId(object message)
        {
            if (message is IWithId withId)
                return withId.Id;
            return string.Empty;
        }
    }

    public ClusterShardingSpecs(ITestOutputHelper output)
    {
        Output = output;
    }

    public ITestOutputHelper Output { get; }

    [Fact]
    public async Task Should_use_ActorRegistry_with_ShardRegion()
    {
        // arrange
        using var host = await TestHelper.CreateHost(builder =>
        {
            builder.WithActors((system, registry) =>
                {
                    var tLevel = system.ActorOf(Props.Create(() => new MyTopLevelActor()), "toplevel");
                    registry.Register<MyTopLevelActor>(tLevel);
                })
                .WithShardRegion<MyEntityActor>("entities", (system, registry) =>
                {
                    var tLevel = registry.Get<MyTopLevelActor>();
                    return s => Props.Create(() => new MyEntityActor(s, tLevel));
                }, new Extractor(), new ShardOptions() { Role = "my-host", StateStoreMode = StateStoreMode.DData });
        }, new ClusterOptions() { Roles = new[] { new Role("my-host") } }, Output);

        var actorSystem = host.Services.GetRequiredService<ActorSystem>();
        var actorRegistry = ActorRegistry.For(actorSystem);
        var shardRegion = actorRegistry.Get<MyEntityActor>();
        
        // act
        var id = await shardRegion.Ask<string>(new MyEntityActor.GetId("foo"), TimeSpan.FromSeconds(3));
        var sourceRef =
            await shardRegion.Ask<IActorRef>(new MyEntityActor.GetSourceRef("foo"), TimeSpan.FromSeconds(3));

        // assert
        id.Should().Be("foo");
        sourceRef.Should().Be(actorRegistry.Get<MyTopLevelActor>());
    }
}