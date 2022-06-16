using System;
using Akka.Actor;
using Akka.Configuration;
using Akka.Hosting;
using Akka.Persistence.Hosting;
using Akka.Persistence.Query.Sql;

namespace Akka.Persistence.SqlServer.Hosting
{
    /// <summary>
    /// Extension methods for Akka.Persistence.SqlServer
    /// </summary>
    public static class AkkaPersistenceSqlServerHostingExtensions
    {
        /// <summary>
        /// Adds Akka.Persistence.SqlServer to this <see cref="ActorSystem"/>.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="connectionString"></param>
        /// <param name="mode"></param>
        /// <param name="configurator"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static AkkaConfigurationBuilder WithSqlServerPersistence(
            this AkkaConfigurationBuilder builder,
            string connectionString,
            PersistenceMode mode = PersistenceMode.Both, Action<AkkaPersistenceJournalBuilder> configurator = null)
        {
            Config journalConfiguration = @$"
            akka.persistence {{
                journal {{
                    plugin = ""akka.persistence.journal.sql-server""
                    sql-server {{
                        class = ""Akka.Persistence.SqlServer.Journal.SqlServerJournal, Akka.Persistence.SqlServer""
                        connection-string = ""{connectionString}""
                        table-name = EventJournal
                        schema-name = dbo
                        auto-initialize = on
                        refresh-interval = 1s
                    }}
                }}
            }}";

            Config snapshotStoreConfig = @$"
            akka.persistence {{
                snapshot-store {{
                    plugin = ""akka.persistence.snapshot-store.sql-server""
                    sql-server {{
                        class = ""Akka.Persistence.SqlServer.Snapshot.SqlServerSnapshotStore, Akka.Persistence.SqlServer""
                        schema-name = dbo
                        table-name = SnapshotStore
                        auto-initialize = on
                        connection-string = ""{connectionString}""
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

                _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Invalid SqlPersistenceMode defined.")
            };

            if (configurator != null) // configure event adapters
            {
                builder.WithJournal("sql-server", configurator);
            }

            return builder.AddHocon(finalConfig.WithFallback(SqlServerPersistence.DefaultConfiguration()));
        }
    }
}