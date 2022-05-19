using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using Akka.Cluster.Sharding;
using Akka.Cluster.Tools.Client;
using Akka.Cluster.Tools.PublishSubscribe;
using Akka.Cluster.Tools.Singleton;
using Akka.Hosting;

namespace Akka.Cluster.Hosting
{
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

    public sealed class ClusterSingletonOptions
    {
        public int? BufferSize { get; set; } = null;
        public string Role { get; set; }
        public object TerminationMessage { get; set; }
    }

    public sealed class ShardOptions
    {
        public StateStoreMode StateStoreMode { get; set; } = StateStoreMode.DData;

        public bool RememberEntities { get; set; } = false;

        public string Role { get; set; }
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
            ClusterOptions options)
        {
            if (options == null)
                return builder;

            if (options.Roles is { Length: > 0 })
                builder = builder.BuildClusterRolesHocon(options.Roles);

            if (options.SeedNodes is { Length: > 0 })
                builder = builder.BuildClusterSeedsHocon(options.SeedNodes);

            // populate all of the possible Clustering default HOCON configurations here
            return builder.AddHocon(ClusterSharding.DefaultConfig()
                .WithFallback(ClusterSingletonManager.DefaultConfig())
                .WithFallback(DistributedPubSub.DefaultConfig())
                .WithFallback(ClusterClientReceptionist.DefaultConfig()));
        }

