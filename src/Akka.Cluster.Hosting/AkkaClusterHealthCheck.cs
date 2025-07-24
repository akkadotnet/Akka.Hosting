using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Akka.Hosting;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Akka.Cluster.Hosting;

internal static class ClusterHealthCheckHelpers
{
    public static IReadOnlyDictionary<string, object> DumpClusterState(this ClusterEvent.CurrentClusterState state)
    {
        return
        [
            new KeyValuePair<string, object>("cluster.members", state.Members.Count),
            new KeyValuePair<string, object>("cluster.unreachable", state.Unreachable.Count),
            new KeyValuePair<string, object>("cluster.leader", state.Leader.ToString())
        ];
    }
}

/// <summary>
/// A programmable health check for Akka.Cluster. Make sure you read the documentation carefully.
///
/// You often will not need this - Akka.NET's Split Brain Resolvers (see https://getakka.net/articles/clustering/split-brain-resolver.html)
/// typically handle most scenarios that require intervention automatically; they're enabled by default; and they will automatically
/// kill unhealthy Akka.NET processes in order to ensure that there is a single, unified Akka.NET cluster afterwards.
///
/// This component is useful for scenarios where you want to trigger node-death when your cluster is missing enough
/// nodes to fulfill its duties and can't really function anyway.
/// </summary>
public sealed class AkkaClusterHealthCheck : IAkkaHealthCheck
{
    /// <summary>
    /// The default value for <see cref="TriggerHealthCheckFailureThreshold"/>. 20 seconds.
    /// </summary>
    public static readonly TimeSpan DefaultFailureEvaluationThreshold = TimeSpan.FromSeconds(20);

    private bool _weHaveJoined;
    private bool _evaluationConditionSatisfied;
    private DateTime? _failureInitiallyTriggered;

    internal bool PassedJoinCondition => !DontEvaluateUntilWeHaveJoined || _weHaveJoined;
    internal bool PassedEvalCondition => _evaluationConditionSatisfied;
    internal bool CanEvaluateFailure => PassedEvalCondition && PassedJoinCondition;

    internal bool ShouldTriggerFailure(DateTime now) => _failureInitiallyTriggered != null &&
                                                        (now - _failureInitiallyTriggered) >=
                                                        TriggerHealthCheckFailureThreshold;

    /// <summary>
    /// Creates a new <see cref="AkkaClusterHealthCheck"/> configuration.
    /// </summary>
    /// <param name="unhealthyWhenTrue">The predicate function that RETURNS TRUE when the cluster is NOT HEALTHY.</param>
    public AkkaClusterHealthCheck(Predicate<ClusterEvent.CurrentClusterState> unhealthyWhenTrue)
        : this(unhealthyWhenTrue, DefaultFailureEvaluationThreshold, true, null)
    {
    }

    /// <summary>
    /// Creates a new <see cref="AkkaClusterHealthCheck"/> configuration.
    /// </summary>
    /// <param name="unhealthyWhenTrue">The predicate function that RETURNS TRUE when the cluster is NOT HEALTHY.</param>
    /// <param name="triggerHealthCheckFailureThreshold">Failure checking threshold - the <see cref="UnhealthyWhenTrue"/> function needs to be true for this amount of time.</param>
    /// <param name="dontEvaluateUntilWeHaveJoined">Don't start evaluating failure conditions until we join the cluster.</param>
    /// <param name="dontEvaluateUntil">Optional. Don't start evaluating failure conditions until this pre-condition is met.</param>
    public AkkaClusterHealthCheck(Predicate<ClusterEvent.CurrentClusterState> unhealthyWhenTrue,
        TimeSpan triggerHealthCheckFailureThreshold, bool dontEvaluateUntilWeHaveJoined,
        Predicate<ClusterEvent.CurrentClusterState>? dontEvaluateUntil)
    {
        // check the range on the triggerHealthCheckFailureThreshold
        if (triggerHealthCheckFailureThreshold <= TimeSpan.FromSeconds(1))
            throw new ArgumentOutOfRangeException(nameof(triggerHealthCheckFailureThreshold),
                "For your own good, you really need a failure threshold value greater than 1s. 10s or 20s would be much better.");

        if (triggerHealthCheckFailureThreshold == Timeout.InfiniteTimeSpan)
            throw new ArgumentOutOfRangeException(nameof(triggerHealthCheckFailureThreshold),
                "Cannot set an infinite value. Pick something reasonable.");

        UnhealthyWhenTrue = unhealthyWhenTrue;
        TriggerHealthCheckFailureThreshold = triggerHealthCheckFailureThreshold;

        DontEvaluateUntilWeHaveJoined = dontEvaluateUntilWeHaveJoined;
        DontEvaluateUntilTrue = dontEvaluateUntil ?? (_ => true);
    }

