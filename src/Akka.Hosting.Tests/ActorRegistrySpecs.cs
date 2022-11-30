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
    
    [Fact]
    public void Should_throw_NullReferenceException_for_Null_IActorRef()
    {
        // arrange
        var registry = new ActorRegistry();

        // act

        var register = () => registry.Register<Nobody>(null);

        // assert
        register.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Should_throw_on_missing_entry_during_Get()
    {
        // arrange
        var registry = new ActorRegistry();
        
        // assert
        registry.Invoking(x => x.Get<Nobody>()).Should().Throw<MissingActorRegistryEntryException>();
    }
    
    [Fact]
    public void Should_not_throw_on_missing_entry_during_TryGet()
    {
        // arrange
        var registry = new ActorRegistry();
        
        // assert
        registry.Invoking(x => x.TryGet<Nobody>(out var actor)).Should().NotThrow<MissingActorRegistryEntryException>();
    }
}