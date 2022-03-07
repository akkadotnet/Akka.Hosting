using Akka.Hosting;
using Akka.Actor;
using Akka.Actor.Dsl;

var builder = WebApplication.CreateBuilder(args);
IActorRef echo = ActorRefs.Nobody;

builder.Services.AddAkka("MyActorSystem", configurationBuilder =>
{
    configurationBuilder.StartActors((system, registry) =>
    {
        system.ActorOf(act =>
        {
            act.ReceiveAny((o, context) =>
            {
                context.Sender.Tell($"{context.Self} rcv {o}");
            });
        }, "echo");
    });
});

var app = builder.Build();

app.MapGet("/", async (context) => await echo.Ask<string>(context.TraceIdentifier, context.RequestAborted));

app.Run();