using Xunit.Abstractions;

namespace Akka.Cluster.Hosting.Tests;

public class ClusterShardingSpecs
{
    public ClusterShardingSpecs(ITestOutputHelper output)
    {
        Output = output;
    }

    public ITestOutputHelper Output { get; }
}