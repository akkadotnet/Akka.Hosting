using System;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Akka.Hosting
{
    public abstract class AkkaHostedService : IHostedService
    {
        private ActorSystem? _actorSystem;
        private CoordinatedShutdown? _coordinatedShutdown; // grab a reference to CoordinatedShutdown early
        
        protected IServiceProvider ServiceProvider { get; }
        protected IHostApplicationLifetime HostApplicationLifetime { get; }
        protected ILogger<AkkaHostedService> Logger { get; }
        
        private readonly AkkaConfigurationBuilder _configurationBuilder;

        protected AkkaHostedService(AkkaConfigurationBuilder configurationBuilder, IServiceProvider serviceProvider,
            ILogger<AkkaHostedService> logger, IHostApplicationLifetime applicationLifetime)
        {
            _configurationBuilder = configurationBuilder;
            HostApplicationLifetime = applicationLifetime;
            ServiceProvider = serviceProvider;
            Logger = logger;
        }
        
        protected ActorSystem ActorSystem
        {
            get
            {
                if (_actorSystem is null)
                    throw new Exception("ActorSystem has not been initialized");
                return _actorSystem;
            }
        }

        protected CoordinatedShutdown CoordinatedShutdown
        {
            get
            {
                if (_coordinatedShutdown is null)
                    throw new Exception("ActorSystem has not been initialized");
                return _coordinatedShutdown;
            }
        }

        protected bool Initialized => _coordinatedShutdown is not null;

        protected async Task StartAkkaAsync(CancellationToken cancellationToken)
        {
            _actorSystem = ServiceProvider.GetRequiredService<ActorSystem>();
            _coordinatedShutdown = CoordinatedShutdown.Get(_actorSystem);
            await _configurationBuilder.StartAsync(_actorSystem);

            async Task TerminationHook()
            {
                await _actorSystem.WhenTerminated.ConfigureAwait(false);
                HostApplicationLifetime.StopApplication();
            }

            // terminate the application if the Sys is terminated first
            // this can happen in instances such as Akka.Cluster membership changes
#pragma warning disable CS4014
            TerminationHook();
#pragma warning restore CS4014
        }

        protected async Task StopAkkaAsync(CancellationToken cancellationToken)
        {
            // ActorSystem may have failed to start - skip shutdown sequence if that's the case
            // so error message doesn't get conflated.
            if (!Initialized)
                return;
            
            // run full CoordinatedShutdown on the Sys
            await CoordinatedShutdown.Run(CoordinatedShutdown.ClrExitReason.Instance)
                .ConfigureAwait(false);
        }
        
        public virtual async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                await StartAkkaAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, ex, "Unable to start AkkaHostedService - shutting down application");
                HostApplicationLifetime.StopApplication();
            }
        }
        
        public virtual async Task StopAsync(CancellationToken cancellationToken)
        {
            await StopAkkaAsync(cancellationToken);
        }
    }
    
    /// <summary>
    /// INTERNAL API
    /// </summary>
    internal sealed class AkkaHostedServiceImpl : AkkaHostedService
    {
        public AkkaHostedServiceImpl(
            AkkaConfigurationBuilder configurationBuilder,
            IServiceProvider serviceProvider,
            ILogger<AkkaHostedService> logger,
            IHostApplicationLifetime applicationLifetime)
        : base(configurationBuilder, serviceProvider, logger, applicationLifetime)
        { }
    }
}