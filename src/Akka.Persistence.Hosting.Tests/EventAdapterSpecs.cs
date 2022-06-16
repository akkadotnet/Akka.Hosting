using System;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Hosting;
using Akka.Persistence.Journal;
using Akka.Util;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Akka.Persistence.Hosting.Tests;

public class EventAdapterSpecs
{
    public static async Task<IHost> StartHost(Action<IServiceCollection> testSetup)
    {
        var host = new HostBuilder()
            .ConfigureServices(testSetup).Build();
        
        await host.StartAsync();
        return host;
    }
    
    public sealed class Event1{ }
    public sealed class Event2{ }

    public sealed class EventMapper1 : IWriteEventAdapter
    {
        public string Manifest(object evt)
        {
            return string.Empty;
        }

        public object ToJournal(object evt)
        {
            return evt;
        }
    }

    public sealed class Tagger : IWriteEventAdapter
    {
        public string Manifest(object evt)
        {
            return string.Empty;
        }

        public object ToJournal(object evt)
        {
            if (evt is Tagged t)
                return t;
            return new Tagged(evt, new[] { "foo" });
        }
    }

    public sealed class ReadAdapter : IReadEventAdapter
    {
        public IEventSequence FromJournal(object evt, string manifest)
        {
            return new SingleEventSequence(evt);
        }
    }

    public sealed class ComboAdapter : IEventAdapter
    {
        public string Manifest(object evt)
        {
            return string.Empty;
        }

        public object ToJournal(object evt)
        {
            return evt;
        }

        public IEventSequence FromJournal(object evt, string manifest)
        {
            return new SingleEventSequence(evt);
        }
    }
    
    [Fact]
    public async Task Should_use_correct_EventAdapter_bindings()
    {
        // arrange
        using var host = await StartHost(collection => collection.AddAkka("MySys", builder =>
        {
            builder.WithJournal("sql-server", journalBuilder =>
            {
                journalBuilder.AddWriteEventAdapter<EventMapper1>("mapper1", new Type[] { typeof(Event1) });
                journalBuilder.AddReadEventAdapter<ReadAdapter>("reader1", new Type[] { typeof(Event1) });
                journalBuilder.AddEventAdapter<ComboAdapter>("combo", boundTypes: new Type[] { typeof(Event2) });
                journalBuilder.AddWriteEventAdapter<Tagger>("tagger",
                    boundTypes: new Type[] { typeof(Event1), typeof(Event2) });
            });
        }));

        // act
        var sys = host.Services.GetRequiredService<ActorSystem>();
        var config = sys.Settings.Config;
        var sqlPersistenceJournal = config.GetConfig("akka.persistence.journal.sql-server");
        
        // assert
        sqlPersistenceJournal.GetStringList($"event-adapter-bindings.\"{typeof(Event1).TypeQualifiedName()}\"").Should()
            .BeEquivalentTo("mapper1", "reader1", "tagger");
        sqlPersistenceJournal.GetStringList($"event-adapter-bindings.\"{typeof(Event2).TypeQualifiedName()}\"").Should()
            .BeEquivalentTo("combo", "tagger");
        
        sqlPersistenceJournal.GetString("event-adapters.mapper1").Should().Be(typeof(EventMapper1).TypeQualifiedName());
        sqlPersistenceJournal.GetString("event-adapters.reader1").Should().Be(typeof(ReadAdapter).TypeQualifiedName());
        sqlPersistenceJournal.GetString("event-adapters.combo").Should().Be(typeof(ComboAdapter).TypeQualifiedName());
        sqlPersistenceJournal.GetString("event-adapters.tagger").Should().Be(typeof(Tagger).TypeQualifiedName());
    }
}