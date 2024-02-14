﻿// -----------------------------------------------------------------------
//  <copyright file="DiPropsFailTest.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2023 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using Akka.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Akka.Hosting.TestKit.MsTest.Tests;

// Regression test for https://github.com/akkadotnet/Akka.Hosting/issues/343

[TestClass]
public class DiPropsFailTest: TestKit
{
    public DiPropsFailTest() : base(nameof(DiPropsFailTest))
    {}
    
    protected override void ConfigureAkka(AkkaConfigurationBuilder builder, IServiceProvider provider)
    { }

    [TestMethod]
    public void DiTest()
    {
        var actor = Sys.ActorOf(NonRootActorWithDi.Props());
        actor.Tell("test");
        ExpectMsg<string>("test");
    }
    
    private class NonRootActorWithDi: ReceiveActor
    {
        public static Props Props() => DependencyResolver.For(Context.System).Props<NonRootActorWithDi>();
        
        public NonRootActorWithDi(ILogger<NonRootActorWithDi> log)
        {
            ReceiveAny(msg =>
            {
                log.LogInformation("Received {Msg}", msg);
                Sender.Tell(msg);
            });
        }
    }
}