using Akka.Actor;
using Akka.Cluster.Hosting;
using Akka.Remote.Hosting;
using Akka.Util;

namespace Akka.Hosting.SimpleDemo;

public class EchoActor : ReceiveActor
{
    private readonly string _entityId;
    public EchoActor(string entityId)
    {
        _entityId = entityId;
        ReceiveAny(message => {
            Sender.Tell($"{Self} rcv {message}");
        });
    }
}

public class Program
{
    private const int NumberOfShards = 5;
    
    private static Option<(string, object)> ExtractEntityId(object message)
        => message switch {
            string id => (id, id),
            _ => Option<(string, object)>.None
        };

    private static string? ExtractShardId(object message)
        => message switch {
            string id => (id.GetHashCode() % NumberOfShards).ToString(),
            _ => null
        };
        
    private static Props PropsFactory(string entityId)
        => Props.Create(() => new EchoActor(entityId));
        
    public static void Main(params string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddAkka("MyActorSystem", configurationBuilder =>
        {
            configurationBuilder
                .WithRemoting(hostname: "localhost", port: 8110)
                .WithClustering(new ClusterOptions{SeedNodes = new []{ "akka.tcp://MyActorSystem@localhost:8110", }})
                .WithShardRegion<Echo>(
                    typeName: "myRegion",
                    entityPropsFactory: PropsFactory, 
                    extractEntityId: ExtractEntityId,
                    extractShardId: ExtractShardId,
                    shardOptions: new ShardOptions());
        });

        var app = builder.Build();

        app.MapGet("/", async (context) =>
        {
            var echo = context.RequestServices.GetRequiredService<ActorRegistry>().Get<Echo>();
            var body = await echo.Ask<string>(
                    message: context.TraceIdentifier, 
                    cancellationToken: context.RequestAborted)
                .ConfigureAwait(false);
            await context.Response.WriteAsync(body);
        });

        app.Run();    
    }
}