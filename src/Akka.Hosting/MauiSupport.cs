// -----------------------------------------------------------------------
//  <copyright file="MauiSupport.cs" company="Akka.NET Project">
//      Copyright (C) 2009-2023 Lightbend Inc. <http://www.lightbend.com>
//      Copyright (C) 2013-2023 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Akka.Hosting
{
    public static class AkkaMauiSupport
    {
        public static async Task StartAkka(IServiceProvider provider, CancellationToken token = default)
        {
            var service = provider.GetRequiredService<AkkaHostedService>();
            await service.StartAsync(token).ConfigureAwait(false);
        }
    
        public static async Task StopAkka(IServiceProvider provider, CancellationToken token = default)
        {
            var service = provider.GetRequiredService<AkkaHostedService>();
            await service.StopAsync(token).ConfigureAwait(false);
        }
    }
}

