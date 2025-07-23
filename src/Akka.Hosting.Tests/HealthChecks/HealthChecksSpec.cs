using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Hosting.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Xunit;

namespace Akka.Hosting.Tests.HealthChecks;

public class HealthChecksSpec : TestKit.TestKit
{
    private class FooActor : UntypedActor
    {
        protected override void OnReceive(object message)
        {
        }
    }

    protected override void ConfigureAkka(AkkaConfigurationBuilder builder, IServiceProvider provider)
    {
        builder.WithHealthCheck("FooActor alive", async (system, registry, cancellationToken) =>
        {
            /*
             * N.B. CancellationToken is set by the call to MSFT.EXT.DIAGNOSTICS.HEALTHCHECK,
             * so that value could be "inifite" by default.
             *
             * Therefore, it might be a really, really good idea to guard this with a non-infinite
             * timeout via a LinkedCancellationToken here.
             */
            try
            {
                var fooActor = await registry.GetAsync<FooActor>(cancellationToken);

                try
                {
                    await fooActor.Ask<ActorIdentity>(new Identify("foo"), cancellationToken: cancellationToken);
                }
                catch (Exception)
                {
                    return HealthCheckResult.Degraded("fooActor found but non-responsive");
                }
            }
            catch (Exception)
            {
                return HealthCheckResult.Unhealthy("fooActor not found in registry");
            }

            return HealthCheckResult.Healthy("fooActor found and responsive");
        });
    }

    [Fact]
    public async Task ShouldHaveDefaultHealthCheckRegistration()
    {
        // arrange
        var configurationBuilder = Host.Services.GetRequiredService<AkkaConfigurationBuilder>();
        
        // act
        
        // assert
        Assert.Equal(2, configurationBuilder.HealthChecks.Count); // 1 built-in, 1 custom
        
        // find the built-in implementation
        var actorSystemHealthCheckRegistration = configurationBuilder.HealthChecks.Single(c => c.HealthCheck is ActorSystemLivenessCheck);
        var akkaHealthCheckContext = new AkkaHealthCheckContext(Sys)
            { Registration = actorSystemHealthCheckRegistration.ToHealthCheckRegistration() };
        
        // invoke the actorSystem liveness check
        var healthCheckResult = await actorSystemHealthCheckRegistration.HealthCheck.CheckHealthAsync(akkaHealthCheckContext, CancellationToken.None);
        
        // assert - system is alive, health check should be healthy
        Assert.Equal(HealthStatus.Healthy, healthCheckResult.Status);
    }
}