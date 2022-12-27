using Akka.Actor;
using Akka.Cluster.Hosting;
using Akka.Remote.Hosting;
using Akka.Util;

namespace Akka.Hosting.SimpleDemo;

public interface IReplyGenerator
{
    string Reply(object input);
}

public class DefaultReplyGenerator : IReplyGenerator
{
    public string Reply(object input)
    {
        return input.ToString()!;
    }
}

public class EchoActor : ReceiveActor
{
    private readonly string _entityId;
    private readonly IReplyGenerator _replyGenerator;
    public EchoActor(string entityId, IReplyGenerator replyGenerator)
    {
        _entityId = entityId;
        _replyGenerator = replyGenerator;
        ReceiveAny(message => {
            Sender.Tell($"{Self} rcv {_replyGenerator.Reply(message)}");
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

    public static void Main(params string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddScoped<IReplyGenerator, DefaultReplyGenerator>();
        builder.Services.AddAkka("MyActorSystem", configurationBuilder =>
        {
            configurationBuilder
                .WithRemoting(hostname: "localhost", port: 8110)
                .WithClustering(new ClusterOptions{SeedNodes = new []{ "akka.tcp://MyActorSystem@localhost:8110", }})
                .WithShardRegion<Echo>(
                    typeName: "myRegion",
                    entityPropsFactory: (_, _, resolver) =>
                    {
                        return s => resolver.Props<EchoActor>(s);
                    },
                    extractEntityId: ExtractEntityId,
                    extractShardId: ExtractShardId,
                    shardOptions: new ShardOptions());
        });

        var app = builder.Build();

        app.MapGet("/", async (HttpContext context, IRequiredActor<Echo> echoActor) =>
        {
            var echo = echoActor.ActorRef;
            var body = await echo.Ask<string>(
                    message: context.TraceIdentifier, 
                    cancellationToken: context.RequestAborted)
                .ConfigureAwait(false);
            await context.Response.WriteAsync(body);
        });

        app.Run();    
    }
}