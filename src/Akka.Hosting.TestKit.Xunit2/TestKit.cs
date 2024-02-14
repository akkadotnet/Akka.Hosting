// -----------------------------------------------------------------------
//  <copyright file="HostingSpec.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using System;
using Akka.Hosting.TestKit.Xunit2.Internals;
using Akka.TestKit.Xunit2;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Akka.Hosting.TestKit.Xunit2
{
    public abstract class TestKit : TestKitBase, IAsyncLifetime
    {
        /// <summary>
        /// Commonly used assertions used throughout the testkit.
        /// </summary>
        private static XunitAssertions Assertions { get; } = new XunitAssertions();

        protected override void ThrowNotInitialized()
        {
            throw new XunitException("Test has not been initialized yet");
        }

        protected ITestOutputHelper? Output { get; }

        protected TestKit(string? actorSystemName = null, ITestOutputHelper? output = null,
            TimeSpan? startupTimeout = null, LogLevel logLevel = LogLevel.Information)
            : base(Assertions, actorSystemName, startupTimeout, logLevel)
        {
            Output = output;
        }

        protected override bool ShouldUseCustomLogger => Output is not null;

        protected override void ConfigureLogging(ILoggingBuilder builder)
        {
            builder.ClearProviders();
            builder.AddProvider(new XUnitLoggerProvider(Output, LogLevel));
            builder.AddFilter("Akka.*", LogLevel);
            base.ConfigureLogging(builder);
        }
    }
}

