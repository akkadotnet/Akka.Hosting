using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Microsoft.Extensions.Hosting;

namespace Akka.Hosting
{
    /// <summary>
    /// INTERNAL API
    /// </summary>
    internal sealed class AkkaHostedService : IHostedService
    {
        private ActorSystem _actorSystem;
        private readonly AkkaConfigurationBuilder _configurationBuilder;
        private readonly IHostApplicationLifetime _hostApplicationLifetime;

        public AkkaHostedService(AkkaConfigurationBuilder configurationBuilder, IHostApplicationLifetime hostApplicationLifetime)
        {
            _configurationBuilder = configurationBuilder;
            _hostApplicationLifetime = hostApplicationLifetime;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _actorSystem = await _configurationBuilder.StartAsync();

            async Task TerminationHook()
            {
                await _actorSystem.WhenTerminated.ConfigureAwait(false);
                _hostApplicationLifetime.StopApplication();
            }

            // terminate the application if the Sys is terminated first
            // this can happen in instances such as Akka.Cluster membership changes
#pragma warning disable CS4014
            TerminationHook();
#pragma warning restore CS4014
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            // run full CoordinatedShutdown on the Sys
            await CoordinatedShutdown.Get(_actorSystem).Run(CoordinatedShutdown.ClrExitReason.Instance).ConfigureAwait(false);
        }
    }
}
