# Akka.Hosting

> **BETA**: this project is currently in beta status as part of the [Akka.NET v1.5 development effort](https://getakka.net/community/whats-new/akkadotnet-v1.5.html), but the packages published in this repository will be backwards compatible for Akka.NET v1.4 users.

HOCON-less configuration, application lifecycle management, `ActorSystem` startup, and actor instantiation for [Akka.NET](https://getakka.net/).

Consists of the following packages:

1. `Akka.Hosting` - core, needed for everything
2. `Akka.Remote.Hosting` - enables Akka.Remote configuration
3. [`Akka.Cluster.Hosting`](src/Akka.Cluster.Hosting/README.md) - used for Akka.Cluster, Akka.Cluster.Sharding, and Akka.Cluster.Tools
4. `Akka.Persistence.SqlServer.Hosting` - used for Akka.Persistence.SqlServer support.
5. `Akka.Persistence.PostgreSql.Hosting` - used for Akka.Persistence.PostgreSql support.
6. [`Akka.Persistence.Azure.Hosting`](https://github.com/petabridge/Akka.Persistence.Azure) - used for Akka.Persistence.Azure support. Documentation can be read [here](https://github.com/petabridge/Akka.Persistence.Azure/blob/master/README.md)
7. [The Akka.Management Project Repository](https://github.com/akkadotnet/Akka.Management) - useful tools for managing Akka.NET clusters running inside containerized or cloud based environment. `Akka.Hosting` is embedded in each of its packages: 
    * [`Akka.Management`](https://github.com/akkadotnet/Akka.Management/tree/dev/src/management/Akka.Management) - core module of the management utilities which provides a central HTTP endpoint for Akka management extensions.
    * [`Akka.Management.Cluster.Bootstrap`](https://github.com/akkadotnet/Akka.Management/tree/dev/src/cluster.bootstrap/Akka.Management.Cluster.Bootstrap) - used to bootstrap a cluster formation inside dynamic deployment environments, relies on `Akka.Discovery` to function.
    * [`Akka.Discovery.AwsApi`](https://github.com/akkadotnet/Akka.Management/tree/dev/src/discovery/aws/Akka.Discovery.AwsApi) - provides dynamic node discovery service for AWS EC2 environment.
    * [`Akka.Discovery.Azure`](https://github.com/akkadotnet/Akka.Management/tree/dev/src/discovery/azure/Akka.Discovery.Azure) - provides a dynamic node discovery service for Azure PaaS ecosystem.
    * [`Akka.Discovery.KubernetesApi`](https://github.com/akkadotnet/Akka.Management/tree/dev/src/discovery/kubernetes/Akka.Discovery.KubernetesApi) - provides a dynamic node discovery service for Kubernetes clusters.
    * [`Akka.Coordination.KubernetesApi`](https://github.com/akkadotnet/Akka.Management/tree/dev/src/coordination/kubernetes/Akka.Coordination.KubernetesApi) - provides a lease-based distributed lock mechanism for Akka Split Brain Resolver, Akka.Cluster.Sharding, and Akka.Cluster.Singleton

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

## Dependency Injection Outside and Inside Akka.NET

One of the other design goals of Akka.Hosting is to make the dependency injection experience with Akka.NET as seamless as any other .NET technology. We accomplish this through two new APIs:

* The `ActorRegistry`, a DI container that is designed to be populated with `Type`s for keys and `IActorRef`s for values, just like the `IServiceCollection` does for ASP.NET services.
* The `IRequiredActor<TKey>` - you can place this type the constructor of any DI'd resource and it will automatically resolve a reference to the actor stored inside the `ActorRegistry` with `TKey`. This is how we inject actors into ASP.NET, SignalR, gRPC, and other Akka.NET actors!

> **N.B.** The `ActorRegistry` and the `ActorSystem` are automatically registered with the `IServiceCollection` / `IServiceProvider` associated with your application.

### Registering Actors with the `ActorRegistry`

As part of Akka.Hosting, we need to provide a means of making it easy to pass around top-level `IActorRef`s via dependency injection both within the `ActorSystem` and outside of it.

The `ActorRegistry` will fulfill this role through a set of generic, typed methods that make storage and retrieval of long-lived `IActorRef`s easy and coherent:

```csharp
var registry = ActorRegistry.For(myActorSystem); // fetch from ActorSystem
registry.TryRegister<Index>(indexer); // register for DI
registry.Get<Index>(); // use in DI
```

### Injecting Actors with `IRequiredActor<TKey>`

Suppose we have a class that depends on having a reference to a top-level actor, a router, a `ShardRegion`, or perhaps a `ClusterSingleton` (common types of actors that often interface with non-Akka.NET parts of a .NET application):

```csharp
public sealed class MyConsumer
{
    private readonly IActorRef _actor;

    public MyConsumer(IRequiredActor<MyActorType> actor)
    {
        _actor = actor.ActorRef;
    }

    public async Task<string> Say(string word)
    {
        return await _actor.Ask<string>(word, TimeSpan.FromSeconds(3));
    }
}
```

The `IRequiredActor<MyActorType>` will cause the Microsoft.Extensions.DependencyInjection mechanism to resolve `MyActorType` from the `ActorRegistry` and inject it into the `IRequired<Actor<MyActorType>` instance passed into `MyConsumer`.

The `IRequiredActor<TActor>` exposes a single property:

```csharp
public interface IRequiredActor<TActor>
{
    /// <summary>
    /// The underlying actor resolved via <see cref="ActorRegistry"/> using the given <see cref="TActor"/> key.
    /// </summary>
    IActorRef ActorRef { get; }
}
```

By default, you can automatically resolve any actors registered with the `ActorRegistry` without having to declare anything special on your `IServiceCollection`:

```csharp
using var host = new HostBuilder()
  .ConfigureServices(services =>
  {
      services.AddAkka("MySys", (builder, provider) =>
      {
          builder.WithActors((system, registry) =>
          {
              var actor = system.ActorOf(Props.Create(() => new MyActorType()), "myactor");
              registry.Register<MyActorType>(actor);
          });
      });
      services.AddScoped<MyConsumer>();
  })
  .Build();
  await host.StartAsync();
```

Adding your actor and your type key into the `ActorRegistry` is sufficient - no additional DI registration is required to access the `IRequiredActor<TActor>` for that type.

#### Resolving `IRequiredActor<TKey>` within Akka.NET

Akka.NET does not use dependency injection to start actors by default primarily because actor lifetime is unbounded by default - this means reasoning about the scope of injected dependencies isn't trivial. ASP.NET, by contrast, is trivial: all HTTP requests are request-scoped and all web socket connections are connection-scoped - these are objects have _bounded_ and typically short lifetimes.

Therefore, users have to explicitly signal when they want to use Microsoft.Extensions.DependencyInjection via [the `IDependencyResolver` interface in Akka.DependencyInjection](https://getakka.net/articles/actors/dependency-injection.html) - which is easy to do in most of the Akka.Hosting APIs for starting actors:

```csharp
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
                // uses DI to inject `IReplyGenerator` into EchoActor
                return s => resolver.Props<EchoActor>(s);
            },
            extractEntityId: ExtractEntityId,
            extractShardId: ExtractShardId,
            shardOptions: new ShardOptions());
});
```

The `dependencyResolver.Props<MySingletonDiActor>()` call will leverage the `ActorSystem`'s built-in `IDependencyResolver` to instantiate the `MySingletonDiActor` and inject it with all of the necessary dependences, including `IRequiredActor<TKey>`.

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

### Microsoft.Extensions.Logging.ILoggerFactory Logging Support

You can now use `ILoggerFactory` from Microsoft.Extensions.Logging as one of the sinks for Akka.NET logger. This logger will use the `ILoggerFactory` service set up inside the dependency injection `ServiceProvider` as its sink.

### Microsoft.Extensions.Logging Log Event Filtering

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
