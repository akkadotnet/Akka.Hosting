//-----------------------------------------------------------------------
// <copyright file="RemainingTests.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2021 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2021 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

namespace Akka.Hosting.TestKit.MsTest.Tests.TestKitBaseTests;

[TestClass]
public class RemainingTests : TestKit
{
    protected override void ConfigureAkka(AkkaConfigurationBuilder builder, IServiceProvider provider)
    {
        
    }

    [TestMethod]
    public void Throw_if_remaining_is_called_outside_Within()
    {
        Assert.ThrowsException<InvalidOperationException>(() => Remaining);
    }
}