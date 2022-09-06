//-----------------------------------------------------------------------
// <copyright file="RemainingTests.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2022 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using Xunit;

namespace Akka.Hosting.TestKit.Tests.TestKitBaseTests
{
    public class RemainingTests : Akka.TestKit.Xunit2.TestKit
    {
        [Fact]
        public void Throw_if_remaining_is_called_outside_Within()
        {
            Assert.Throws<InvalidOperationException>(() => Remaining);
        }
    }
}

