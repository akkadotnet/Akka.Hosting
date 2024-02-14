//-----------------------------------------------------------------------
// <copyright file="WrappedTerminated.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2021 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2021 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

namespace Akka.Hosting.TestKit.MsTest.Tests.TestActorRefTests;

public class WrappedTerminated
{
    private readonly Terminated _terminated;

    public WrappedTerminated(Terminated terminated)
    {
        _terminated = terminated;
    }

    public Terminated Terminated { get { return _terminated; } }
}