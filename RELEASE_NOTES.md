#### 1.5.52 October 9th 2025 ####

**API Changes**
* [Deprecate JournalOptions.Adapters property in favor of callback API](https://github.com/akkadotnet/Akka.Hosting/pull/669) - resolved [issue #665](https://github.com/akkadotnet/Akka.Hosting/issues/665) by deprecating the `JournalOptions.Adapters` property. Users should migrate to the unified callback pattern: `builder.WithJournal(options, journal => journal.AddWriteEventAdapter<T>(...))`. The deprecated property will be removed in v1.6.0.

**Updates**
* [Bump Akka version from 1.5.51 to 1.5.52](https://github.com/akkadotnet/akka.net/releases/tag/1.5.52)

#### 1.5.51.1 October 2nd 2025 ####

**Bug Fixes**
* [Fix journal health check registration without event adapters](https://github.com/akkadotnet/Akka.Hosting/pull/667) - resolved [issue #666](https://github.com/akkadotnet/Akka.Hosting/issues/666) where journal health checks were not being registered when using `.WithHealthCheck()` without adding event adapters

#### 1.5.51 October 1st 2025 ####

**New Features**
* [Added Akka.Persistence health checks](https://github.com/akkadotnet/Akka.Hosting/pull/662) - health check support for Akka.Persistence journal and snapshot stores with unified configuration API
* [Added dependency-injected health checks](https://github.com/akkadotnet/Akka.Hosting/pull/659) - `WithHealthCheck<T>()` generic methods for DI-resolved health checks

**Updates**
* [Bump Akka version from 1.5.50 to 1.5.51](https://github.com/akkadotnet/akka.net/releases/tag/1.5.51)