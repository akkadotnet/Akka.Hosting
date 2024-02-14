﻿//-----------------------------------------------------------------------
// <copyright file="DeadLettersEventFilterTests.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2021 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2021 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using Akka.Event;
using Akka.TestKit;
using Akka.TestKit.TestActors;

namespace Akka.Hosting.TestKit.MsTest.Tests.TestEventListenerTests;

public abstract class DeadLettersEventFilterTestsBase : EventFilterTestBase
{
    private IActorRef? _deadActor;

    // ReSharper disable ConvertToLambdaExpression
    protected DeadLettersEventFilterTestsBase() : base(Event.LogLevel.ErrorLevel)
    {
    }

    protected override async Task BeforeTestStart()
    {
        await base.BeforeTestStart();
        _deadActor = Sys.ActorOf(BlackHoleActor.Props, "dead-actor");
        Watch(_deadActor);
        Sys.Stop(_deadActor);
        ExpectTerminated(_deadActor);
    }

    protected override void SendRawLogEventMessage(object message)
    {
        Sys.EventStream.Publish(new Error(null, "DeadLettersEventFilterTests", GetType(), message));
    }

    protected abstract EventFilterFactory CreateTestingEventFilter();

    [TestMethod]
    public void Should_be_able_to_filter_dead_letters()
    {
        var eventFilter = CreateTestingEventFilter();
        eventFilter.DeadLetter().ExpectOne(() =>
        {
            _deadActor.Tell("whatever");
        });
    }


    // ReSharper restore ConvertToLambdaExpression
}

[TestClass]
public class DeadLettersEventFilterTests : DeadLettersEventFilterTestsBase
{
    protected override EventFilterFactory CreateTestingEventFilter()
    {
        return EventFilter;
    }
}

[TestClass]
public class DeadLettersCustomEventFilterTests : DeadLettersEventFilterTestsBase
{
    protected override EventFilterFactory CreateTestingEventFilter()
    {
        return CreateEventFilter(Sys);
    }
}