using System.IO;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Configuration;
using Akka.Hosting;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Akka.Remote.Hosting.Tests;

public class RemoteConfigurationSpecs
{
    [Fact(DisplayName = "Empty WithRemoting should return default remoting settings")]
    public async Task EmptyWithRemotingConfigTest()
    {
        // arrange
        using var host = new HostBuilder().ConfigureServices(services =>
        {
            services.AddAkka("RemoteSys", (builder, provider) =>
            {
                builder.WithRemoting();
            });
        }).Build();

        // act
        await host.StartAsync();
        var actorSystem = (ExtendedActorSystem)host.Services.GetRequiredService<ActorSystem>();
        var config = actorSystem.Settings.Config;
        var adapters = config.GetStringList("akka.remote.enabled-transports");
        var tcpConfig = config.GetConfig("akka.remote.dot-netty.tcp");
        
        // assert
        adapters.Count.Should().Be(1);
        adapters[0].Should().Be("akka.remote.dot-netty.tcp");
        
        tcpConfig.GetString("hostname").Should().BeEmpty();
        tcpConfig.GetInt("port").Should().Be(2552);
        tcpConfig.GetString("public-hostname").Should().BeEmpty();
        tcpConfig.GetInt("public-port").Should().Be(0);
    }
    
    [Fact(DisplayName = "WithRemoting should override remote settings")]
    public async Task WithRemotingConfigTest()
    {
        // arrange
        using var host = new HostBuilder().ConfigureServices(services =>
        {
            services.AddAkka("RemoteSys", (builder, provider) =>
            {
                builder.WithRemoting("0.0.0.0", 0, "localhost", 12345);
            });
        }).Build();

        // act
        await host.StartAsync();
        var actorSystem = (ExtendedActorSystem)host.Services.GetRequiredService<ActorSystem>();
        var config = actorSystem.Settings.Config;
        var adapters = config.GetStringList("akka.remote.enabled-transports");
        var tcpConfig = config.GetConfig("akka.remote.dot-netty.tcp");
        
        // assert
        adapters.Count.Should().Be(1);
        adapters[0].Should().Be("akka.remote.dot-netty.tcp");
        
        tcpConfig.GetString("hostname").Should().Be("0.0.0.0");
        tcpConfig.GetInt("port").Should().Be(0);
        tcpConfig.GetString("public-hostname").Should().Be("localhost");
        tcpConfig.GetInt("public-port").Should().Be(12345);
    }
    
    [Fact(DisplayName = "WithRemoting with RemoteOptions should override remote settings")]
    public async Task WithRemotingOptionsTest()
    {
        // arrange
        using var host = new HostBuilder().ConfigureServices(services =>
        {
            services.AddAkka("RemoteSys", (builder, provider) =>
            {
                builder.WithRemoting(new RemoteOptions
                {
                    HostName = "0.0.0.0", 
                    Port = 0, 
                    PublicHostName = "localhost",
                    PublicPort = 12345
                });
            });
        }).Build();

        // act
        await host.StartAsync();
        var actorSystem = (ExtendedActorSystem)host.Services.GetRequiredService<ActorSystem>();
        var config = actorSystem.Settings.Config;
        var adapters = config.GetStringList("akka.remote.enabled-transports");
        var tcpConfig = config.GetConfig("akka.remote.dot-netty.tcp");
        
        // assert
        adapters.Count.Should().Be(1);
        adapters[0].Should().Be("akka.remote.dot-netty.tcp");
        
        tcpConfig.GetString("hostname").Should().Be("0.0.0.0");
        tcpConfig.GetInt("port").Should().Be(0);
        tcpConfig.GetString("public-hostname").Should().Be("localhost");
        tcpConfig.GetInt("public-port").Should().Be(12345);
    }
    
    [Fact(DisplayName = "WithRemoting should override remote settings that are overriden")]
    public async Task WithRemotingConfigOverrideTest()
    {
        // arrange
        using var host = new HostBuilder().ConfigureServices(services =>
        {
            services.AddAkka("RemoteSys", (builder, provider) =>
            {
                builder.WithRemoting(publicHostname: "localhost", publicPort:12345);
            });
        }).Build();

        // act
        await host.StartAsync();
        var actorSystem = (ExtendedActorSystem)host.Services.GetRequiredService<ActorSystem>();
        var config = actorSystem.Settings.Config;
        var adapters = config.GetStringList("akka.remote.enabled-transports");
        var tcpConfig = config.GetConfig("akka.remote.dot-netty.tcp");
        
        // assert
        adapters.Count.Should().Be(1);
        adapters[0].Should().Be("akka.remote.dot-netty.tcp");
        
        tcpConfig.GetString("hostname").Should().BeEmpty();
        tcpConfig.GetInt("port").Should().Be(2552);
        tcpConfig.GetString("public-hostname").Should().Be("localhost");
        tcpConfig.GetInt("public-port").Should().Be(12345);
    }
    
    [Fact]
    public async Task AkkaRemoteShouldUsePublicHostnameCorrectly()
    {
        // arrange
        using var host = new HostBuilder().ConfigureServices(services =>
        {
            services.AddAkka("RemoteSys", (builder, provider) =>
            {
                builder.WithRemoting("0.0.0.0", 0, "localhost");
            });
        }).Build();

        // act
        await host.StartAsync();
        var actorSystem = (ExtendedActorSystem)host.Services.GetRequiredService<ActorSystem>();

        // assert
        actorSystem.Provider.DefaultAddress.Host.Should().Be("localhost");
    }
    
    [Fact(DisplayName = "RemoteOptions should be bindable using Microsoft.Extensions.Configuration")]
    public async Task ClusterOptionsConfigurationTest()
    {
        const string json = @"
{
  ""Logging"": {
    ""LogLevel"": {
      ""Default"": ""Information"",
      ""Microsoft.AspNetCore"": ""Warning""
    }
  },
  ""ConnectionStrings"": {
    ""sqlServerLocal"": ""Server=localhost,1533;Database=Akka;User Id=sa;Password=l0lTh1sIsOpenSource;"",
  },
  ""Akka"": {
    ""RemoteOptions"": {
      ""HostName"": ""0.0.0.0"",
      ""Port"" : 0,
      ""PublicHostName"": ""localhost"",
      ""PublicPort"": 12345
    }
  }
}";
        
        // arrange
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var jsonConfig = new ConfigurationBuilder().AddJsonStream(stream).Build();
        var remoteOptions = jsonConfig.GetSection("Akka:RemoteOptions").Get<RemoteOptions>();

        using var host = new HostBuilder().ConfigureServices(services =>
        {
            services.AddAkka("RemoteSys", (builder, provider) =>
            {
                builder.WithRemoting(remoteOptions);
            });
        }).Build();

        // act
        await host.StartAsync();
        var actorSystem = (ExtendedActorSystem)host.Services.GetRequiredService<ActorSystem>();
        var config = actorSystem.Settings.Config;
        var adapters = config.GetStringList("akka.remote.enabled-transports");
        var tcpConfig = config.GetConfig("akka.remote.dot-netty.tcp");
        
        // assert
        adapters.Count.Should().Be(1);
        adapters[0].Should().Be("akka.remote.dot-netty.tcp");
        
        tcpConfig.GetString("hostname").Should().Be("0.0.0.0");
        tcpConfig.GetInt("port").Should().Be(0);
        tcpConfig.GetString("public-hostname").Should().Be("localhost");
        tcpConfig.GetInt("public-port").Should().Be(12345);
    }
}