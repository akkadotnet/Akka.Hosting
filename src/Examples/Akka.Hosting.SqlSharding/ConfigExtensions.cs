using Akka.Cluster.Sharding;
using Akka.Cluster.Tools.Singleton;

namespace Akka.Hosting.SqlSharding;

public static class ShardSettings
{
    public static ClusterShardingSettings Default()
    {
        var shardingConfig = ClusterSharding.DefaultConfig();
        var singletonConfig = shardingConfig.GetString("coordinator-singleton");
        return ClusterShardingSettings.Create(shardingConfig.
            GetConfig("akka.cluster.sharding"), shardingConfig.GetConfig(singletonConfig));
    }
}