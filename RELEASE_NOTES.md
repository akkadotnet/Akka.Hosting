## [1.5.0-beta3] / 27 February 2023

Version 1.5.0-beta3 integrates Akka.NET v1.5 into Akka.Hosting

* [Update Akka.NET to 1.5.0-beta3](https://github.com/akkadotnet/akka.net/releases/tag/1.5.0-beta3)

#### Upgrading From v1.4 To v1.5
As noted in [the upgrade advisories](https://github.com/akkadotnet/akka.net/blob/c9ccc25207b5a4cfa963a5a23f96c0676fbbef10/docs/community/whats-new/akkadotnet-v1.5-upgrade-advisories.md), there was a major change in Akka.Cluster.Sharding state storage. These notes contains the same documentation, but tailored for `Akka.Hosting` users

The recommended settings for maximum ease-of-use for Akka.Cluster.Sharding in new applications going forward will be:

```csharp
var options = new ShardOptions
{
    StateStoreMode = StateStoreMode.DData,
    RememberEntitiesStore = RememberEntitiesStore.Eventsourced
};
```

You will need to set these options manually because the default settings are set for backward compatibility.

#### Migrating to New Sharding Storage From Akka.Persistence

> **NOTE**
> 
> This section applies only to users who were using `StateStoreMode = StateStoreMode.Persistence`.

Switching over to using `RememberEntitiesStore = RememberEntitiesStore.Eventsourced` will cause an initial migration of data from the `ShardCoordinator`'s journal into separate event journals going forward.

Upgrading to Akka.NET v1.5 will **cause an irreversible migration of Akka.Cluster.Sharding data** for users who were previously running `StateStoreMode = StateStoreMode.Persistence`, so follow the steps below carefully:

##### Step 1 - Upgrade to Akka.NET v1.5 With New Options Setup

Update your Akka.Cluster.Sharding options to look like the following (adjust as necessary for your custom settings):

```csharp
hostBuilder.Services.AddAkka("MyActorSystem", builder =>
{
    var shardOptions = new ShardOptions
    {
        // If you don't run Akka.Cluster.Sharding with `RememberEntities = true`,
        // then set this to false
        RememberEntities = true,
        RememberEntitiesStore = RememberEntitiesStore.Eventsourced,
        StateStoreMode = StateStoreMode.Persistence,
    
        //fail if upgrade doesn't succeed
        FailOnInvalidEntityStateTransition = true
    };

    // Modify these two options to suit your application, SqlServer used
    // only as an illustration
    var journalOptions = new SqlServerJournalOptions();
    var snapshotOptions = new SqlServerSnapshotOptions();

    builder
        .WithClustering()
        .WithSqlServerPersistence(journalOptions, snapshotOptions)
        .WithShardRegion<UserActionsEntity>(
            "userActions", 
            s => UserActionsEntity.Props(s),
            new UserMessageExtractor(),
            // shardOptions is being used here
            shardOptions);
    
    // Add the Akka.Cluster.Sharding migration journal event adapter
    builder.WithClusterShardingJournalMigrationAdapter(journalOptions);
    
    // you can also declare the adapter by referencing the journal ID directly
    builder.WithClusterShardingJournalMigrationAdapter("akka.persistence.journal.sql-server");
})
```

With these HOCON settings in-place the following will happen:

1. The old `PersitentShardCoordinator` state will be broken up - Remember entities data will be distributed to each of the `PersistentShard` actors, who will now use the new `RememberEntitiesStore = RememberEntitiesStore.Eventsourced` setting going forward;
2. Old `Akka.Cluster.Sharding.ShardCoordinator.IDomainEvent` events will be upgraded to a new storage format via the injected Akka.Persistence event adapter; and
3. The `PersistentShardCoordinator` will migrate its journal to the new format as well.

##### Step 2 - Migrating Away From Persistence to DData

Once your cluster has successfully booted up with these settings, you can now optionally move to using distributed data to store sharding state by changing `StateStoreMode = StateStoreMode.DData` and deploying a second time:

```csharp
hostBuilder.Services.AddAkka("MyActorSystem", builder =>
{
    var shardOptions = new ShardOptions
    {
        RememberEntities = true,
        RememberEntitiesStore = RememberEntitiesStore.Eventsourced,
        // Change this line of code
        StateStoreMode = StateStoreMode.DData,
    
        FailOnInvalidEntityStateTransition = true
    };

    var journalOptions = new SqlServerJournalOptions();
    var snapshotOptions = new SqlServerSnapshotOptions();

    builder
        .WithClustering()
        .WithSqlServerPersistence(journalOptions, snapshotOptions)
        .WithShardRegion<UserActionsEntity>(
            "userActions", 
            s => UserActionsEntity.Props(s),
            new UserMessageExtractor(),
            shardOptions);
    
    builder.WithClusterShardingJournalMigrationAdapter(journalOptions);
})
```

Now you'll be running Akka.Cluster.Sharding with the recommended settings.

#### Migrating to New Sharding Storage From Akka.DistributedData

The migration process onto Akka.NET v1.5's new Cluster.Sharding storage system is less involved for users who were already using `StateStoreMode = StateStoreMode.DData`. All these users need to do is change the `RememberEntitiesStore` option to `RememberEntitiesStore.Eventsourced`

```csharp
hostBuilder.Services.AddAkka("MyActorSystem", builder =>
{
    var shardOptions = new ShardOptions
    {
        RememberEntities = true,
        // Use this option setting
        RememberEntitiesStore = RememberEntitiesStore.Eventsourced,
        StateStoreMode = StateStoreMode.DData,
    
        FailOnInvalidEntityStateTransition = true
    };

    var journalOptions = new SqlServerJournalOptions();
    var snapshotOptions = new SqlServerSnapshotOptions();

    builder
        .WithClustering()
        .WithSqlServerPersistence(journalOptions, snapshotOptions)
        .WithShardRegion<UserActionsEntity>(
            "userActions", 
            s => UserActionsEntity.Props(s),
            new UserMessageExtractor(),
            shardOptions);
    
    builder.WithClusterShardingJournalMigrationAdapter(journalOptions);
})
```
