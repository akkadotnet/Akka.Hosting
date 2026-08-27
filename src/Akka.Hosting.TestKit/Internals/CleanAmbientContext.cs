//-----------------------------------------------------------------------
// <copyright file="CleanAmbientContext.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2022 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2025 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

#nullable enable

using System;
using System.Threading;
using Akka.Actor;
using Akka.Actor.Internal;
using Akka.Annotations;
using Akka.TestKit;

namespace Akka.Hosting.TestKit.Internals
{
    /// <summary>
    /// INTERNAL API.
    /// <para/>
    /// Pins the ambient <see cref="InternalCurrentActorCellKeeper.Current"/> to a running
    /// test's TestActor cell on the body thread and installs an
    /// <see cref="ActorCellKeepingSynchronizationContext"/> that re-pins the cell across
    /// <c>await</c> continuations. The previously observed cell and
    /// <see cref="SynchronizationContext"/> are restored in <see cref="Dispose"/> so a
    /// seeded cell never survives onto a pooled thread reused by unrelated work.
    /// <para/>
    /// xUnit2 port of the v3 <c>AkkaCleanAmbientContextAttribute</c> (PR #735). The v3 variant
    /// is an attribute because xUnit v3 exposes the test instance via
    /// <c>TestContext.Current.TestClassInstance</c>. xUnit2's
    /// BeforeAfterTestAttribute does not, so this helper is driven from
    /// the test kit's own <c>IAsyncLifetime</c> hooks (<c>InitializeAsync</c>/<c>DisposeAsync</c>),
    /// which run on the body thread and have direct access to the instance.
    /// </summary>
    [InternalApi]
    internal sealed class CleanAmbientContext : IDisposable
    {
        private readonly Func<ActorCell?> _cellSource;

        private SynchronizationContext? _previousContext;
        private ActorCell? _previousCell;
        private bool _disposed;

        private CleanAmbientContext(Func<ActorCell?> cellSource)
        {
            _cellSource = cellSource;
        }

        /// <summary>
        /// Captures the current ambient cell and <see cref="SynchronizationContext"/>, pins
        /// <paramref name="cellSource"/> as <see cref="InternalCurrentActorCellKeeper.Current"/>, and
        /// installs an <see cref="ActorCellKeepingSynchronizationContext"/> that re-pins it across
        /// continuations. Returns a handle whose <see cref="Dispose"/> restores the prior state.
        /// </summary>
        /// <param name="cellSource">
        /// A function that resolves the <see cref="Akka.Actor.ActorCell"/> to pin, or
        /// <see langword="null"/> to pin "no implicit sender". Resolved lazily on each continuation so it
        /// stays correct when the TestActor is created after the SynchronizationContext is installed.
        /// </param>
        public static CleanAmbientContext Apply(Func<ActorCell?> cellSource)
        {
            var ctx = new CleanAmbientContext(cellSource);
            ctx._previousContext = SynchronizationContext.Current;
            ctx._previousCell = InternalCurrentActorCellKeeper.Current;

            // Pin the current value on the body thread. This may be null at install time (the TestActor
            // is created during host startup), but the SC re-resolves lazily for every continuation, so
            // it stays correct once the TestActor exists. Guard against a resolver that throws while the
            // TestActor is not yet initialized.
            try
            {
                InternalCurrentActorCellKeeper.Current = cellSource();
            }
            catch
            {
                // no-op — TestActor not yet created; the SC will resolve it on the first continuation
            }
            SynchronizationContext.SetSynchronizationContext(
                new ActorCellKeepingSynchronizationContext(cellSource, ctx._previousContext));

            return ctx;
        }

        /// <summary>
        /// Restores the ambient cell and <see cref="SynchronizationContext"/> captured at
        /// <see cref="Apply"/>. Safe to call more than once.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            InternalCurrentActorCellKeeper.Current = _previousCell;
            SynchronizationContext.SetSynchronizationContext(_previousContext);
        }

        internal static ActorCell? TryGetCell(TestKitBase testKit)
        {
            // TestActor may be null before host startup completes (it is created inside a StartActors
            // callback), and the SC resolves this lazily on every continuation. Return null in that
            // window rather than throwing — the cell becomes available once the TestActor exists.
            return testKit.TestActor is ActorRefWithCell { Underlying: ActorCell cell }
                ? cell
                : null;
        }
    }
}
