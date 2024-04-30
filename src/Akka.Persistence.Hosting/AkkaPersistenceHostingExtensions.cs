using System;
using System.Collections.Generic;
using System.Text;
using Akka.Configuration;
using Akka.Hosting;
using Akka.Persistence.Journal;
using Akka.Util;
using Akka.Actor;

#nullable enable
namespace Akka.Persistence.Hosting
{
    public enum PersistenceMode
    {
        /// <summary>
        /// Sets both the akka.persistence.journal and the akka.persistence.snapshot-store to use this plugin.
        /// </summary>
        Both,

        /// <summary>
        /// Sets ONLY the akka.persistence.journal to use this plugin.
        /// </summary>
        Journal,

        /// <summary>
        /// Sets ONLY the akka.persistence.snapshot-store to use this plugin.
        /// </summary>
        SnapshotStore,
    }

    /// <summary>
    /// Used to help build journal configurations
    /// </summary>
    public sealed class AkkaPersistenceJournalBuilder
    {
        internal readonly string JournalId;
        internal readonly AkkaConfigurationBuilder Builder;
        internal readonly Dictionary<Type, HashSet<string>> Bindings = new Dictionary<Type, HashSet<string>>();
        internal readonly Dictionary<string, Type> Adapters = new Dictionary<string, Type>();

        public AkkaPersistenceJournalBuilder(string journalId, AkkaConfigurationBuilder builder)
        {
            JournalId = journalId;
            Builder = builder;
        }

        public AkkaPersistenceJournalBuilder AddEventAdapter<TAdapter>(string eventAdapterName,
            IEnumerable<Type> boundTypes) where TAdapter : IEventAdapter
        {
            AddAdapter<TAdapter>(eventAdapterName, boundTypes);

            return this;
        }

        public AkkaPersistenceJournalBuilder AddReadEventAdapter<TAdapter>(string eventAdapterName,
            IEnumerable<Type> boundTypes) where TAdapter : IReadEventAdapter
        {
            AddAdapter<TAdapter>(eventAdapterName, boundTypes);

            return this;
        }

        public AkkaPersistenceJournalBuilder AddWriteEventAdapter<TAdapter>(string eventAdapterName,
            IEnumerable<Type> boundTypes) where TAdapter : IWriteEventAdapter
        {
            AddAdapter<TAdapter>(eventAdapterName, boundTypes);

            return this;
        }

        private void AddAdapter<TAdapter>(string eventAdapterName, IEnumerable<Type> boundTypes)
        {
            Adapters[eventAdapterName] = typeof(TAdapter);
            foreach (var t in boundTypes)
            {
                if (!Bindings.ContainsKey(t))
                    Bindings[t] = new HashSet<string>();
                Bindings[t].Add(eventAdapterName);
            }
        }

        /// <summary>
        /// INTERNAL API - Builds the HOCON and then injects it.
        /// </summary>
        internal void Build()
        {
            // useless configuration - don't bother.
            if (Adapters.Count == 0 || Bindings.Count == 0)
                return;

            var adapters = new StringBuilder()
                .Append($"akka.persistence.journal.{JournalId}").Append("{");

            AppendAdapters(adapters);

            adapters.AppendLine("}");

            var finalHocon = ConfigurationFactory.ParseString(adapters.ToString())
                .WithFallback(Persistence.DefaultConfig()); // add the default config as a fallback
            Builder.AddHocon(finalHocon, HoconAddMode.Prepend);
        }

        internal void AppendAdapters(StringBuilder sb)
        {
            // useless configuration - don't bother.
            if (Adapters.Count == 0 || Bindings.Count == 0)
                return;

            sb.AppendLine("event-adapters {");
            foreach (var kv in Adapters)
            {
                sb.AppendLine($"{kv.Key} = \"{kv.Value.TypeQualifiedName()}\"");
            }

            sb.AppendLine("}").AppendLine("event-adapter-bindings {");
            foreach (var kv in Bindings)
            {
                sb.AppendLine($"\"{kv.Key.TypeQualifiedName()}\" = [{string.Join(",", kv.Value)}]");
            }

            sb.AppendLine("}");
        }
    }

