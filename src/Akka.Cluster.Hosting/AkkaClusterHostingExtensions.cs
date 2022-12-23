using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Cluster.Hosting.SBR;
using Akka.Cluster.Sharding;
using Akka.Cluster.Tools.Client;
using Akka.Cluster.Tools.PublishSubscribe;
using Akka.Cluster.Tools.Singleton;
using Akka.Configuration;
using Akka.Coordination;
using Akka.DependencyInjection;
using Akka.Hosting;
using Akka.Hosting.Coordination;
using Akka.Persistence.Hosting;
using Akka.Util;

#nullable enable
namespace Akka.Cluster.Hosting
{
    /// <summary>
    ///     The set of options for enabling Akka.Cluster support.
    /// </summary>
    public sealed class ClusterOptions
    {
        /// <summary>
        ///     The akka.cluster.roles values.
        /// </summary>
        public string[]? Roles { get; set; }

        /// <summary>
        ///     Optional cluster role check to consider if a specific cluster role have enough
        ///     members to be considered to be up. The default value is 1 node per role. 
        /// </summary>
        public Dictionary<string, int>? MinimumNumberOfMembersPerRole { get; set; }

        /// <summary>
        ///     If populated, the akka.cluster.seed-nodes that will be used.
        /// </summary>
        public string[]? SeedNodes { get; set; }

        /// <summary>
        ///     <para>
        ///         Minimum required number of members before the leader changes member status
        ///         of 'Joining' members to 'Up'. Typically used together with
        ///         <see cref="Cluster.RegisterOnMemberUp"/> to defer some action, such as starting actors,
        ///         until the cluster has reached a certain size.
        ///     </para>
        ///     <b>Default:</b> 1
        /// </summary>
        public int? MinimumNumberOfMembers { get; set; }

        /// <summary>
        ///     <para>
        ///         Application version of the deployment. Used by rolling update features
        ///         to distinguish between old and new nodes. The typical convention is to use
        ///         3 digit version numbers `major.minor.patch`, but 1 or two digits are also
        ///         supported.
        ///     </para>
        ///     <para>
        ///         If no `.` is used it is interpreted as a single digit version number or as
        ///         plain alphanumeric if it couldn't be parsed as a number.
        ///     </para>
        ///     <para>
        ///         It may also have a qualifier at the end for 2 or 3 digit version numbers such
        ///         as "1.2-RC1".<br/>
        ///         For 1 digit with qualifier, 1-RC1, it is interpreted as plain alphanumeric.
        ///     </para>
        ///     <para>
        ///         It has support for https://github.com/dwijnand/sbt-dynver format with `+` or
        ///         `-` separator. The number of commits from the tag is handled as a numeric part.
        ///         For example `1.0.0+3-73475dce26` is less than `1.0.10+10-ed316bd024` (3 &lt; 10).
        ///     </para>
        ///     <para>
        ///         Values can be "assembly-version" or a version string as defined above, i.e.<br/>
        ///         app-version = "1.0.0"<br/>
        ///         app-version = "1.1-beta1"<br/>
        ///         app-version = "1"<br/>
        ///         app-version = "1.1"<br/>
        ///     </para>
        ///     <b>Default:</b> by default the app-version will default to the entry assembly's version,
        ///     i.e. the assembly of the executable running `Program.cs`
        /// </summary>
        public string? AppVersion { get; set; }

        /// <summary>
        ///     <para>
        ///         Enable/disable info level logging of cluster events
        ///     </para>
        ///     <b>Default:</b> <c>true</c>
        /// </summary>
        public bool? LogInfo { get; set; }

        /// <summary>
        ///     <para>
        ///         Enable/disable verbose info-level logging of cluster events for temporary troubleshooting.
        ///     </para>
        ///     <b>Default:</b> <c>false</c>
        /// </summary>
        public bool? LogInfoVerbose { get; set; }

        /// <summary>
        ///     Split brain resolver configuration parameters. This can be an instance of one of these classes:
        ///     <list type="bullet">
        ///         <item><see cref="StaticQuorumOption"/></item>
        ///         <item><see cref="KeepMajorityOption"/></item>
        ///         <item><see cref="KeepOldestOption"/></item>
        ///         <item><see cref="LeaseMajorityOption"/></item>
        ///     </list>
        ///     To use the default split brain resolver options, use <see cref="SplitBrainResolverOption.Default"/> which
        ///     uses the keep majority resolving strategy.
        /// </summary>
        public SplitBrainResolverOption? SplitBrainResolver { get; set; }
    }

    public sealed class ClusterSingletonOptions
    {
        /// <summary>
        ///     <para>
        ///         The number of messages <see cref="ClusterSingletonProxy"/> will buffer when the cluster singleton
        ///         location is unknown. Older messages will be dropped on buffer overflow. Setting this property to 0
        ///         will disable the buffer.
        ///     </para>
        ///     <b>Valid values:</b> 0 - 10000<br/>
        ///     <b>Default:</b> 1000
        /// </summary>
        public int? BufferSize { get; set; } = null;

        /// <summary>
        /// If set, the singleton will only be instantiated on nodes set with the role name.
        /// </summary>
        public string? Role { get; set; }

        /// <summary>
        /// When handing over to a new oldest node this <see cref="TerminationMessage"/> is sent to the singleton actor
        /// to tell it to finish its work, close resources, and stop. The hand-over to the new oldest node
        /// is completed when the singleton actor is terminated. Note that <see cref="PoisonPill"/> is a
        /// perfectly fine <see cref="TerminationMessage"/> if you only need to stop the actor.
        /// </summary>
        public object? TerminationMessage { get; set; }

        /// <summary>
        /// An class instance that extends <see cref="LeaseOptionBase"/>, used to configure the lease provider used in this
        /// cluster singleton.
        /// </summary>
        public LeaseOptionBase? LeaseImplementation { get; set; }

