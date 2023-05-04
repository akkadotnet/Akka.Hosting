using System;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Akka.Hosting
{
    /// <summary>
    /// INTERNAL API
    /// </summary>
    internal sealed class AkkaHostedService : IHostedService
    {
        private ActorSystem? _actorSystem;
        private CoordinatedShutdown? _coordinatedShutdown; // grab a reference to CoordinatedShutdown early
        private readonly IServiceProvider _serviceProvider;
        private readonly AkkaConfigurationBuilder _configurationBuilder;
        private readonly IHostApplicationLifetime? _hostApplicationLifetime;
        private readonly ILogger<AkkaHostedService> _logger;

        public AkkaHostedService(AkkaConfigurationBuilder configurationBuilder, IServiceProvider serviceProvider,
            ILogger<AkkaHostedService> logger, IHostApplicationLifetime? applicationLifetime)
        {
            _configurationBuilder = configurationBuilder;
            _hostApplicationLifetime = applicationLifetime;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                _actorSystem = _serviceProvider.GetRequiredService<ActorSystem>();
                _coordinatedShutdown = CoordinatedShutdown.Get(_actorSystem);
                await _configurationBuilder.StartAsync(_actorSystem);

                async Task TerminationHook()
                {
                    await _actorSystem.WhenTerminated.ConfigureAwait(false);
                    _hostApplicationLifetime?.StopApplication();
                }

                // terminate the application if the Sys is terminated first
                // this can happen in instances such as Akka.Cluster membership changes
#pragma warning disable CS4014
                TerminationHook();
#pragma warning restore CS4014
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Critical, ex, "Unable to start AkkaHostedService - shutting down application");
                _hostApplicationLifetime?.StopApplication();
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            // ActorSystem may have failed to start - skip shutdown sequence if that's the case
            // so error message doesn't get conflated.
            if (_coordinatedShutdown == null)
            {
                return;
            }
            
            // run full CoordinatedShutdown on the Sys
            await _coordinatedShutdown.Run(CoordinatedShutdown.ClrExitReason.Instance)
                .ConfigureAwait(false);
        }
    }
}