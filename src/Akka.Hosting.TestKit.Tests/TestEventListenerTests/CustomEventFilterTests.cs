//-----------------------------------------------------------------------
// <copyright file="CustomEventFilterTests.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2022 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System.Threading.Tasks;
using Akka.Event;
using Akka.TestKit;
using Xunit;
using Xunit.Abstractions;

namespace Akka.Hosting.TestKit.Tests.TestEventListenerTests
{
    public abstract class CustomEventFilterTestsBase : EventFilterTestBase
    {
        protected CustomEventFilterTestsBase(string systemName, ITestOutputHelper output) 
            : base("akka.loglevel=ERROR", systemName, output) { }

        protected override void SendRawLogEventMessage(object message)
        {
            Sys.EventStream.Publish(new Error(null, "CustomEventFilterTests", GetType(), message));
        }

        protected abstract EventFilterFactory CreateTestingEventFilter();

        [Fact]
        public async Task Custom_filter_should_match()
        {
            var eventFilter = CreateTestingEventFilter();
            await eventFilter.Custom(logEvent => logEvent is Error && (string) logEvent.Message == "whatever")
                .ExpectOneAsync(() =>
                {
                    Log.Error("whatever");
                    return Task.CompletedTask;
                });
        }

        [Fact]
        public async Task Custom_filter_should_match2()
        {
            var eventFilter = CreateTestingEventFilter();
            await eventFilter.Custom<Error>(logEvent => (string)logEvent.Message == "whatever")
                .ExpectOneAsync(() =>
                {
                    Log.Error("whatever");
                    return Task.CompletedTask;
                });
        }
    }

    public class CustomEventFilterTests : CustomEventFilterTestsBase
    {
        protected override EventFilterFactory CreateTestingEventFilter()
        {
            return EventFilter;
        }

        public CustomEventFilterTests(ITestOutputHelper output) : base(nameof(CustomEventFilterTests), output)
        {
        }
    }

    public class CustomEventFilterCustomFilterTests : CustomEventFilterTestsBase
    {
        protected override EventFilterFactory CreateTestingEventFilter()
        {
            return CreateEventFilter(Sys);
        }

        public CustomEventFilterCustomFilterTests(ITestOutputHelper output) : base(nameof(CustomEventFilterCustomFilterTests), output)
        {
        }
    }
}

