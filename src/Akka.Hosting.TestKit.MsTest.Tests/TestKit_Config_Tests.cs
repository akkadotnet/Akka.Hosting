//-----------------------------------------------------------------------
// <copyright file="TestKit_Config_Tests.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2021 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2021 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System.Reflection;
using Akka.TestKit;

namespace Akka.Hosting.TestKit.MsTest.Tests;

[TestClass]
// ReSharper disable once InconsistentNaming
public class TestKit_Config_Tests : TestKit
{
    protected override void ConfigureAkka(AkkaConfigurationBuilder builder, IServiceProvider provider)
    {
    }

    [TestMethod]
    public void DefaultValues_should_be_correct()
    {
        TestKitSettings.DefaultTimeout.Should().Be(TimeSpan.FromSeconds(5));
        TestKitSettings.SingleExpectDefault.Should().Be(TimeSpan.FromSeconds(3));
        TestKitSettings.TestEventFilterLeeway.Should().Be(TimeSpan.FromSeconds(3));
        TestKitSettings.TestTimeFactor.Should().Be(1);
        var callingThreadDispatcherTypeName = typeof(CallingThreadDispatcherConfigurator).FullName + ", " + typeof(CallingThreadDispatcher).GetTypeInfo().Assembly.GetName().Name;
        Sys.Settings.Config.IsEmpty.Should().BeFalse();
        Sys.Settings.Config.GetString("akka.test.calling-thread-dispatcher.type", null).Should().Be(callingThreadDispatcherTypeName);
        Sys.Settings.Config.GetString("akka.test.test-actor.dispatcher.type", null).Should().Be(callingThreadDispatcherTypeName);
        CallingThreadDispatcher.Id.Should().Be("akka.test.calling-thread-dispatcher");
    }
}