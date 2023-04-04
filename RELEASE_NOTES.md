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
