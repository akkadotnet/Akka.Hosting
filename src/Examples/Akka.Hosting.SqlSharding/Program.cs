using Akka.Actor;
using Akka.Actor.Dsl;
using Akka.Cluster.Hosting;
using Akka.Cluster.Sharding;
using Akka.Configuration;
using Akka.Hosting;
using Akka.Hosting.SqlSharding;
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
});

var app = builder.Build();

var system = app.Services.GetRequiredService<ActorSystem>();

system.Scheduler.Advanced.ScheduleRepeatedly(TimeSpan.Zero, TimeSpan.FromSeconds(10), () =>
{
    var entityRegion = ActorRegistry.For(system).Get<UserActionsEntity>();
    var user = UserGenerator.CreateRandom();
    entityRegion.Tell(new CreateUser(user));
});

app.MapGet("/", async (ActorRegistry registry) =>
{
    var index = registry.Get<Index>();
    return await index.Ask<IEnumerable<UserDescriptor>>(FetchUsers.Instance, TimeSpan.FromSeconds(3))
        .ConfigureAwait(false);
});

app.MapGet("/{userId}", async (string userId, ActorRegistry registry) =>
{
    var index = registry.Get<UserActionsEntity>();
    return await index.Ask<UserDescriptor>(new FetchUser(userId), TimeSpan.FromSeconds(3))
        .ConfigureAwait(false);
});

app.Run();