        /// <summary>
        /// The interval between retries for acquiring the lease
        /// </summary>
        public TimeSpan? LeaseRetryInterval { get; set; }
    }

    public sealed class ShardOptions
    {
        /// <summary>
        ///     Defines how the coordinator stores its state. The same setting is also used by the
        ///     shards for RememberEntities.
        /// </summary>
        public StateStoreMode StateStoreMode { get; set; } = StateStoreMode.DData;

        /// <summary>
        ///     When set to <c>true</c>, the active entity actors will automatically be restarted
        ///     upon Shard restart. i.e. if the Shard is started on a different ShardRegion
        ///     due to re-balance or crash.
        /// </summary>
        public bool RememberEntities { get; set; } = false;

        /// <summary>
        ///     Specifies that entities should be instantiated on cluster nodes with a specific role.
        ///     If not specified, all nodes in the cluster are used.
        /// </summary>
        public string? Role { get; set; }

        /// <summary>
        ///     <para>
        ///         The journal plugin configuration identifier used by persistence mode, eg. "sql-server" or
        ///         "postgresql".<br/>
        ///         You only need to declare <see cref="JournalPluginId"/> or <see cref="JournalOptions"/>,
        ///         <see cref="JournalOptions"/> Identifier will be used if both are declared.
        ///     </para>
        ///     <b>NOTE</b> This setting is only used when <see cref="StateStoreMode"/> is set to
        ///     <see cref="Akka.Cluster.Sharding.StateStoreMode.Persistence"/>
        /// </summary>
        public string? JournalPluginId { get; set; }

        /// <summary>
        ///     <para>
        ///         The journal plugin options used by persistence mode, eg. <c>SqlServerJournalOptions</c>
        ///         or <c>PostgreSqlJournalOptions</c>.<br/>
        ///         You only need to declare <see cref="JournalPluginId"/> or <see cref="JournalOptions"/>,
        ///         <see cref="JournalOptions"/> Identifier will be used if both are declared.
        ///     </para>
        ///     <b>NOTE</b> This setting is only used when <see cref="StateStoreMode"/> is set to
        ///     <see cref="Akka.Cluster.Sharding.StateStoreMode.Persistence"/>
        /// </summary>
        public JournalOptions? JournalOptions { get; set; }

        /// <summary>
        ///     <para>
        ///         The snapshot store plugin configuration identifier used by persistence mode, eg. "sql-server" or
        ///         "postgresql".<br/>
        ///         You only need to declare <see cref="SnapshotPluginId"/> or <see cref="SnapshotOptions"/>,
        ///         <see cref="SnapshotOptions"/> Identifier will be used if both are declared.
        ///     </para>
        ///     <b>NOTE</b> This setting is only used when <see cref="StateStoreMode"/> is set to
        ///     <see cref="Akka.Cluster.Sharding.StateStoreMode.Persistence"/>
        /// </summary>
        public string? SnapshotPluginId { get; set; }

        /// <summary>
        ///     <para>
        ///         The snapshot store plugin options used by persistence mode, eg. <c>SqlServerSnapshotOptions</c>
        ///         or <c>PostgreSqlSnapshotOptions</c>.<br/>
        ///         You only need to declare <see cref="SnapshotPluginId"/> or <see cref="SnapshotOptions"/>,
        ///         <see cref="SnapshotOptions"/> Identifier will be used if both are declared.
        ///     </para>
        ///     <b>NOTE</b> This setting is only used when <see cref="StateStoreMode"/> is set to
        ///     <see cref="Akka.Cluster.Sharding.StateStoreMode.Persistence"/>
        /// </summary>
        public SnapshotOptions? SnapshotOptions { get; set; }

        /// <summary>
        /// An class instance that extends <see cref="LeaseOptionBase"/>, used to configure the lease provider used in this
        /// sharding region.
        /// </summary>
        public LeaseOptionBase? LeaseImplementation { get; set; }

        /// <summary>
        /// The interval between retries for acquiring the lease
        /// </summary>
        public TimeSpan? LeaseRetryInterval { get; set; }
    }

    public static class AkkaClusterHostingExtensions
    {
        internal static AkkaConfigurationBuilder BuildClusterHocon(
            this AkkaConfigurationBuilder builder,
            ClusterOptions? options)
        {
            if (options == null)
                return builder;

            var sb = new StringBuilder()
                .AppendLine("akka.cluster {");

            if (options.Roles is { Length: > 0 })
            {
                sb.AppendLine($"roles = [{string.Join(",", options.Roles)}]");
            }

            if (options.MinimumNumberOfMembersPerRole is { Count: > 0 })
            {
                sb.AppendLine("role {");
                foreach (var kvp in options.MinimumNumberOfMembersPerRole)
                {
                    sb.AppendLine($"{kvp.Key}.min-nr-of-members = {kvp.Value}");
                }

                sb.AppendLine("}");
            }

            if (options.SeedNodes is { Length: > 0 })
            {
                // Validate that all addresses are valid.
                sb.Append("seed-nodes = [");
                foreach (var addrString in options.SeedNodes)
                {
                    Address.Parse(addrString);
                    sb.Append($"{addrString.ToHocon()}, ");
                }

                sb.AppendLine("]");
            }

            if (options.MinimumNumberOfMembers is { })
                sb.AppendLine($"min-nr-of-members = {options.MinimumNumberOfMembers}");

            if (options.AppVersion is { })
                sb.AppendLine($"app-version = {options.AppVersion.ToHocon()}");

            if (options.LogInfo is { })
                sb.AppendLine($"log-info = {options.LogInfo.ToHocon()}");

            if (options.LogInfoVerbose is { })
                sb.AppendLine($"log-info-verbose = {options.LogInfoVerbose.ToHocon()}");

            sb.AppendLine("}");

            // prepend the composed configuration
            builder.AddHocon(sb.ToString(), HoconAddMode.Prepend);

            options.SplitBrainResolver?.Apply(builder);

            // populate all of the possible Clustering default HOCON configurations here
            return builder.AddHocon(ClusterSharding.DefaultConfig()
                .WithFallback(ClusterSingletonManager.DefaultConfig())
                .WithFallback(DistributedPubSub.DefaultConfig())
                .WithFallback(ClusterClientReceptionist.DefaultConfig()), HoconAddMode.Append);
        }

