using System;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Actor.Dsl;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Akka.Hosting.Tests;

public class Bugfix208Specs : TestKit.TestKit
{
    private class MyTestActor : ReceiveActor
    {
        public record SetData(string Data);

        public record GetData();
        
        private string _data = string.Empty;
        
        public MyTestActor()
        {
            Receive<SetData>(s =>
            {
                _data = s.Data;
            });
            
            Receive<GetData>(g =>
            {
                Sender.Tell(_data);
            });
        }
    }
    
    private class TestActorKey{}
    
    private class MyBackgroundService : BackgroundService
    {
        private readonly IActorRef _testActor;

        public MyBackgroundService(IRequiredActor<TestActorKey> requiredActor)
        {
            _testActor = requiredActor.ActorRef;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _testActor.Tell("BackgroundService started");
            return Task.CompletedTask;
        }
    }

    protected override void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
        services.AddHostedService<MyBackgroundService>();
        base.ConfigureServices(context, services);
    }

    protected override void ConfigureAkka(AkkaConfigurationBuilder builder, IServiceProvider provider)
    {
        builder.WithActors((system, registry, arg3) =>
        {
            registry.Register<TestActorKey>(system.ActorOf(Props.Create(() => new MyTestActor()), "test-actor"));
        });
    }

    /// <summary>
    /// Reproduction for https://github.com/akkadotnet/Akka.Hosting/issues/208
    /// </summary>
    [Fact]
    public async Task ShouldStartHostedServiceThatDependsOnActor()
    {
        // arrange
        var testActorRef = ActorRegistry.Get<TestActorKey>();

        // act

        // assert
        await AwaitAssertAsync(async () =>
        {
            var r = await testActorRef.Ask<string>(new MyTestActor.GetData(), TimeSpan.FromMilliseconds(100));
            r.Should().Be("BackgroundService started");
        });
    }
}