using System;
using Akka.Actor;
using Akka.Configuration;
using Akka.Hosting;
using Akka.Persistence.Hosting;
using Akka.Persistence.Query.Sql;

#nullable enable
namespace Akka.Persistence.SqlServer.Hosting
{
    /// <summary>
    /// Extension methods for Akka.Persistence.SqlServer
    /// </summary>
    public static class AkkaPersistenceSqlServerHostingExtensions
    {
        /// <summary>
        ///     Adds Akka.Persistence.SqlServer support to this <see cref="ActorSystem"/>.
        /// </summary>
        /// <param name="builder">
        ///     The builder instance being configured.
        /// </param>
        /// <param name="connectionString">
        ///     Connection string used for database access.
        /// </param>
        /// <param name="autoInitialize">
        ///     Should the SQL store table be initialized automatically.
        /// </param>
        /// <param name="mode"></param>
        /// <param name="configurator">
        ///     An Action delegate used to configure an <see cref="AkkaPersistenceJournalBuilder"/> instance.
        /// </param>
        /// <returns>
        ///     The same <see cref="AkkaConfigurationBuilder"/> instance originally passed in.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static AkkaConfigurationBuilder WithSqlServerPersistence(
            this AkkaConfigurationBuilder builder,
            string connectionString,
            PersistenceMode mode = PersistenceMode.Both, 
            Action<AkkaPersistenceJournalBuilder>? configurator = null,
            bool autoInitialize = true)
        {
            Config journalConfiguration = @$"
            akka.persistence {{
                journal {{
                    plugin = ""akka.persistence.journal.sql-server""
                    sql-server {{
                        connection-string = ""{connectionString}""
                        auto-initialize = {autoInitialize.ToHocon()}
                    }}
                }}
                query.journal.sql.refresh-interval = 1s
            }}";

            Config snapshotStoreConfig = @$"
            akka.persistence {{
                snapshot-store {{
                    plugin = ""akka.persistence.snapshot-store.sql-server""
                    sql-server {{
                        connection-string = ""{connectionString}""
                        auto-initialize = {autoInitialize.ToHocon()}
                    }}
                }}
            }}";

            return builder.WithSqlServerPersistence(journalConfiguration, snapshotStoreConfig, mode, configurator);
        }

        /// <summary>
        ///     Adds Akka.Persistence.SqlServer support to this <see cref="ActorSystem"/>.
        /// </summary>
        /// <param name="builder">
        ///     The builder instance being configured.
        /// </param>
        /// <param name="journalConfigurator">
        ///     An Action delegate to configure a <see cref="SqlServerJournalOptions"/> instance.
        /// </param>
        /// <param name="snapshotConfigurator">
        ///     An Action delegate to configure a <see cref="SqlServerSnapshotOptions"/> instance.
        /// </param>
        /// <param name="configurator">
        ///     An Action delegate used to configure an <see cref="AkkaPersistenceJournalBuilder"/> instance.
        /// </param>
        /// <returns>
        ///     The same <see cref="AkkaConfigurationBuilder"/> instance originally passed in.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when both <paramref name="journalConfigurator"/> and <paramref name="snapshotConfigurator"/> are null.
        /// </exception>
        public static AkkaConfigurationBuilder WithSqlServerPersistence(
            this AkkaConfigurationBuilder builder,
            Action<SqlServerJournalOptions>? journalConfigurator = null,
            Action<SqlServerSnapshotOptions>? snapshotConfigurator = null,
            Action<AkkaPersistenceJournalBuilder>? configurator = null)
        {
            var journalOptions = new SqlServerJournalOptions();
            journalConfigurator?.Invoke(journalOptions);

            var snapshotOptions = new SqlServerSnapshotOptions();
            snapshotConfigurator?.Invoke(snapshotOptions);

            return (journalConfigurator, snapshotConfigurator) switch
            {
                (null, null) => throw new ArgumentException($"{nameof(journalConfigurator)} and {nameof(snapshotConfigurator)} could not both be null"),
                (null, _) => builder.WithSqlServerPersistence(Config.Empty, snapshotOptions.ToConfig(), PersistenceMode.SnapshotStore, configurator),
                (_, null) => builder.WithSqlServerPersistence(journalOptions.ToConfig(), Config.Empty, PersistenceMode.Journal, configurator),
                (_, _) => builder.WithSqlServerPersistence(journalOptions.ToConfig(), snapshotOptions.ToConfig(), PersistenceMode.Both, configurator),
            };
        }

