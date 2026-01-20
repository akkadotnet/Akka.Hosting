using Akka.Actor;
using Akka.Cluster.Hosting;
using Akka.Cluster.Sharding.Delivery;
using Akka.Hosting.Benchmark.Tests.Configurations;
using Akka.Persistence.Delivery;
using Akka.Persistence.Hosting;
using Akka.Remote.Hosting;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Akka.Hosting.Benchmark.Tests.Cluster.Delivery;

[Config(typeof(MacroBenchmarkConfig))]
public class ClusterDeliveryDurableQueueBenchmark
{
    private IHost? _host;
    private IActorRef? _producer;
    private IActorRef? _region;
    private IActorRef? _controller;
    private IActorRef? _aggregator;

    [Params(3000, 6000)]
    public int MessageCount;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddAkka("BenchmarkSystem", (builder, provider) =>
                {
                    builder
                        .WithRemoting()
                        .WithClustering()
                        .WithShardRegion<TestRegion>(
                            typeName: "TestConsumer",
                            entityPropsFactory: (system, _, resolver) => id => ShardingConsumerController.Create<Job>(
                                c => resolver.Props<TestConsumerEntity>(id, c),
                                ShardingConsumerController.Settings.Create(system)),
                            messageExtractor: new MessageExtractor(),
                            shardOptions: new ShardOptions())
                        .WithInMemoryJournal()
                        .WithInMemorySnapshotStore()
                        .WithActors((system, registry, resolver) =>
                        {
                            var actor = system.ActorOf(Props.Create(() => new AggregateActor(MessageCount)));
                            registry.Register<AggregateActor>(actor);
                        });
                });
            })
            .Build();

        _host.StartAsync().GetAwaiter().GetResult();
        
        var system = _host.Services.GetRequiredService<ActorSystem>();
        var registry = _host.Services.GetRequiredService<IActorRegistry>();
        
        // Join cluster
        var tcs = new TaskCompletionSource();
        var cluster = Akka.Cluster.Cluster.Get(system);
        cluster.RegisterOnMemberUp(() =>
        {
            tcs.SetResult();
        });
        cluster.Join(cluster.SelfAddress);
        tcs.Task.WaitAsync(TimeSpan.FromSeconds(3)).GetAwaiter().GetResult();
        
        // Register the sharding region for later use
        var region = registry.Get<TestRegion>();
        _region = region;
        
        // Get the test completed aggregator
        _aggregator = registry.Get<AggregateActor>();
        
        // Create a durable queue props
        var durableQueueProps = EventSourcedProducerQueue.Create<Job>(
            persistentId: "eventSourced-durableQueue",
            system
        );

        // Create the ShardingProducerController
        _controller = system.ActorOf(
            ShardingProducerController.Create<Job>(
                producerId: "test-producer",
                shardRegion: region,
                durableQueue: durableQueueProps,
                settings: ShardingProducerController.Settings.Create(system)
            ),
            "producerController"
        );
        
        // Create the producer actor
        _producer = system.ActorOf(Props.Create(() => new ProducerActor(_controller)), "producer");
    }

    [GlobalCleanup]
    public void Teardown()
    {
        if (_host is not null)
        {
            var sys = _host.Services.GetRequiredService<ActorSystem>();
            if(_aggregator is not null)
                sys.Stop(_aggregator);
            if(_producer is not null)
                sys.Stop(_producer);
            
            _host.StopAsync().GetAwaiter().GetResult();
        }

        _aggregator = null;
        _producer = null;
        _region = null;
        _controller = null;
    }

    [IterationSetup]
    public void IterationSetup()
    {
        _aggregator.Ask<Done>(Reset.Instance).GetAwaiter().GetResult();
    }
    
    [Benchmark]
    public async Task ClusterShardingDeliveryDurableQueueMessageThroughputBenchmark()
    {
        foreach (var i in Enumerable.Range(1, MessageCount))
            _producer.Tell(i);
        
        await _aggregator.Ask<Completed>(GetCompleted.Instance);
    }
}