        /// <summary>
        ///     Adds Akka.Cluster support to the <see cref="ActorSystem"/>.
        /// </summary>
        /// <param name="builder">
        ///     The builder instance being configured.
        /// </param>
        /// <param name="options">
        ///     Optional. Akka.Cluster configuration parameters.
        /// </param>
        /// <returns>
        ///     The same <see cref="AkkaConfigurationBuilder"/> instance originally passed in.
        /// </returns>
        public static AkkaConfigurationBuilder WithClustering(
            this AkkaConfigurationBuilder builder,
            ClusterOptions? options = null)
        {
            var hoconBuilder = BuildClusterHocon(builder, options);

            if (builder.ActorRefProvider.HasValue)
            {
                switch (builder.ActorRefProvider.Value)
                {
                    case ProviderSelection.Cluster:
                    case ProviderSelection.Custom:
                        return hoconBuilder; // no-op
                }
            }

            return hoconBuilder.WithActorRefProvider(ProviderSelection.Cluster.Instance);
        }

        private static ExtractEntityId ToExtractEntityId(this IMessageExtractor self)
        {
            Option<(string, object)> ExtractEntityId(object msg)
            {
                if (self.EntityId(msg) != null) return (self.EntityId(msg), self.EntityMessage(msg));

                return Option<(string, object)>.None;
            }

            return ExtractEntityId;
        }

        private static ExtractShardId ToExtractShardId(this IMessageExtractor self)
        {
            string? ExtractShardId(object msg)
            {
                return self.EntityId(msg) != null ? self.ShardId(msg) : null;
            }

            return ExtractShardId;
        }

        /// <summary>
        /// INTERNAL API
        ///
        /// Generates the <see cref="ClusterShardingSettings"/> for the specified <paramref name="shardOptions"/>.
        /// </summary>
        /// <param name="shardOptions">The options to use.</param>
        /// <param name="system">The current <see cref="ActorSystem"/>.</param>
        /// <returns>A fully populated <see cref="ClusterShardingSettings"/> instance for use with a specific <see cref="ShardRegion"/>.</returns>
        private static ClusterShardingSettings PopulateClusterShardingSettings(ShardOptions shardOptions,
            ActorSystem system)
        {
            var settings = ClusterShardingSettings.Create(system)
                .WithRole(shardOptions.Role)
                .WithRememberEntities(shardOptions.RememberEntities)
                .WithStateStoreMode(shardOptions.StateStoreMode);

            if (shardOptions.LeaseImplementation is { })
            {
                var retry = shardOptions.LeaseRetryInterval ?? TimeSpan.FromSeconds(5);
                settings = settings
                    .WithLeaseSettings(new LeaseUsageSettings(shardOptions.LeaseImplementation.ConfigPath, retry));
            }

            if (shardOptions.StateStoreMode == StateStoreMode.Persistence)
                settings = settings
                    .WithJournalPluginId(shardOptions.JournalOptions?.Identifier ?? shardOptions.JournalPluginId)
                    .WithSnapshotPluginId(shardOptions.SnapshotOptions?.Identifier ?? shardOptions.SnapshotPluginId);
            return settings;
        }

        /// <summary>
        ///     Starts a <see cref="ShardRegion"/> actor for the given entity <see cref="typeName"/>
        ///     and registers the ShardRegion <see cref="IActorRef"/> with <see cref="TKey"/> in the
        ///     <see cref="ActorRegistry"/> for this <see cref="ActorSystem"/>.
        /// </summary>
        /// <param name="builder">
        ///     The builder instance being configured.
        /// </param>
        /// <param name="typeName">
        ///     The name of the entity type
        /// </param>
        /// <param name="entityPropsFactory">
        ///     Function that, given an entity id, returns the <see cref="Actor.Props"/> of the entity actors that will be created by the <see cref="Sharding.ShardRegion"/>
        /// </param>
        /// <param name="messageExtractor">
        ///     Functions to extract the entity id, shard id, and the message to send to the entity from the incoming message.
        /// </param>
        /// <param name="shardOptions">
        ///     The set of options for configuring <see cref="ClusterShardingSettings"/>
        /// </param>
        /// <typeparam name="TKey">
        ///     The type key to use to retrieve the <see cref="IActorRef"/> for this <see cref="ShardRegion"/>.
        /// </typeparam>
        /// <returns>
        ///     The same <see cref="AkkaConfigurationBuilder"/> instance originally passed in.
        /// </returns>
        public static AkkaConfigurationBuilder WithShardRegion<TKey>(
            this AkkaConfigurationBuilder builder,
            string typeName,
            Func<string, Props> entityPropsFactory,
            IMessageExtractor messageExtractor,
            ShardOptions shardOptions)
        {
            return builder.WithShardRegion<TKey>(typeName, (_, _, _) => entityPropsFactory,
                messageExtractor.ToExtractEntityId(), messageExtractor.ToExtractShardId(), shardOptions);
        }

