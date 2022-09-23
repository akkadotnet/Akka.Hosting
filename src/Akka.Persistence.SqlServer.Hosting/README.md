# Akka.Persistence.SqlServer.Hosting

Akka.Hosting extension methods to add Akka.Persistence.SqlServer to an ActorSystem

# Akka.Persistence.SqlServer Extension Methods

## WithSqlServerPersistence() Method

```csharp
public static AkkaConfigurationBuilder WithSqlServerPersistence(
    this AkkaConfigurationBuilder builder,
    string connectionString,
    PersistenceMode mode = PersistenceMode.Both, 
    Action<AkkaPersistenceJournalBuilder>? configurator = null,
    bool autoInitialize = true);
```

```csharp
public static AkkaConfigurationBuilder WithSqlServerPersistence(
    this AkkaConfigurationBuilder builder,
    Action<SqlServerJournalOptions>? journalConfigurator = null,
    Action<SqlServerSnapshotOptions>? snapshotConfigurator = null,
    Action<AkkaPersistenceJournalBuilder>? configurator = null);
```

```csharp
public static AkkaConfigurationBuilder WithSqlServerPersistence(
    this AkkaConfigurationBuilder builder,
    SqlServerJournalOptions? journalOptions = null,
    SqlServerSnapshotOptions? snapshotOptions = null,
    Action<AkkaPersistenceJournalBuilder>? configurator = null);
```

### Parameters

* `connectionString` __string__

  Connection string used for database access.

* `mode` __PersistenceMode__

  Determines which settings should be added by this method call.

  * `PersistenceMode.Journal`: Only add the journal settings
  * `PersistenceMode.SnapshotStore`: Only add the snapshot store settings
  * `PersistenceMode.Both`: Add both journal and snapshot store settings

* `configurator` __Action\<AkkaPersistenceJournalBuilder\>__

  An Action delegate used to configure an `AkkaPersistenceJournalBuilder` instance. Used to configure [Event Adapters](https://getakka.net/articles/persistence/event-adapters.html)

* `journalConfigurator` __Action\<SqlServerJournalOptions\>__

  An Action delegate to configure a `SqlServerJournalOptions` instance.

* `snapshotConfigurator` __Action\<SqlServerSnapshotOptions\>__

  An Action delegate to configure a `SqlServerSnapshotOptions` instance.

* `journalOptions` __SqlServerJournalOptions__

  An `SqlServerJournalOptions` instance to configure the SqlServer journal.

* `snapshotOptions` __SqlServerSnapshotOptions__

  An `SqlServerSnapshotOptions` instance to configure the SqlServer snapshot store.

## Example

```csharp
using var host = new HostBuilder()
    .ConfigureServices((context, services) =>
    {
        services.AddAkka("ecsBootstrapDemo", (builder, provider) =>
        {
            builder
                .WithRemoting("localhost", 8110)
                .WithClustering()
                .WithSqlServerPersistence("your-sqlserver-connection-string");
        });
    }).Build();

await host.RunAsync();
```