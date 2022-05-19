using System;
using Akka.Actor;
using FluentAssertions;
using Xunit;

namespace Akka.Hosting.Tests;

public class ActorRegistrySpecs
{
    [Fact]
    public void Should_throw_upon_duplicate_registration()
    {
        // arrange
        var registry = new ActorRegistry();
        registry.Register<Nobody>(Nobody.Instance);
        
        // act

        var register = () => registry.Register<Nobody>(Nobody.Instance);

        // assert
        register.Should().Throw<DuplicateActorRegistryException>();
    }
    
    [Fact]
    public void Should_not_throw_upon_duplicate_registration_when_overwrite_allowed()
    {
        // arrange
        var registry = new ActorRegistry();
        registry.Register<Nobody>(Nobody.Instance);
        
        // act

        var register = () => registry.Register<Nobody>(Nobody.Instance, true);

        // assert
        register.Should().NotThrow<DuplicateActorRegistryException>();
    }
}