    /// <summary>
    /// The set of options for generic Akka.Persistence.
    /// </summary>
    public static class AkkaPersistenceHostingExtensions
    {
        /// <summary>
        /// A generic way to add both journal and snapshot store configuration to the <see cref="ActorSystem"/>
        /// </summary>
        /// <param name="builder">The builder instance being configured.</param>
        /// <param name="journalOptions">The specific journal options instance used to configure the journal. For example, an instance of <c>SqlServerJournalOptions</c></param>
        /// <param name="snapshotOptions">The specific snapshot store options instance used to configure the snapshot store. For example, an instance of <c>SqlServerSnapshotOptions</c></param>
        /// <returns>The same <see cref="AkkaConfigurationBuilder"/> instance originally passed in.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static AkkaConfigurationBuilder WithJournalAndSnapshot(
            this AkkaConfigurationBuilder builder,
            JournalOptions journalOptions,
            SnapshotOptions snapshotOptions)
        {
            if (journalOptions is null)
                throw new ArgumentNullException(nameof(journalOptions));
            if (snapshotOptions is null)
                throw new ArgumentNullException(nameof(snapshotOptions));

            builder.AddHocon(journalOptions.ToConfig(), HoconAddMode.Prepend);
            var defaultConfig = journalOptions.DefaultConfig;

            builder.AddHocon(snapshotOptions.ToConfig(), HoconAddMode.Prepend);
            defaultConfig = defaultConfig.Equals(snapshotOptions.DefaultConfig)
                ? defaultConfig
                : defaultConfig.WithFallback(snapshotOptions.DefaultConfig);

            return builder.AddHocon(defaultConfig, HoconAddMode.Append);
        }

