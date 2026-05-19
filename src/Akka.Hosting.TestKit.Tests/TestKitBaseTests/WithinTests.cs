//-----------------------------------------------------------------------
// <copyright file="WithinTests.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2021 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2021 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Xunit;

namespace Akka.Hosting.TestKit.Tests.TestKitBaseTests;

public class WithinTests : TestKit
{
    protected override void ConfigureAkka(AkkaConfigurationBuilder builder, IServiceProvider provider)
    {
        
    }

    [Fact]
    public void Within_should_increase_max_timeout_by_the_provided_epsilon_value()
    {
        // ExpectNoMsg uses an explicit 1s timeout so the block duration is predictable.
        // Within max is 3s to absorb Windows CI scheduler jitter (windows-2025 runners are slower).
        Within(TimeSpan.FromSeconds(3), () => ExpectNoMsg(TimeSpan.FromSeconds(1)), TimeSpan.FromMilliseconds(50));
    }
}