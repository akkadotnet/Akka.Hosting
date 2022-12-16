// -----------------------------------------------------------------------
//  <copyright file="UtilSpec.cs" company="Akka.NET Project">
//      Copyright (C) 2009-2022 Lightbend Inc. <http://www.lightbend.com>
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using Akka.Configuration;
using FluentAssertions;
using Xunit;

namespace Akka.Hosting.Tests;

public class UtilSpec
{
    [Fact(DisplayName = "Config.MoveTo() should move config to a new path properly")]
    public void MoveToSpec()
    {
        var hocon = (Config)"a: 1, b: { c: 2 }";
        var moved = hocon.MoveTo("x.y.z");
        moved.GetInt("x.y.z.a").Should().Be(1);
        moved.GetInt("x.y.z.b.c").Should().Be(2);
    }
}