        /// <summary>
        /// A generic way to add journal configuration to the <see cref="ActorSystem"/>
        /// </summary>
        /// <param name="builder">The builder instance being configured.</param>
        /// <param name="journalOptions">The specific journal options instance used to configure the journal. For example, an instance of <c>SqlServerJournalOptions</c></param>
        /// <returns>The same <see cref="AkkaConfigurationBuilder"/> instance originally passed in.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static AkkaConfigurationBuilder WithJournal(
            this AkkaConfigurationBuilder builder,
            JournalOptions journalOptions)
        {
            if (journalOptions is null)
                throw new ArgumentNullException(nameof(journalOptions));

            builder.AddHocon(journalOptions.ToConfig(), HoconAddMode.Prepend);
            return builder.AddHocon(journalOptions.DefaultConfig, HoconAddMode.Append);
        }

        /// <summary>
        /// A generic way to add snapshot store configuration to the <see cref="ActorSystem"/>
        /// </summary>
        /// <param name="builder">The builder instance being configured.</param>
        /// <param name="snapshotOptions">The specific snapshot store options instance used to configure the snapshot store. For example, an instance of <c>SqlServerSnapshotOptions</c></param>
        /// <returns>The same <see cref="AkkaConfigurationBuilder"/> instance originally passed in.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static AkkaConfigurationBuilder WithSnapshot(
            this AkkaConfigurationBuilder builder,
            SnapshotOptions snapshotOptions)
        {
            if (snapshotOptions is null)
                throw new ArgumentNullException(nameof(snapshotOptions));

            builder.AddHocon(snapshotOptions.ToConfig(), HoconAddMode.Prepend);
            return builder.AddHocon(snapshotOptions.DefaultConfig, HoconAddMode.Append);
        }

        /// <summary>
        /// Used to configure a specific Akka.Persistence.Journal instance, primarily to support <see cref="IEventAdapter"/>s.
        /// </summary>
        /// <param name="builder">The builder instance being configured.</param>
        /// <param name="journalId">The id of the journal. i.e. if you want to apply this adapter to the `akka.persistence.journal.sql-server` journal, just type `sql-server`.</param>
        /// <param name="journalBuilder">Configuration method for configuring the journal.</param>
        /// <returns>The same <see cref="AkkaConfigurationBuilder"/> instance originally passed in.</returns>
        /// <remarks>
        /// This method can be called multiple times for different <see cref="IEventAdapter"/>s.
        /// </remarks>
        public static AkkaConfigurationBuilder WithJournal(
            this AkkaConfigurationBuilder builder,
            string journalId,
            Action<AkkaPersistenceJournalBuilder> journalBuilder)
        {
            var jBuilder = new AkkaPersistenceJournalBuilder(journalId, builder);
            journalBuilder(jBuilder);

            // build and inject the HOCON
            jBuilder.Build();
            return builder;
        }

        public static AkkaConfigurationBuilder WithInMemoryJournal(this AkkaConfigurationBuilder builder)
        {
            return WithInMemoryJournal(builder, _ => { });
        }

        public static AkkaConfigurationBuilder WithInMemoryJournal(
            this AkkaConfigurationBuilder builder,
            Action<AkkaPersistenceJournalBuilder> journalBuilder,
            string journalId = "inmem",
            bool isDefaultPlugin = true)
        {
            builder.WithJournal(journalId, journalBuilder);

            var liveConfig =
                @$"{(isDefaultPlugin ? $"akka.persistence.journal.plugin = akka.persistence.journal.{journalId}" : "")} 
akka.persistence.journal.{journalId} {{
    class = ""Akka.Persistence.Journal.MemoryJournal, Akka.Persistence""
    plugin-dispatcher = ""akka.actor.default-dispatcher""
}}";

            return builder.AddHocon(liveConfig, HoconAddMode.Prepend);
        }

        public static AkkaConfigurationBuilder WithInMemorySnapshotStore(
            this AkkaConfigurationBuilder builder,
            string snapshotStoreId = "inmem",
            bool isDefaultPlugin = true)
        {
            var liveConfig =
                $@"{(isDefaultPlugin ? $"akka.persistence.snapshot-store.plugin = akka.persistence.snapshot-store.{snapshotStoreId}" : "")}
akka.persistence.snapshot-store.{snapshotStoreId} {{
    class = ""Akka.Persistence.Snapshot.MemorySnapshotStore, Akka.Persistence""
    plugin-dispatcher = ""akka.actor.default-dispatcher""
}}";

            return builder.AddHocon(liveConfig, HoconAddMode.Prepend);
        }

        /// <summary>
        /// Adds the Akka.NET v1.4 to v1.5 Akka.Cluster.Sharding persistence event migration adapter to a journal.
        /// </summary>
        /// <param name="builder">The builder instance being configured.</param>
        /// <param name="journalOptions">The specific journal options instance used by Akka.Cluster.Sharding persistence. For example, an instance of <c>SqlServerJournalOptions</c></param>
        /// <returns>The same <see cref="AkkaConfigurationBuilder"/> instance originally passed in.</returns>
        public static AkkaConfigurationBuilder WithClusterShardingJournalMigrationAdapter(
            this AkkaConfigurationBuilder builder,
            JournalOptions journalOptions)
            => builder.WithClusterShardingJournalMigrationAdapter(journalOptions.PluginId);

        /// <summary>
        /// Adds the Akka.NET v1.4 to v1.5 Akka.Cluster.Sharding persistence event migration adapter to a journal.
        /// </summary>
        /// <param name="builder">The builder instance being configured.</param>
        /// <param name="journalId">The specific journal identifier used by Akka.Cluster.Sharding persistence. For example, "akka.persistence.journal.sql-server"</param>
        /// <returns>The same <see cref="AkkaConfigurationBuilder"/> instance originally passed in.</returns>
        public static AkkaConfigurationBuilder WithClusterShardingJournalMigrationAdapter(
            this AkkaConfigurationBuilder builder,
            string journalId)
        {
            var config = @$"{journalId} {{
     event-adapters {{
        coordinator-migration = ""Akka.Cluster.Sharding.OldCoordinatorStateMigrationEventAdapter, Akka.Cluster.Sharding""
    }}

    event-adapter-bindings {{
        ""Akka.Cluster.Sharding.ShardCoordinator+IDomainEvent, Akka.Cluster.Sharding"" = coordinator-migration
    }}
}}";
            return builder.AddHocon(config, HoconAddMode.Prepend);
        }
    }
}