        /// <summary>
        ///     Starts a <see cref="ShardRegion"/> actor for the given entity <see cref="typeName"/>
        ///     and registers the ShardRegion <see cref="IActorRef"/> with <see cref="TKey"/> in the
        ///     <see cref="ActorRegistry"/> for this <see cref="ActorSystem"/>.
        /// </summary>
        /// <param name="builder">
        ///     The builder instance being configured.
        /// </param>
        /// <param name="typeName">
        ///     The name of the entity type
        /// </param>
        /// <param name="entityPropsFactory">
        ///     Function that, given an entity id, returns the <see cref="Actor.Props"/> of the entity actors that will be created by the <see cref="Sharding.ShardRegion"/>
        /// </param>
        /// <param name="extractEntityId">
        ///     Partial function to extract the entity id and the message to send to the entity from the incoming message,
        ///     if the partial function does not match the message will be `unhandled`,
        ///     i.e.posted as `Unhandled` messages on the event stream
        /// </param>
        /// <param name="extractShardId">
        ///     Function to determine the shard id for an incoming message, only messages that passed the `extractEntityId` will be used
        /// </param>
        /// <param name="shardOptions">
        ///     The set of options for configuring <see cref="ClusterShardingSettings"/>
        /// </param>
        /// <typeparam name="TKey">
        ///     The type key to use to retrieve the <see cref="IActorRef"/> for this <see cref="ShardRegion"/>.
        /// </typeparam>
        /// <returns>
        ///     The same <see cref="AkkaConfigurationBuilder"/> instance originally passed in.
        /// </returns>
        public static AkkaConfigurationBuilder WithShardRegion<TKey>(
            this AkkaConfigurationBuilder builder,
            string typeName,
            Func<string, Props> entityPropsFactory,
            ExtractEntityId extractEntityId,
            ExtractShardId extractShardId,
            ShardOptions shardOptions)
        {
            return builder.WithShardRegion<TKey>(typeName, (_, _, _) => entityPropsFactory,
                extractEntityId, extractShardId, shardOptions);
        }

        /// <summary>
        ///     Starts a <see cref="ShardRegion"/> actor for the given entity <see cref="typeName"/>
        ///     and registers the ShardRegion <see cref="IActorRef"/> with <see cref="TKey"/> in the
        ///     <see cref="ActorRegistry"/> for this <see cref="ActorSystem"/>.
        /// </summary>
        /// <param name="builder">
        ///     The builder instance being configured.
        /// </param>
        /// <param name="typeName">
        ///     The name of the entity type
        /// </param>
        /// <param name="entityPropsFactory">
        ///     Function that, given an entity id, returns the <see cref="Actor.Props"/> of the entity actors that will be created by the <see cref="Sharding.ShardRegion"/>.
        ///
        ///     This function also accepts the <see cref="ActorSystem"/> and the <see cref="IActorRegistry"/> as inputs.    
        /// </param>
        /// <param name="messageExtractor">
        ///     Functions to extract the entity id, shard id, and the message to send to the entity from the incoming message.
        /// </param>
        /// <param name="shardOptions">
        ///     The set of options for configuring <see cref="ClusterShardingSettings"/>
        /// </param>
        /// <typeparam name="TKey">
        ///     The type key to use to retrieve the <see cref="IActorRef"/> for this <see cref="ShardRegion"/>.
        /// </typeparam>
        /// <returns>
        ///     The same <see cref="AkkaConfigurationBuilder"/> instance originally passed in.
        /// </returns>
        public static AkkaConfigurationBuilder WithShardRegion<TKey>(
            this AkkaConfigurationBuilder builder,
            string typeName,
            Func<ActorSystem, IActorRegistry, Func<string, Props>> entityPropsFactory,
            IMessageExtractor messageExtractor,
            ShardOptions shardOptions)
        {
            return builder.WithShardRegion<TKey>(typeName,
                (system, registry, _) => entityPropsFactory(system, registry),
                messageExtractor.ToExtractEntityId(), messageExtractor.ToExtractShardId(), shardOptions);
        }

        /// <summary>
        ///     Starts a <see cref="ShardRegion"/> actor for the given entity <see cref="typeName"/>
        ///     and registers the ShardRegion <see cref="IActorRef"/> with <see cref="TKey"/> in the
        ///     <see cref="ActorRegistry"/> for this <see cref="ActorSystem"/>.
        /// </summary>
        /// <param name="builder">
        ///     The builder instance being configured.
        /// </param>
        /// <param name="typeName">
        ///     The name of the entity type
        /// </param>
        /// <param name="entityPropsFactory">
        ///     Function that, given an entity id, returns the <see cref="Actor.Props"/> of the entity actors that will be created by the <see cref="Sharding.ShardRegion"/>.
        ///
        ///     This function also accepts the <see cref="ActorSystem"/> and the <see cref="IActorRegistry"/> as inputs.    
        /// </param>
        /// <param name="extractEntityId">
        ///     Partial function to extract the entity id and the message to send to the entity from the incoming message,
        ///     if the partial function does not match the message will be `unhandled`,
        ///     i.e.posted as `Unhandled` messages on the event stream
        /// </param>
        /// <param name="extractShardId">
        ///     Function to determine the shard id for an incoming message, only messages that passed the `extractEntityId` will be used
        /// </param>
        /// <param name="shardOptions">
        ///     The set of options for configuring <see cref="ClusterShardingSettings"/>
        /// </param>
        /// <typeparam name="TKey">
        ///     The type key to use to retrieve the <see cref="IActorRef"/> for this <see cref="ShardRegion"/>.
        /// </typeparam>
        /// <returns>
        ///     The same <see cref="AkkaConfigurationBuilder"/> instance originally passed in.
        /// </returns>
        public static AkkaConfigurationBuilder WithShardRegion<TKey>(
            this AkkaConfigurationBuilder builder,
            string typeName,
            Func<ActorSystem, IActorRegistry, Func<string, Props>> entityPropsFactory,
            ExtractEntityId extractEntityId,
            ExtractShardId extractShardId,
            ShardOptions shardOptions)
        {
            return builder.WithShardRegion<TKey>(typeName,
                (system, registry, _) => entityPropsFactory(system, registry),
                extractEntityId, extractShardId, shardOptions);
        }

