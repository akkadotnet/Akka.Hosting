## [0.4.2] / 11 August 2022
- [Update Akka.NET from 1.4.39 to 1.4.40](https://github.com/akkadotnet/akka.net/releases/tag/1.4.40)
- [Add `WithExtensions()` method](https://github.com/akkadotnet/Akka.Hosting/pull/92)
- [Add `AddStartup` method](https://github.com/akkadotnet/Akka.Hosting/pull/90)

__WithExtensions()__

`AkkaConfigurationBuilder.WithExtensions()` is used to configure the `akka.extensions` HOCON settings. It is used to set an Akka.NET extension provider to start-up automatically during `ActorSystem` start-up.

Example:
```csharp
// Starts distributed pub-sub, cluster metrics, and cluster bootstrap extensions at start-up
builder.WithExtensions(
     typeof(DistributedPubSubExtensionProvider),
     typeof(ClusterMetricsExtensionProvider),
     typeof(ClusterBootstrapProvider));
```

__AddStartup()__

`AddStartup()` method adds `StartupTask` delegate to the configuration builder. 

This feature is useful when a user need to run a specific initialization code if anf only if the `ActorSystem` and all of the actors have been started. Although it is semantically the same as `AddActors` and `WithActors`, it disambiguate the use-case with a guarantee that it will only be executed after everything is ready.

For example, this feature is useful for:
- kicking off actor initializations by using Tell()s once all of the actor infrastructure are in place, or
- pre-populating certain persistence or database data after everything is set up and running, useful for unit testing or adding fake data for local development.