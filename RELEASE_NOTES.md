## [0.4.0] / 18 July 2022
- [Add `Microsoft.Extensions.Logging.ILoggerFactory` logging support](https://github.com/akkadotnet/Akka.Hosting/pull/72)

You can now use `ILoggerFactory` from Microsoft.Extensions.Logging as one of the sinks for Akka.NET logger. This logger will use the `ILoggerFactory` service set up inside the dependency injection `ServiceProvider` as its sink.

Example:
```
builder.Services.AddAkka("MyActorSystem", (configurationBuilder, serviceProvider) =>
{
    configurationBuilder
        .AddHocon("akka.loglevel = DEBUG")
        .WithLoggerFactory()
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

There are two `Akka.Hosting` extension methods provided:
- `.WithLoggerFactory()`: Replaces all Akka.NET loggers with the new `ILoggerFactory` logger.
- `.AddLoggerFactory()`: Inserts the new `ILoggerFactory` logger into the Akka.NET logger list.

__Log Event Filtering__

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
