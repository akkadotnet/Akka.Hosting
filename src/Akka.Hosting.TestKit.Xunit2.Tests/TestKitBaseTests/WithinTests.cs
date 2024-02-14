//-----------------------------------------------------------------------
// <copyright file="WithinTests.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2021 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2021 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using Xunit;

namespace Akka.Hosting.TestKit.Xunit2.Tests.TestKitBaseTests;

public class WithinTests : Hosting.TestKit.Xunit2.TestKit
{
    protected override void ConfigureAkka(AkkaConfigurationBuilder builder, IServiceProvider provider)
    {
        
    }

    [Fact]
    public void Within_should_increase_max_timeout_by_the_provided_epsilon_value()
    {
        Within(TimeSpan.FromSeconds(1), () => ExpectNoMsg(), TimeSpan.FromMilliseconds(50));
    }
}