        /// <summary>
        ///     Starts a <see cref="ShardRegion"/> actor for the given entity <see cref="typeName"/>
        ///     and registers the ShardRegion <see cref="IActorRef"/> with <see cref="TKey"/> in the
        ///     <see cref="ActorRegistry"/> for this <see cref="ActorSystem"/>.
        /// </summary>
        /// <param name="builder">
        ///     The builder instance being configured.
        /// </param>
        /// <param name="typeName">
        ///     The name of the entity type
        /// </param>
        /// <param name="entityPropsFactory">
        ///     Function that, given an entity id, returns the <see cref="Actor.Props"/> of the entity actors that will be created by the <see cref="Sharding.ShardRegion"/>.
        ///
        ///     This function also accepts the <see cref="ActorSystem"/> and the <see cref="IActorRegistry"/> as inputs.    
        /// </param>
        /// <param name="messageExtractor">
        ///     Functions to extract the entity id, shard id, and the message to send to the entity from the incoming message.
        /// </param>
        /// <param name="shardOptions">
        ///     The set of options for configuring <see cref="ClusterShardingSettings"/>
        /// </param>
        /// <typeparam name="TKey">
        ///     The type key to use to retrieve the <see cref="IActorRef"/> for this <see cref="ShardRegion"/>.
        /// </typeparam>
        /// <returns>
        ///     The same <see cref="AkkaConfigurationBuilder"/> instance originally passed in.
        /// </returns>
        public static AkkaConfigurationBuilder WithShardRegion<TKey>(
            this AkkaConfigurationBuilder builder,
            string typeName,
            Func<ActorSystem, IActorRegistry, IDependencyResolver, Func<string, Props>> entityPropsFactory,
            IMessageExtractor messageExtractor,
            ShardOptions shardOptions)
        {
            return builder.WithShardRegion<TKey>(typeName, entityPropsFactory,
                messageExtractor.ToExtractEntityId(), messageExtractor.ToExtractShardId(), shardOptions);
        }

        /// <summary>
        ///     Starts a <see cref="ShardRegion"/> actor for the given entity <see cref="typeName"/>
        ///     and registers the ShardRegion <see cref="IActorRef"/> with <see cref="TKey"/> in the
        ///     <see cref="ActorRegistry"/> for this <see cref="ActorSystem"/>.
        /// </summary>
        /// <param name="builder">
        ///     The builder instance being configured.
        /// </param>
        /// <param name="typeName">
        ///     The name of the entity type
        /// </param>
        /// <param name="entityPropsFactory">
        ///     Function that, given an entity id, returns the <see cref="Actor.Props"/> of the entity actors that will be created by the <see cref="Sharding.ShardRegion"/>.
        ///
        ///     This function also accepts the <see cref="ActorSystem"/> and the <see cref="IActorRegistry"/> as inputs.    
        /// </param>
        /// <param name="extractEntityId">
        ///     Partial function to extract the entity id and the message to send to the entity from the incoming message,
        ///     if the partial function does not match the message will be `unhandled`,
        ///     i.e.posted as `Unhandled` messages on the event stream
        /// </param>
        /// <param name="extractShardId">
        ///     Function to determine the shard id for an incoming message, only messages that passed the `extractEntityId` will be used
        /// </param>
        /// <param name="shardOptions">
        ///     The set of options for configuring <see cref="ClusterShardingSettings"/>
        /// </param>
        /// <typeparam name="TKey">
        ///     The type key to use to retrieve the <see cref="IActorRef"/> for this <see cref="ShardRegion"/>.
        /// </typeparam>
        /// <returns>
        ///     The same <see cref="AkkaConfigurationBuilder"/> instance originally passed in.
        /// </returns>
        public static AkkaConfigurationBuilder WithShardRegion<TKey>(
            this AkkaConfigurationBuilder builder,
            string typeName,
            Func<ActorSystem, IActorRegistry, IDependencyResolver, Func<string, Props>> entityPropsFactory,
            ExtractEntityId extractEntityId,
            ExtractShardId extractShardId,
            ShardOptions shardOptions)
        {
            async Task Resolver(ActorSystem system, IActorRegistry registry, IDependencyResolver resolver)
            {
                var props = entityPropsFactory(system, registry, resolver);
                var settings = ClusterShardingSettings.Create(system).WithRole(shardOptions.Role);
                var shardRegion = await ClusterSharding.Get(system)
                    .StartAsync(typeName, props, settings, extractEntityId, extractShardId).ConfigureAwait(false);
                registry.Register<TKey>(shardRegion);
            }

            return builder.StartActors(Resolver);
        }

        /// <summary>
        ///     Starts a ShardRegionProxy that points to a <see cref="ShardRegion"/> hosted on a different role inside the cluster
        ///     and registers the <see cref="IActorRef"/> with <see cref="TKey"/> in the
        ///     <see cref="ActorRegistry"/> for this <see cref="ActorSystem"/>. 
        /// </summary>
        /// <param name="builder">
        ///     The builder instance being configured.
        /// </param>
        /// <param name="typeName">
        ///     The name of the entity type
        /// </param>
        /// <param name="roleName">
        ///     The role of the Akka.Cluster member that is hosting this <see cref="ShardRegion"/>.
        /// </param>
        /// <param name="extractEntityId">
        ///     Partial function to extract the entity id and the message to send to the entity from the incoming message,
        ///     if the partial function does not match the message will be `unhandled`,
        ///     i.e.posted as `Unhandled` messages on the event stream
        /// </param>
        /// <param name="extractShardId">
        ///     Function to determine the shard id for an incoming message, only messages that passed the `extractEntityId` will be used
        /// </param>
        /// <typeparam name="TKey">
        ///     The type key to use to retrieve the <see cref="IActorRef"/> for this <see cref="ShardRegion"/>.
        /// </typeparam>
        /// <returns>
        ///     The same <see cref="AkkaConfigurationBuilder"/> instance originally passed in.
        /// </returns>
        public static AkkaConfigurationBuilder WithShardRegionProxy<TKey>(
            this AkkaConfigurationBuilder builder,
            string typeName,
            string roleName,
            ExtractEntityId extractEntityId,
            ExtractShardId extractShardId)
        {
            return builder.WithActors(async (system, registry) =>
            {
                var shardRegionProxy = await ClusterSharding.Get(system)
                    .StartProxyAsync(typeName, roleName, extractEntityId, extractShardId);

                registry.Register<TKey>(shardRegionProxy);
            });
        }

