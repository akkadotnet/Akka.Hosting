// -----------------------------------------------------------------------
//  <copyright file="HostingSpec.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Actor.Internal;
using Akka.Actor.Setup;
using Akka.Annotations;
using Akka.Configuration;
using Akka.TestKit;
using Akka.TestKit.MsTest;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Akka.Hosting.TestKit.MsTest;

public abstract class TestKit: TestKitBase
{
    /// <summary>
    /// Commonly used assertions used throughout the testkit.
    /// </summary>
    private static ITestKitAssertions Assertions { get; } = new MsTestAssertions();

    protected override void ThrowNotInitialized()
    {
        throw new AssertFailedException("Test has not been initialized yet");
    }
    protected TestKit(string? actorSystemName = null, TimeSpan? startupTimeout = null, LogLevel logLevel = LogLevel.Information)
        : base(Assertions, actorSystemName, startupTimeout, logLevel)
    {
    }
        
    [InternalApi]
    [TestInitialize]
    public new Task InitializeAsync()
    {
        return base.InitializeAsync();
    }
}