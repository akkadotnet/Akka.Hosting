using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Configuration;
using Akka.Hosting;
using Akka.Persistence.Journal;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Akka.Persistence.Hosting.Tests;

/// <summary>
/// Tests that verify event adapters are actually INVOKED at runtime when using the NEW callback API.
/// This is a regression test for https://github.com/akkadotnet/Akka.Persistence.Sql/issues/552
/// where event adapters were present in HOCON but never invoked at runtime.
/// </summary>
public class EventAdapterRuntimeInvocationSpecs : Akka.Hosting.TestKit.TestKit
{
    private readonly ITestOutputHelper _output;

    public EventAdapterRuntimeInvocationSpecs(ITestOutputHelper output)
    {
        _output = output;
    }

    #region Test Events and Actors

    public sealed class TestEvent
    {
        public TestEvent(string data) { Data = data; }
        public string Data { get; }
    }

    public sealed class InvocationCountingAdapter : IWriteEventAdapter
    {
        public static int CallCount = 0;

        public InvocationCountingAdapter()
        {
            // Parameterless constructor for Akka.NET instantiation
        }

        public InvocationCountingAdapter(ExtendedActorSystem system)
        {
            // Constructor with ActorSystem parameter (also supported)
        }

        public string Manifest(object evt) => string.Empty;

        public object ToJournal(object evt)
        {
            Interlocked.Increment(ref CallCount);

            // Tag the event so we can verify it worked
            return evt switch
            {
                TestEvent => new Tagged(evt, new[] { "test-tag" }),
                _ => evt
            };
        }
    }

    public sealed class TestPersistentActor : ReceivePersistentActor
    {
        private readonly List<string> _events = new();

        public sealed class SaveEvent
        {
            public SaveEvent(string data) { Data = data; }
            public string Data { get; }
        }

        public sealed class GetEvents
        {
            public static readonly GetEvents Instance = new();
            private GetEvents() { }
        }

        public TestPersistentActor(string persistenceId)
        {
            PersistenceId = persistenceId;

            Command<SaveEvent>(cmd =>
            {
                var evt = new TestEvent(cmd.Data);
                Persist(evt, _ =>
                {
                    _events.Add(cmd.Data);
                    Sender.Tell("OK");
                });
            });

            Command<GetEvents>(_ =>
            {
                Sender.Tell(_events.ToArray());
            });

            Recover<TestEvent>(evt =>
            {
                _events.Add(evt.Data);
            });
        }

        public override string PersistenceId { get; }
    }

    #endregion

    protected override void ConfigureAkka(AkkaConfigurationBuilder builder, IServiceProvider provider)
    {
        // Use NEW callback API to add event adapters
        builder.WithInMemoryJournal(
            journalBuilder: journal =>
            {
                journal.AddWriteEventAdapter<InvocationCountingAdapter>(
                    "counting-adapter",
                    new[] { typeof(TestEvent) });
            });
    }

    [Fact]
    public async Task EventAdapter_Should_Be_Invoked_At_Runtime_With_New_Callback_API()
    {
        // Reset the counter
        InvocationCountingAdapter.CallCount = 0;

        // Verify adapter is in HOCON configuration
        var config = Sys.Settings.Config;
        var journalConfig = config.GetConfig("akka.persistence.journal.inmem");

        _output.WriteLine("=== HOCON Configuration ===");
        _output.WriteLine(journalConfig.ToString());

        journalConfig.HasPath("event-adapters").Should().BeTrue("event-adapters should be in HOCON");
        journalConfig.HasPath("event-adapter-bindings").Should().BeTrue("event-adapter-bindings should be in HOCON");

        // Create persistent actor
        var actor = Sys.ActorOf(Props.Create(() => new TestPersistentActor("test-1")));

        // Persist 3 events
        await actor.Ask<string>(new TestPersistentActor.SaveEvent("event-1"), TimeSpan.FromSeconds(3));
        await actor.Ask<string>(new TestPersistentActor.SaveEvent("event-2"), TimeSpan.FromSeconds(3));
        await actor.Ask<string>(new TestPersistentActor.SaveEvent("event-3"), TimeSpan.FromSeconds(3));

        // CRITICAL ASSERTION: Verify the adapter was actually INVOKED at runtime
        await AwaitAssertAsync(() =>
        {
            _output.WriteLine($"Adapter was called {InvocationCountingAdapter.CallCount} times");
            InvocationCountingAdapter.CallCount.Should().Be(3,
                "event adapter should be invoked once for each persisted event");
        });

        // Verify events were persisted correctly
        var events = await actor.Ask<string[]>(TestPersistentActor.GetEvents.Instance, TimeSpan.FromSeconds(3));
        events.Should().BeEquivalentTo(new[] { "event-1", "event-2", "event-3" });
    }
}