        /// <summary>
        ///     Adds Akka.Persistence.SqlServer support to this <see cref="ActorSystem"/>.
        /// </summary>
        /// <param name="builder">
        ///     The builder instance being configured.
        /// </param>
        /// <param name="journalOptions">
        ///     An <see cref="SqlServerJournalOptions"/> instance to configure the SqlServer journal.
        /// </param>
        /// <param name="snapshotOptions">
        ///     An <see cref="SqlServerSnapshotOptions"/> instance to configure the SqlServer snapshot store.
        /// </param>
        /// <param name="configurator">
        ///     An Action delegate used to configure a <see cref="AkkaPersistenceJournalBuilder"/> instance.
        /// </param>
        /// <returns>
        ///     The same <see cref="AkkaConfigurationBuilder"/> instance originally passed in.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when both <paramref name="journalOptions"/> and <paramref name="snapshotOptions"/> are null.
        /// </exception>
        public static AkkaConfigurationBuilder WithSqlServerPersistence(
            this AkkaConfigurationBuilder builder,
            SqlServerJournalOptions? journalOptions = null,
            SqlServerSnapshotOptions? snapshotOptions = null,
            Action<AkkaPersistenceJournalBuilder>? configurator = null)
        {
            var mode = (journalOptions, snapshotOptions) switch
            {
                (null, null) => throw new ArgumentException($"{nameof(journalOptions)} and {nameof(snapshotOptions)} could not both be null"),
                (null, _) => PersistenceMode.SnapshotStore,
                (_, null) => PersistenceMode.Journal,
                (_, _) => PersistenceMode.Both
            };
            
            return builder.WithSqlServerPersistence(
                journalConfiguration: journalOptions?.ToConfig() ?? Config.Empty,
                snapshotStoreConfig: snapshotOptions?.ToConfig() ?? Config.Empty,
                mode: mode, 
                configurator: configurator);
        }
        
        private static AkkaConfigurationBuilder WithSqlServerPersistence(
            this AkkaConfigurationBuilder builder,
            Config journalConfiguration,
            Config snapshotStoreConfig,
            PersistenceMode mode = PersistenceMode.Both, 
            Action<AkkaPersistenceJournalBuilder>? configurator = null)
        {
            switch (mode)
            {
                case PersistenceMode.Both:
                    builder.AddHocon(journalConfiguration, HoconAddMode.Prepend);
                    builder.AddHocon(snapshotStoreConfig, HoconAddMode.Prepend);
                    builder.AddHocon(SqlReadJournal.DefaultConfiguration(), HoconAddMode.Append);
                    break;
                
                case PersistenceMode.Journal:
                    builder.AddHocon(journalConfiguration, HoconAddMode.Prepend);
                    builder.AddHocon(SqlReadJournal.DefaultConfiguration(), HoconAddMode.Append);
                    break;
                
                case PersistenceMode.SnapshotStore:
                    builder.AddHocon(snapshotStoreConfig, HoconAddMode.Prepend);
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, "Invalid SqlPersistenceMode defined.");
            }
            
            if (configurator != null) // configure event adapters
            {
                builder.WithJournal("sql-server", configurator);
            }

            return builder.AddHocon(SqlServerPersistence.DefaultConfiguration(), HoconAddMode.Append);
        }
    }
}