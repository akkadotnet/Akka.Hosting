using System;
using Akka.Actor;
using Akka.Configuration;
using Akka.Hosting;
using Akka.Persistence.Hosting;
using Akka.Persistence.Query.Sql;

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
        ///     Determines which settings should be added by this method call.
        /// </param>
        /// <param name="schemaName">
        ///     The schema name for the journal and snapshot store table.
        /// </param>
        /// <param name="autoInitialize">
        ///     Should the SQL store table be initialized automatically.
        /// </param>
        /// <param name="storedAsType">
        ///     Determines how data are being de/serialized into the table.
        /// </param>
        /// <param name="sequentialAccess">
        ///     Uses the `CommandBehavior.SequentialAccess` when creating SQL commands, providing a performance
        ///     improvement for reading large BLOBS.
        /// </param>
        /// <param name="useBigintIdentityForOrderingColumn">
        ///     When set to true, persistence will use `BIGINT` and `GENERATED ALWAYS AS IDENTITY` for journal table
        ///     schema creation.
        /// </param>
        /// <param name="configurator">
        ///     An Action delegate used to configure an <see cref="AkkaPersistenceJournalBuilder"/> instance.
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
            bool useBigintIdentityForOrderingColumn = false, Action<AkkaPersistenceJournalBuilder> configurator = null)
        {
            var storedAs = storedAsType switch
            {
                StoredAsType.ByteA => "bytea",
                StoredAsType.Json => "json",
                StoredAsType.JsonB => "jsonb",
                _ => throw new ArgumentOutOfRangeException(nameof(storedAsType), storedAsType, "Invalid StoredAsType defined.")
            };

            Config journalConfiguration = @$"
            akka.persistence {{
                journal {{
                    plugin = ""akka.persistence.journal.postgresql""
                    postgresql {{
                        class = ""Akka.Persistence.PostgreSql.Journal.PostgreSqlJournal, Akka.Persistence.PostgreSql""
                        plugin-dispatcher = ""akka.actor.default-dispatcher""
                        connection-string = ""{connectionString}""
                        connection-timeout = 30s
                        schema-name = {schemaName}
                        table-name = event_journal
                        auto-initialize = {(autoInitialize ? "on" : "off")}
                        timestamp-provider = ""Akka.Persistence.Sql.Common.Journal.DefaultTimestampProvider, Akka.Persistence.Sql.Common""
                        metadata-table-name = metadata
                        stored-as = {storedAs}
                        sequential-access = {(sequentialAccess ? "on" : "off")}
                        use-bigint-identity-for-ordering-column = {(useBigintIdentityForOrderingColumn ? "on" : "off")}
                    }}
                }}
            }}";

            Config snapshotStoreConfig = @$"
            akka.persistence {{
                snapshot-store {{
                    plugin = ""akka.persistence.snapshot-store.postgresql""
                    postgresql {{
                        class = ""Akka.Persistence.PostgreSql.Snapshot.PostgreSqlSnapshotStore, Akka.Persistence.PostgreSql""
                        plugin-dispatcher = ""akka.actor.default-dispatcher""
                        connection-string = ""{connectionString}""
                        connection-timeout = 30s
                        schema-name = {schemaName}
                        table-name = snapshot_store
                        auto-initialize = {(autoInitialize ? "on" : "off")}
                        stored-as = {storedAs}
                        sequential-access = {(sequentialAccess ? "on" : "off")}
                    }}
                }}
            }}";

            var finalConfig = mode switch
            {
                PersistenceMode.Both => journalConfiguration
                    .WithFallback(snapshotStoreConfig)
                    .WithFallback(SqlReadJournal.DefaultConfiguration()),

                PersistenceMode.Journal => journalConfiguration
                    .WithFallback(SqlReadJournal.DefaultConfiguration()),

                PersistenceMode.SnapshotStore => snapshotStoreConfig,

                _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Invalid PersistenceMode defined.")
            };
            
            if (configurator != null) // configure event adapters
            {
                builder.WithJournal("postgresql", configurator);
            }

            return builder.AddHocon(finalConfig.WithFallback(PostgreSqlPersistence.DefaultConfiguration()), HoconAddMode.Append);
        }
    }
}
