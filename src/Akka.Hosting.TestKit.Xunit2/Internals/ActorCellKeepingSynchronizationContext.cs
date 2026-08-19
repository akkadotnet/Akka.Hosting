//-----------------------------------------------------------------------
// <copyright file="ActorCellKeepingSynchronizationContext.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2022 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2025 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor.Internal;
using Akka.Annotations;

namespace Akka.Hosting.TestKit.Internals
{
    /// <summary>
    /// INTERNAL API.
    /// <para/>
    /// A <see cref="SynchronizationContext"/> used by the xUnit2 test kit to pin
    /// the ambient <see cref="InternalCurrentActorCellKeeper.Current"/>
    /// <see cref="Akka.Actor.ActorCell"/> across <c>await</c> continuations that
    /// originate from a test body.
    /// <para/>
    /// <see cref="InternalCurrentActorCellKeeper.Current"/> is a
    /// <see cref="ThreadStaticAttribute"/> slot — it does not flow through
    /// <see cref="System.Threading.ExecutionContext"/>. When a test awaits, the
    /// continuation can resume on an arbitrary <see cref="ThreadPool"/> thread whose
    /// <see cref="ThreadStaticAttribute"/> slot is either empty or polluted by
    /// unrelated work (e.g. a sibling parallel test). Installing this SC on the
    /// test-body thread causes every posted continuation to run inside a
    /// save/pin/restore window, so the test's cell is visible to
    /// <c>IActorRef.Tell(message)</c> implicit-sender resolution and to anything
    /// else reading <see cref="InternalCurrentActorCellKeeper.Current"/> from the
    /// continuation.
    /// <para/>
    /// xUnit-agnostic port of the type shipped in <c>Akka.TestKit</c>; it is kept
    /// internal to this assembly so the v3 and xUnit2 test kits each own a copy
    /// without a cross-package dependency. Not intended for use outside the test
    /// kits.
    /// </summary>
    [InternalApi]
    internal sealed class ActorCellKeepingSynchronizationContext : SynchronizationContext
    {
        private readonly Func<Akka.Actor.ActorCell?> _cellSource;
        private readonly SynchronizationContext? _inner;

        /// <summary>
        /// Creates a new <see cref="ActorCellKeepingSynchronizationContext"/>
        /// that pins the given <paramref name="cellSource"/> as
        /// <see cref="InternalCurrentActorCellKeeper.Current"/> for the duration
        /// of every callback posted through it.
        /// </summary>
        /// <param name="cellSource">
        /// A function that resolves the <see cref="Akka.Actor.ActorCell"/> to pin, or
        /// <see langword="null"/> to pin "no implicit sender". Resolved lazily on each continuation so it
        /// stays correct when the TestActor is created after the SynchronizationContext is installed.
        /// </param>
        /// <param name="inner">
        /// An optional outer <see cref="SynchronizationContext"/> to delegate
        /// scheduling to. When non-null, <see cref="Post"/> and <see cref="Send"/>
        /// dispatch through the outer SC (preserving its scheduling) while wrapping
        /// callbacks with the cell-pinning window. When null, falls back to
        /// <see cref="ThreadPool"/> dispatch.
        /// </param>
        public ActorCellKeepingSynchronizationContext(Func<Akka.Actor.ActorCell?> cellSource, SynchronizationContext? inner = null)
        {
            _cellSource = cellSource;
            _inner = inner;
        }

        /// <summary>
        /// Queues the given callback with <see cref="InternalCurrentActorCellKeeper.Current"/>
        /// pinned to the cell this SC was constructed with, then restores the
        /// previously pinned value when the callback returns. Delegates scheduling
        /// to the inner <see cref="SynchronizationContext"/> when available, otherwise
        /// falls back to <see cref="ThreadPool.QueueUserWorkItem(WaitCallback, object)"/>.
        /// </summary>
        public override void Post(SendOrPostCallback d, object? state)
        {
            void WrappedCallback(object? s)
            {
                var oldCell = InternalCurrentActorCellKeeper.Current;
                var oldContext = Current;
                SetSynchronizationContext(this);
                InternalCurrentActorCellKeeper.Current = _cellSource();
                try
                {
                    d(s);
                }
                finally
                {
                    InternalCurrentActorCellKeeper.Current = oldCell;
                    SetSynchronizationContext(oldContext);
                }
            }

            if (_inner != null)
                _inner.Post(WrappedCallback, state);
            else
                ThreadPool.QueueUserWorkItem(WrappedCallback, state);
        }

        /// <summary>
        /// Synchronously dispatches the given callback with cell pinning. Delegates to
        /// the inner <see cref="SynchronizationContext"/> when available, otherwise
        /// falls back to <see cref="Post"/> with a blocking wait.
        /// </summary>
        public override void Send(SendOrPostCallback d, object? state)
        {
            if (_inner != null)
            {
                _inner.Send(_ =>
                {
                    var oldCell = InternalCurrentActorCellKeeper.Current;
                    var oldContext = Current;
                    SetSynchronizationContext(this);
                    InternalCurrentActorCellKeeper.Current = _cellSource();
                    try
                    {
                        d(state);
                    }
                    finally
                    {
                        InternalCurrentActorCellKeeper.Current = oldCell;
                        SetSynchronizationContext(oldContext);
                    }
                }, state);
            }
            else
            {
                var tcs = new TaskCompletionSource<int>();
                Post(_ =>
                {
                    try
                    {
                        d(state);
                        tcs.SetResult(0);
                    }
                    catch (Exception e)
                    {
                        tcs.TrySetException(e);
                    }
                }, state);
                tcs.Task.Wait();
            }
        }

        /// <inheritdoc/>
        public override SynchronizationContext CreateCopy()
            => new ActorCellKeepingSynchronizationContext(_cellSource, _inner);
    }
}
