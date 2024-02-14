// -----------------------------------------------------------------------
//  <copyright file="HostingSpecSpec.cs" company="Akka.NET Project">
//      Copyright (C) 2009-2022 Lightbend Inc. <http://www.lightbend.com>
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using Akka.Event;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Akka.Hosting.TestKit.MsTest.Tests;

[TestClass]
public class HostingSpecSpec: TestKit
{
    private enum Echo
    { }

    public HostingSpecSpec()
        : base(nameof(HostingSpecSpec), logLevel: LogLevel.Debug)
    {
    }

    protected override void ConfigureAkka(AkkaConfigurationBuilder builder, IServiceProvider provider)
    {
        builder.WithActors((system, registry) =>
        {
            var echo = system.ActorOf(Props.Create(() => new SimpleEchoActor()));
            registry.Register<Echo>(echo);
        });
    }

    [TestMethod]
    public void ActorTest()
    {
        var echo = ActorRegistry.Get<Echo>();
        var probe = CreateTestProbe();
            
        echo.Tell("TestMessage", probe);
        var msg = probe.ExpectMsg("TestMessage");
        Log.Info(msg);
    }
        
    private class SimpleEchoActor : ReceiveActor
    {
        public SimpleEchoActor()
        {
            var log = Context.GetLogger();
                
            ReceiveAny(msg =>
            {
                log.Info($"Received {msg}");
                Sender.Tell(msg);
            });
        }
    }
}