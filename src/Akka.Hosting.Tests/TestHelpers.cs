using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Akka.Hosting.Tests;

public static class TestHelpers
{
    public static async Task<IHost> StartHost(Action<IServiceCollection> testSetup)
    {
        var host = new HostBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton<DiSanityCheckSpecs.IMySingletonInterface, DiSanityCheckSpecs.MySingletonImpl>();
                testSetup(services);
            }).Build();
        
        await host.StartAsync();
        return host;
    }
}