using System;
using Akka.Actor;
using Akka.Hosting;
using Akka.Persistence.Hosting;

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
        /// <param name="journalBuilder">
        ///     An Action delegate used to configure an <see cref="AkkaPersistenceJournalBuilder"/> instance.
        /// </param>
        /// <param name="pluginIdentifier">The configuration identifier for the plugins</param>
        /// <param name="isDefaultPlugin">
        ///     <para>
        ///         A <c>bool</c> flag to set the plugin as the default persistence plugin for the <see cref="ActorSystem"/>
        ///     </para>
        ///     <b>Default</b>: <c>true</c>
        /// </param>
        /// <returns>
        ///     The same <see cref="AkkaConfigurationBuilder"/> instance originally passed in.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static AkkaConfigurationBuilder WithSqlServerPersistence(
            this AkkaConfigurationBuilder builder,
            string connectionString,
            PersistenceMode mode = PersistenceMode.Both, 
            Action<AkkaPersistenceJournalBuilder>? journalBuilder = null,
            bool autoInitialize = true,
            string pluginIdentifier = "sql-server",
            bool isDefaultPlugin = true)
        {
            if (mode == PersistenceMode.SnapshotStore && journalBuilder is { })
                throw new Exception($"{nameof(journalBuilder)} can only be set when {nameof(mode)} is set to either {PersistenceMode.Both} or {PersistenceMode.Journal}");
            
            var journalOpt = new SqlServerJournalOptions(isDefaultPlugin, pluginIdentifier)
            {
                ConnectionString = connectionString,
                AutoInitialize = autoInitialize,
                QueryRefreshInterval = TimeSpan.FromSeconds(1),
            };

            var adapters = new AkkaPersistenceJournalBuilder(journalOpt.Identifier, builder);
            journalBuilder?.Invoke(adapters);
            journalOpt.Adapters = adapters;

            var snapshotOpt = new SqlServerSnapshotOptions(isDefaultPlugin, pluginIdentifier)
            {
                ConnectionString = connectionString,
                AutoInitialize = autoInitialize,
            };

            return mode switch
            {
                PersistenceMode.Journal => builder.WithSqlServerPersistence(journalOpt, null),
                PersistenceMode.SnapshotStore => builder.WithSqlServerPersistence(null, snapshotOpt),
                PersistenceMode.Both => builder.WithSqlServerPersistence(journalOpt, snapshotOpt),
                _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Invalid PersistenceMode defined.")
            };
        }

        /// <summary>
        ///     Adds Akka.Persistence.SqlServer support to this <see cref="ActorSystem"/>.
        /// </summary>
        /// <param name="builder">
        ///     The builder instance being configured.
        /// </param>
        /// <param name="journalOptionConfigurator">
        ///     An Action delegate to configure a <see cref="SqlServerJournalOptions"/> instance.
        /// </param>
        /// <param name="snapshotOptionConfigurator">
        ///     An Action delegate to configure a <see cref="SqlServerSnapshotOptions"/> instance.
        /// </param>
        /// <param name="isDefaultPlugin">
        ///     <para>
        ///         A <c>bool</c> flag to set the plugin as the default persistence plugin for the <see cref="ActorSystem"/>
        ///     </para>
        ///     <b>Default</b>: <c>true</c>
        /// </param>
        /// <returns>
        ///     The same <see cref="AkkaConfigurationBuilder"/> instance originally passed in.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when both <paramref name="journalOptionConfigurator"/> and <paramref name="snapshotOptionConfigurator"/> are null.
        /// </exception>
        public static AkkaConfigurationBuilder WithSqlServerPersistence(
            this AkkaConfigurationBuilder builder,
            Action<SqlServerJournalOptions>? journalOptionConfigurator = null,
            Action<SqlServerSnapshotOptions>? snapshotOptionConfigurator = null,
            bool isDefaultPlugin = true)
        {
            if (journalOptionConfigurator is null && snapshotOptionConfigurator is null)
                throw new ArgumentException($"{nameof(journalOptionConfigurator)} and {nameof(snapshotOptionConfigurator)} could not both be null");
            
            SqlServerJournalOptions? journalOptions = null;
            if (journalOptionConfigurator is { })
            {
                journalOptions = new SqlServerJournalOptions(isDefaultPlugin);
                journalOptionConfigurator(journalOptions);
            }

            SqlServerSnapshotOptions? snapshotOptions = null;
            if (snapshotOptionConfigurator is { })
            {
                snapshotOptions = new SqlServerSnapshotOptions(isDefaultPlugin);
                snapshotOptionConfigurator(snapshotOptions);
            }

            return builder.WithSqlServerPersistence(journalOptions, snapshotOptions);
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
        /// <returns>
        ///     The same <see cref="AkkaConfigurationBuilder"/> instance originally passed in.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when both <paramref name="journalOptions"/> and <paramref name="snapshotOptions"/> are null.
        /// </exception>
        public static AkkaConfigurationBuilder WithSqlServerPersistence(
            this AkkaConfigurationBuilder builder,
            SqlServerJournalOptions? journalOptions = null,
            SqlServerSnapshotOptions? snapshotOptions = null)
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