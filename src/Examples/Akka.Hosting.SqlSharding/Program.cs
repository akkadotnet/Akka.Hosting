using Akka.Actor;
using Akka.Actor.Dsl;
using Akka.Cluster.Hosting;
using Akka.Cluster.Sharding;
using Akka.Configuration;
using Akka.Hosting;
using Akka.Hosting.SqlSharding;
using Akka.Hosting.SqlSharding.Actors;
using Akka.Hosting.SqlSharding.Messages;
using Akka.Persistence.SqlServer.Hosting;
using Akka.Remote.Hosting;

var builder = WebApplication.CreateBuilder(args);

#if USE_OPTIONS
builder.Services.AddAkka("MyActorSystem", configurationBuilder =>
    {
        var localConn = builder.Configuration.GetConnectionString("sqlServerLocal");
        var shardingConn = builder.Configuration.GetConnectionString("sqlServerSharding");

        var shardingJournalOptions = new SqlServerJournalOptions(isDefaultPlugin: false)
        {
            Identifier = "sharding",
            ConnectionString = shardingConn,
            AutoInitialize = true
        };
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
                SeedNodes = new[] { Address.Parse("akka.tcp://MyActorSystem@localhost:8110") }
            })
            .WithSqlServerPersistence(localConn)
            .WithSqlServerPersistence(shardingJournalOptions, shardingSnapshotOptions)
            .WithShardRegion<UserActionsEntity>("userActions", s => UserActionsEntity.Props(s),
                new UserMessageExtractor(),
                new ShardOptions
                {
                    StateStoreMode = StateStoreMode.Persistence, 
                    Role = "myRole", 
                    JournalPluginId = shardingJournalOptions.PluginId,
                    SnapshotPluginId = shardingSnapshotOptions.PluginId 
                })
            .WithActors((system, registry) =>
            {
                var userActionsShard = registry.Get<UserActionsEntity>();
                var indexer = system.ActorOf(Props.Create(() => new Indexer(userActionsShard)), "index");
                registry.TryRegister<Index>(indexer); // register for DI
            });
    })
    .AddHostedService<TestDataPopulatorService>();

#else
builder.Services.AddAkka("MyActorSystem", configurationBuilder =>
{
    configurationBuilder
        .WithRemoting("localhost", 8110)
        .WithClustering(new ClusterOptions()
        {
            Roles = new[] { "myRole" },
            SeedNodes = new[] { Address.Parse("akka.tcp://MyActorSystem@localhost:8110") }
        })
        .WithSqlServerPersistence(builder.Configuration.GetConnectionString("sqlServerLocal"))
        .WithShardRegion<UserActionsEntity>("userActions", s => UserActionsEntity.Props(s),
            new UserMessageExtractor(),
            new ShardOptions(){ StateStoreMode = StateStoreMode.DData, Role = "myRole"})
        .WithActors((system, registry) =>
        {
            var userActionsShard = registry.Get<UserActionsEntity>();
            var indexer = system.ActorOf(Props.Create(() => new Indexer(userActionsShard)), "index");
            registry.TryRegister<Index>(indexer); // register for DI
        });
})
    .AddHostedService<TestDataPopulatorService>();

#endif

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