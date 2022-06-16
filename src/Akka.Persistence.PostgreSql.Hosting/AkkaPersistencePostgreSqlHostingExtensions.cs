using System;
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

            return builder.AddHocon(finalConfig.WithFallback(PostgreSqlPersistence.DefaultConfiguration()));
        }
    }
}