/// <summary>
/// Tests the scenario from issue #552 where event adapters fail when
/// WithInMemoryJournal (with adapters) is followed by a second journal configuration.
/// This mimics the pattern: WithSqlPersistence (with adapters) + WithJournalAndSnapshot (for sharding).
/// </summary>
public class EventAdapterWithMultipleJournalConfigsSpecs : Akka.Hosting.TestKit.TestKit
{
    private readonly ITestOutputHelper _output;

    public EventAdapterWithMultipleJournalConfigsSpecs(ITestOutputHelper output)
    {
        _output = output;
    }

    #region Reuse test infrastructure from EventAdapterRuntimeInvocationSpecs

    public sealed class TestEvent
    {
        public TestEvent(string data) { Data = data; }
        public string Data { get; }
    }

    public sealed class InvocationCountingAdapter : IWriteEventAdapter
    {
        public static int CallCount = 0;

        public InvocationCountingAdapter() { }
        public InvocationCountingAdapter(ExtendedActorSystem system) { }

        public string Manifest(object evt) => string.Empty;

        public object ToJournal(object evt)
        {
            Interlocked.Increment(ref CallCount);
            return evt switch
            {
                TestEvent => new Tagged(evt, new[] { "test-tag" }),
                _ => evt
            };
        }
    }

    public sealed class TestPersistentActor : ReceivePersistentActor
    {
        private readonly List<string> _events = new();

        public sealed class SaveEvent
        {
            public SaveEvent(string data) { Data = data; }
            public string Data { get; }
        }

        public sealed class GetEvents
        {
            public static readonly GetEvents Instance = new();
            private GetEvents() { }
        }

        public TestPersistentActor(string persistenceId, string? journalPluginId = null)
        {
            PersistenceId = persistenceId;
            if (journalPluginId != null)
                JournalPluginId = journalPluginId;

            Command<SaveEvent>(cmd =>
            {
                var evt = new TestEvent(cmd.Data);
                Persist(evt, _ =>
                {
                    _events.Add(cmd.Data);
                    Sender.Tell("OK");
                });
            });

            Command<GetEvents>(_ =>
            {
                Sender.Tell(_events.ToArray());
            });

            Recover<TestEvent>(evt =>
            {
                _events.Add(evt.Data);
            });
        }

        public override string PersistenceId { get; }
    }

    #endregion

    protected override void ConfigureAkka(AkkaConfigurationBuilder builder, IServiceProvider provider)
    {
        // STEP 1: Configure default journal with event adapters using NEW callback API
        builder.WithInMemoryJournal(
            journalBuilder: journal =>
            {
                journal.AddWriteEventAdapter<InvocationCountingAdapter>(
                    "counting-adapter",
                    new[] { typeof(TestEvent) });
            },
            isDefaultPlugin: true);

        // STEP 2: Configure a second journal (like sharding does)
        // This mimics the pattern from issue #552:
        //   builder.WithSqlPersistence(..., journalBuilder: ...)
        //   builder.WithJournalAndSnapshot(shardingJournalOptions, shardingSnapshotOptions)
        builder.WithInMemoryJournal(
            journalBuilder: _ => { },  // No adapters on sharding journal
            journalId: "sharding",
            isDefaultPlugin: false);
    }

    [Fact]
    public async Task EventAdapter_Should_Still_Work_After_Second_Journal_Config()
    {
        // Reset the counter
        InvocationCountingAdapter.CallCount = 0;

        // Verify both journals exist in config
        var config = Sys.Settings.Config;
        config.HasPath("akka.persistence.journal.inmem").Should().BeTrue("default journal should exist");
        config.HasPath("akka.persistence.journal.sharding").Should().BeTrue("sharding journal should exist");

        // Verify adapter is in HOCON for default journal
        var defaultJournalConfig = config.GetConfig("akka.persistence.journal.inmem");

        _output.WriteLine("=== Default Journal Configuration ===");
        _output.WriteLine(defaultJournalConfig.ToString());

        defaultJournalConfig.HasPath("event-adapters").Should().BeTrue(
            "event-adapters should be in HOCON for default journal");
        defaultJournalConfig.HasPath("event-adapter-bindings").Should().BeTrue(
            "event-adapter-bindings should be in HOCON for default journal");

        // Create persistent actor using DEFAULT journal
        var actor = Sys.ActorOf(Props.Create(() => new TestPersistentActor("test-multi-1", null)));

        // Persist 3 events
        await actor.Ask<string>(new TestPersistentActor.SaveEvent("event-1"), TimeSpan.FromSeconds(3));
        await actor.Ask<string>(new TestPersistentActor.SaveEvent("event-2"), TimeSpan.FromSeconds(3));
        await actor.Ask<string>(new TestPersistentActor.SaveEvent("event-3"), TimeSpan.FromSeconds(3));

        // CRITICAL ASSERTION: Verify the adapter was actually INVOKED at runtime
        // even though we configured a second journal afterwards
        await AwaitAssertAsync(() =>
        {
            _output.WriteLine($"Adapter was called {InvocationCountingAdapter.CallCount} times");
            InvocationCountingAdapter.CallCount.Should().Be(3,
                "event adapter should be invoked for default journal even after second journal configuration");
        });

        // Verify events were persisted correctly
        var events = await actor.Ask<string[]>(TestPersistentActor.GetEvents.Instance, TimeSpan.FromSeconds(3));
        events.Should().BeEquivalentTo(new[] { "event-1", "event-2", "event-3" });
    }

