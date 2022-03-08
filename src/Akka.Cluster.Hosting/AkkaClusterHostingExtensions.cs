using Akka.Actor;
using Akka.Cluster.Sharding;
using Akka.Cluster.Tools.Client;
using Akka.Cluster.Tools.PublishSubscribe;
using Akka.Cluster.Tools.Singleton;
using Akka.Hosting;

namespace Akka.Cluster.Hosting;

/// <summary>
/// The set of options for enabling Akka.Cluster support.
/// </summary>
public sealed class ClusterOptions
{
    /// <summary>
    /// The akka.cluster.roles values.
    /// </summary>
    public string[] Roles { get; set; }

    /// <summary>
    /// If populated, the akka.cluster.seed-nodes that will be used.
    /// </summary>
    public Address[] SeedNodes { get; set; }
}

public static class AkkaClusterHostingExtensions
{
    private static AkkaConfigurationBuilder BuildClusterRolesHocon(this AkkaConfigurationBuilder builder,
        IReadOnlyCollection<string> roles)
    {
        var config = $"akka.cluster.roles = [{string.Join(",", roles)}]";

        // prepend the roles configuration to the front
        return builder.AddHocon(config, HoconAddMode.Prepend);
    }

    private static AkkaConfigurationBuilder BuildClusterSeedsHocon(this AkkaConfigurationBuilder builder,
        IReadOnlyCollection<Address> seeds)
    {
        var config = $"akka.cluster.seed-nodes = [{string.Join(",", seeds.Select(c => "\"" + c + "\""))}]";

        // prepend the roles configuration to the front
        return builder.AddHocon(config, HoconAddMode.Prepend);
    }

    private static AkkaConfigurationBuilder BuildClusterHocon(this AkkaConfigurationBuilder builder,
        ClusterOptions? options)
    {
        if (options == null)
            return builder;

        if (options.Roles is { Length: > 0 })
            builder = builder.BuildClusterRolesHocon(options.Roles);

        if (options.SeedNodes is { Length: > 0 })
            builder = builder.BuildClusterSeedsHocon(options.SeedNodes);

        // populate all of the possible Clustering default HOCON configurations here
        return builder.AddHocon(ClusterSharding.DefaultConfig().WithFallback(ClusterSingletonManager.DefaultConfig())
            .WithFallback(DistributedPubSub.DefaultConfig()).WithFallback(ClusterClientReceptionist.DefaultConfig()));
    }

    /// <summary>
    /// Adds Akka.Cluster support to the <see cref="ActorSystem"/>.
    /// </summary>
    /// <param name="builder">The builder instance being configured.</param>
    /// <param name="options">Optional. Akka.Cluster configuration parameters.</param>
    /// <returns>The same <see cref="AkkaConfigurationBuilder"/> instance originally passed in.</returns>
    public static AkkaConfigurationBuilder WithClustering(this AkkaConfigurationBuilder builder,
        ClusterOptions? options = null)
    {
        var hoconBuilder = BuildClusterHocon(builder, options);

        if (builder.ActorRefProvider.HasValue)
        {
            switch (builder.ActorRefProvider.Value)
            {
                case ProviderSelection.Cluster _:
                    return hoconBuilder; // no-op
            }
        }

        return hoconBuilder.WithActorRefProvider(ProviderSelection.Cluster.Instance);
    }

    /// <summary>
    /// Starts a <see cref="ShardRegion"/> actor for the given entity <see cref="typeName"/>
    /// and registers the ShardRegion <see cref="IActorRef"/> with <see cref="TKey"/> in the
    /// <see cref="ActorRegistry"/> for this <see cref="ActorSystem"/>.
    /// </summary>
    /// <param name="builder">The builder instance being configured.</param>
    /// <param name="typeName"></param>
    /// <param name="entityFactory"></param>
    /// <param name="extractor"></param>
    /// <param name="settings"></param>
    /// <param name="allocationStrategy"></param>
    /// <param name="handOffStopMessage"></param>
    /// <typeparam name="TKey"></typeparam>
    /// <returns></returns>
    public static AkkaConfigurationBuilder WithShardRegion<TKey>(this AkkaConfigurationBuilder builder, string typeName,
        Func<string, Props> entityFactory, IMessageExtractor extractor, ClusterShardingSettings settings,
        IShardAllocationStrategy allocationStrategy,
        object handOffStopMessage)
    {
        return builder.WithActors(async (system, registry) =>
        {
            var shardRegion = await ClusterSharding.Get(system).StartAsync(typeName, entityFactory, settings, extractor,
                allocationStrategy, handOffStopMessage);
            registry.TryRegister<TKey>(shardRegion);
        });
    }
    
    public static AkkaConfigurationBuilder WithShardRegion<TKey>(this AkkaConfigurationBuilder builder, string typeName,
        Func<string, Props> entityFactory, ExtractEntityId entityExtractor, ExtractShardId shardExtractor, ClusterShardingSettings settings,
        IShardAllocationStrategy allocationStrategy,
        object handOffStopMessage)
    {
        return builder.WithActors(async (system, registry) =>
        {
            var shardRegion = await ClusterSharding.Get(system)
                .StartAsync(typeName, entityFactory, settings, entityExtractor, shardExtractor,
                allocationStrategy, handOffStopMessage);
            registry.TryRegister<TKey>(shardRegion);
        });
    }
    
    public static AkkaConfigurationBuilder WithShardRegion<TKey>(this AkkaConfigurationBuilder builder, string typeName,
        Func<string, Props> entityFactory, IMessageExtractor extractor, ClusterShardingSettings settings)
    {

        return builder.WithActors(async (system, registry) =>
        {
            var shardRegion = await ClusterSharding.Get(system).StartAsync(typeName, entityFactory, 
                settings, extractor);
            registry.TryRegister<TKey>(shardRegion);
        });
    }
    
    public static AkkaConfigurationBuilder WithShardRegion<TKey>(this AkkaConfigurationBuilder builder, string typeName,
        Func<string, Props> entityFactory, ExtractEntityId entityExtractor, ExtractShardId shardExtractor, ClusterShardingSettings settings)
    {

        return builder.WithActors(async (system, registry) =>
        {
            var shardRegion = await ClusterSharding.Get(system).StartAsync(typeName, entityFactory, 
                settings, entityExtractor, shardExtractor);
            registry.TryRegister<TKey>(shardRegion);
        });
    }
}