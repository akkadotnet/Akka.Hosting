using Akka.Hosting;
using Akka.Actor;
using Akka.Actor.Dsl;
using Akka.Cluster.Hosting;
using Akka.Event;
using Akka.Hosting.Asp.LoggingDemo;
using Akka.Remote.Hosting;
using LogLevel = Akka.Event.LogLevel;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHealthChecks();

builder.Services.AddAkka("MyActorSystem", (configurationBuilder, serviceProvider) =>
{
    configurationBuilder
        .ConfigureLoggers(setup =>
        {
            // This sets the minimum log level
            setup.LogLevel = LogLevel.DebugLevel;
            
            // Clear all loggers (remove the default console logger)
            setup.ClearLoggers();
            
            // Add the ILoggerFactory logger
            // NOTE:
            //   - You can also use setup.AddLogger<LoggerFactoryLogger>();
            //   - To use a specific ILoggerFactory instance, you can use setup.AddLoggerFactory(myILoggerFactory);
            setup.AddLoggerFactory();
        })
        .WithRemoting("localhost", 8110)
        .WithClustering(new ClusterOptions { 
            Roles = ["myRole"], 
            SeedNodes = ["akka.tcp://MyActorSystem@localhost:8110"]
        })
        .WithActors((system, registry) =>
        {
            var echo = system.ActorOf(act =>
            {
                act.ReceiveAny((o, context) =>
                {
                    Logging.GetLogger(context.System, "echo").Info($"Actor received {o}");
                    context.Sender.Tell($"{context.Self} rcv {o}");
                });
            }, "echo");
            registry.TryRegister<Echo>(echo); // register for DI
        });
});

var app = builder.Build();

app.MapGet("/", async (context) =>
{
    var echo = context.RequestServices.GetRequiredService<ActorRegistry>().Get<Echo>();
    var body = await echo.Ask<string>(context.TraceIdentifier, context.RequestAborted).ConfigureAwait(false);
    await context.Response.WriteAsync(body);
});

app.MapHealthChecks("/healthz");

app.Run();