# Akka.Persistence.PostgreSql.Hosting

Akka.Hosting extension methods to add Akka.Persistence.PostgreSql to an ActorSystem

# Akka.Persistence.PostgreSql Extension Methods

## WithPostgreSqlPersistence() Method

```csharp
public static AkkaConfigurationBuilder WithPostgreSqlPersistence(
    this AkkaConfigurationBuilder builder,
    string connectionString,
    PersistenceMode mode = PersistenceMode.Both,
    string schemaName = "public",
    bool autoInitialize = false,
    StoredAsType storedAsType = StoredAsType.ByteA,
    bool sequentialAccess = false,
    bool useBigintIdentityForOrderingColumn = false, 
    Action<AkkaPersistenceJournalBuilder> configurator = null);
```

### Parameters

* `connectionString` __string__

  Connection string used for database access.

* `mode` __PersistenceMode__

  Determines which settings should be added by this method call. __Default__: `PersistenceMode.Both`

    * `PersistenceMode.Journal`: Only add the journal settings
    * `PersistenceMode.SnapshotStore`: Only add the snapshot store settings
    * `PersistenceMode.Both`: Add both journal and snapshot store settings

* `schemaName` __string__

  The schema name for the journal and snapshot store table. __Default__: `"public"`

* `autoInitialize` __bool__

  Should the SQL store table be initialized automatically. __Default__: `false`

* `storedAsType` __StoredAsType__

  Determines how data are being de/serialized into the table. __Default__: `StoredAsType.ByteA`

  * `StoredAsType.ByteA`: Byte array
  * `StoredAsType.Json`: JSON
  * `StoredAsType.JsonB`: Binary JSON

* `sequentialAccess` __bool__

  Uses the `CommandBehavior.SequentialAccess` when creating SQL commands, providing a performance improvement for reading large BLOBS. __Default__: `false`

* `useBigintIdentityForOrderingColumn` __bool__

  When set to true, persistence will use `BIGINT` and `GENERATED ALWAYS AS IDENTITY` for journal table schema creation. __Default__: false

  > __NOTE__
  > 
  > This only affects newly created tables, as such, it should not affect any existing database.
 
  > __WARNING__
  > 
  > To use this feature, you have to have PorsgreSql version 10 or above

* `configurator` __Action\<AkkaPersistenceJournalBuilder\>__

  An Action delegate used to configure an `AkkaPersistenceJournalBuilder` instance. Used to configure [Event Adapters](https://getakka.net/articles/persistence/event-adapters.html)

