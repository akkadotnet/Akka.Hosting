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
