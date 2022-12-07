using System;
using Akka.Actor;
using Akka.Hosting;
using Akka.Persistence.Hosting;

#nullable enable
namespace Akka.Persistence.PostgreSql.Hosting
{
    /// <summary>
    /// Extension methods for Akka.Persistence.PostgreSql
    /// </summary>
    public static class AkkaPersistencePostgreSqlHostingExtensions
    {
        /// <summary>
        ///     Add Akka.Persistence.PostgreSql support to the <see cref="ActorSystem"/>
        /// </summary>
        /// <param name="builder">
        ///     The builder instance being configured.
        /// </param>
        /// <param name="connectionString">
        ///     Connection string used for database access.
        /// </param>
        /// <param name="mode">
        ///     <para>
        ///         Determines which settings should be added by this method call.
        ///     </para>
        ///     <i>Default</i>: <see cref="PersistenceMode.Both"/>
        /// </param>
        /// <param name="schemaName">
        ///     <para>
        ///         The schema name for the journal and snapshot store table.
        ///     </para>
        ///     <i>Default</i>: <c>"public"</c>
        /// </param>
        /// <param name="autoInitialize">
        ///     <para>
        ///         Should the SQL store table be initialized automatically.
        ///     </para>
        ///     <i>Default</i>: <c>false</c>
        /// </param>
        /// <param name="storedAsType">
        ///     <para>
        ///         Determines how data are being de/serialized into the table.
        ///     </para>
        ///     <i>Default</i>: <see cref="StoredAsType.ByteA"/>
        /// </param>
        /// <param name="sequentialAccess">
        ///     <para>
        ///         Uses the `CommandBehavior.SequentialAccess` when creating SQL commands, providing a performance
        ///         improvement for reading large BLOBS.
        ///     </para>
        ///     <i>Default</i>: <c>false</c>
        /// </param>
        /// <param name="useBigintIdentityForOrderingColumn">
        ///     <para>
        ///         When set to true, persistence will use `BIGINT` and `GENERATED ALWAYS AS IDENTITY` for journal table
        ///         schema creation.
        ///     </para>
        ///     <i>Default</i>: <c>false</c>
        /// </param>
        /// <param name="journalBuilder">
        ///     <para>
        ///         An <see cref="Action{T}"/> used to configure an <see cref="AkkaPersistenceJournalBuilder"/> instance.
        ///     </para>
        ///     <i>Default</i>: <c>null</c>
        /// </param>
        /// <param name="pluginIdentifier">
        ///     <para>
        ///         The configuration identifier for the plugins
        ///     </para>
        ///     <i>Default</i>: <c>"postgresql"</c>
        /// </param>
        /// <param name="isDefaultPlugin">
        ///     <para>
        ///         A <c>bool</c> flag to set the plugin as the default persistence plugin for the <see cref="ActorSystem"/>
        ///     </para>
        ///     <i>Default</i>: <c>true</c>
        /// </param>
        /// <returns>
        ///     The same <see cref="AkkaConfigurationBuilder"/> instance originally passed in.
        /// </returns>
        public static AkkaConfigurationBuilder WithPostgreSqlPersistence(
            this AkkaConfigurationBuilder builder,
            string connectionString,
            PersistenceMode mode = PersistenceMode.Both,
            string schemaName = "public",
            bool autoInitialize = false,
            StoredAsType storedAsType = StoredAsType.ByteA,
            bool sequentialAccess = false,
            bool useBigintIdentityForOrderingColumn = false, 
            Action<AkkaPersistenceJournalBuilder>? journalBuilder = null,
            string pluginIdentifier = "postgresql",
            bool isDefaultPlugin = true)
        {
            if (mode == PersistenceMode.SnapshotStore && journalBuilder is { })
                throw new Exception($"{nameof(journalBuilder)} can only be set when {nameof(mode)} is set to either {PersistenceMode.Both} or {PersistenceMode.Journal}");
            
            var journalOpt = new PostgreSqlJournalOptions(isDefaultPlugin, pluginIdentifier)
            {
                ConnectionString = connectionString,
                SchemaName = schemaName,
                AutoInitialize = autoInitialize,
                StoredAs = storedAsType,
                SequentialAccess = sequentialAccess,
                UseBigIntIdentityForOrderingColumn = useBigintIdentityForOrderingColumn
            };

            var adapters = new AkkaPersistenceJournalBuilder(journalOpt.Identifier, builder);
            journalBuilder?.Invoke(adapters);
            journalOpt.Adapters = adapters;

            var snapshotOpt = new PostgreSqlSnapshotOptions(isDefaultPlugin, pluginIdentifier)
            {
                ConnectionString = connectionString,
                SchemaName = schemaName,
                AutoInitialize = autoInitialize,
                StoredAs = storedAsType,
                SequentialAccess = sequentialAccess
            };

            return mode switch
            {
                PersistenceMode.Journal => builder.WithPostgreSqlPersistence(journalOpt, null),
                PersistenceMode.SnapshotStore => builder.WithPostgreSqlPersistence(null, snapshotOpt),
                PersistenceMode.Both => builder.WithPostgreSqlPersistence(journalOpt, snapshotOpt),
                _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Invalid PersistenceMode defined.")
            };
        }

