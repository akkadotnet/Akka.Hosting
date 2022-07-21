## [0.4.1] / 21 July 2022
- [Fix `Microsoft.Extensions.Logging.ILoggerFactory` logging support](https://github.com/akkadotnet/Akka.Hosting/pull/81)
- [Add `InMemory` snapshot store and journal persistence support](https://github.com/akkadotnet/Akka.Hosting/pull/84)

Due to a bad API design, we're rolling back the `Microsoft.Extensions.Logging.ILoggerFactory` logger support introduced in version 0.4.0, the 0.4.0 NuGet version is now considered as deprecated in support of the new API design introduced in version 0.4.1.

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

__InMemory Snapshot Store And Journal Support__

You can now use these `AkkaConfigurationBuilder` extension methods to enable `InMemory` persistence back end:
- `WithInMemoryJournal()`: Sets the `InMemory` journal as the default journal persistence plugin
- `WithInMemorySnapshotStore()`: Sets the `InMemory` snapshot store as the default snapshot-store persistence plugin.