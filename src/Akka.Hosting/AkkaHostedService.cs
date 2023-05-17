﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Akka.Hosting
{
    /// <summary>
    /// INTERNAL API
    /// </summary>
    /// <remarks>
    /// Open for modification in cases where users need fine-grained control over <see cref="Actor.ActorSystem"/> startup and
    /// DI - however, extend at your own risk. Look at the Akka.Hosting source code for ideas on how to extend this.
    /// </remarks>
    [InternalApi]
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    public class AkkaHostedService : IHostedService
    {
        protected ActorSystem? ActorSystem;
        protected CoordinatedShutdown? CoordinatedShutdown; // grab a reference to CoordinatedShutdown early
        protected readonly IServiceProvider ServiceProvider;
        protected readonly AkkaConfigurationBuilder ConfigurationBuilder;
        protected readonly IHostApplicationLifetime? HostApplicationLifetime;
        protected readonly ILogger<AkkaHostedService> Logger;

        public AkkaHostedService(AkkaConfigurationBuilder configurationBuilder, IServiceProvider serviceProvider,
            ILogger<AkkaHostedService> logger, IHostApplicationLifetime? applicationLifetime)
        {
            ConfigurationBuilder = configurationBuilder;
            HostApplicationLifetime = applicationLifetime;
            ServiceProvider = serviceProvider;
            Logger = logger;
        }

        public virtual async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                ActorSystem = ServiceProvider.GetRequiredService<ActorSystem>();
                CoordinatedShutdown = CoordinatedShutdown.Get(ActorSystem);
                await ConfigurationBuilder.StartAsync(ActorSystem);

                async Task TerminationHook()
                {
                    await ActorSystem.WhenTerminated.ConfigureAwait(false);
                    HostApplicationLifetime?.StopApplication();
                }

                // terminate the application if the Sys is terminated first
                // this can happen in instances such as Akka.Cluster membership changes
#pragma warning disable CS4014
                TerminationHook();
#pragma warning restore CS4014
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, ex, "Unable to start AkkaHostedService - shutting down application");
                HostApplicationLifetime?.StopApplication();
            }
        }

        public virtual async Task StopAsync(CancellationToken cancellationToken)
        {
            // ActorSystem may have failed to start - skip shutdown sequence if that's the case
            // so error message doesn't get conflated.
            if (CoordinatedShutdown == null)
            {
                return;
            }

            // run full CoordinatedShutdown on the Sys
            await CoordinatedShutdown.Run(CoordinatedShutdown.ClrExitReason.Instance)
                .ConfigureAwait(false);
        }
    }
}