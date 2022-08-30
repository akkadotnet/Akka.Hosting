// -----------------------------------------------------------------------
//  <copyright file="SplitBrainResolverOption.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using System;
using Akka.Annotations;
using Akka.Coordination;

namespace Akka.Cluster.Hosting.SBR
{
    public abstract class SplitBrainResolverOption
    {
        public static readonly SplitBrainResolverOption Default = new KeepMajorityOption();
        
        /// <summary>
        /// if the <see cref="Role"/> is defined the decision is based only on members with that <see cref="Role"/>
        /// </summary>
        public string Role { get; set; }
    }

    /// <summary>
    /// <para>
    /// Down the unreachable nodes if the number of remaining nodes are greater than or equal to the given
    /// <see cref="QuorumSize"/>. Otherwise down the reachable nodes, i.e. it will shut down that side of the partition.
    /// In other words, the <see cref="QuorumSize"/> defines the minimum number of nodes that the cluster must have
    /// to be operational. If there are unreachable nodes when starting up the cluster, before reaching this limit,
    /// the cluster may shutdown itself immediately. This is not an issue if you start all nodes at approximately
    /// the same time.
    /// </para>
    /// <para>
    /// Note that you must not add more members to the cluster than '<see cref="QuorumSize"/> * 2 - 1', because then
    /// both sides may down each other and thereby form two separate clusters. For example, <see cref="QuorumSize"/>
    /// configured to 3 in a 6 node cluster may result in a split where each side consists of 3 nodes each,
    /// i.e. each side thinks it has enough nodes to continue by itself. A warning is logged if this recommendation is violated.
    /// </para>
    /// </summary>
    public sealed class StaticQuorumOption : SplitBrainResolverOption
    {
        /// <summary>
        /// Minimum number of nodes that the cluster must have
        /// </summary>
        public int QuorumSize { get; set; } = 0;
    }

    /// <summary>
    /// Down the unreachable nodes if the current node is in the majority part based the last known membership
    /// information. Otherwise down the reachable nodes, i.e. the own part. If the the parts are of equal size the part
    /// containing the node with the lowest address is kept.
    /// Note that if there are more than two partitions and none is in majority each part will shutdown itself,
    /// terminating the whole cluster.
    /// </summary>
    public sealed class KeepMajorityOption : SplitBrainResolverOption
    {
    }

    /// <summary>
    /// <para>
    /// Down the part that does not contain the oldest member (current singleton).
    /// </para>
    /// When <see cref="DownIfAlone"/> is <c>true</c>:
    /// <list type="bullet">
    ///   <item>If the oldest node crashes the others will remove it from the cluster.</item>
    ///   <item>If oldest node is partitioned from all other nodes, the oldest will down itself and keep all other nodes running.</item>
    ///   <item>The strategy will not down the single oldest node when it is the only remaining node in the cluster.</item>
    /// </list>
    /// When <see cref="DownIfAlone"/> is <c>false</c> and the oldest node crashes, all other nodes will down themselves,
    /// i.e. shutdown the whole cluster together with the oldest node.
    /// </summary>
    public sealed class KeepOldestOption : SplitBrainResolverOption
    {
        /// <summary>
        /// Enable downing of the oldest node when it is partitioned from all other nodes
        /// </summary>
        public bool DownIfAlone { get; set; } = true;
    }

    /// <summary>
    /// Keep the part that can acquire the lease, and down the other part.
    /// Best effort is to keep the side that has most nodes, i.e. the majority side.
    /// This is achieved by adding a delay before trying to acquire the lease on the
    /// minority side.
    /// </summary>
    public sealed class LeaseMajorityOption : SplitBrainResolverOption
    {
        /// <summary>
        /// The absolute HOCON path to the <see cref="Lease"/> implementation HOCON configuration block
        /// </summary>
        public string LeaseImplementation { get; set; }
        
        /// <summary>
        /// <para>The name of the lease.</para>
        /// 
        /// The recommended format for the lease name is "{service-name}-akka-sbr".
        /// When lease-name is not defined, the name will be set to "{actor-system-name}-akka-sbr"
        /// </summary>
        public string LeaseName { get; set; }
    }
}