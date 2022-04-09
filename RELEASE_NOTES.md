## [0.2.0] / 09 April 2022
- [Bugfix: Fixed issues with duplicate `IServiceProvider` registration](https://github.com/akkadotnet/Akka.Hosting/pull/32), this could cause multiple instances of dependencies to be instantiated since Akka.Hosting creating multiple `IServiceProvider` instances during the construction process. This has been resolved.
