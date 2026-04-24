// -----------------------------------------------------------------------
//  <copyright file="HostingCleanAmbientContextAttribute.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using System;
using System.Reflection;
using System.Threading;
using Akka.Actor;
using Akka.Actor.Internal;
using Akka.TestKit;
using Xunit;
using Xunit.v3;

namespace Akka.Hosting.TestKit.Internals
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    internal sealed class HostingCleanAmbientContextAttribute : BeforeAfterTestAttribute
    {
        // AsyncLocal flows across await boundaries via ExecutionContext, unlike [ThreadStatic].
        // This is critical because xUnit v3's runner awaits the test body between Before() and After(),
        // so After() can resume on a different thread than Before() ran on.
        private static readonly AsyncLocal<SynchronizationContext?> _previousContext = new();
        private static readonly AsyncLocal<bool> _applied = new();

        public override void Before(MethodInfo methodUnderTest, IXunitTest test)
        {
            var instance = TestContext.Current.TestClassInstance;
            if (instance is not TestKitBase testKit)
            {
                _applied.Value = false;
                return;
            }

            _applied.Value = true;
            var cell = testKit is INoImplicitSender ? null : TryGetCell(testKit);

            InternalCurrentActorCellKeeper.Current = cell;
            _previousContext.Value = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(
                new ActorCellKeepingSynchronizationContext(cell, _previousContext.Value));
        }

        public override void After(MethodInfo methodUnderTest, IXunitTest test)
        {
            if (!_applied.Value)
                return;

            _applied.Value = false;
            InternalCurrentActorCellKeeper.Current = null;
            SynchronizationContext.SetSynchronizationContext(_previousContext.Value);
            _previousContext.Value = null;
        }

        private static ActorCell? TryGetCell(TestKitBase testKit)
            => testKit.TestActor is ActorRefWithCell withCell
                ? withCell.Underlying as ActorCell
                : null;
    }
}
