// -----------------------------------------------------------------------
//  <copyright file="HostingExtensionsSpec.cs" company="Akka.NET Project">
//      Copyright (C) 2009-2022 Lightbend Inc. <http://www.lightbend.com>
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using System;
using FluentAssertions;
using FluentAssertions.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Akka.Hosting.Tests;

public class HostingExtensionsSpec
{
    [Fact(DisplayName = "WithActorAskTimeout should inject proper HOCON")]
    public void WithActorAskTimeoutTest()
    {
        var builder = new AkkaConfigurationBuilder(new ServiceCollection(), "fake")
            .WithActorAskTimeout(10.Seconds());
        builder.Configuration.HasValue.Should().BeTrue();
        builder.Configuration.Value.GetTimeSpan("akka.actor.ask-timeout").Should().Be(10.Seconds());
    }
    
    [Fact(DisplayName = "WithActorAskTimeout should be able to infer infinite timespan")]
    public void WithActorAskTimeoutInfiniteTest()
    {
        var builder = new AkkaConfigurationBuilder(new ServiceCollection(), "fake")
            .WithActorAskTimeout(TimeSpan.Zero);
        builder.Configuration.HasValue.Should().BeTrue();
        builder.Configuration.Value.GetString("akka.actor.ask-timeout").Should().Be("infinite");
    }
}