    /// <summary>
    /// When set to <c>true</c>, prevents this check from even being evaluated until after we've successfully
    /// joined the cluster (generally, a good idea to leave on.)
    /// </summary>
    /// <remarks>
    /// Defaults to <c>true</c>.
    /// </remarks>
    public bool DontEvaluateUntilWeHaveJoined { get; set; }

    /// <summary>
    /// Optional.
    /// 
    /// When set, we can't begin evaluating this health check until certain conditions within the cluster are met.
    ///
    /// Minimum number of members, members of a certain role type, etc.
    /// </summary>
    public Predicate<ClusterEvent.CurrentClusterState> DontEvaluateUntilTrue { get; set; }

    /// <summary>
    /// Predicate function for determining when the cluster isn't healthy.
    /// </summary>
    public Predicate<ClusterEvent.CurrentClusterState> UnhealthyWhenTrue { get; }

    /// <summary>
    /// Immediately triggering a health check failure the instant a problem is detected within
    /// an Akka.NET cluster is a truly awful idea - it will render your system's partition tolerance
    /// to zero and make the entire system overreact at a massive scale if blips or other small,
    /// inevitable, easy-to-overcome network problems occur.
    ///
    /// SET THIS VALUE TO SOMETHING REASONABLE. 20s is the default value. It's a good default. Be
    /// very, very careful going any lower than that.
    /// </summary>
    public TimeSpan TriggerHealthCheckFailureThreshold { get; }

    public Task<HealthCheckResult> CheckHealthAsync(AkkaHealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var cluster = Cluster.Get(context.ActorSystem);

        if (!_weHaveJoined)
        {
            // satisfy "ARE WE UP?" criteria
            var selfMember = cluster.SelfMember;
            _weHaveJoined = selfMember.Status > MemberStatus.Joining;
        }

        if (!_evaluationConditionSatisfied)
        {
            // evaluate any pre-conditions (this only has to happen once)
            _evaluationConditionSatisfied = DontEvaluateUntilTrue(cluster.State);
        }


        if (!CanEvaluateFailure) return Task.FromResult(HealthCheckResult.Healthy());

        var areInFailure = UnhealthyWhenTrue(cluster.State);
        switch (areInFailure, _failureInitiallyTriggered)
        {
            case (false, not null):
                // we were in failure; now we are not
                _failureInitiallyTriggered = null; // clear the status
                break;
            case (true, null):
                // first time entering failure
                _failureInitiallyTriggered = DateTime.UtcNow;
                break;
            case (true, not null) when ShouldTriggerFailure(DateTime.UtcNow):
                // time to signal failure publicly, which might have repercussions
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    $"Cluster has been unhealthy for [{DateTime.UtcNow - _failureInitiallyTriggered:g}]",
                    data: cluster.State.DumpClusterState()));
        }

        // don't know enough
        return Task.FromResult(HealthCheckResult.Healthy(data: cluster.State.DumpClusterState()));
    }
}

/// <summary>
/// Checks to see if we've joined a cluster.
/// </summary>
internal sealed class AkkaClusterReadinessCheck : IAkkaHealthCheck
{
    /// <summary>
    /// Have we successfully joined the cluster?
    /// </summary>
    public bool WeHaveJoined { get; private set; }

    public DateTime BeganJoining { get; } = DateTime.UtcNow;

    public DateTime? FinishedJoining { get; private set; }

    public HealthCheckResult HealthyResult(DateTime finishedJoining) => HealthCheckResult.Healthy(
        $"Successfully joined Akka.NET cluster after [{finishedJoining - BeganJoining:g}].");

    public HealthCheckResult UnhealthyResult(DateTime now) =>
        HealthCheckResult.Unhealthy($"Have not yet joined Akka.NET cluster [{now - BeganJoining:g}] elapsed");

    public Task<HealthCheckResult> CheckHealthAsync(AkkaHealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (WeHaveJoined && FinishedJoining != null)
            return Task.FromResult(HealthyResult(FinishedJoining.Value));

        var cluster = Cluster.Get(context.ActorSystem);
        WeHaveJoined = cluster.SelfMember.Status > MemberStatus.Joining;

        if (WeHaveJoined)
        {
            FinishedJoining = DateTime.UtcNow;
            return Task.FromResult(HealthyResult(FinishedJoining.Value));
        }

        return Task.FromResult(UnhealthyResult(DateTime.UtcNow));
    }
}