        /// <summary>
        /// Adds Akka.Cluster support to the <see cref="ActorSystem"/>.
        /// </summary>
        /// <param name="builder">The builder instance being configured.</param>
        /// <param name="options">Optional. Akka.Cluster configuration parameters.</param>
        /// <returns>The same <see cref="AkkaConfigurationBuilder"/> instance originally passed in.</returns>
        public static AkkaConfigurationBuilder WithClustering(this AkkaConfigurationBuilder builder,
            ClusterOptions options = null)
        {
            var hoconBuilder = BuildClusterHocon(builder, options);

            if (builder.ActorRefProvider.HasValue)
            {
                switch (builder.ActorRefProvider.Value)
                {
                    case ProviderSelection.Cluster _:
                    case ProviderSelection.Custom _:
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
        /// <param name="messageExtractor">
        /// Functions to extract the entity id, shard id, and the message to send to the entity from the incoming message.
        /// </param>
        /// <param name="shardOptions">The set of options for configuring <see cref="ClusterShardingSettings"/></param>
        /// <typeparam name="TKey">The type key to use to retrieve the <see cref="IActorRef"/> for this <see cref="ShardRegion"/>.</typeparam>
        /// <returns>The same <see cref="AkkaConfigurationBuilder"/> instance originally passed in.</returns>
        public static AkkaConfigurationBuilder WithShardRegion<TKey>(this AkkaConfigurationBuilder builder,
            string typeName,
            Func<string, Props> entityPropsFactory, IMessageExtractor messageExtractor, ShardOptions shardOptions)
        {
            return builder.WithActors(async (system, registry) =>
            {
                var shardRegion = await ClusterSharding.Get(system).StartAsync(typeName, entityPropsFactory,
                    ClusterShardingSettings.Create(system)
                        .WithRole(shardOptions.Role)
                        .WithRememberEntities(shardOptions.RememberEntities)
                        .WithStateStoreMode(shardOptions.StateStoreMode), messageExtractor);

                // TODO: should throw here if duplicate key used

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
        /// <param name="extractEntityId">
        /// Partial function to extract the entity id and the message to send to the entity from the incoming message,
        /// if the partial function does not match the message will be `unhandled`,
        /// i.e.posted as `Unhandled` messages on the event stream
        /// </param>
        /// <param name="extractShardId">
        /// Function to determine the shard id for an incoming message, only messages that passed the `extractEntityId` will be used
        /// </param>
        /// <param name="shardOptions">The set of options for configuring <see cref="ClusterShardingSettings"/></param>
        /// <typeparam name="TKey">The type key to use to retrieve the <see cref="IActorRef"/> for this <see cref="ShardRegion"/>.</typeparam>
        /// <returns>The same <see cref="AkkaConfigurationBuilder"/> instance originally passed in.</returns>
        public static AkkaConfigurationBuilder WithShardRegion<TKey>(this AkkaConfigurationBuilder builder,
            string typeName,
            Func<string, Props> entityPropsFactory, ExtractEntityId extractEntityId, ExtractShardId extractShardId,
            ShardOptions shardOptions)
        {
            return builder.WithActors(async (system, registry) =>
            {
                var shardRegion = await ClusterSharding.Get(system).StartAsync(typeName, entityPropsFactory,
                    ClusterShardingSettings.Create(system)
                        .WithRole(shardOptions.Role)
                        .WithRememberEntities(shardOptions.RememberEntities)
                        .WithStateStoreMode(shardOptions.StateStoreMode), extractEntityId, extractShardId);

                // TODO: should throw here if duplicate key used

                registry.TryRegister<TKey>(shardRegion);
            });
        }

        /// <summary>
        /// Starts a ShardRegionProxy that points to a <see cref="ShardRegion"/> hosted on a different role inside the cluster
        /// and registers the <see cref="IActorRef"/> with <see cref="TKey"/> in the
        /// <see cref="ActorRegistry"/> for this <see cref="ActorSystem"/>. 
        /// </summary>
        /// <param name="builder">The builder instance being configured.</param>
        /// <param name="typeName">The name of the entity type</param>
        /// <param name="roleName">The role of the Akka.Cluster member that is hosting this <see cref="ShardRegion"/>.</param>
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
        public static AkkaConfigurationBuilder WithShardRegionProxy<TKey>(this AkkaConfigurationBuilder builder,
            string typeName, string roleName, ExtractEntityId extractEntityId, ExtractShardId extractShardId)
        {
            return builder.WithActors(async (system, registry) =>
            {
                var shardRegionProxy = await ClusterSharding.Get(system)
                    .StartProxyAsync(typeName, roleName, extractEntityId, extractShardId);

                // TODO: should throw here if duplicate key used

                registry.TryRegister<TKey>(shardRegionProxy);
            });
        }

        /// <summary>
        /// Starts a ShardRegionProxy that points to a <see cref="ShardRegion"/> hosted on a different role inside the cluster
        /// and registers the <see cref="IActorRef"/> with <see cref="TKey"/> in the
        /// <see cref="ActorRegistry"/> for this <see cref="ActorSystem"/>. 
        /// </summary>
        /// <param name="builder">The builder instance being configured.</param>
        /// <param name="typeName">The name of the entity type</param>
        /// <param name="roleName">The role of the Akka.Cluster member that is hosting this <see cref="ShardRegion"/>.</param>
        /// <param name="messageExtractor">
        /// Functions to extract the entity id, shard id, and the message to send to the entity from the incoming message.
        /// </param>
        /// <typeparam name="TKey">The type key to use to retrieve the <see cref="IActorRef"/> for this <see cref="ShardRegion"/>.</typeparam>
        /// <returns>The same <see cref="AkkaConfigurationBuilder"/> instance originally passed in.</returns>
        public static AkkaConfigurationBuilder WithShardRegionProxy<TKey>(this AkkaConfigurationBuilder builder,
            string typeName, string roleName, IMessageExtractor messageExtractor)
        {
            return builder.WithActors(async (system, registry) =>
            {
                var shardRegionProxy = await ClusterSharding.Get(system)
                    .StartProxyAsync(typeName, roleName, messageExtractor);

                // TODO: should throw here if duplicate key used

                registry.TryRegister<TKey>(shardRegionProxy);
            });
        }

        /// <summary>
        /// Starts <see cref="DistributedPubSub"/> on this node immediately upon <see cref="ActorSystem"/> startup.
        /// </summary>
        /// <param name="builder">The builder instance being configured.</param>
        /// <param name="role">Specifies which role <see cref="DistributedPubSub"/> will broadcast gossip to. If this value
        /// is left blank then ALL roles will be targeted.</param>
        /// <returns>The same <see cref="AkkaConfigurationBuilder"/> instance originally passed in.</returns>
        /// <remarks>
        /// Stores the mediator <see cref="IActorRef"/> in the registry using the <see cref="DistributedPubSub"/> key.
        /// </remarks>
        public static AkkaConfigurationBuilder WithDistributedPubSub(this AkkaConfigurationBuilder builder,
            string role)
        {
            var middle = builder.AddHocon(DistributedPubSub.DefaultConfig());
            if (!string.IsNullOrEmpty(role)) // add role config
            {
                middle = middle.AddHocon($"akka.cluster.pub-sub = \"{role}\"");
            }

            return middle.WithActors((system, registry) =>
            {
                // force the initialization
                var mediator = DistributedPubSub.Get(system).Mediator;
                registry.TryRegister<DistributedPubSub>(mediator);
            });
        }

        /// <summary>
        /// Creates a new <see cref="ClusterSingletonManager"/> to host an actor created via <see cref="actorProps"/>.
        ///
        /// If <see cref="createProxyToo"/> is set to <c>true</c> then this method will also create a <see cref="ClusterSingletonProxy"/> that
        /// will be added to the <see cref="ActorRegistry"/> using the key <see cref="TKey"/>. Otherwise this method will register nothing with
        /// the <see cref="ActorRegistry"/>.
        /// </summary>
        /// <param name="builder">The builder instance being configured.</param>
        /// <param name="singletonName">The name of this singleton instance. Will also be used in the <see cref="ActorPath"/> for the <see cref="ClusterSingletonManager"/> and
        /// optionally, the <see cref="ClusterSingletonProxy"/> created by this method.</param>
        /// <param name="actorProps">The underlying actor type. SHOULD NOT BE CREATED USING <see cref="ClusterSingletonManager.Props"/></param>
        /// <param name="options">Optional. The set of options for configuring both the <see cref="ClusterSingletonManager"/> and
        /// optionally, the <see cref="ClusterSingletonProxy"/>.</param>
        /// <param name="createProxyToo">When set to <c>true></c>, creates a <see cref="ClusterSingletonProxy"/> that automatically points to the <see cref="ClusterSingletonManager"/> created by this method.</param>
        /// <typeparam name="TKey">The key type to use for the <see cref="ActorRegistry"/> when <see cref="createProxyToo"/> is set to <c>true</c>.</typeparam>
        /// <returns>The same <see cref="AkkaConfigurationBuilder"/> instance originally passed in.</returns>
        public static AkkaConfigurationBuilder WithSingleton<TKey>(this AkkaConfigurationBuilder builder,
            string singletonName, Props actorProps, ClusterSingletonOptions options = null, bool createProxyToo = true)
        {
            return builder.WithActors((system, registry) =>
            {
                options ??= new ClusterSingletonOptions();
                var clusterSingletonManagerSettings =
                    ClusterSingletonManagerSettings.Create(system).WithSingletonName(singletonName);

                var singletonProxySettings =
                    ClusterSingletonProxySettings.Create(system).WithSingletonName(singletonName);

                if (!string.IsNullOrEmpty(options.Role))
                {
                    clusterSingletonManagerSettings = clusterSingletonManagerSettings.WithRole(options.Role);
                    singletonProxySettings = singletonProxySettings.WithRole(options.Role);
                }

                var singletonProps = options.TerminationMessage == null
                    ? ClusterSingletonManager.Props(actorProps, clusterSingletonManagerSettings)
                    : ClusterSingletonManager.Props(actorProps, options.TerminationMessage,
                        clusterSingletonManagerSettings);

                var singletonManagerRef = system.ActorOf(singletonProps, singletonName);

                // create a proxy that can talk to the singleton we just created
                // and add it to the ActorRegistry
                if (createProxyToo)
                {
                    if (options.BufferSize != null)
                    {
                        singletonProxySettings = singletonProxySettings.WithBufferSize(options.BufferSize.Value);
                    }

                    CreateAndRegisterSingletonProxy<TKey>($"/user/{singletonManagerRef.Path.Name}", singletonManagerRef.Path.Name, singletonProxySettings, system, registry);
                }
            });
        }

        private static void CreateAndRegisterSingletonProxy<TKey>(string singletonActorName, string singletonActorPath,
            ClusterSingletonProxySettings singletonProxySettings, ActorSystem system, IActorRegistry registry)
        {
            var singletonProxyProps = ClusterSingletonProxy.Props(singletonActorPath,
                singletonProxySettings);
            var singletonProxy = system.ActorOf(singletonProxyProps, $"{singletonActorName}-proxy");
            
            registry.Register<TKey>(singletonProxy);
        }

        /// <summary>
        /// Creates a <see cref="ClusterSingletonProxy"/> and adds it to the <see cref="ActorRegistry"/> using the given
        /// <see cref="TKey"/>.
        /// </summary>
        /// <param name="builder">The builder instance being configured.</param>
        /// <param name="singletonName">The name of this singleton instance. Will also be used in the <see cref="ActorPath"/> for the <see cref="ClusterSingletonManager"/> and
        /// optionally, the <see cref="ClusterSingletonProxy"/> created by this method.</param>
        /// <param name="options">Optional. The set of options for configuring the <see cref="ClusterSingletonProxy"/>.</param>
        /// <param name="singletonManagerPath">Optional. By default Akka.Hosting will assume the <see cref="ClusterSingletonManager"/> is hosted at "/user/{singletonName}" - but
        /// if for some reason the path is different you can use this property to override that value.</param>
        /// <typeparam name="TKey">The key type to use for the <see cref="ActorRegistry"/>.</typeparam>
        /// <returns>The same <see cref="AkkaConfigurationBuilder"/> instance originally passed in.</returns>
        public static AkkaConfigurationBuilder WithSingletonProxy<TKey>(this AkkaConfigurationBuilder builder,
            string singletonName, ClusterSingletonOptions options = null, string singletonManagerPath = null)
        {
            return builder.WithActors((system, registry) =>
            {
                options ??= new ClusterSingletonOptions();

                var singletonProxySettings =
                    ClusterSingletonProxySettings.Create(system).WithSingletonName(singletonName);

                if (!string.IsNullOrEmpty(options.Role))
                {
                    singletonProxySettings = singletonProxySettings.WithRole(options.Role);
                }
                
                if (options.BufferSize != null)
                {
                    singletonProxySettings = singletonProxySettings.WithBufferSize(options.BufferSize.Value);
                }

                singletonManagerPath ??= $"/user/{singletonName}";
                
                CreateAndRegisterSingletonProxy<TKey>(singletonName, singletonManagerPath, singletonProxySettings, system, registry);
            });
        }
    }
}