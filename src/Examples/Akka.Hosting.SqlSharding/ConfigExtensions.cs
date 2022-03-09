using Akka.Cluster.Sharding;
using Akka.Cluster.Tools.Singleton;

namespace Akka.Hosting.SqlSharding;

public static class ShardSettings
{
    public static ClusterShardingSettings Default()
    {
        return ClusterShardingSettings.Create(ClusterSharding.DefaultConfig(), ClusterSingletonManager.DefaultConfig());
    }
}