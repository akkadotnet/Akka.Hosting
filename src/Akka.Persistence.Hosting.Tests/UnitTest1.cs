using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Akka.Persistence.Hosting.Tests;

public class UnitTest1
{
    public static async Task<IHost> StartHost(Action<IServiceCollection> testSetup)
    {
        var host = new HostBuilder()
            .ConfigureServices(testSetup).Build();
        
        await host.StartAsync();
        return host;
    }
    
    [Fact]
    public async Task Should_use_correct_EventAdapter_bindings()
    {
    }
}