        /// <summary>
        ///     Starts a ShardRegionProxy that points to a <see cref="ShardRegion"/> hosted on a different role inside the cluster
        ///     and registers the <see cref="IActorRef"/> with <see cref="TKey"/> in the
        ///     <see cref="ActorRegistry"/> for this <see cref="ActorSystem"/>. 
        /// </summary>
        /// <param name="builder">
        ///     The builder instance being configured.
        /// </param>
        /// <param name="typeName">
        ///     The name of the entity type
        /// </param>
        /// <param name="roleName">
        ///     The role of the Akka.Cluster member that is hosting this <see cref="ShardRegion"/>.
        /// </param>
        /// <param name="messageExtractor">
        ///     Functions to extract the entity id, shard id, and the message to send to the entity from the incoming message.
        /// </param>
        /// <typeparam name="TKey">
        ///     The type key to use to retrieve the <see cref="IActorRef"/> for this <see cref="ShardRegion"/>.
        /// </typeparam>
        /// <returns>
        ///     The same <see cref="AkkaConfigurationBuilder"/> instance originally passed in.
        /// </returns>
        public static AkkaConfigurationBuilder WithShardRegionProxy<TKey>(
            this AkkaConfigurationBuilder builder,
            string typeName,
            string roleName,
            IMessageExtractor messageExtractor)
        {
            return builder.WithActors(async (system, registry) =>
            {
                var shardRegionProxy = await ClusterSharding.Get(system)
                    .StartProxyAsync(typeName, roleName, messageExtractor);

                registry.Register<TKey>(shardRegionProxy);
            });
        }

        /// <summary>
        ///     Starts <see cref="DistributedPubSub"/> on this node immediately upon <see cref="ActorSystem"/> startup.
        /// </summary>
        /// <param name="builder">
        ///     The builder instance being configured.
        /// </param>
        /// <param name="role">
        ///     Specifies which role <see cref="DistributedPubSub"/> will broadcast gossip to. If this value
        ///     is left blank then ALL roles will be targeted.
        /// </param>
        /// <returns>
        ///     The same <see cref="AkkaConfigurationBuilder"/> instance originally passed in.
        /// </returns>
        /// <remarks>
        ///     Stores the mediator <see cref="IActorRef"/> in the registry using the <see cref="DistributedPubSub"/> key.
        /// </remarks>
        public static AkkaConfigurationBuilder WithDistributedPubSub(
            this AkkaConfigurationBuilder builder,
            string role)
        {
            var middle = builder.AddHocon(DistributedPubSub.DefaultConfig(), HoconAddMode.Append);
            if (!string.IsNullOrEmpty(role)) // add role config
            {
                middle = middle.AddHocon($"akka.cluster.pub-sub.role = \"{role}\"", HoconAddMode.Prepend);
            }

            return middle.WithActors((system, registry) =>
            {
                // force the initialization
                var mediator = DistributedPubSub.Get(system).Mediator;
                registry.Register<DistributedPubSub>(mediator);
            });
        }

        /// <summary>
        ///     <para>
        ///         Creates a new <see cref="ClusterSingletonManager"/> to host an actor created via <see cref="actorProps"/>.
        ///     </para>
        ///
        ///     If <paramref name="createProxyToo"/> is set to <c>true</c> then this method will also create a
        ///     <see cref="ClusterSingletonProxy"/> that will be added to the <see cref="ActorRegistry"/> using the key
        ///     <see cref="TKey"/>. Otherwise this method will register nothing with the <see cref="ActorRegistry"/>.
        /// </summary>
        /// <param name="builder">
        ///     The builder instance being configured.
        /// </param>
        /// <param name="singletonName">
        ///     The name of this singleton instance. Will also be used in the <see cref="ActorPath"/> for the
        ///     <see cref="ClusterSingletonManager"/> and optionally, the <see cref="ClusterSingletonProxy"/> created
        ///     by this method.
        /// </param>
        /// <param name="propsFactory">
        ///     A function that accepts the <see cref="ActorSystem"/>, <see cref="ActorRegistry"/>, and <see cref="IDependencyResolver"/>
        ///     and returns the <see cref="Props"/> for the actor
        /// </param>
        /// <param name="options">
        ///     Optional. The set of options for configuring both the <see cref="ClusterSingletonManager"/> and
        ///     optionally, the <see cref="ClusterSingletonProxy"/>.
        /// </param>
        /// <param name="createProxyToo">
        ///     When set to <c>true></c>, creates a <see cref="ClusterSingletonProxy"/> that automatically points to
        ///     the <see cref="ClusterSingletonManager"/> created by this method.
        /// </param>
        /// <typeparam name="TKey">
        ///     The key type to use for the <see cref="ActorRegistry"/> when <paramref name="createProxyToo"/> is set to <c>true</c>.
        /// </typeparam>
        /// <returns>
        ///     The same <see cref="AkkaConfigurationBuilder"/> instance originally passed in.
        /// </returns>
        public static AkkaConfigurationBuilder WithSingleton<TKey>(
            this AkkaConfigurationBuilder builder,
            string singletonName,
            Func<ActorSystem, IActorRegistry, IDependencyResolver, Props> propsFactory,
            ClusterSingletonOptions? options = null,
            bool createProxyToo = true)
        {
            return builder.WithActors((system, registry, resolver) =>
            {
                var actorProps = propsFactory(system, registry, resolver);

                options ??= new ClusterSingletonOptions();
                var clusterSingletonManagerSettings =
                    ClusterSingletonManagerSettings.Create(system).WithSingletonName(singletonName);

                if (options.LeaseImplementation is { })
                {
                    var retry = options.LeaseRetryInterval ?? TimeSpan.FromSeconds(5);
                    clusterSingletonManagerSettings = clusterSingletonManagerSettings
                        .WithLeaseSettings(new LeaseUsageSettings(options.LeaseImplementation.ConfigPath, retry));
                }

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

                    CreateAndRegisterSingletonProxy<TKey>(singletonManagerRef.Path.Name,
                        $"/user/{singletonManagerRef.Path.Name}", singletonProxySettings, system, registry);
                }
            });
        }

