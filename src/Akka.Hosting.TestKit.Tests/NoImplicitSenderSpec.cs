//-----------------------------------------------------------------------
// <copyright file="NoImplicitSenderSpec.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2022 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.TestKit;
using Akka.Actor.Dsl;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Akka.Hosting.TestKit.Tests
{
    public class NoImplicitSenderSpec : HostingSpec, INoImplicitSender
    {
        [Fact(Skip = "Type assertion on null message causes NullReferenceException")]
        public async Task When_Not_ImplicitSender_then_testActor_is_not_sender()
        {
            var echoActor = Sys.ActorOf(c => c.ReceiveAny((m, ctx) => TestActor.Tell(ctx.Sender)));
            echoActor.Tell("message");
            await ExpectMsgAsync<IActorRef>(actorRef => Equals(actorRef, ActorRefs.NoSender));
        }

        public NoImplicitSenderSpec(ITestOutputHelper output) : base(nameof(NoImplicitSenderSpec), output)
        { }

        protected override void ConfigureAkka(AkkaConfigurationBuilder builder, IServiceProvider provider)
        { }
    }

    public class ImplicitSenderSpec : HostingSpec
    {
        public ImplicitSenderSpec(ITestOutputHelper output) : base(nameof(ImplicitSenderSpec), output)
        { }

        protected override void ConfigureAkka(AkkaConfigurationBuilder builder, IServiceProvider provider)
        { }
        
        [Fact]
        public async Task ImplicitSender_should_have_testActor_as_sender()
        {
            var echoActor = Sys.ActorOf(c => c.ReceiveAny((m, ctx) => TestActor.Tell(ctx.Sender)));
            echoActor.Tell("message");
            await ExpectMsgAsync<IActorRef>(actorRef => Equals(actorRef, TestActor));

            //Test that it works after we know that context has been changed
            echoActor.Tell("message");
            await ExpectMsgAsync<IActorRef>(actorRef => Equals(actorRef, TestActor));
        }


        [Fact]
        public async Task ImplicitSender_should_not_change_when_creating_Testprobes()
        {
            //Verifies that bug #459 has been fixed
            var testProbe = CreateTestProbe();
            TestActor.Tell("message");
            await ReceiveOneAsync();
            LastSender.Should().Be(TestActor);
        }

        [Fact]
        public async Task ImplicitSender_should_not_change_when_creating_TestActors()
        {
            var testActor2 = CreateTestActor("test2");
            TestActor.Tell("message");
            await ReceiveOneAsync();
            LastSender.Should().Be(TestActor);
        }
    }
}

