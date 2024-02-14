﻿//-----------------------------------------------------------------------
// <copyright file="WatchAndForwardActor.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2021 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2021 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

namespace Akka.Hosting.TestKit.MsTest.Tests.TestActorRefTests;

public class WatchAndForwardActor : ActorBase
{
    private readonly IActorRef _forwardToActor;

    public WatchAndForwardActor(IActorRef watchedActor, IActorRef forwardToActor)
    {
        _forwardToActor = forwardToActor;
        Context.Watch(watchedActor);
    }

    protected override bool Receive(object message)
    {
        var terminated = message as Terminated;
        if(terminated != null)
            _forwardToActor.Tell(new WrappedTerminated(terminated), Sender);
        else
            _forwardToActor.Tell(message, Sender);
        return true;
    }
}