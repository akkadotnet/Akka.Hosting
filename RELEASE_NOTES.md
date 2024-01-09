## [1.5.14] / 09 January 2024

* [Update Akka.NET to 1.5.14](https://github.com/akkadotnet/akka.net/releases/tag/1.5.14)
* [Akka.Cluster.Hosting: don't use sharding delegates](https://github.com/akkadotnet/Akka.Hosting/pull/424)
* [Akka.Hosting.TestKit: Add method to configure `IHostBuilder`](https://github.com/akkadotnet/Akka.Hosting/pull/423)

## [1.5.12.1] / 31 August 2023

* [Add IConfiguration to HOCON adapter key name normalization toggle flag](https://github.com/akkadotnet/Akka.Hosting/pull/365)
* [Expand Cluster.Hosting and Remote.Hosting options](https://github.com/akkadotnet/Akka.Hosting/pull/366)

You can now specify whether IConfiguration key strings should be normalized to lower case or not when they are being converted into HOCON keys. You can read the documentation [here](https://github.com/akkadotnet/Akka.Hosting/#special-characters-and-case-sensitivity)

## [1.5.12] / 3 August 2023

* [Update Akka.NET to 1.5.12](https://github.com/akkadotnet/akka.net/releases/tag/1.5.12)
* [TestKit: Fix missing actor context on test start](https://github.com/akkadotnet/Akka.Hosting/pull/346)
* [Remote: Add SSL settings into RemoteOptions](https://github.com/akkadotnet/Akka.Hosting/pull/345)

## [1.5.8.1] / 12 July 2023

* [[Akka.Cluster.Hosting] Fix missing ClusterClient default HOCON configuration](https://github.com/akkadotnet/Akka.Hosting/pull/337)

## [1.5.8] / 21 June 2023

* [Update Akka.NET to 1.5.8](https://github.com/akkadotnet/akka.net/releases/tag/1.5.8)
* [[Akka.Cluster.Hosting] PassivateEntityAfter should not override HOCON settings](https://github.com/akkadotnet/Akka.Hosting/pull/318)
* [[Akka.Hosting] Set application exit code to -1 if CoordinatedShutdown was caused by cluster down or leave](https://github.com/akkadotnet/Akka.Hosting/pull/329)

## [1.5.7] / 23 May 2023

* [Update Akka.NET to 1.5.7](https://github.com/akkadotnet/akka.net/releases/tag/1.5.7)

## [1.5.6.1] / 17 May 2023

* [Akka.Hosting now throws `PlatformNotSupportedException`](https://github.com/akkadotnet/Akka.Hosting/pull/293) when attempting to run on Maui, due to https://github.com/dotnet/maui/issues/2244. Maui support will be added in https://github.com/akkadotnet/Akka.Hosting.Maui
* [make `AkkaHostedService` `public` + `virtual` so it can be extended and customized](https://github.com/akkadotnet/Akka.Hosting/pull/306) - advanced feature.

## [1.5.6] / 10 May 2023

* [Update Akka.NET to 1.5.6](https://github.com/akkadotnet/akka.net/releases/tag/1.5.6)

## [1.5.5] / 4 May 2023

* [Update Akka.NET to 1.5.5](https://github.com/akkadotnet/akka.net/releases/tag/1.5.5)

## [1.5.4.1] / 1 May 2023

* [Add ShardOptions.PassivateIdleEntityAfter option property](https://github.com/akkadotnet/Akka.Hosting/pull/290)

## [1.5.4] / 24 April 2023

* [Update Akka.NET to 1.5.4](https://github.com/akkadotnet/akka.net/releases/tag/1.5.4)
* Update Akka.Persistence.SqlServer to 1.5.4
* Update Akka.Persistence.PostgreSql to 1.5.4

## [1.5.3] / 24 April 2023

* [Update Akka.NET to 1.5.3](https://github.com/akkadotnet/akka.net/releases/tag/1.5.3)
* [Add SQL transaction isolation level support](https://github.com/akkadotnet/Akka.Hosting/pull/284)

**SQL Transaction Isolation Level Setting**

In 1.5.3, we're introducing fine-grained control over transaction isolation level inside the `Akka.Persistence.Hosting`, `Akka.Persistence.SqlServer.Hosting`, and `Akka.Persistence.PostgreSql.Hosting` plugins.

you can go to the [official Microsoft documentation](https://learn.microsoft.com/en-us/dotnet/api/system.data.isolationlevel?#fields) to read more about these isolation level settings.

## [1.5.2] / 6 April 2023

* [Update Akka.NET to 1.5.2](https://github.com/akkadotnet/akka.net/releases/tag/1.5.2)
* [Bump Akka.Persistence.PostgreSql to 1.5.2](https://github.com/akkadotnet/Akka.Hosting/pull/276)
* [Bump Akka.Persistence.SqlServer to 1.5.2](https://github.com/akkadotnet/Akka.Hosting/pull/276)
* [Persistence: Change persistence default serializer from json to null](https://github.com/akkadotnet/Akka.Hosting/pull/274)

**Major Changes**
* Persistence.Hosting default value for default serializer has been changed from `json` to `null`. This only affects persistence write and not reads. All objects with no serializer mapping will now be serialized using Akka default object serializer. If you have old persistence data where they do not have their serializer ID column populated, you will need to change this setting.

**Inherited Changes From Akka.NET Core**
* Persistence will now ignore the `serializer` setting inside journal settings when writing new events. This setting will only be used when data retrieved from the database does not have the serializer id column unpopulated.
* Cluster.Hosting, by default, will have `SplitBrainResolver` enabled.

## [1.5.1.1] / 4 April 2023

* [Fix missing default DData configuration in cluster hosting extension](https://github.com/akkadotnet/Akka.Hosting/pull/272)

## [1.5.1] / 16 March 2023

* [Update Akka.NET to 1.5.1](https://github.com/akkadotnet/akka.net/releases/tag/1.5.1)
* [Bump Akka.Persistence.PostgreSql to 1.5.1](https://github.com/akkadotnet/Akka.Hosting/pull/268)
* [Bump Akka.Persistence.SqlServer to 1.5.1](https://github.com/akkadotnet/Akka.Hosting/pull/268)
* [Add GetAsync method to `IRequiredActor` to resolve IActorRef in background services](https://github.com/akkadotnet/Akka.Hosting/pull/264)
* [Add cluster Distributed Data options support for Akka.Cluster.Hosting extensions](https://github.com/akkadotnet/Akka.Hosting/pull/263)

## [1.5.0] / 2 March 2023

Version 1.5.0 is the RTM release of Akka.Hosting and Akka.NET v1.5.0 RTM integration.

Full changes 
* [Update Akka.NET to 1.5.0](https://github.com/akkadotnet/akka.net/releases/tag/1.5.0)
* [Fix missing cluster configuration on certain edge cases](https://github.com/akkadotnet/Akka.Hosting/pull/214)
* [Add new Cluster.Sharding RememberEntitiesStore](https://github.com/akkadotnet/Akka.Hosting/pull/224)
* [Add Cluster.Sharding journal migration adapter convenience method](https://github.com/akkadotnet/Akka.Hosting/pull/226)
* [Add LogMessageFormatter formatter support to logging](https://github.com/akkadotnet/Akka.Hosting/pull/248)

#### Upgrading From v1.4 To v1.5

As noted in [the upgrade advisories](https://getakka.net/community/whats-new/akkadotnet-v1.5-upgrade-advisories.html), there was some major changes inside Akka.NET that directly affects Akka.Hosting. To upgrade from v1.4 to v1.5, please watch our video [here](https://www.youtube.com/watch?v=-UPestlIw4k) covering this process.
