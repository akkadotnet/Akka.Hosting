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
/// <remarks>
/// These will all get converted into <see cref="HealthCheckRegistration"/>s by the <see cref="AkkaConfigurationBuilder"/>,
/// and a default "akka.net" tag will be added to each of those registrations for filtering purposes.
/// </remarks>
public sealed class AkkaHealthCheckRegistration
{
    private string _name;
    private Func<ActorSystem, IAkkaHealthCheck>  _factory;
    private TimeSpan _timeout;

    /// <summary>
    /// Creates a new <see cref="AkkaHealthCheckRegistration"/> for an existing <see cref="IAkkaHealthCheck"/>
    /// </summary>
    /// <param name="name">The healthcheck name.</param>
    /// <param name="instance">The <see cref="IAkkaHealthCheck"/> instance.</param>
    /// <param name="failureStatus">
    /// The <see cref="HealthStatus"/> that should be reported upon failure of the health check. If the provided value
    /// is <c>null</c>, then <see cref="HealthStatus.Unhealthy"/> will be reported.
    /// </param>
    /// <param name="tags">A list of tags that can be used for filtering health checks.</param>
    public AkkaHealthCheckRegistration(string name, IAkkaHealthCheck instance, HealthStatus? failureStatus,
        IEnumerable<string>? tags)
        : this(name, instance, failureStatus, tags, default)
    {
        
    }

    /// <summary>
    /// Creates a new <see cref="AkkaHealthCheckRegistration"/> for an existing <see cref="IAkkaHealthCheck"/>
    /// </summary>
    /// <param name="name">The healthcheck name.</param>
    /// <param name="instance">The <see cref="IAkkaHealthCheck"/> instance.</param>
    /// <param name="failureStatus">
    /// The <see cref="HealthStatus"/> that should be reported upon failure of the health check. If the provided value
    /// is <c>null</c>, then <see cref="HealthStatus.Unhealthy"/> will be reported.
    /// </param>
    /// <param name="tags">A list of tags that can be used for filtering health checks.</param>
    /// <param name="timeout">An optional <see cref="TimeSpan"/> representing the timeout of the check.</param>
    /// <exception cref="ArgumentNullException">Thrown if <see cref="name"/> or <see cref="instance"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if a negative timeout other than <see cref="System.Threading.Timeout.InfiniteTimeSpan"/> is used.</exception>
    public AkkaHealthCheckRegistration(string name, IAkkaHealthCheck instance, HealthStatus? failureStatus,
        IEnumerable<string>? tags, TimeSpan? timeout)
    {
        if(name == null)  throw new ArgumentNullException(nameof(name));
        if(instance == null) throw new ArgumentNullException(nameof(instance));

        if (timeout <= TimeSpan.Zero && timeout != System.Threading.Timeout.InfiniteTimeSpan)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout));
        }
        
        _name = name;
        FailureStatus = failureStatus ?? HealthStatus.Unhealthy;
        Tags = new HashSet<string>(tags ?? [], StringComparer.OrdinalIgnoreCase);
        _factory =  (_) => instance;
        Timeout = timeout ?? System.Threading.Timeout.InfiniteTimeSpan;
    }
    
    /// <summary>
    /// Creates a new <see cref="AkkaHealthCheckRegistration"/> for an existing <see cref="IAkkaHealthCheck"/>
    /// </summary>
    /// <param name="name">The healthcheck name.</param>
    /// <param name="factory">A delegate that produces an <see cref="IAkkaHealthCheck"/> instance from the <see cref="ActorSystem"/>.</param>
    /// <param name="failureStatus">
    /// The <see cref="HealthStatus"/> that should be reported upon failure of the health check. If the provided value
    /// is <c>null</c>, then <see cref="HealthStatus.Unhealthy"/> will be reported.
    /// </param>
    /// <param name="tags">A list of tags that can be used for filtering health checks.</param>
    /// <param name="timeout">An optional <see cref="TimeSpan"/> representing the timeout of the check.</param>
    /// <exception cref="ArgumentNullException">Thrown if <see cref="name"/> or <see cref="factory"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if a negative timeout other than <see cref="System.Threading.Timeout.InfiniteTimeSpan"/> is used.</exception>
    public AkkaHealthCheckRegistration(string name, Func<ActorSystem, IAkkaHealthCheck> factory, HealthStatus? failureStatus,
        IEnumerable<string>? tags, TimeSpan? timeout)
    {
        if(name == null)  throw new ArgumentNullException(nameof(name));
        if(factory == null) throw new ArgumentNullException(nameof(factory));

        if (timeout <= TimeSpan.Zero && timeout != System.Threading.Timeout.InfiniteTimeSpan)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout));
        }
        
        _name = name;
        FailureStatus = failureStatus ?? HealthStatus.Unhealthy;
        Tags = new HashSet<string>(tags ?? [], StringComparer.OrdinalIgnoreCase);
        _factory = factory;
        Timeout = timeout ?? System.Threading.Timeout.InfiniteTimeSpan;
    }
    
    /// <summary>
    /// Gets or sets the delay applied to the health check after the application starts up.
    ///
    /// The delay is applied once at startup and does not apply to subsequent iterations.
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
    
    /// <summary>
    /// Gets or sets the <see cref="HealthStatus"/> that should be reported upon failure of the health check.
    /// </summary>
    public HealthStatus FailureStatus { get; set; }

    /// <summary>
    /// Gets or sets the timeout used for the test.
    /// </summary>
    public TimeSpan Timeout
    {
        get => _timeout;
        set
        {
            if (value <= TimeSpan.Zero && value != System.Threading.Timeout.InfiniteTimeSpan)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            _timeout = value;
        }
    }
}

/// <summary>
/// Health check context. Provides health check registrations to <see cref="IAkkaHealthCheck.CheckHealthAsync(AkkaHealthCheckContext, System.Threading.CancellationToken)"/>
/// </summary>
public sealed class AkkaHealthCheckContext
{
    /// <summary>
    /// Gets or sets the <see cref="AkkaHealthCheckRegistration"/> of the currently executing <see cref="IAkkaHealthCheck"/>.
    /// </summary>
    /// <remarks>
    /// This allows null values for convenience during unit testing. This is expected to be non-null when within application code.
    /// </remarks>
    public AkkaHealthCheckRegistration Registration { get; set; } = default!;
}

/// <summary>
/// Healthcheck aimed at testing the health of Akka.NET-specific resources.
/// </summary>
public interface IAkkaHealthCheck
{
    Task<HealthCheckResult> CheckHealthAsync(AkkaHealthCheckContext context, CancellationToken cancellationToken = default);
}