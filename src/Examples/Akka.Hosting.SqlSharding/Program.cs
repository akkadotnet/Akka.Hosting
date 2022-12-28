using Akka.Actor;
using Akka.Cluster.Hosting;
using Akka.Cluster.Sharding;
using Akka.Hosting;
using Akka.Hosting.SqlSharding;
using Akka.Hosting.SqlSharding.Actors;
using Akka.Hosting.SqlSharding.Messages;
using Akka.Persistence.SqlServer.Hosting;
using Akka.Remote.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAkka("MyActorSystem", configurationBuilder =>
{
    configurationBuilder
        .WithRemoting("localhost", 8110)
        .WithClustering(new ClusterOptions()
        {
            Roles = new[] { "myRole" },
            SeedNodes = new[] { "akka.tcp://MyActorSystem@localhost:8110" }
        })
        .WithSqlServerPersistence(builder.Configuration.GetConnectionString("sqlServerLocal"))
        .WithShardRegion<UserActionsEntity>("userActions", s => UserActionsEntity.Props(s),
            new UserMessageExtractor(),
            new ShardOptions(){ StateStoreMode = StateStoreMode.DData, Role = "myRole"})
        .WithActors((system, registry, resolver) =>
        {
            var props = resolver.Props<Indexer>();
            var indexer = system.ActorOf(props, "index");
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