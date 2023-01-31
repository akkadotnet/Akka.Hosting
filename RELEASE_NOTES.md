## [1.0.2] / 31 January 2023

Version 1.0.2 introduces a new method to the `ActorRegistry.GetAsync` in order to allow users to force parts of their application to wait until a specific `IActorRef` has been started and added to the `ActorRegistry`.

```csharp
// arrange
var registry = new ActorRegistry();

// act
var task = registry.GetAsync<Nobody>();
task.IsCompletedSuccessfully.Should().BeFalse();

registry.Register<Nobody>(Nobody.Instance);
var result = await task;

// assert
result.Should().Be(Nobody.Instance);
```

This method is designed to allow users to wait via `async Task<IActorRef>` for an actor to be registered in the event that a specific `IRequiredActor<TKey>` is needed before Akka.Hosting can start the `ActorSystem` inside its `IHostedService`.

## [1.0.1] / 6 January 2023

Version 1.0.1 fixes options bug used in the cluster sharding Akka.Hosting extension method.

* [Update Akka.NET from 1.4.45 to 1.4.48](https://github.com/akkadotnet/akka.net/releases/tag/1.4.48)
* [Fix SimpleDemo project failing on `Development` environment](https://github.com/akkadotnet/Akka.Hosting/pull/184)
* [Add F# CustomJournalIdDemo project](https://github.com/akkadotnet/Akka.Hosting/pull/183)
* [Fix cluster sharding hosting extension method options bug](https://github.com/akkadotnet/Akka.Hosting/pull/186)

## [1.0.0] / 27 December 2022

This 1.0.0 release is the RTM release for `Akka.Hosting` and contains major API breaking changes with a lot of its API. All current API will be frozen for all future releases and will be backed with our backward compatibility promise.

**Change List**
* [Update Akka.NET from 1.4.45 to 1.4.47](https://github.com/akkadotnet/akka.net/releases/tag/1.4.47)
* [The `HoconAddMode` argument in `AddHocon()` and `AddHoconFile()` extension methods are not optional anymore](https://github.com/akkadotnet/Akka.Hosting/pull/135)
* [`ActorRegistry.Get&lt;T&gt;` will throw if no actor with key T has been registered with the `ActorRegistry`](https://github.com/akkadotnet/Akka.Hosting/pull/147)
* [Modularize and make Hosting.Persistence methods use the options pattern](https://github.com/akkadotnet/Akka.Hosting/pull/146)
* [Add lease support to singleton and sharding extensions](https://github.com/akkadotnet/Akka.Hosting/pull/150)
* [Fix bug in `AddActor` and `AddStartup` to make sure that they're executed in order](https://github.com/akkadotnet/Akka.Hosting/pull/155)
* [Add IConfiguration to HOCON adapter](https://github.com/akkadotnet/Akka.Hosting/pull/158)
* [Make `JournalOptions` and `SnapshotOptions` IConfigurable bindable](https://github.com/akkadotnet/Akka.Hosting/pull/161)
* [Make `RemoteOptions` IConfiguration bindable](https://github.com/akkadotnet/Akka.Hosting/pull/160)
* [Better integration with Akka.DependencyInjection](https://github.com/akkadotnet/Akka.Hosting/pull/169)
* [Add `WithActorAskTimeout` and extends debug logging options](https://github.com/akkadotnet/Akka.Hosting/pull/173)
* Updated NuGet package versions:
  * [Bump Akka.Persistence.PostgreSql from 1.4.45 to 1.4.46](https://github.com/akkadotnet/Akka.Hosting/pull/148)

**General Changes**

* Almost all options properties are changed to value types to allow direct binding to `Miscosoft.Extensions.Configuration` `IConfiguration` instance.
* Implements `Nullable` on all projects.

**Changes To `Akka.Hosting`**

* The `HoconAddMode` argument in `AddHocon()` and `AddHoconFile()` extension methods are not optional anymore; you will need to declare that your HOCON will to append, prepend, or replace existing HOCON configuration. In almost all cases, unless you're adding a default configuration, you only need to use `HoconAddMode.Prepend`.

* `ActorRegistry.Get&lt;T&gt;` will now throw an `ActorRegistryException` if no actor with key T has been registered with the `ActorRegistry`, this is done to make the API more consistent with all other .NET APIs. Use `ActorRegistry.TryGet&lt;T&gt;` if you do not want this behavior.

* Better integration with `Akka.DependencyInjection`. Documentation can be read [here](https://github.com/akkadotnet/Akka.Hosting/blob/dev/README.md#dependency-injection-outside-and-inside-akkanet)

* Added `WithActorAskTimeout()` extension method to configure the actor ask timeout settings.

* Added extended debug logging support for dead letters and actor messages and events.

* Adds a variation to `AddHocon` that can convert `Microsoft.Extensions.Configuration` `IConfiguration` into HOCON `Config` instance and adds it to the ActorSystem being configured.
  * All variable name are automatically converted to lower case.
  * All "." (period) in the `IConfiguration` key will be treated as a HOCON object key separator
  * For environment variable configuration provider:
    * "__" (double underline) will be converted to "." (period).
    * "_" (single underline) will be converted to "-" (dash).
    * If all keys are composed of integer parseable keys, the whole object is treated as an array
    
  Example:
  
  JSON configuration:
  ```json
  {
     "akka.cluster": {
         "roles": [ "front-end", "back-end" ],
         "min-nr-of-members": 3,
         "log-info": true
     }
  }
  ```
  
  and environment variables:
 
  ```powershell
  AKKA__CLUSTER__ROLES__0=front-end
  AKKA__CLUSTER__ROLES__1=back-end
  AKKA__CLUSTER__MIN_NR_OF_MEMBERS=3
  AKKA__CLUSTER__LOG_INFO=true
  ```
  
  is equivalent to HOCON configuration of:
 
  ```HOCON
  akka {
      cluster {
          roles: [ front-end, back-end ]
          min-nr-of-members: 3
          log-info: true
      }
  }
  ```

**Changes to `Akka.Persistence.Hosting`**

* You can now use option classes to configure persistence. Currently, the persistence plugins that supports this are `Akka.Persistence.PostgreSql.Hosting` and `Akka.Persistence.SqlServer.Hosting`. Support for other Akka.Hosting enabled plugins will be rolled out after this release.
* These option classes are modular: 
  * Multiple options can be registered with the builder using different names 
  * The same persistence plugin (e.g. PostgreSql) can be declared and registered multiple times using different names and configuration.
  * Options can be declared as the default persistence plugin and not.
  * Different or the same registered option can be used as the normal persistence options and `Akka.Cluster.Hosting` sharding extension methods.
  * There can only be one option declared as the default journal and one option declared as the default snapshot plugin. If multiple default plugin options are declared, only the last registered option will have an effect.
* Journal event adapters can now be composed directly using the `Adapters` property inside the journal option class.
* An example project can be seen [here](https://github.com/akkadotnet/Akka.Hosting/tree/dev/src/Examples/Akka.Hosting.CustomJournalIdDemo)

Example code:
```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAkka("MyActorSystem", configurationBuilder =>
{
    // Grab connection strings from appsettings.json
    var localConn = builder.Configuration.GetConnectionString("sqlServerLocal");
    var shardingConn = builder.Configuration.GetConnectionString("sqlServerSharding");

    // Custom journal options with the id "sharding"
    // The absolute id will be "akka.persistence.journal.sharding"
    var shardingJournalOptions = new SqlServerJournalOptions(
        isDefaultPlugin: false)
    {
        Identifier = "sharding",
        ConnectionString = shardingConn,
        AutoInitialize = true
    };
    
    // Custom snapshots options with the id "sharding"
    // The absolute id will be "akka.persistence.snapshot-store.sharding"
    var shardingSnapshotOptions = new SqlServerSnapshotOptions(
        isDefaultPlugin: false)
    {
        Identifier = "sharding",
        ConnectionString = shardingConn,
        AutoInitialize = true
    };
    
    configurationBuilder
        // Standard way to create a default persistence 
        // journal and snapshot
        .WithSqlServerPersistence(localConn)
        // This is a custom persistence setup using the options 
        // instances we've set up earlier.
        // Note that we are calling WithSqlServerPersistence()
        // twice, these two calls registers two different
        // persistence options with two different identifier names.
        .WithSqlServerPersistence(shardingJournalOptions, shardingSnapshotOptions) 
        .WithShardRegion<UserActionsEntity>(
            "userActions", 
            s => UserActionsEntity.Props(s),
            new UserMessageExtractor(),
            new ShardOptions
            {
                StateStoreMode = StateStoreMode.Persistence, 
                Role = "myRole",
                // Supplying sharding with separate persistence plugin options
                JournalOptions = shardingJournalOptions,
                SnapshotOptions = shardingSnapshotOptions
                // NOTE: you can supply the plugin id instead
                // JournalPluginId = shardingJournalOptions.PluginId,
                // SnapshotPluginId = shardingSnapshotOptions.PluginId 
            });
})
```

**Changes to `Akka.Cluster.Hosting`**

* `ClusterOptions` are expanded with more properties to make it more configurable. These new properties are:
  * `MinimumNumberOfMembers`: Allows you to set the minimum number of joined cluster members for a cluster to be considered to be in the `Up` state.
  * `MinimumNumberOfMembersPerRole`: Similar to ``MinimumNumberOfMembers`, but it is scoped to each cluster role.
  * `AppVersion`: Allows you to set the current cluster application version, useful for performing rolling update of the cluster members.
  * `LogInfo`: Enable info level logging of cluster events.
  * `LogInfoVerbose`: Enable a more verbose info level logging of cluster events, used for temporary troubleshooting.
  * `SplitBrainResolver`: The split brain resolver property is moved into the options class instead of being part of the extension method arguments.
* `ClusterSingletonOptions` and `ShardOptions` now have a `LeaseImplementation` property that can be used to configure leasing for cluster singleton and sharding. Currently, two lease plugins are supported: `Akka.Coordination.KubernetesApi` and `Akka.Coordination.Azure` by assigning `KubernetesLeaseOption.Instance` or `AzureLeaseOption.Instance` respectively to the property.

## [0.5.2-beta1] / 29 November 2022
* [Update Akka.NET from 1.4.45 to 1.4.46](https://github.com/akkadotnet/akka.net/releases/tag/1.4.46)
* [Remove default `HoconAddMode` value from `AddHocon` and `AddHoconFile`](https://github.com/akkadotnet/Akka.Hosting/pull/135)
* [First release of Akka.Hosting.TestKit NuGet package](https://github.com/akkadotnet/Akka.Hosting/pull/143)

In 0.5.2-beta1, the `HoconAddMode` argument in `AddHocon()` and `AddHoconFile()` extension methods are not optional anymore; you will need to declare that your HOCON will to append, prepend, or replace existing HOCON configuration. In almost all cases, unless you're adding a default configuration, you only need to use `HoconAddMode.Prepend`.

## [0.5.1] / 20 October 2022
* [Update Akka.NET from 1.4.41 to 1.4.45](https://github.com/akkadotnet/akka.net/releases/tag/1.4.45)

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
- [Hosting: Add `WithExtension&lt;T&gt;()` extension method](https://github.com/akkadotnet/Akka.Hosting/pull/97)
 
__WithExtension&lt;T&gt;()__

`AkkaConfigurationBuilder.WithExtension&lt;T&gt;()` works similarly to `AkkaConfigurationBuilder.WithExtensions()` and is used to configure the `akka.extensions` HOCON settings. The difference is that it is statically typed to only accept classes that extends the `IExtensionId` interface.

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
