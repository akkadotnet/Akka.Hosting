//-----------------------------------------------------------------------
// <copyright file="ExceptionEventFilterTests.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2021 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2021 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using Akka.Event;

namespace Akka.Hosting.TestKit.MsTest.Tests.TestEventListenerTests;

[TestClass]
public class ExceptionEventFilterTests : EventFilterTestBase
{
    public ExceptionEventFilterTests()
        : base(Event.LogLevel.ErrorLevel)
    {
    }
    
    public class SomeException : Exception { }

    protected override void SendRawLogEventMessage(object message)
    {
        Sys.EventStream.Publish(new Error(null, nameof(ExceptionEventFilterTests), GetType(), message));
    }

    [TestMethod]
    public void SingleExceptionIsIntercepted()
    {
        EventFilter.Exception<SomeException>()
            .ExpectOne(() => Log.Error(new SomeException(), "whatever"));
        ExpectNoMsg(TimeSpan.FromMilliseconds(100));
    }

    [TestMethod]
    public void CanInterceptMessagesWhenStartIsSpecified()
    {
        EventFilter.Exception<SomeException>(start: "what")
            .ExpectOne(() => Log.Error(new SomeException(), "whatever"));
        ExpectNoMsg(TimeSpan.FromMilliseconds(100));
    }

    [TestMethod]
    public void DoNotInterceptMessagesWhenStartDoesNotMatch()
    {
        EventFilter.Exception<SomeException>(start: "this is clearly not in message");
        Log.Error(new SomeException(), "whatever");
        ExpectMsg<Error>(err => (string)err.Message == "whatever");
    }

    [TestMethod]
    public void CanInterceptMessagesWhenMessageIsSpecified()
    {
        EventFilter.Exception<SomeException>(message: "whatever")
            .ExpectOne(() => Log.Error(new SomeException(), "whatever"));
        ExpectNoMsg(TimeSpan.FromMilliseconds(100));
    }

    [TestMethod]
    public void DoNotInterceptMessagesWhenMessageDoesNotMatch()
    {
        EventFilter.Exception<SomeException>(message: "this is clearly not the message");
        Log.Error(new SomeException(), "whatever");
        ExpectMsg<Error>(err => (string)err.Message == "whatever");
    }

    [TestMethod]
    public void CanInterceptMessagesWhenContainsIsSpecified()
    {
        EventFilter.Exception<SomeException>(contains: "ate")
            .ExpectOne(() => Log.Error(new SomeException(), "whatever"));
        ExpectNoMsg(TimeSpan.FromMilliseconds(100));
    }

    [TestMethod]
    public void DoNotInterceptMessagesWhenContainsDoesNotMatch()
    {
        EventFilter.Exception<SomeException>(contains: "this is clearly not in the message");
        Log.Error(new SomeException(), "whatever");
        ExpectMsg<Error>(err => (string)err.Message == "whatever");
    }


    [TestMethod]
    public void CanInterceptMessagesWhenSourceIsSpecified()
    {
        EventFilter.Exception<SomeException>(source: LogSource.Create(this, Sys).Source)
            .ExpectOne(() =>
            {
                Log.Error(new SomeException(), "whatever");
            });
        ExpectNoMsg(TimeSpan.FromMilliseconds(100));
    }

    [TestMethod]
    public void DoNotInterceptMessagesWhenSourceDoesNotMatch()
    {
        EventFilter.Exception<SomeException>(source: "this is clearly not the source");
        Log.Error(new SomeException(), "whatever");
        ExpectMsg<Error>(err => (string)err.Message == "whatever");
    }


    [TestMethod]
    public void SpecifiedNumbersOfExceptionsCanBeIntercepted()
    {
        EventFilter.Exception<SomeException>()
            .Expect(2, () =>
            {
                Log.Error(new SomeException(), "whatever");
                Log.Error(new SomeException(), "whatever");
            });
        ExpectNoMsg(TimeSpan.FromMilliseconds(100));
    }

    [TestMethod]
    public void ShouldFailIfMoreExceptionsThenSpecifiedAreLogged()
    {
        Invoking(() =>
            EventFilter.Exception<SomeException>().Expect(2, () =>
            {
                Log.Error(new SomeException(), "whatever");
                Log.Error(new SomeException(), "whatever");
                Log.Error(new SomeException(), "whatever");
            }))
            .Should().Throw<AssertFailedException>().WithMessage("*1 message too many*");
    }

    [TestMethod]
    public void ShouldReportCorrectMessageCount()
    {
        var toSend = "Eric Cartman";
        var actor = ActorOf( ExceptionTestActor.Props() );

        EventFilter
            .Exception<InvalidOperationException>(source: actor.Path.ToString())
            // expecting 2 because the same exception is logged in PostRestart
            .Expect(2, () => actor.Tell( toSend ));
    }

    internal sealed class ExceptionTestActor : UntypedActor
    {
        private ILoggingAdapter Log { get; } = Context.GetLogger();

        protected override void PostRestart(Exception reason)
        {
            Log.Error(reason, "[PostRestart]");
            base.PostRestart(reason);
        }

        protected override void OnReceive( object message )
        {
            switch (message)
            {
                case string msg:
                    throw new InvalidOperationException( "I'm sailing away. Set an open course" );

                default:
                    Unhandled( message );
                    break;
            }
        }

        public static Props Props()
        {
            return Actor.Props.Create( () => new ExceptionTestActor() );
        }
    }
}