//-----------------------------------------------------------------------
// <copyright file="DeadLettersEventFilterTests.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2022 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Event;
using Akka.TestKit;
using Akka.TestKit.TestActors;
using Xunit;
using Xunit.Abstractions;

namespace Akka.Hosting.TestKit.Tests.TestEventListenerTests
{
    public abstract class DeadLettersEventFilterTestsBase : EventFilterTestBase
    {
        private enum DeadActorKey
        { }
        
        private IActorRef _deadActor;

        protected DeadLettersEventFilterTestsBase(string systemName, ITestOutputHelper output) 
            : base("akka.loglevel=ERROR", systemName, output)
        {
            
        }

        protected override void ConfigureAkka(AkkaConfigurationBuilder builder, IServiceProvider provider)
        {
            base.ConfigureAkka(builder, provider);
            builder.WithActors((system, registry) =>
            {
                var actor = system.ActorOf(BlackHoleActor.Props, "dead-actor");
                registry.Register<DeadActorKey>(actor);
            });
        }

        protected override async Task BeforeTestStart()
        {
            _deadActor = ActorRegistry.Get<DeadActorKey>();
            Watch(_deadActor);
            Sys.Stop(_deadActor);
            await ExpectTerminatedAsync(_deadActor);
        }

        protected override void SendRawLogEventMessage(object message)
        {
            Sys.EventStream.Publish(new Error(null, "DeadLettersEventFilterTests", GetType(), message));
        }

        protected abstract EventFilterFactory CreateTestingEventFilter();

        [Fact]
        public async Task Should_be_able_to_filter_dead_letters()
        {
            var eventFilter = CreateTestingEventFilter();
            await eventFilter.DeadLetter().ExpectOneAsync(() =>
            {
                _deadActor.Tell("whatever");
                return Task.CompletedTask;
            });
        }
    }

    public class DeadLettersEventFilterTests : DeadLettersEventFilterTestsBase
    {
        protected override EventFilterFactory CreateTestingEventFilter()
        {
            return EventFilter;
        }

        public DeadLettersEventFilterTests(ITestOutputHelper output) : base(nameof(DeadLettersEventFilterTests), output)
        {
        }
    }

    public class DeadLettersCustomEventFilterTests : DeadLettersEventFilterTestsBase
    {
        protected override EventFilterFactory CreateTestingEventFilter()
        {
            return CreateEventFilter(Sys);
        }

        public DeadLettersCustomEventFilterTests(ITestOutputHelper output) : base(nameof(DeadLettersCustomEventFilterTests), output)
        {
        }
    }
}

