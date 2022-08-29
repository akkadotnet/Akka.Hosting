# Akka.Hosting

> **BETA**: this project is currently in beta status as part of the [Akka.NET v1.5 development effort](https://getakka.net/community/whats-new/akkadotnet-v1.5.html), but the packages published in this repository will be backwards compatible for Akka.NET v1.4 users.

HOCON-less configuration, application lifecycle management, `ActorSystem` startup, and actor instantiation for [Akka.NET](https://getakka.net/).

Consists of the following packages:

1. `Akka.Hosting` - core, needed for everything
2. `Akka.Remote.Hosting` - enables Akka.Remote configuration
3. `Akka.Cluster.Hosting` - used for Akka.Cluster, Akka.Cluster.Sharding
4. `Akka.Persistence.SqlServer.Hosting` - used for Akka.Persistence.SqlServer support.
5. `Akka.Persistence.PostgreSql.Hosting` - used for Akka.Persistence.PostgreSql support.

See the ["Introduction to Akka.Hosting - HOCONless, "Pit of Success" Akka.NET Runtime and Configuration" video](https://www.youtube.com/watch?v=Mnb9W9ClnB0) for a walkthrough of the library and how it can save you a tremendous amount of time and trouble.

## Summary

We want to make Akka.NET something that can be instantiated more typically per the patterns often used with the Microsoft.Extensions.Hosting APIs that are common throughout .NET.

```csharp
using Akka.Hosting;
using Akka.Actor;
using Akka.Actor.Dsl;
using Akka.Cluster.Hosting;
using Akka.Remote.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAkka("MyActorSystem", configurationBuilder =>
{
    configurationBuilder
        .WithRemoting("localhost", 8110)
        .WithClustering(new ClusterOptions(){ Roles = new[]{ "myRole" },
            SeedNodes = new[]{ Address.Parse("akka.tcp://MyActorSystem@localhost:8110")}})
        .WithActors((system, registry) =>
    {
        var echo = system.ActorOf(act =>
        {
            act.ReceiveAny((o, context) =>
            {
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

app.Run();
```

No HOCON. Automatically runs all Akka.NET application lifecycle best practices behind the scene. Automatically binds the `ActorSystem` and the `ActorRegistry`, another new 1.5 feature, to the `IServiceCollection` so they can be safely consumed via both actors and non-Akka.NET parts of users' .NET applications.

This should be open to extension in other child plugins, such as `Akka.Persistence.SqlServer`:

```csharp
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
```

#### `ActorRegistry`

As part of Akka.Hosting, we need to provide a means of making it easy to pass around top-level `IActorRef`s via dependency injection both within the `ActorSystem` and outside of it.

The `ActorRegistry` will fulfill this role through a set of generic, typed methods that make storage and retrieval of long-lived `IActorRef`s easy and coherent:

```csharp
var registry = ActorRegistry.For(myActorSystem); // fetch from ActorSystem
registry.TryRegister<Index>(indexer); // register for DI
registry.Get<Index>(); // use in DI
```

## Microsoft.Extensions.Logging Integration

__Logger Configuration Support__

You can now use the new `AkkaConfigurationBuilder` extension method called `ConfigureLoggers(Action<LoggerConfigBuilder>)` to configure how Akka.NET logger behave.

Example:
```csharp
builder.Services.AddAkka("MyActorSystem", configurationBuilder =>
{
    configurationBuilder
        .ConfigureLoggers(setup =>
        {
            // Example: This sets the minimum log level
            setup.LogLevel = LogLevel.DebugLevel;
            
            // Example: Clear all loggers
            setup.ClearLoggers();
            
            // Example: Add the default logger
            // NOTE: You can also use setup.AddLogger<DefaultLogger>();
            setup.AddDefaultLogger();
            
            // Example: Add the ILoggerFactory logger
            // NOTE:
            //   - You can also use setup.AddLogger<LoggerFactoryLogger>();
            //   - To use a specific ILoggerFactory instance, you can use setup.AddLoggerFactory(myILoggerFactory);
            setup.AddLoggerFactory();
            
            // Example: Adding a serilog logger
            setup.AddLogger<SerilogLogger>();
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
```

A complete code sample can be viewed [here](https://github.com/akkadotnet/Akka.Hosting/tree/dev/src/Examples/Akka.Hosting.LoggingDemo).

Exposed properties are:
- `LogLevel`: Configure the Akka.NET minimum log level filter, defaults to `InfoLevel`
- `LogConfigOnStart`: When set to true, Akka.NET will log the complete HOCON settings it is using at start up, this can then be used for debugging purposes.

Currently supported logger methods:
- `ClearLoggers()`: Clear all registered logger types.
- `AddLogger<TLogger>()`: Add a logger type by providing its class type.
- `AddDefaultLogger()`: Add the default Akka.NET console logger.
- `AddLoggerFactory()`: Add the new `ILoggerFactory` logger.

__Microsoft.Extensions.Logging.ILoggerFactory Logging Support__

You can now use `ILoggerFactory` from Microsoft.Extensions.Logging as one of the sinks for Akka.NET logger. This logger will use the `ILoggerFactory` service set up inside the dependency injection `ServiceProvider` as its sink.

__Microsoft.Extensions.Logging Log Event Filtering__

There will be two log event filters acting on the final log input, the Akka.NET `akka.loglevel` setting and the `Microsoft.Extensions.Logging` settings, make sure that both are set correctly or some log messages will be missing.

To set up the `Microsoft.Extensions.Logging` log filtering, you will need to edit the `appsettings.json` file. Note that we also set the `Akka` namespace to be filtered at debug level in the example below.

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information",
      "Akka": "Debug"
    }
  }
}
```
