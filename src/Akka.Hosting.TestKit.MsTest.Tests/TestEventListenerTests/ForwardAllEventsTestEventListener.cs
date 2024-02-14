﻿//-----------------------------------------------------------------------
// <copyright file="ForwardAllEventsTestEventListener.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2021 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2021 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using Akka.Event;
using Akka.TestKit;

namespace Akka.Hosting.TestKit.MsTest.Tests.TestEventListenerTests;

[TestClass]
public class ForwardAllEventsTestEventListener : TestEventListener
{
    private IActorRef? _forwarder;

    protected override void Print(LogEvent m)
    {           
        if(m.Message is ForwardAllEventsTo to)
        {
            _forwarder = to.Forwarder;
            _forwarder.Tell("OK");
        }
        else if(_forwarder != null)
        {
            _forwarder.Forward(m);
        }
        else
        {
            base.Print(m);
        }
    }

    public class ForwardAllEventsTo
    {
        public ForwardAllEventsTo(IActorRef forwarder)
        {
            Forwarder = forwarder;
        }

        public IActorRef Forwarder { get; }
    }
}