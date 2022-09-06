//-----------------------------------------------------------------------
// <copyright file="TestKit_Config_Tests.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2022 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Reflection;
using Akka.TestKit;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Akka.Hosting.TestKit.Tests
{
    // ReSharper disable once InconsistentNaming
    public class TestKit_Config_Tests : HostingSpec
    {
        public TestKit_Config_Tests(ITestOutputHelper output) : base("TestKitConfigTests", output)
        {
        }

        protected override void ConfigureAkka(AkkaConfigurationBuilder builder, IServiceProvider provider)
        { }
        
        [Fact]
        public void DefaultValues_should_be_correct()
        {
            TestKitSettings.DefaultTimeout.Should().Be(TimeSpan.FromSeconds(5));
            TestKitSettings.SingleExpectDefault.Should().Be(TimeSpan.FromSeconds(3));
            TestKitSettings.TestEventFilterLeeway.Should().Be(TimeSpan.FromSeconds(3));
            TestKitSettings.TestTimeFactor.Should().Be(1);
            var callingThreadDispatcherTypeName = typeof(CallingThreadDispatcherConfigurator).FullName + ", " + typeof(CallingThreadDispatcher).GetTypeInfo().Assembly.GetName().Name;
            Assert.False(Sys.Settings.Config.IsEmpty);
            Sys.Settings.Config.GetString("akka.test.calling-thread-dispatcher.type", null).Should().Be(callingThreadDispatcherTypeName);
            Sys.Settings.Config.GetString("akka.test.test-actor.dispatcher.type", null).Should().Be(callingThreadDispatcherTypeName);
            CallingThreadDispatcher.Id.Should().Be("akka.test.calling-thread-dispatcher");
        }
    }
}

