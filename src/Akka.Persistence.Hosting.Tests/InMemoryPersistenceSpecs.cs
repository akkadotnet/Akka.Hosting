﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Event;
using Akka.Hosting;
using Akka.TestKit.Xunit2.Internals;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;
using Xunit.Abstractions;

namespace Akka.Persistence.Hosting.Tests
{
    public class InMemoryPersistenceSpecs
    {
        
        private readonly ITestOutputHelper _output;

        public InMemoryPersistenceSpecs(ITestOutputHelper output)
        {
            _output = output;
        }

        public sealed class MyPersistenceActor : ReceivePersistentActor
        {
            private List<int> _values = new List<int>();

            public MyPersistenceActor(string persistenceId)
            {
                PersistenceId = persistenceId;
                
                Recover<SnapshotOffer>(offer =>
                {
                    if (offer.Snapshot is IEnumerable<int> ints)
                    {
                        _values = new List<int>(ints);
                    }
                });
                
                Recover<int>(i =>
                {
                    _values.Add(i);
                });
                
                Command<int>(i =>
                {
                    Persist(i, i1 =>
                    {
                        _values.Add(i);
                        if (LastSequenceNr % 2 == 0)
                        {
                            SaveSnapshot(_values);
                        }
                        Sender.Tell("ACK");
                    });
                });

                Command<string>(str => str.Equals("getall"), s =>
                {
                    Sender.Tell(_values.ToArray());
                });
                
                Command<SaveSnapshotSuccess>(s => {});
            }

            public override string PersistenceId { get; }
        }
        
        public static async Task<IHost> StartHost(Action<IServiceCollection> testSetup)
        {
            var host = new HostBuilder()
                .ConfigureServices(testSetup).Build();
        
            await host.StartAsync();
            return host;
        }

        [Fact]
        public async Task Should_Start_ActorSystem_wth_InMemory_Persistence()
        {
            // arrange
            using var host = await StartHost(collection => collection.AddAkka("MySys", builder =>
            {
                builder.WithInMemoryJournal().WithInMemorySnapshotStore()
                    .StartActors((system, registry) =>
                    {
                        var myActor = system.ActorOf(Props.Create(() => new MyPersistenceActor("ac1")), "actor1");
                        registry.Register<MyPersistenceActor>(myActor);
                    })
                    .WithActors((system, registry) =>
                    {
                        var extSystem = (ExtendedActorSystem)system;
                        var logger = extSystem.SystemActorOf(Props.Create(() => new TestOutputLogger(_output)), "log-test");
                        logger.Tell(new InitializeLogger(system.EventStream));
                    });;
            }));

            var actorSystem = host.Services.GetRequiredService<ActorSystem>();
            var actorRegistry = host.Services.GetRequiredService<ActorRegistry>();
            var myPersistentActor = actorRegistry.Get<MyPersistenceActor>();
            
            // act
            var resp1 = await myPersistentActor.Ask<string>(1, TimeSpan.FromSeconds(3));
            var resp2 = await myPersistentActor.Ask<string>(2, TimeSpan.FromSeconds(3));
            var snapshot = await myPersistentActor.Ask<int[]>("getall", TimeSpan.FromSeconds(3));

            // assert
            snapshot.Should().BeEquivalentTo(new[] {1, 2});

            // kill + recreate actor with same PersistentId
            await myPersistentActor.GracefulStop(TimeSpan.FromSeconds(3));
            var myPersistentActor2 = actorSystem.ActorOf(Props.Create(() => new MyPersistenceActor("ac1")), "actor1a");
            
            var snapshot2 = await myPersistentActor2.Ask<int[]>("getall", TimeSpan.FromSeconds(3));
            snapshot2.Should().BeEquivalentTo(new[] {1, 2});
            
            // validate configs
            var config = actorSystem.Settings.Config;
            config.GetString("akka.persistence.journal.plugin").Should().Be("akka.persistence.journal.inmem");
            config.GetString("akka.persistence.snapshot-store.plugin").Should().Be("akka.persistence.snapshot-store.inmem");
        }
    }
}