// -----------------------------------------------------------------------
//  <copyright file="SnapshotOptions.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using System;
using System.Text;
using Akka.Configuration;
using Akka.Hosting;
using Microsoft.Extensions.Diagnostics.HealthChecks;

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
    
    /// <summary>
    /// Base class for all snapshot store options class. If you're writing an options class for SQL plugins, use
    /// <see cref="SqlSnapshotOptions"/> instead.
    /// </summary>
    public abstract class SnapshotOptions
    {
        protected SnapshotOptions(bool isDefault)
        {
            IsDefaultPlugin = isDefault;
        }
        
        public bool IsDefaultPlugin { get; set; }
        
        /// <summary>
        /// <b>NOTE</b> If you're implementing an option class for new Akka.Hosting persistence, you need to override
        /// this property and provide a default value. The default value have to be the default journal configuration
        /// identifier name (i.e. "sql-server" or "postgresql"
        /// </summary>
        public abstract string Identifier { get; set; }
        
        /// <summary>
        ///     <para>
        ///         Should corresponding snapshot store be initialized automatically, if applicable.
        ///     </para>
        ///     <b>Default</b>: <c>false</c>
        /// </summary>
        public bool AutoInitialize { get; set; } = false;
        
        /// <summary>
        ///     <para>
        ///         Default serializer used as manifest serializer when applicable and payload serializer when no
        ///         specific binding overrides are specified
        ///     </para>
        ///     <b>Default</b>: <c>null</c>
        /// </summary>
        public string? Serializer { get; set; }
        
        /// <summary>
        ///     <para>
        ///         The default configuration for this snapshot store. This must be the actual configuration block for this journal.
        ///     </para>
        ///     Example:
        ///     <code>
        ///         protected override Config InternalDefaultConfig = PostgreSqlPersistence.DefaultConfiguration()
        ///             .GetConfig("akka.persistence.snapshot-store.postgresql");
        ///     </code>
        /// </summary>
        protected abstract Config InternalDefaultConfig { get; }
        
        /// <summary>
        /// The default HOCON configuration for this specific journal configuration identifier, normalized to the
        /// plugin identifier name
        /// </summary>
        public Config DefaultConfig => InternalDefaultConfig.MoveTo(PluginId);

        public string PluginId => $"akka.persistence.snapshot-store.{Identifier}";
        
        /// <summary>
        /// The chain config builder.
        /// The top <see cref="SnapshotOptions.Build"/> caps the configuration with the outer `akka.persistence.snapshot-store.{id}` HOCON block.
        /// If you need to add more properties into this block, append them __BEFORE__ calling <c>base.Build()</c>.
        /// If you need to add more properties outside of this block, append them __AFTER__ calling <c>base.Build()</c>.
        /// </summary>
        /// <param name="sb"><see cref="StringBuilder"/> instance from lower chain</param>
        /// <returns>The fully built <see cref="StringBuilder"/> containing the `akka.persistence.snapshot-store.{id}` HOCON block.</returns>
        /// <exception cref="Exception">Thrown when <see cref="Identifier"/> is null, contains only whitespaces, or contains illegal characters</exception>
        protected virtual StringBuilder Build(StringBuilder sb)
        {
            if(string.IsNullOrWhiteSpace(Identifier))
                throw new Exception($"Invalid {GetType()}, {nameof(Identifier)} is null or whitespace");
            
            var illegalChars = Identifier.IsIllegalHoconKey();
            if (illegalChars.Length > 0)
            {
                throw new Exception($"Invalid {GetType()}, {nameof(Identifier)} contains illegal character(s) {string.Join(", ", illegalChars)}");
            }
            
            sb.Insert(0, $"{PluginId} {{{Environment.NewLine}");
            sb.AppendLine($"serializer = {Serializer.ToHocon()}");
            sb.AppendLine($"auto-initialize = {AutoInitialize.ToHocon()}");
            sb.AppendLine("}");
            
            if (IsDefaultPlugin)
                sb.AppendLine($"akka.persistence.snapshot-store.plugin = {PluginId}");
            
            return sb;
        }
        
        /// <summary>
        /// Transforms the snapshot store options class into a HOCON <see cref="Config"/> instance 
        /// </summary>
        /// <returns>The <see cref="Config"/> equivalence of this options instance</returns>
        public Config ToConfig()
            => ToString();
        
        /// <summary>
        /// Transforms the snapshot store options class into a HOCON string
        /// </summary>
        /// <returns>The HOCON string representation of this options instance</returns>
        public sealed override string ToString()
            => Build(new StringBuilder()).ToString();
    }
}