        /// <summary>
        ///     <para>
        ///         Creates a new <see cref="ClusterSingletonManager"/> to host an actor created via <see cref="actorProps"/>.
        ///     </para>
        ///
        ///     If <paramref name="createProxyToo"/> is set to <c>true</c> then this method will also create a
        ///     <see cref="ClusterSingletonProxy"/> that will be added to the <see cref="ActorRegistry"/> using the key
        ///     <see cref="TKey"/>. Otherwise this method will register nothing with the <see cref="ActorRegistry"/>.
        /// </summary>
        /// <param name="builder">
        ///     The builder instance being configured.
        /// </param>
        /// <param name="singletonName">
        ///     The name of this singleton instance. Will also be used in the <see cref="ActorPath"/> for the
        ///     <see cref="ClusterSingletonManager"/> and optionally, the <see cref="ClusterSingletonProxy"/> created
        ///     by this method.
        /// </param>
        /// <param name="actorProps">
        ///     The underlying actor type. SHOULD NOT BE CREATED USING <see cref="Props"/>
        /// </param>
        /// <param name="options">
        ///     Optional. The set of options for configuring both the <see cref="ClusterSingletonManager"/> and
        ///     optionally, the <see cref="ClusterSingletonProxy"/>.
        /// </param>
        /// <param name="createProxyToo">
        ///     When set to <c>true></c>, creates a <see cref="ClusterSingletonProxy"/> that automatically points to
        ///     the <see cref="ClusterSingletonManager"/> created by this method.
        /// </param>
        /// <typeparam name="TKey">
        ///     The key type to use for the <see cref="ActorRegistry"/> when <paramref name="createProxyToo"/> is set to <c>true</c>.
        /// </typeparam>
        /// <returns>
        ///     The same <see cref="AkkaConfigurationBuilder"/> instance originally passed in.
        /// </returns>
        public static AkkaConfigurationBuilder WithSingleton<TKey>(
            this AkkaConfigurationBuilder builder,
            string singletonName,
            Props actorProps,
            ClusterSingletonOptions? options = null,
            bool createProxyToo = true)
        {
            return builder.WithSingleton<TKey>(singletonName, (_, _, _) => actorProps, options,
                createProxyToo);
        }

        private static void CreateAndRegisterSingletonProxy<TKey>(
            string singletonActorName,
            string singletonActorPath,
            ClusterSingletonProxySettings singletonProxySettings,
            ActorSystem system,
            IActorRegistry registry)
        {
            var singletonProxyProps = ClusterSingletonProxy.Props(singletonActorPath,
                singletonProxySettings);
            var singletonProxy = system.ActorOf(singletonProxyProps, $"{singletonActorName}-proxy");

            registry.Register<TKey>(singletonProxy);
        }

        /// <summary>
        ///     Creates a <see cref="ClusterSingletonProxy"/> and adds it to the <see cref="ActorRegistry"/> using the
        ///     given <see cref="TKey"/>.
        /// </summary>
        /// <param name="builder">
        ///     The builder instance being configured.
        /// </param>
        /// <param name="singletonName">
        ///     The name of this singleton instance. Will also be used in the <see cref="ActorPath"/> for the
        ///     <see cref="ClusterSingletonManager"/> and optionally, the <see cref="ClusterSingletonProxy"/> created
        ///     by this method.
        /// </param>
        /// <param name="options">
        ///     Optional. The set of options for configuring the <see cref="ClusterSingletonProxy"/>.
        /// </param>
        /// <param name="singletonManagerPath">
        ///     Optional. By default Akka.Hosting will assume the <see cref="ClusterSingletonManager"/> is hosted at
        ///     "/user/{singletonName}" - but if for some reason the path is different you can use this property to
        ///     override that value.
        /// </param>
        /// <typeparam name="TKey">
        ///     The key type to use for the <see cref="ActorRegistry"/>.
        /// </typeparam>
        /// <returns>
        ///     The same <see cref="AkkaConfigurationBuilder"/> instance originally passed in.
        /// </returns>
        public static AkkaConfigurationBuilder WithSingletonProxy<TKey>(
            this AkkaConfigurationBuilder builder,
            string singletonName,
            ClusterSingletonOptions? options = null,
            string? singletonManagerPath = null)
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

