// -----------------------------------------------------------------------
//  <copyright file="ActorCellKeepingSynchronizationContext.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Actor.Internal;

namespace Akka.Hosting.TestKit.Internals
{
    /// <summary>
    /// A decorator <see cref="SynchronizationContext"/> that preserves the outer
    /// SC's scheduling (e.g. xUnit v3's MaxConcurrencySyncContext) while pinning
    /// <see cref="InternalCurrentActorCellKeeper.Current"/> around every callback.
    /// When no outer SC exists, falls back to <see cref="ThreadPool"/> dispatch.
    /// </summary>
    internal sealed class ActorCellKeepingSynchronizationContext : SynchronizationContext
    {
        private readonly ActorCell? _cell;
        private readonly SynchronizationContext? _inner;

        internal ActorCellKeepingSynchronizationContext(ActorCell? cell, SynchronizationContext? inner)
        {
            _cell = cell;
            _inner = inner;
        }

        public override void Post(SendOrPostCallback d, object? state)
        {
            void WrappedCallback(object? s)
            {
                var oldCell = InternalCurrentActorCellKeeper.Current;
                var oldCtx = Current;
                SetSynchronizationContext(this);
                InternalCurrentActorCellKeeper.Current = _cell;
                try
                {
                    d(s);
                }
                finally
                {
                    InternalCurrentActorCellKeeper.Current = oldCell;
                    SetSynchronizationContext(oldCtx);
                }
            }

            if (_inner != null)
                _inner.Post(WrappedCallback, state);
            else
                ThreadPool.QueueUserWorkItem(WrappedCallback, state);
        }

        public override void Send(SendOrPostCallback d, object? state)
        {
            if (_inner != null)
            {
                _inner.Send(_ =>
                {
                    var oldCell = InternalCurrentActorCellKeeper.Current;
                    var oldCtx = Current;
                    SetSynchronizationContext(this);
                    InternalCurrentActorCellKeeper.Current = _cell;
                    try
                    {
                        d(state);
                    }
                    finally
                    {
                        InternalCurrentActorCellKeeper.Current = oldCell;
                        SetSynchronizationContext(oldCtx);
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

        public override SynchronizationContext CreateCopy()
            => new ActorCellKeepingSynchronizationContext(_cell, _inner);
    }
}