    [Fact]
    public async Task Sharding_Journal_Should_Not_Have_Adapters()
    {
        // Reset the counter
        InvocationCountingAdapter.CallCount = 0;

        // Verify sharding journal does NOT have adapters (we only configured them on default)
        var config = Sys.Settings.Config;
        var shardingJournalConfig = config.GetConfig("akka.persistence.journal.sharding");

        _output.WriteLine("=== Sharding Journal Configuration ===");
        _output.WriteLine(shardingJournalConfig.ToString());

        shardingJournalConfig.HasPath("event-adapters").Should().BeFalse(
            "sharding journal should NOT have event-adapters (we didn't configure any)");

        // Create persistent actor using SHARDING journal
        var actor = Sys.ActorOf(Props.Create(() =>
            new TestPersistentActor("test-multi-2", "akka.persistence.journal.sharding")));

        // Persist events
        await actor.Ask<string>(new TestPersistentActor.SaveEvent("event-1"), TimeSpan.FromSeconds(3));
        await actor.Ask<string>(new TestPersistentActor.SaveEvent("event-2"), TimeSpan.FromSeconds(3));

        // Adapter should NOT be called for sharding journal
        await AwaitAssertAsync(() =>
        {
            _output.WriteLine($"Adapter was called {InvocationCountingAdapter.CallCount} times");
            InvocationCountingAdapter.CallCount.Should().Be(0,
                "event adapter should NOT be invoked for sharding journal (no adapters configured)");
        });

        // Verify events were still persisted (just without adapters)
        var events = await actor.Ask<string[]>(TestPersistentActor.GetEvents.Instance, TimeSpan.FromSeconds(3));
        events.Should().BeEquivalentTo(new[] { "event-1", "event-2" });
    }
}

/// <summary>
/// Tests using explicit JournalOptions + WithJournalAndSnapshot pattern.
/// This more closely mimics the SQL persistence scenario from issue #552.
/// </summary>
public class EventAdapterWithExplicitJournalOptionsSpecs : Akka.Hosting.TestKit.TestKit
{
    private readonly ITestOutputHelper _output;

    public EventAdapterWithExplicitJournalOptionsSpecs(ITestOutputHelper output)
    {
        _output = output;
    }

    // Mock journal options that use in-memory journal as the implementation
    private sealed class MockJournalOptions : JournalOptions
    {
        public MockJournalOptions(bool isDefault, string id) : base(isDefault)
        {
            Identifier = id;
        }

        public override string Identifier { get; set; }

        protected override Config InternalDefaultConfig =>
            ConfigurationFactory.ParseString(@"
                class = ""Akka.Persistence.Journal.MemoryJournal, Akka.Persistence""
                plugin-dispatcher = ""akka.actor.default-dispatcher""
            ");
    }

    private sealed class MockSnapshotOptions : SnapshotOptions
    {
        public MockSnapshotOptions(bool isDefault, string id) : base(isDefault)
        {
            Identifier = id;
        }

        public override string Identifier { get; set; }

        protected override Config InternalDefaultConfig =>
            ConfigurationFactory.ParseString(@"
                class = ""Akka.Persistence.Snapshot.MemorySnapshotStore, Akka.Persistence""
                plugin-dispatcher = ""akka.actor.default-dispatcher""
            ");
    }

    #region Reuse test infrastructure

    public sealed class TestEvent
    {
        public TestEvent(string data) { Data = data; }
        public string Data { get; }
    }

    public sealed class InvocationCountingAdapter : IWriteEventAdapter
    {
        public static int CallCount = 0;

        public InvocationCountingAdapter() { }
        public InvocationCountingAdapter(ExtendedActorSystem system) { }

