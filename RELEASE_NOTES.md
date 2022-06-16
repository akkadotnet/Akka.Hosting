## [0.3.3] / 16 June 2022
- [Added common `Akka.Persistence.Hosting` package to make it easier to add `IEventAdapter`s to journals](https://github.com/akkadotnet/Akka.Hosting/issues/64).
- [Made Akka.Persistence.SqlServer.Hosting and Akka.Persistence.PostgreSql.Hosting both take a shared overload / dependency on Akka.Persistence.Hosting](https://github.com/akkadotnet/Akka.Hosting/pull/67) - did this to make it easier to add `IEventAdapter`s to each of those.
- [Add Akka.Cluster.Tools.Client support](https://github.com/akkadotnet/Akka.Hosting/pull/66) - now possible to start `ClusterClient` and `ClusterClientReceptionist`s easily from Akka.Hosting.