                CreateAndRegisterSingletonProxy<TKey>(singletonName, singletonManagerPath, singletonProxySettings,
                    system, registry);
            });
        }

        /// <summary>
        ///     Configures a <see cref="ClusterClientReceptionist"/> for the <see cref="ActorSystem"/>
        /// </summary>
        /// <param name="builder">
        ///     The builder instance being configured.
        /// </param>
        /// <param name="name">
        ///     Actor name of the ClusterReceptionist actor under the system path, by default it is /system/receptionist
        /// </param>
        /// <param name="role">
        ///     Checks that the receptionist only start on members tagged with this role. All members are used if empty.
        /// </param>
        /// <returns>
        ///     The same <see cref="AkkaConfigurationBuilder"/> instance originally passed in.
        /// </returns>
        public static AkkaConfigurationBuilder WithClusterClientReceptionist(
            this AkkaConfigurationBuilder builder,
            string name = "receptionist",
            string? role = null)
        {
            builder.AddHocon(CreateReceptionistConfig(name, role), HoconAddMode.Prepend);
            return builder;
        }

        internal static Config CreateReceptionistConfig(string name, string? role)
        {
            const string root = "akka.cluster.client.receptionist.";

            var sb = new StringBuilder()
                .Append(root).Append("name:").AppendLine(name.ToHocon());

            if (!string.IsNullOrEmpty(role))
                sb.Append(root).Append("role:").AppendLine(role!.ToHocon());

            return ConfigurationFactory.ParseString(sb.ToString());
        }

        /// <summary>
        ///     Creates a <see cref="ClusterClient"/> and adds it to the <see cref="ActorRegistry"/> using the given
        ///     <see cref="TKey"/>.
        /// </summary>
        /// <param name="builder">
        ///     The builder instance being configured.
        /// </param>
        /// <param name="initialContacts">
        ///     <para>
        ///         List of <see cref="ClusterClientReceptionist"/> <see cref="ActorPath"/> that will be used as a seed
        ///         to discover all of the receptionists in the cluster.
        ///     </para>
        ///     <para>
        ///         This should look something like "akka.tcp://systemName@networkAddress:2552/system/receptionist"
        ///     </para>
        /// </param>
        /// <typeparam name="TKey">
        ///     The key type to use for the <see cref="ActorRegistry"/>.
        /// </typeparam>
        /// <returns>
        ///     The same <see cref="AkkaConfigurationBuilder"/> instance originally passed in.
        /// </returns>
        public static AkkaConfigurationBuilder WithClusterClient<TKey>(
            this AkkaConfigurationBuilder builder,
            IList<ActorPath> initialContacts)
        {
            if (initialContacts == null)
                throw new ArgumentNullException(nameof(initialContacts));

            if (initialContacts.Count < 1)
                throw new ArgumentException("Must specify at least one initial contact", nameof(initialContacts));

            return builder.WithActors((system, registry) =>
            {
                var clusterClient = system.ActorOf(ClusterClient.Props(
                    CreateClusterClientSettings(system.Settings.Config, initialContacts)));
                registry.TryRegister<TKey>(clusterClient);
            });
        }

        /// <summary>
        ///     Creates a <see cref="ClusterClient"/> and adds it to the <see cref="ActorRegistry"/> using the given
        ///     <see cref="TKey"/>.
        /// </summary>
        /// <param name="builder">
        ///     The builder instance being configured.
        /// </param>
        /// <param name="initialContactAddresses">
        ///     <para>
        ///         List of node addresses where the <see cref="ClusterClientReceptionist"/> are located that will be
        ///         used as seed to discover all of the receptionists in the cluster.
        ///     </para>
        ///     <para>
        ///         This should look something like "akka.tcp://systemName@networkAddress:2552"
        ///     </para>
        /// </param>
        /// <param name="receptionistActorName">
        ///     The name of the <see cref="ClusterClientReceptionist"/> actor. <br/>
        ///     Defaults to "receptionist"
        /// </param>
        /// <typeparam name="TKey">
        ///     The key type to use for the <see cref="ActorRegistry"/>.
        /// </typeparam>
        /// <returns>
        ///     The same <see cref="AkkaConfigurationBuilder"/> instance originally passed in.
        /// </returns>
        public static AkkaConfigurationBuilder WithClusterClient<TKey>(
            this AkkaConfigurationBuilder builder,
            IEnumerable<Address> initialContactAddresses,
            string receptionistActorName = "receptionist")
            => builder.WithClusterClient<TKey>(initialContactAddresses
                .Select(address => new RootActorPath(address) / "system" / receptionistActorName)
                .ToList());

        /// <summary>
        ///     Creates a <see cref="ClusterClient"/> and adds it to the <see cref="ActorRegistry"/> using the given
        ///     <see cref="TKey"/>.
        /// </summary>
        /// <param name="builder">
        ///     The builder instance being configured.
        /// </param>
        /// <param name="initialContacts">
        ///     <para>
        ///         List of actor paths that will be used as a seed to discover all of the receptionists in the cluster.
        ///     </para>
        ///     <para>
        ///         This should look something like "akka.tcp://systemName@networkAddress:2552/system/receptionist"
        ///     </para>
        /// </param>
        /// <typeparam name="TKey">
        ///     The key type to use for the <see cref="ActorRegistry"/>.
        /// </typeparam>
        /// <returns>
        ///     The same <see cref="AkkaConfigurationBuilder"/> instance originally passed in.
        /// </returns>
        public static AkkaConfigurationBuilder WithClusterClient<TKey>(
            this AkkaConfigurationBuilder builder,
            IEnumerable<string> initialContacts)
            => builder.WithClusterClient<TKey>(initialContacts.Select(ActorPath.Parse).ToList());

        internal static ClusterClientSettings CreateClusterClientSettings(Config config,
            IEnumerable<ActorPath> initialContacts)
        {
            var clientConfig = config.GetConfig("akka.cluster.client");
            return ClusterClientSettings.Create(clientConfig)
                .WithInitialContacts(initialContacts.ToImmutableHashSet());
        }
    }
}