## [0.1.2] / 31 March 2022
- Removed all `Cluster.Sharding` methods that rely on `ClusterShardingSettings`, since it's not practical to create those prior to starting the `ActorSystem`.
