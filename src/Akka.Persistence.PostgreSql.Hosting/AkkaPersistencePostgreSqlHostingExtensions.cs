using System;
using Akka.Configuration;
using Akka.Hosting;
using Akka.Persistence.Query.Sql;

namespace Akka.Persistence.PostgreSql.Hosting
{
    public enum SqlPersistenceMode
    {
        /// <summary>
        /// Sets both the akka.persistence.journal and the akka.persistence.snapshot-store to use
        /// Akka.Persistence.PostgreSql.
        /// </summary>
        Both,

        /// <summary>
        /// Sets ONLY the akka.persistence.journal to use Akka.Persistence.PostgreSql.
        /// </summary>
        Journal,

        /// <summary>
        /// Sets ONLY the akka.persistence.snapshot-store to use Akka.Persistence.PostgreSql.
        /// </summary>
        SnapshotStore,
    }

    /// <summary>
    /// Extension methods for Akka.Persistence.PostgreSql
    /// </summary>
    public static class AkkaPersistencePostgreSqlHostingExtensions
    {
        public static AkkaConfigurationBuilder WithPostgreSqlPersistence(
            this AkkaConfigurationBuilder builder,
            string connectionString,
            SqlPersistenceMode mode = SqlPersistenceMode.Both,
            string schemaName = "public",
            bool autoInitialize = false,
            StoredAsType storedAsType = StoredAsType.ByteA,
            bool sequentialAccess = false,
            bool useBigintIdentityForOrderingColumn = false)
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
                SqlPersistenceMode.Both => journalConfiguration
                    .WithFallback(snapshotStoreConfig)
                    .WithFallback(SqlReadJournal.DefaultConfiguration()),

                SqlPersistenceMode.Journal => journalConfiguration
                    .WithFallback(SqlReadJournal.DefaultConfiguration()),

                SqlPersistenceMode.SnapshotStore => snapshotStoreConfig,

                _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Invalid SqlPersistenceMode defined.")
            };

            return builder.AddHocon(finalConfig.WithFallback(PostgreSqlPersistence.DefaultConfiguration()));
        }
    }
}
