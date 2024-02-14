//-----------------------------------------------------------------------
// <copyright file="ExpectTests.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2021 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2021 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

namespace Akka.Hosting.TestKit.MsTest.Tests.TestKitBaseTests;

[TestClass]
public class ExpectTests : TestKit
{
    protected override void ConfigureAkka(AkkaConfigurationBuilder builder, IServiceProvider provider)
    {
    }

    [TestMethod]
    public void ExpectMsgAllOf_should_receive_correct_messages()
    {
        TestActor.Tell("1");
        TestActor.Tell("2");
        TestActor.Tell("3");
        TestActor.Tell("4");
        ExpectMsgAllOf(new []{"3", "1", "4", "2"}).Should()
            .BeEquivalentTo(new[] { "1", "2", "3", "4" }, opt => opt.WithStrictOrdering());
    }

    [TestMethod]
    public void ExpectMsgAllOf_should_fail_when_receiving_unexpected()
    {
        TestActor.Tell("1");
        TestActor.Tell("2");
        TestActor.Tell("Totally unexpected");
        TestActor.Tell("3");
        Invoking(() => ExpectMsgAllOf(new []{"3", "1", "2"} ))
            .Should().Throw<Exception>();
    }

    [TestMethod]
    public void ExpectMsgAllOf_should_timeout_when_not_receiving_any_messages()
    {
        Invoking(() => ExpectMsgAllOf(TimeSpan.FromMilliseconds(100), new []{"3", "1", "2"} ))
            .Should().Throw<Exception>();
    }

    [TestMethod]
    public void ExpectMsgAllOf_should_timeout_if_to_few_messages()
    {
        TestActor.Tell("1");
        TestActor.Tell("2");
        Invoking(() => ExpectMsgAllOf(TimeSpan.FromMilliseconds(100), new[]{"3", "1", "2"} ))
            .Should().Throw<Exception>();
    }

}