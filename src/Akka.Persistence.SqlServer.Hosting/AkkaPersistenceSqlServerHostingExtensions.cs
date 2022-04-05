using System;
using Akka.Configuration;
using Akka.Hosting;
using Akka.Persistence.Query.Sql;

namespace Akka.Persistence.SqlServer.Hosting
{
    public enum SqlPersistenceMode
    {
        /// <summary>
        /// Sets both the akka.persistence.journal and the akka.persistence.snapshot-store to use
        /// Akka.Persistence.SqlServer.
        /// </summary>
        Both,

        /// <summary>
        /// Sets ONLY the akka.persistence.journal to use Akka.Persistence.SqlServer.
        /// </summary>
        Journal,

        /// <summary>
        /// Sets ONLY the akka.persistence.snapshot-store to use Akka.Persistence.SqlServer.
        /// </summary>
        SnapshotStore,
    }

    /// <summary>
    /// Extension methods for Akka.Persistence.SqlServer
    /// </summary>
    public static class AkkaPersistenceSqlServerHostingExtensions
    {
        public static AkkaConfigurationBuilder WithSqlServerPersistence(this AkkaConfigurationBuilder builder,
            string connectionString, SqlPersistenceMode mode = SqlPersistenceMode.Both)
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

            var finalConfig = journalConfiguration;

            switch (mode)
            {
                case SqlPersistenceMode.Both:
                    finalConfig = finalConfig.WithFallback(snapshotStoreConfig)
                        .WithFallback(SqlReadJournal.DefaultConfiguration());
                    break;
                case SqlPersistenceMode.Journal:
                    finalConfig = finalConfig.WithFallback(SqlReadJournal.DefaultConfiguration());
                    break;
                case SqlPersistenceMode.SnapshotStore:
                    finalConfig = snapshotStoreConfig;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }

            return builder.AddHocon(finalConfig.WithFallback(SqlServerPersistence.DefaultConfiguration()));
        }
    }
}
