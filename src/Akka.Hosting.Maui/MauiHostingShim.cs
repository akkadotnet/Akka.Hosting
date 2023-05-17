// -----------------------------------------------------------------------
//  <copyright file="MauiHostingShim.cs" company="Akka.NET Project">
//      Copyright (C) 2009-2023 Lightbend Inc. <http://www.lightbend.com>
//      Copyright (C) 2013-2023 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using Akka.Actor;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Akka.Hosting.Maui;

/// <summary>
/// Extension methods for configuring Akka.Hosting on Maui
/// </summary>
public static class MauiAkkaHostingExtensions
{
    /// <summary>
    /// Work-around for MAUI support.
    /// </summary>
    private class MauiApplicationLifetime : IHostApplicationLifetime
    {
        public void StopApplication()
        {
            // trigger the process to exit, if it hasn't started already
            Application.Current?.Quit();
        }

        public CancellationToken ApplicationStarted => throw new NotImplementedException();
        public CancellationToken ApplicationStopping => throw new NotImplementedException();
        public CancellationToken ApplicationStopped => throw new NotImplementedException();
    }
    
    /// <summary>
    /// Registers an <see cref="ActorSystem"/> to this instance and creates a
    /// <see cref="AkkaConfigurationBuilder"/> that can be used to configure its
    /// behavior and Sys spawning.
    /// </summary>
    /// <param name="services">The service collection to which we are binding Akka.NET.</param>
    /// <param name="actorSystemName">The name of the <see cref="ActorSystem"/> that will be instantiated.</param>
    /// <param name="builder">A configuration delegate.</param>
    /// <returns>The <see cref="IServiceCollection"/> instance.</returns>
    /// <remarks>
    /// Akka.Hosting would work normally for Maui were it not for https://github.com/dotnet/maui/issues/2244. 
    /// </remarks>
    public static IServiceCollection AddAkkaMaui(this IServiceCollection services, string actorSystemName, Action<AkkaConfigurationBuilder> builder)
    {
        return AddAkkaMaui(services, actorSystemName, (configurationBuilder, provider) =>
        {
            builder(configurationBuilder);
        });
    }
    
    /// <summary>
    /// Registers an <see cref="ActorSystem"/> to this instance and creates a
    /// <see cref="AkkaConfigurationBuilder"/> that can be used to configure its
    /// behavior and Sys spawning.
    /// </summary>
    /// <param name="services">The service collection to which we are binding Akka.NET.</param>
    /// <param name="actorSystemName">The name of the <see cref="ActorSystem"/> that will be instantiated.</param>
    /// <param name="builder">A configuration delegate that accepts an <see cref="IServiceProvider"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> instance.</returns>
    /// <remarks>
    /// Akka.Hosting would work normally for Maui were it not for https://github.com/dotnet/maui/issues/2244. 
    /// </remarks>
    public static IServiceCollection AddAkkaMaui(this IServiceCollection services, string actorSystemName,
        Action<AkkaConfigurationBuilder, IServiceProvider> builder)
    {
        var b = new AkkaConfigurationBuilder(services, actorSystemName);
        services.AddSingleton<AkkaConfigurationBuilder>(sp =>
        {
            builder(b, sp);
            return b;
        });
        
        b.Bind();
        
        services.AddSingleton<AkkaHostedService>(provider =>
            {
                var configBuilder = provider.GetRequiredService<AkkaConfigurationBuilder>();
                var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger<AkkaHostedService>();
                var akka = new AkkaHostedService(configBuilder, provider, logger, new MauiApplicationLifetime());

                return akka;
            })
            .AddTransient<IMauiInitializeService, MauiAkkaService>();

        return services;
    }
}