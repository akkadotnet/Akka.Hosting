// -----------------------------------------------------------------------
//  <copyright file="HostingSpec.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Akka.Actor.Internal;
using Akka.Annotations;
using Akka.Hosting.TestKit.Internals;
using Xunit;

namespace Akka.Hosting.TestKit
{
    public abstract partial class TestKit : IAsyncLifetime
    {
        // Ambient-context pin handle for the current test. Installed at the top of InitializeAsync
        // (before host-startup awaits) so the SynchronizationContext we install is captured by xUnit2
        // when it resumes the async test body, and re-pins the implicit-sender cell across every await
        // continuation. Disposed in DisposeAsync so the seeded cell never survives onto a pooled thread.
        // This is the xUnit2 port of the #735 fix (AkkaCleanAmbientContextAttribute) for the v3 variant —
        // without it the implicit-sender cell leaks across ActorSystems under parallel-collection execution (#764).
        private CleanAmbientContext? _ambientContext;

        [InternalApi]
        public Task InitializeAsync()
        {
            // Install the ambient-context pin BEFORE host startup so the SynchronizationContext we
            // install is captured by xUnit2 when it resumes the async test body, re-pinning the
            // implicit-sender cell across every await continuation. The TestActor is created during
            // host startup (not in the constructor), so the cell is resolved lazily — the SC calls
            // this resolver on every continuation, by which time TestActor exists and is valid.
            var noSender = this is Akka.TestKit.INoImplicitSender;
            _ambientContext = CleanAmbientContext.Apply(() =>
            {
                if (noSender)
                    return null;

                try
                {
                    return CleanAmbientContext.TryGetCell(this);
                }
                catch
                {
                    // TestActor not yet initialized (or already disposed) — treat as no cell for now;
                    // the SC re-resolves on the next continuation once the TestActor exists.
                    return null;
                }
            });

            return InitializeAsyncCore();
        }

        protected virtual Task BeforeTestStart()
        {
            return Task.CompletedTask;
        }

        protected virtual Task AfterAllAsync()
        {
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            try
            {
                await DisposeAsyncCore();
            }
            finally
            {
                // Defense-in-depth: guarantee the ambient cell is cleared at teardown regardless of
                // whether the test body threw or took an unusual SC path, so a leaked cell can never
                // outlive the test onto a thread reused by unrelated code.
                _ambientContext?.Dispose();
                _ambientContext = null;
            }
        }
    }
}
