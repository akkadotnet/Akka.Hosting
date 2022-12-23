using System;
using System.Threading.Tasks;
using Akka.Actor;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Akka.Hosting.Tests;

public class RequiredActorSpecs
{
    public sealed class MyActorType : ReceiveActor
    {
        public MyActorType()
        {
            ReceiveAny(_ => Sender.Tell(_));
        }
    }

    public sealed class MyConsumer
    {
        private readonly IActorRef _actor;

        public MyConsumer(IRequiredActor<MyActorType> actor)
        {
            _actor = actor.ActorRef;
        }

        public async Task<string> Say(string word)
        {
            return await _actor.Ask<string>(word, TimeSpan.FromSeconds(3));
        }
    }
    
    [Fact]
    public async Task ShouldRetrieveRequiredActorFromIServiceProvider()
    {
        // arrange
        using var host = new HostBuilder()
            .ConfigureServices(services =>
            {
                services.AddAkka("MySys", (builder, provider) =>
                {
                    builder.WithActors((system, registry) =>
                    {
                        var actor = system.ActorOf(Props.Create(() => new MyActorType()), "myactor");
                        registry.Register<MyActorType>(actor);
                    });
                });
                services.AddScoped<MyConsumer>();
            })
            .Build();
            await host.StartAsync();

        // act
        var myConsumer = host.Services.GetRequiredService<MyConsumer>();
        var input = "foo";
        var spoken = await myConsumer.Say(input);

        // assert
        spoken.Should().Be(input);
    }
}