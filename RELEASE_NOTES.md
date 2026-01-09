#### 1.5.58 January 9th 2026 ####

**Updates**
* [Bump Akka version from 1.5.57 to 1.5.58](https://github.com/akkadotnet/akka.net/releases/tag/1.5.58)

#### 1.5.57 December 16th 2025 ####

**New Features**
* [Add semantic logging support for Akka.NET 1.5.56+](https://github.com/akkadotnet/Akka.Hosting/pull/693) - enables Microsoft.Extensions.Logging to receive properly structured state dictionaries instead of pre-formatted strings. When using Akka.NET 1.5.56+, log messages now include structured properties from the semantic logging API along with Akka metadata (ActorPath, Timestamp, Thread, LogSource). Fully backwards compatible with older Akka.NET versions.

**Updates**
* [Bump Akka version from 1.5.55 to 1.5.57](https://github.com/akkadotnet/akka.net/releases/tag/1.5.57)

#### 1.5.55.1 October 27th 2025 ####

**Enhancements**
* [Expose options in journal and snapshot builders](https://github.com/akkadotnet/Akka.Hosting/pull/691) - resolved [issue #690](https://github.com/akkadotnet/Akka.Hosting/issues/690) by adding `Options` property to `AkkaPersistenceJournalBuilder` and `AkkaPersistenceSnapshotBuilder`. Extension methods can now access configuration details without requiring options as explicit parameters, eliminating redundant option passing for connectivity health checks and other plugin-specific features

#### 1.5.55 October 26th 2025 ####

**New Features**
* [Support custom health check registrations on Journal and Snapshot Builders](https://github.com/akkadotnet/Akka.Hosting/pull/683) - added API to support custom health check registrations for Akka.Persistence plugins, related to [issue #678](https://github.com/akkadotnet/Akka.Hosting/issues/678)
* [Add support for custom certificate validation callbacks](https://github.com/akkadotnet/Akka.Hosting/pull/686) - integrated the new `CertificateValidationCallback` feature from Akka.NET v1.5.55, allowing users to provide custom certificate validation logic for SSL/TLS connections. Enables advanced scenarios including certificate pinning, subject/issuer matching, custom business validation rules, and advanced mTLS scenarios

**Enhancements**
* [Add customizable tags parameter to health check methods](https://github.com/akkadotnet/Akka.Hosting/pull/681) - resolved [issue #679](https://github.com/akkadotnet/Akka.Hosting/issues/679) by adding new overload allowing custom tags for health checks while maintaining backward compatibility
* [Made it easier to customize failureState and tags for all health checks](https://github.com/akkadotnet/Akka.Hosting/pull/682) - simplified health check configuration API for all health checks

**Updates**
* [Bump Akka version from 1.5.53 to 1.5.55](https://github.com/akkadotnet/akka.net/releases/tag/1.5.55)

#### 1.5.55-beta1 October 26th 2025 ####

**New Features**
* [Support custom health check registrations on Journal and Snapshot Builders](https://github.com/akkadotnet/Akka.Hosting/pull/683) - added API to support custom health check registrations for Akka.Persistence plugins, related to [issue #678](https://github.com/akkadotnet/Akka.Hosting/issues/678)

**Enhancements**
* [Add customizable tags parameter to health check methods](https://github.com/akkadotnet/Akka.Hosting/pull/681) - resolved [issue #679](https://github.com/akkadotnet/Akka.Hosting/issues/679) by adding new overload allowing custom tags for health checks while maintaining backward compatibility
* [Made it easier to customize failureState and tags for all health checks](https://github.com/akkadotnet/Akka.Hosting/pull/682) - simplified health check configuration API for all health checks

**Updates**
* [Bump Akka version from 1.5.53 to 1.5.55](https://github.com/akkadotnet/akka.net/releases/tag/1.5.55)

#### 1.5.53 October 14th 2025 ####

**Bug Fixes**
* [Fix event adapter callback API not invoking adapters at runtime](https://github.com/akkadotnet/Akka.Hosting/pull/674) - resolved critical bug where event adapters configured via the callback API were not being invoked at runtime. This fix is especially important for users who have migrated to the callback pattern following the deprecation of `JournalOptions.Adapters` property. The issue was caused by unnecessary fallback configuration that interfered with adapter registration during HOCON merging.

**Updates**
* [Add SSL/TLS configuration settings from Akka.NET 1.5.52 and 1.5.53](https://github.com/akkadotnet/Akka.Hosting/pull/675) - updated SSL/TLS configuration options to support new features and settings introduced in Akka.NET versions 1.5.52 and 1.5.53
* [Bump Akka version from 1.5.52 to 1.5.53](https://github.com/akkadotnet/akka.net/releases/tag/1.5.53)

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