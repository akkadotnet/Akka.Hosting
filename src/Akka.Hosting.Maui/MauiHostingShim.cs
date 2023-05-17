// -----------------------------------------------------------------------
//  <copyright file="MauiHostingShim.cs" company="Akka.NET Project">
//      Copyright (C) 2009-2023 Lightbend Inc. <http://www.lightbend.com>
//      Copyright (C) 2013-2023 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Akka.Hosting.Maui;

/// <summary>
///  INTERNAL API
/// </summary>
internal static class HostingShimSetter
{
    static HostingShimSetter()
    {
        // need this in order to make shim available for binding
        MauiShimHolder.Shim = new MauiHostingShim();
    }
}

/// <summary>
/// INTERNAL API
/// </summary>
internal sealed class MauiHostingShim : IMauiShim
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

    public void BindAkkaService(IServiceCollection services)
    {
        services.AddSingleton<AkkaHostedService>(provider =>
            {
                var configBuilder = provider.GetRequiredService<AkkaConfigurationBuilder>();
                var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger<AkkaHostedService>();
                var akka = new AkkaHostedService(configBuilder, provider, logger, new MauiApplicationLifetime());

                return akka;
            })
            .AddTransient<IMauiInitializeService, MauiAkkaService>();
    }
}