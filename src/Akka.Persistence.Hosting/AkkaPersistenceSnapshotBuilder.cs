using Akka.Hosting;
using Microsoft.Extensions.Diagnostics.HealthChecks;

#nullable enable
namespace Akka.Persistence.Hosting
{
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
        /// <returns>The current builder instance for method chaining.</returns>
        public AkkaPersistenceSnapshotBuilder WithHealthCheck(HealthStatus unHealthyStatus = HealthStatus.Degraded,
            string? name = null)
        {
            var registration = AddHealthCheck(name, unHealthyStatus);
            HealthCheckRegistration = registration;
            return this;
        }

        private AkkaHealthCheckRegistration AddHealthCheck(string? name, HealthStatus unHealthyStatus)
        {
            var registration = new AkkaHealthCheckRegistration(
                name ?? $"Akka.Persistence.SnapshotStore.{SnapshotStoreId}",
                new SnapshotStoreHealthCheck(SnapshotStoreId),
                unHealthyStatus,
                ["akka", "persistence", "snapshot-store"]);
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
}
