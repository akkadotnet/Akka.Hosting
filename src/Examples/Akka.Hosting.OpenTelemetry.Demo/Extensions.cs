// -----------------------------------------------------------------------
//  <copyright file="Extensions.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2024 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using OpenTelemetry.Trace;

namespace Akka.Hosting.OpenTelemetry.Demo;

/// <summary>
/// Extension methods for configuring Aspire service defaults.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Adds Aspire service defaults including OpenTelemetry and health checks.
    /// </summary>
    public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
    {
        // Add service discovery
        builder.Services.AddServiceDiscovery();

        // Configure OpenTelemetry tracing
        builder.Services.AddOpenTelemetry()
            .WithTracing(tracing =>
            {
                tracing.AddSource("Akka.Hosting.OpenTelemetry.Demo");
            });

        // Add health checks
        builder.Services.AddHealthChecks();

        return builder;
    }
}