        /// <summary>
        ///     Add Akka.Persistence.PostgreSql support to the <see cref="ActorSystem"/>
        /// </summary>
        /// <param name="builder">
        ///     The builder instance being configured.
        /// </param>
        /// <param name="snapshotOptionConfigurator">
        ///     <para>
        ///         An <see cref="Action{T}"/> that modifies an instance of <see cref="PostgreSqlSnapshotOptions"/>,
        ///         used to configure the snapshot store plugin
        ///     </para>
        ///     <i>Default</i>: <c>null</c>
        /// </param>
        /// <param name="journalOptionConfigurator">
        ///     <para>
        ///         An <see cref="Action{T}"/> that modifies an instance of <see cref="PostgreSqlJournalOptions"/>,
        ///         used to configure the journal plugin
        ///     </para>
        ///     <i>Default</i>: <c>null</c>
        /// </param>
        /// <param name="isDefaultPlugin">
        ///     <para>
        ///         A <c>bool</c> flag to set the plugin as the default persistence plugin for the <see cref="ActorSystem"/>
        ///     </para>
        ///     <i>Default</i>: <c>true</c>
        /// </param>
        /// <returns>
        ///     The same <see cref="AkkaConfigurationBuilder"/> instance originally passed in.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when both <param name="journalOptionConfigurator"/> and
        ///     <param name="snapshotOptionConfigurator"/> are null.
        /// </exception>
        public static AkkaConfigurationBuilder WithPostgreSqlPersistence(
            this AkkaConfigurationBuilder builder,
            Action<PostgreSqlJournalOptions>? journalOptionConfigurator = null,
            Action<PostgreSqlSnapshotOptions>? snapshotOptionConfigurator = null,
            bool isDefaultPlugin = true)
        {
            if (journalOptionConfigurator is null && snapshotOptionConfigurator is null)
                throw new ArgumentException($"{nameof(journalOptionConfigurator)} and {nameof(snapshotOptionConfigurator)} could not both be null");
            
            PostgreSqlJournalOptions? journalOptions = null;
            if(journalOptionConfigurator is { })
            {
                journalOptions = new PostgreSqlJournalOptions(isDefaultPlugin);
                journalOptionConfigurator(journalOptions);
            }

            PostgreSqlSnapshotOptions? snapshotOptions = null;
            if (snapshotOptionConfigurator is { })
            {
                snapshotOptions = new PostgreSqlSnapshotOptions(isDefaultPlugin);
                snapshotOptionConfigurator(snapshotOptions);
            }

            return builder.WithPostgreSqlPersistence(journalOptions, snapshotOptions);
        }

        /// <summary>
        ///     Add Akka.Persistence.PostgreSql support to the <see cref="ActorSystem"/>
        /// </summary>
        /// <param name="builder">
        ///     The builder instance being configured.
        /// </param>
        /// <param name="snapshotOptions">
        ///     <para>
        ///         An instance of <see cref="PostgreSqlSnapshotOptions"/>, used to configure the snapshot store plugin
        ///     </para>
        ///     <i>Default</i>: <c>null</c>
        /// </param>
        /// <param name="journalOptions">
        ///     <para>
        ///         An instance of <see cref="PostgreSqlJournalOptions"/>, used to configure the journal plugin
        ///     </para>
        ///     <i>Default</i>: <c>null</c>
        /// </param>
        /// <returns>
        ///     The same <see cref="AkkaConfigurationBuilder"/> instance originally passed in.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when both <param name="journalOptions"/> and <param name="snapshotOptions"/> are null.
        /// </exception>
        public static AkkaConfigurationBuilder WithPostgreSqlPersistence(
            this AkkaConfigurationBuilder builder,
            PostgreSqlJournalOptions? journalOptions = null,
            PostgreSqlSnapshotOptions? snapshotOptions = null)
        {
            if (journalOptions is null && snapshotOptions is null)
                throw new ArgumentException($"{nameof(journalOptions)} and {nameof(snapshotOptions)} could not both be null");
            
            return (journalOptions, snapshotOptions) switch
            {
                (null, null) => 
                    throw new ArgumentException($"{nameof(journalOptions)} and {nameof(snapshotOptions)} could not both be null"),
                
                (_, null) => 
                    builder
                        .AddHocon(journalOptions.ToConfig(), HoconAddMode.Prepend)
                        .AddHocon(journalOptions.DefaultConfig, HoconAddMode.Append),
                
                (null, _) => 
                    builder
                        .AddHocon(snapshotOptions.ToConfig(), HoconAddMode.Prepend)
                        .AddHocon(snapshotOptions.DefaultConfig, HoconAddMode.Append),
                
                (_, _) => 
                    builder
                        .AddHocon(journalOptions.ToConfig(), HoconAddMode.Prepend)
                        .AddHocon(snapshotOptions.ToConfig(), HoconAddMode.Prepend)
                        .AddHocon(journalOptions.DefaultConfig, HoconAddMode.Append)
                        .AddHocon(snapshotOptions.DefaultConfig, HoconAddMode.Append),
            };
        }
        
    }
}
