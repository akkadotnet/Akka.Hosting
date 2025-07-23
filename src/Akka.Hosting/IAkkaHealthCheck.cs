using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Akka.Hosting;

/// <summary>
/// The registration record used to track this Akka.HealthCheck inside the <see cref="AkkaConfigurationBuilder"/>
/// </summary>
public sealed class AkkaHealthCheckRegistration
{
    private string _name;
    private Func<ActorSystem, IAkkaHealthCheck>  _factory;
    
    /// <summary>
    /// Gets or sets the delay applied to the health check after the application starts up.
    ///
    /// THe delay is applied once at startup and does not apply to subsequent iterations.
    /// </summary>
    public TimeSpan? Delay { get; set; }
    
    /// <summary>
    /// Gets or sets the recurring period used for the check
    /// </summary>
    public TimeSpan? Period { get; set; }
    
    public ISet<string> Tags { get; }
    
    /// <summary>
    /// Gets or sets the healthcheck name.
    /// </summary>
    public string Name
    {
        get => _name;
        set => _name = value ?? throw new ArgumentNullException(nameof(value));
    }
    
    /// <summary>
    /// Gets or sets a delegate used to create the <see cref="IAkkaHealthCheck"/>
    /// </summary>
    /// <exception cref="ArgumentNullException"></exception>
    public Func<ActorSystem, IAkkaHealthCheck> Factory 
    {
        get => _factory;
        set => _factory = value ?? throw new ArgumentNullException(nameof(value));
    }
}

/// <summary>
/// Healthcheck aimed at testing the health of Akka.NET-specific resources.
/// </summary>
public interface IAkkaHealthCheck
{
    Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default);
}