        public string Manifest(object evt) => string.Empty;

        public object ToJournal(object evt)
        {
            Interlocked.Increment(ref CallCount);
            return evt switch
            {
                TestEvent => new Tagged(evt, new[] { "test-tag" }),
                _ => evt
            };
        }
    }

    public sealed class TestPersistentActor : ReceivePersistentActor
    {
        private readonly List<string> _events = new();

        public sealed class SaveEvent
        {
            public SaveEvent(string data) { Data = data; }
            public string Data { get; }
        }

        public sealed class GetEvents
        {
            public static readonly GetEvents Instance = new();
            private GetEvents() { }
        }

        public TestPersistentActor(string persistenceId)
        {
            PersistenceId = persistenceId;

            Command<SaveEvent>(cmd =>
            {
                var evt = new TestEvent(cmd.Data);
                Persist(evt, _ =>
                {
                    _events.Add(cmd.Data);
                    Sender.Tell("OK");
                });
            });

            Command<GetEvents>(_ =>
            {
                Sender.Tell(_events.ToArray());
            });

            Recover<TestEvent>(evt =>
            {
                _events.Add(evt.Data);
            });
        }

        public override string PersistenceId { get; }
    }

    #endregion

    protected override void ConfigureAkka(AkkaConfigurationBuilder builder, IServiceProvider provider)
    {
        // STEP 1: Configure default journal + snapshot WITH event adapters using NEW callback API
        // This mimics: builder.WithSqlPersistence(..., journalBuilder: ...)
        var defaultJournalOptions = new MockJournalOptions(isDefault: true, "sql");
        var defaultSnapshotOptions = new MockSnapshotOptions(isDefault: true, "sql");

        builder.WithJournalAndSnapshot(
            journalOptions: defaultJournalOptions,
            snapshotOptions: defaultSnapshotOptions,
            configureJournal: journal =>
            {
                journal.AddWriteEventAdapter<InvocationCountingAdapter>(
                    "counting-adapter",
                    new[] { typeof(TestEvent) });
            },
            configureSnapshot: null);

        // STEP 2: Configure sharding journal + snapshot WITHOUT adapters
        // This mimics: builder.WithJournalAndSnapshot(shardingJournalOptions, shardingSnapshotOptions)
        var shardingJournalOptions = new MockJournalOptions(isDefault: false, "sharding");
        var shardingSnapshotOptions = new MockSnapshotOptions(isDefault: false, "sharding");

        builder.WithJournalAndSnapshot(
            journalOptions: shardingJournalOptions,
            snapshotOptions: shardingSnapshotOptions);
    }

    [Fact]
    public async Task EventAdapter_Should_Work_With_Explicit_JournalOptions_Pattern()
    {
        // Reset the counter
        InvocationCountingAdapter.CallCount = 0;

        // Verify adapter is in HOCON for default journal
        var config = Sys.Settings.Config;
        var defaultJournalConfig = config.GetConfig("akka.persistence.journal.sql");

        _output.WriteLine("=== Default Journal Configuration (Explicit Options) ===");
        _output.WriteLine(defaultJournalConfig.ToString());

        defaultJournalConfig.HasPath("event-adapters").Should().BeTrue(
            "event-adapters should be in HOCON for default journal");
        defaultJournalConfig.HasPath("event-adapter-bindings").Should().BeTrue(
            "event-adapter-bindings should be in HOCON for default journal");

        // Create persistent actor using DEFAULT journal
        var actor = Sys.ActorOf(Props.Create(() => new TestPersistentActor("test-explicit-1")));

        // Persist 3 events
        await actor.Ask<string>(new TestPersistentActor.SaveEvent("event-1"), TimeSpan.FromSeconds(3));
        await actor.Ask<string>(new TestPersistentActor.SaveEvent("event-2"), TimeSpan.FromSeconds(3));
        await actor.Ask<string>(new TestPersistentActor.SaveEvent("event-3"), TimeSpan.FromSeconds(3));

        // CRITICAL ASSERTION: Verify the adapter was actually INVOKED at runtime
        // This is the key test from issue #552
        await AwaitAssertAsync(() =>
        {
            _output.WriteLine($"Adapter was called {InvocationCountingAdapter.CallCount} times");
            InvocationCountingAdapter.CallCount.Should().Be(3,
                "event adapter should be invoked with explicit JournalOptions + WithJournalAndSnapshot pattern");
        });

        // Verify events were persisted correctly
        var events = await actor.Ask<string[]>(TestPersistentActor.GetEvents.Instance, TimeSpan.FromSeconds(3));
        events.Should().BeEquivalentTo(new[] { "event-1", "event-2", "event-3" });
    }
}
