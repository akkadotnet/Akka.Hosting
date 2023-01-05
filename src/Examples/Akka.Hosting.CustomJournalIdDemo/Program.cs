using Akka.Actor;
using Akka.Cluster.Hosting;
using Akka.Cluster.Sharding;
using Akka.Hosting;
using Akka.Hosting.CustomJournalIdDemo;
using Akka.Hosting.CustomJournalIdDemo.Actors;
using Akka.Hosting.CustomJournalIdDemo.Messages;
using Akka.Persistence.SqlServer.Hosting;
using Akka.Remote.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAkka("MyActorSystem", configurationBuilder =>
    {
        // Grab connection strings from appsettings.json
        var localConn = builder.Configuration.GetConnectionString("sqlServerLocal");
        var shardingConn = builder.Configuration.GetConnectionString("sqlServerSharding");

        // Custom journal options with the id "sharding"
        // The absolute id will be "akka.persistence.journal.sharding"
        var shardingJournalOptions = new SqlServerJournalOptions(isDefaultPlugin: false)
        {
            Identifier = "sharding",
            ConnectionString = shardingConn,
            AutoInitialize = true
        };
        
        // Custom snapshots options with the id "sharding"
        // The absolute id will be "akka.persistence.snapshot-store.sharding"
        var shardingSnapshotOptions = new SqlServerSnapshotOptions(isDefaultPlugin: false)
        {
            Identifier = "sharding",
            ConnectionString = shardingConn,
            AutoInitialize = true
        };
        
        configurationBuilder
            .WithRemoting("localhost", 8110)
            .WithClustering(new ClusterOptions()
            {
                Roles = new[] { "myRole" },
                SeedNodes = new[] { "akka.tcp://MyActorSystem@localhost:8110" }
            })
            .WithSqlServerPersistence(localConn) // Standard way to create a default persistence journal and snapshot
            .WithSqlServerPersistence(shardingJournalOptions, shardingSnapshotOptions) // This is a custom persistence setup using the options instances we've set up earlier
            .WithShardRegion<UserActionsEntity>("userActions", UserActionsEntity.Props,
                new UserMessageExtractor(),
                new ShardOptions
                {
                    StateStoreMode = StateStoreMode.Persistence, 
                    Role = "myRole", 
                    JournalOptions = shardingJournalOptions,
                    SnapshotOptions = shardingSnapshotOptions 
                })
            .WithActors((system, registry) =>
            {
                var userActionsShard = registry.Get<UserActionsEntity>();
                var indexer = system.ActorOf(Props.Create(() => new Indexer(userActionsShard)), "index");
                registry.TryRegister<Index>(indexer); // register for DI
            });
    })
    .AddHostedService<TestDataPopulatorService>();

var app = builder.Build();

app.MapGet("/", async (ActorRegistry registry) =>
{
    var index = registry.Get<Index>();
    return await index.Ask<IEnumerable<UserDescriptor>>(FetchUsers.Instance, TimeSpan.FromSeconds(3))
        .ConfigureAwait(false);
});

app.MapGet("/user/{userId}", async (string userId, ActorRegistry registry) =>
{
    var index = registry.Get<UserActionsEntity>();
    return await index.Ask<UserDescriptor>(new FetchUser(userId), TimeSpan.FromSeconds(3))
        .ConfigureAwait(false);
});

app.Run();