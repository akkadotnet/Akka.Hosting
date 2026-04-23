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
        [ThreadStatic]
        private static SynchronizationContext? _previousContext;

        [ThreadStatic]
        private static bool _applied;

        public override void Before(MethodInfo methodUnderTest, IXunitTest test)
        {
            var instance = TestContext.Current.TestClassInstance;
            if (instance is not TestKitBase testKit)
            {
                _applied = false;
                return;
            }

            _applied = true;
            var cell = testKit is INoImplicitSender ? null : TryGetCell(testKit);

            InternalCurrentActorCellKeeper.Current = cell;
            _previousContext = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(
                new ActorCellKeepingSynchronizationContext(cell, _previousContext));
        }

        public override void After(MethodInfo methodUnderTest, IXunitTest test)
        {
            if (!_applied)
                return;

            _applied = false;
            InternalCurrentActorCellKeeper.Current = null;
            SynchronizationContext.SetSynchronizationContext(_previousContext);
            _previousContext = null;
        }

        private static ActorCell? TryGetCell(TestKitBase testKit)
            => testKit.TestActor is ActorRefWithCell withCell
                ? withCell.Underlying as ActorCell
                : null;
    }
}
