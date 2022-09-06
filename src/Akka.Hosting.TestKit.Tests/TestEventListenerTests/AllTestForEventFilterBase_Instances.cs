//-----------------------------------------------------------------------
// <copyright file="AllTestForEventFilterBase_Instances.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2022 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using Akka.Event;
using Akka.TestKit;
using Xunit.Abstractions;

namespace Akka.Hosting.TestKit.Tests.TestEventListenerTests
{
    public class EventFilterDebugTests : AllTestForEventFilterBase<Debug>
    {
        public EventFilterDebugTests(ITestOutputHelper output) 
            : base("akka.loglevel=DEBUG", nameof(EventFilterDebugTests), output){}

        protected override EventFilterFactory CreateTestingEventFilter()
        {
            return EventFilter;
        }

        protected override void PublishMessage(object message, string source)
        {
            Sys.EventStream.Publish(new Debug(source,GetType(),message));
        }
    }

    public class CustomEventFilterDebugTests : AllTestForEventFilterBase<Debug>
    {
        public CustomEventFilterDebugTests(ITestOutputHelper output) 
            : base("akka.loglevel=DEBUG", nameof(CustomEventFilterDebugTests), output) { }

        protected override EventFilterFactory CreateTestingEventFilter()
        {
            return CreateEventFilter(Sys);
        }

        protected override void PublishMessage(object message, string source)
        {
            Sys.EventStream.Publish(new Debug(source, GetType(), message));
        }
    }

    public class EventFilterInfoTests : AllTestForEventFilterBase<Info>
    {
        public EventFilterInfoTests(ITestOutputHelper output) 
            : base("akka.loglevel=INFO", nameof(EventFilterInfoTests), output) { }

        protected override EventFilterFactory CreateTestingEventFilter()
        {
            return EventFilter;
        }

        protected override void PublishMessage(object message, string source)
        {
            Sys.EventStream.Publish(new Info(source, GetType(), message));
        }
    }

    public class CustomEventFilterInfoTests : AllTestForEventFilterBase<Info>
    {
        public CustomEventFilterInfoTests(ITestOutputHelper output) : base(
            "akka.loglevel=INFO", nameof(CustomEventFilterInfoTests), output) { }

        protected override EventFilterFactory CreateTestingEventFilter()
        {
            return CreateEventFilter(Sys);
        }

        protected override void PublishMessage(object message, string source)
        {
            Sys.EventStream.Publish(new Info(source, GetType(), message));
        }
    }


    public class EventFilterWarningTests : AllTestForEventFilterBase<Warning>
    {
        public EventFilterWarningTests(ITestOutputHelper output) : base(
            "akka.loglevel=WARNING", nameof(EventFilterWarningTests), output) { }

        protected override EventFilterFactory CreateTestingEventFilter()
        {
            return EventFilter;
        }

        protected override void PublishMessage(object message, string source)
        {
            Sys.EventStream.Publish(new Warning(source, GetType(), message));
        }
    }

    public class CustomEventFilterWarningTests : AllTestForEventFilterBase<Warning>
    {
        public CustomEventFilterWarningTests(ITestOutputHelper output) 
            : base("akka.loglevel=WARNING", nameof(CustomEventFilterWarningTests), output) { }

        protected override EventFilterFactory CreateTestingEventFilter()
        {
            return CreateEventFilter(Sys);
        }

        protected override void PublishMessage(object message, string source)
        {
            Sys.EventStream.Publish(new Warning(source, GetType(), message));
        }
    }

    public class EventFilterErrorTests : AllTestForEventFilterBase<Error>
    {
        public EventFilterErrorTests(ITestOutputHelper output) 
            : base("akka.loglevel=ERROR", nameof(EventFilterErrorTests), output) { }

        protected override EventFilterFactory CreateTestingEventFilter()
        {
            return EventFilter;
        }

        protected override void PublishMessage(object message, string source)
        {
            Sys.EventStream.Publish(new Error(null, source, GetType(), message));
        }
    }

    public class CustomEventFilterErrorTests : AllTestForEventFilterBase<Error>
    {
        public CustomEventFilterErrorTests(ITestOutputHelper output) 
            : base("akka.loglevel=ERROR", nameof(CustomEventFilterErrorTests), output) { }

        protected override EventFilterFactory CreateTestingEventFilter()
        {
            return CreateEventFilter(Sys);
        }

        protected override void PublishMessage(object message, string source)
        {
            Sys.EventStream.Publish(new Error(null, source, GetType(), message));
        }
    }
}

