using System.Collections.Generic;
using System.Linq;
using Akka.Hosting;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Akka.Persistence.Hosting;

/// <summary>
/// Used to help build snapshot store configurations
/// </summary>
public sealed class AkkaPersistenceSnapshotBuilder
{
    internal readonly string SnapshotStoreId;
    internal readonly AkkaConfigurationBuilder Builder;
    internal AkkaHealthCheckRegistration? HealthCheckRegistration = null;

    public AkkaPersistenceSnapshotBuilder(string snapshotStoreId, AkkaConfigurationBuilder builder)
    {
        SnapshotStoreId = snapshotStoreId;
        Builder = builder;
    }

    /// <summary>
    /// Uses the built-in snapshot store health check on the Akka.Persistence.SnapshotStore.
    /// </summary>
    /// <param name="unHealthyStatus">Default status to return when the plugin reports <see cref="PersistenceHealthStatus.Unhealthy"/>
    /// or <see cref="PersistenceHealthStatus.Degraded"/>. Defaults to degraded.</param>
    /// <param name="name">Optional name to add to the health check.</param>
    /// <param name="tags">Custom tags for the health check. If null, defaults to ["akka", "persistence", "snapshot-store"].</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public AkkaPersistenceSnapshotBuilder WithHealthCheck(HealthStatus unHealthyStatus = HealthStatus.Degraded,
        string? name = null,
        IEnumerable<string>? tags = null)
    {
        var registration = AddHealthCheck(name, unHealthyStatus, tags);
        HealthCheckRegistration = registration;
        return this;
    }

    /// <summary>
    /// Backward-compatible overload for external plugins that use the 2-parameter version.
    /// </summary>
    internal AkkaHealthCheckRegistration AddHealthCheck(string? name, HealthStatus unHealthyStatus)
    {
        return AddHealthCheck(name, unHealthyStatus, tags: null);
    }

    internal AkkaHealthCheckRegistration AddHealthCheck(string? name, HealthStatus unHealthyStatus, IEnumerable<string>? tags)
    {
        var pluginId = $"akka.persistence.snapshot-store.{SnapshotStoreId}";
        var healthCheckTags = tags?.ToList() ?? new List<string> { "akka", "persistence", "snapshot-store" };
        var registration = new AkkaHealthCheckRegistration(
            name ?? $"Akka.Persistence.SnapshotStore.{SnapshotStoreId}",
            new SnapshotStoreHealthCheck(pluginId),
            unHealthyStatus,
            healthCheckTags);
        return registration;
    }

    /// <summary>
    /// INTERNAL API - Registers health checks if configured.
    /// </summary>
    internal void Build()
    {
        // add the health checks if specified
        if(HealthCheckRegistration != null)
            Builder.WithHealthCheck(HealthCheckRegistration);
    }
}