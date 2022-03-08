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
    /// <param name="typeName">The name of the entity type</param>
    /// <param name="entityPropsFactory">
    /// Function that, given an entity id, returns the <see cref="Actor.Props"/> of the entity actors that will be created by the <see cref="Sharding.ShardRegion"/>
    /// </param>
    /// <param name="settings">Configuration settings, see <see cref="ClusterShardingSettings"/></param>
    /// <param name="messageExtractor">
    /// Functions to extract the entity id, shard id, and the message to send to the entity from the incoming message.
    /// </param>
    /// <param name="allocationStrategy">Possibility to use a custom shard allocation and rebalancing logic</param>
    /// <param name="handOffStopMessage">
    /// The message that will be sent to entities when they are to be stopped for a rebalance or
    /// graceful shutdown of a <see cref="Sharding.ShardRegion"/>, e.g. <see cref="PoisonPill"/>.
    /// </param>
    /// <typeparam name="TKey">The type key to use to retrieve the <see cref="IActorRef"/> for this <see cref="ShardRegion"/>.</typeparam>
    /// <returns>The same <see cref="AkkaConfigurationBuilder"/> instance originally passed in.</returns>
    public static AkkaConfigurationBuilder WithShardRegion<TKey>(this AkkaConfigurationBuilder builder, string typeName,
        Func<string, Props> entityPropsFactory, IMessageExtractor messageExtractor, ClusterShardingSettings settings,
        IShardAllocationStrategy allocationStrategy,
        object handOffStopMessage)
    {
        return builder.WithActors(async (system, registry) =>
        {
            var shardRegion = await ClusterSharding.Get(system).StartAsync(typeName, entityPropsFactory, settings, messageExtractor,
                allocationStrategy, handOffStopMessage);
            registry.TryRegister<TKey>(shardRegion);
        });
    }
    
    /// <summary>
    /// Starts a <see cref="ShardRegion"/> actor for the given entity <see cref="typeName"/>
    /// and registers the ShardRegion <see cref="IActorRef"/> with <see cref="TKey"/> in the
    /// <see cref="ActorRegistry"/> for this <see cref="ActorSystem"/>.
    /// </summary>
    /// <param name="builder">The builder instance being configured.</param>
    /// <param name="typeName">The name of the entity type</param>
    /// <param name="entityPropsFactory">
    /// Function that, given an entity id, returns the <see cref="Actor.Props"/> of the entity actors that will be created by the <see cref="Sharding.ShardRegion"/>
    /// </param>
    /// <param name="settings">Configuration settings, see <see cref="ClusterShardingSettings"/></param>
    /// <param name="extractEntityId">
    /// Partial function to extract the entity id and the message to send to the entity from the incoming message,
    /// if the partial function does not match the message will be `unhandled`,
    /// i.e.posted as `Unhandled` messages on the event stream
    /// </param>
    /// <param name="extractShardId">
    /// Function to determine the shard id for an incoming message, only messages that passed the `extractEntityId` will be used
    /// </param>
    /// <param name="allocationStrategy">Possibility to use a custom shard allocation and rebalancing logic</param>
    /// <param name="handOffStopMessage">
    /// The message that will be sent to entities when they are to be stopped for a rebalance or
    /// graceful shutdown of a <see cref="Sharding.ShardRegion"/>, e.g. <see cref="PoisonPill"/>.
    /// </param>
    /// <typeparam name="TKey">The type key to use to retrieve the <see cref="IActorRef"/> for this <see cref="ShardRegion"/>.</typeparam>
    /// <returns>The same <see cref="AkkaConfigurationBuilder"/> instance originally passed in.</returns>
    public static AkkaConfigurationBuilder WithShardRegion<TKey>(this AkkaConfigurationBuilder builder, string typeName,
        Func<string, Props> entityPropsFactory, ExtractEntityId extractEntityId, ExtractShardId extractShardId, ClusterShardingSettings settings,
        IShardAllocationStrategy allocationStrategy,
        object handOffStopMessage)
    {
        return builder.WithActors(async (system, registry) =>
        {
            var shardRegion = await ClusterSharding.Get(system)
                .StartAsync(typeName, entityPropsFactory, settings, extractEntityId, extractShardId,
                allocationStrategy, handOffStopMessage);
            registry.TryRegister<TKey>(shardRegion);
        });
    }
    
    /// <summary>
    /// Starts a <see cref="ShardRegion"/> actor for the given entity <see cref="typeName"/>
    /// and registers the ShardRegion <see cref="IActorRef"/> with <see cref="TKey"/> in the
    /// <see cref="ActorRegistry"/> for this <see cref="ActorSystem"/>.
    /// </summary>
    /// <param name="builder">The builder instance being configured.</param>
    /// <param name="typeName">The name of the entity type</param>
    /// <param name="entityPropsFactory">
    /// Function that, given an entity id, returns the <see cref="Actor.Props"/> of the entity actors that will be created by the <see cref="Sharding.ShardRegion"/>
    /// </param>
    /// <param name="settings">Configuration settings, see <see cref="ClusterShardingSettings"/></param>
    /// <param name="messageExtractor">
    /// Functions to extract the entity id, shard id, and the message to send to the entity from the incoming message.
    /// </param>
    /// <typeparam name="TKey">The type key to use to retrieve the <see cref="IActorRef"/> for this <see cref="ShardRegion"/>.</typeparam>
    /// <returns>The same <see cref="AkkaConfigurationBuilder"/> instance originally passed in.</returns>
    public static AkkaConfigurationBuilder WithShardRegion<TKey>(this AkkaConfigurationBuilder builder, string typeName,
        Func<string, Props> entityPropsFactory, IMessageExtractor messageExtractor, ClusterShardingSettings settings)
    {

        return builder.WithActors(async (system, registry) =>
        {
            var shardRegion = await ClusterSharding.Get(system).StartAsync(typeName, entityPropsFactory, 
                settings, messageExtractor);
            registry.TryRegister<TKey>(shardRegion);
        });
    }
    
    /// <summary>
    /// Starts a <see cref="ShardRegion"/> actor for the given entity <see cref="typeName"/>
    /// and registers the ShardRegion <see cref="IActorRef"/> with <see cref="TKey"/> in the
    /// <see cref="ActorRegistry"/> for this <see cref="ActorSystem"/>.
    /// </summary>
    /// <param name="builder">The builder instance being configured.</param>
    /// <param name="typeName">The name of the entity type</param>
    /// <param name="entityPropsFactory">
    /// Function that, given an entity id, returns the <see cref="Actor.Props"/> of the entity actors that will be created by the <see cref="Sharding.ShardRegion"/>
    /// </param>
    /// <param name="settings">Configuration settings, see <see cref="ClusterShardingSettings"/></param>
    /// <param name="extractEntityId">
    /// Partial function to extract the entity id and the message to send to the entity from the incoming message,
    /// if the partial function does not match the message will be `unhandled`,
    /// i.e.posted as `Unhandled` messages on the event stream
    /// </param>
    /// <param name="extractShardId">
    /// Function to determine the shard id for an incoming message, only messages that passed the `extractEntityId` will be used
    /// </param>
    /// <typeparam name="TKey">The type key to use to retrieve the <see cref="IActorRef"/> for this <see cref="ShardRegion"/>.</typeparam>
    /// <returns>The same <see cref="AkkaConfigurationBuilder"/> instance originally passed in.</returns>
    public static AkkaConfigurationBuilder WithShardRegion<TKey>(this AkkaConfigurationBuilder builder, string typeName,
        Func<string, Props> entityPropsFactory, ExtractEntityId extractEntityId, ExtractShardId extractShardId, ClusterShardingSettings settings)
    {

        return builder.WithActors(async (system, registry) =>
        {
            var shardRegion = await ClusterSharding.Get(system).StartAsync(typeName, entityPropsFactory, 
                settings, extractEntityId, extractShardId);
            registry.TryRegister<TKey>(shardRegion);
        });
    }
}