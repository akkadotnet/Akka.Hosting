﻿//-----------------------------------------------------------------------
// <copyright file="CustomEventFilterTests.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2021 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2021 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using Akka.Event;
using Akka.TestKit;
using Xunit;

namespace Akka.Hosting.TestKit.Tests.TestEventListenerTests;

public abstract class CustomEventFilterTestsBase : EventFilterTestBase
{
    // ReSharper disable ConvertToLambdaExpression
    public CustomEventFilterTestsBase() : base(Event.LogLevel.ErrorLevel) { }

    protected override void SendRawLogEventMessage(object message)
    {
        Sys.EventStream.Publish(new Error(null, "CustomEventFilterTests", GetType(), message));
    }

    protected abstract EventFilterFactory CreateTestingEventFilter();

    [Fact]
    public void Custom_filter_should_match()
    {
        var eventFilter = CreateTestingEventFilter();
        eventFilter.Custom(logEvent => logEvent is Error && (string) logEvent.Message == "whatever").ExpectOne(() =>
        {
            Log.Error("whatever");
        });
    }

    [Fact]
    public void Custom_filter_should_match2()
    {
        var eventFilter = CreateTestingEventFilter();
        eventFilter.Custom<Error>(logEvent => (string)logEvent.Message == "whatever").ExpectOne(() =>
        {
            Log.Error("whatever");
        });
    }
    // ReSharper restore ConvertToLambdaExpression
}

public class CustomEventFilterTests : CustomEventFilterTestsBase
{
    protected override EventFilterFactory CreateTestingEventFilter()
    {
        return EventFilter;
    }
}

public class CustomEventFilterCustomFilterTests : CustomEventFilterTestsBase
{
    protected override EventFilterFactory CreateTestingEventFilter()
    {
        return CreateEventFilter(Sys);
    }
}