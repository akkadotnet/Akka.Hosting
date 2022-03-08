using Akka.Actor;
using Akka.Cluster.Sharding;
using Akka.Cluster.Tools.Client;
using Akka.Cluster.Tools.PublishSubscribe;
using Akka.Cluster.Tools.Singleton;
using Akka.Hosting;

namespace Akka.Cluster.Hosting;

public sealed class ClusterOptions
{
    public string[] Roles { get; set; }

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

    public static AkkaConfigurationBuilder WithShardRegion(this AkkaConfigurationBuilder builder, string shardRegion,
        Func<string, Props> entityFactory, IMessageExtractor extractor, ClusterShardingSettings settings)
    {
        
    }
}