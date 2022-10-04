## [0.5.0] / 9 October 2022

* [Update Akka.NET from 1.4.41 to 1.4.43](https://github.com/akkadotnet/akka.net/releases/tag/1.4.43)
* [Add full options support to Akka.Persistence.SqlServer.Hosting](https://github.com/akkadotnet/Akka.Hosting/pull/107)
* [Improved Akka.Remote.Hosting implementation](https://github.com/akkadotnet/Akka.Hosting/pull/108)
* [Add a standardized option code pattern for Akka.Hosting developer](https://github.com/akkadotnet/Akka.Hosting/pull/110)
* [Add Akka.Hosting.TestKit module for unit testing projects using Akka.Hosting](https://github.com/akkadotnet/Akka.Hosting/pull/102)

**Add full options support to Akka.Persistence.SqlServer.Hosting**

You can now use an option class in Akka.Persistence.SqlServer.Hosting to replace HOCON configuration fully.

**Add Akka.Hosting.TestKit module**

The biggest difference between _Akka.Hosting.TestKit_ and _Akka.TestKit_ is that, since the test is started asynchronously, the _TestKit_ properties and methods would not be available in the unit test class constructor anymore. Since the spec depends on Microsoft `HostBuilder`, configuration has to be broken down into steps. There are overridable methods that user can use to override virtually all of the setup process.

These are steps of what overridable methods gets called. Not all of the methods needs to be overriden, at the minimum, if you do not need a custom hosting environment, you need to override the `ConfigureAkka` method.

1. `ConfigureLogging(ILoggingBuilder)`

   Add custom logger and filtering rules on the `HostBuilder` level.
2. `ConfigureHostConfiguration(IConfigurationBuilder)` 

   Inject any additional hosting environment configuration here, such as faking environment variables, in the `HostBuilder` level.
3. `ConfigureAppConfiguration(HostBuilderContext, IConfigurationBuilder)`

   Inject the application configuration.
4. `ConfigureServices(HostBuilderContext, IServiceCollection)`

   Add additional services needed by the test, such as mocked up services used inside the dependency injection.
5. User defined HOCON configuration is injected by overriding the `Config` property, it is not passed as part of the constructor anymore.
6. `ConfigureAkka(AkkaConfigurationBuilder, IServiceProvider)`

   This is called inside `AddAkka`, use this to configure the `AkkaConfigurationBuilder`
7. `BeforeTestStart()`

   This method is called after the TestKit is initialized. Move all of the codes that used to belong in the constructor here.

`Akka.Hosting.TestKit` extends `Akka.TestKit.TestKitBase` directly, all testing methods are available out of the box.
All of the properties, such as `Sys` and `TestActor` will be available once the unit test class is invoked.

**Add a standardized option code pattern for Akka.Hosting developer**

This new feature is intended for Akka.Hosting module developer only, it is used to standardize how Akka.Hosting addresses a very common HOCON configuration pattern. This allows for a HOCON-less programmatic setup replacement for the HOCON path used to configure the HOCON property.

The pattern:

```text
# This HOCON property references to a config block below
akka.discovery.method = akka.discovery.config

akka.discovery.config {
    class = "Akka.Discovery.Config.ConfigServiceDiscovery, Akka.Discovery"
    # other options goes here
}
```

Example implementation:
```csharp
// The base class for the option, needs to implement the IHoconOption template interface
public abstract class DiscoveryOptionBase : IHoconOption
{ }

// The actual option implementation
public class ConfigOption : DiscoveryOptionBase
{
    // The path value in the akka.discovery.method property above
    public string ConfigPath => "akka.discovery.config";

    // The FQCN value in the akka.discovery.config.class property above
    public Type Class => typeof(ConfigServiceDiscovery);

    // Generate the same HOCON config as above
    public void Apply(AkkaConfigurationBuilder builder, Setup setup = null)
    {
        // Modifies Akka.NET configuration either via HOCON or setup class
        builder.AddHocon(
            $"akka.discovery.method = {ConfigPath.ToHocon()}",
            HoconAddMode.Prepend);
        builder.AddHocon($"akka.discovery.config.class = {
            Class.AssemblyQualifiedName.ToHocon()}",
            HoconAddMode.Prepend);

        // Rest of configuration goes here
    }
}

// Akka.Hosting extension implementation
public static AkkaConfigurationBuilder WithDiscovery(
    this AkkaConfigurationBuilder builder,
    DiscoveryOptionBase discOption)
{
    var setup = new DiscoverySetup();

    // gets called here
    discOption.Apply(builder, setup);
}
```

## [0.4.3] / 9 September 2022
- [Update Akka.NET from 1.4.40 to 1.4.41](https://github.com/akkadotnet/akka.net/releases/tag/1.4.41)
- [Cluster.Hosting: Add split-brain resolver support](https://github.com/akkadotnet/Akka.Hosting/pull/95)
- [Hosting: Add `WithExtension<T>()` extension method](https://github.com/akkadotnet/Akka.Hosting/pull/97)
 
__WithExtension<T>()__

`AkkaConfigurationBuilder.WithExtension<T>()` works similarly to `AkkaConfigurationBuilder.WithExtensions()` and is used to configure the `akka.extensions` HOCON settings. The difference is that it is statically typed to only accept classes that extends the `IExtensionId` interface.

This pull request also adds a validation code to the `AkkaConfigurationBuilder.WithExtensions()` method to make sure that all the types passed in actually extends the `IExtensionId` interface. The method will throw a `ConfigurationException` exception if one of the types did not extend `IExtensionId` or if they are abstract or static class types.

Example:
```csharp
// Starts distributed pub-sub, cluster metrics, and cluster bootstrap extensions at start-up
builder
    .WithExtension<DistributedPubSubExtensionProvider>()
    .WithExtension<ClusterMetricsExtensionProvider>()
    .WithExtension<ClusterBootstrapProvider>();
```

__Clustering split-brain resolver support__

The split-brain resolver can now be set using the second parameter named `sbrOption` in the `.WithClustering()` extension method. You can read more about this in the [documentation](https://github.com/akkadotnet/Akka.Hosting/tree/dev/src/Akka.Cluster.Hosting#configure-a-cluster-with-split-brain-resolver-sbr).

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