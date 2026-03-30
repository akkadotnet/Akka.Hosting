// -----------------------------------------------------------------------
//  <copyright file="LogStateSnapshotSpecs.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2025 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using Akka.Event;
using Akka.Hosting;
using Microsoft.Extensions.Logging;
using VerifyXunit;
using Xunit;
using Xunit.Abstractions;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Akka.Hosting.Tests.Logging;

/// <summary>
/// Verify snapshot tests for LoggerFactoryLogger structured state output.
///
/// These tests capture the exact structure of the log state dictionary that
/// LoggerFactoryLogger passes to ILogger.Log(). Any change to the state keys,
/// value types, or metadata presence will cause a snapshot mismatch.
/// </summary>
[UsesVerify]
public class LogStateSnapshotSpecs : TestKit.TestKit
{
    private readonly BugReproTestSink _sink;

    public LogStateSnapshotSpecs(ITestOutputHelper output) : base(output: output)
    {
        _sink = new BugReproTestSink(output);
    }

    protected override void ConfigureAkka(AkkaConfigurationBuilder builder, IServiceProvider provider)
    {
        builder.ConfigureLoggers(setup =>
        {
            setup.LogLevel = Event.LogLevel.InfoLevel;
            setup.ClearLoggers();
            setup.AddLoggerFactory(new BugReproTestLoggerFactory(_sink));
        });
    }

    /// <summary>
    /// Snapshot the state dictionary for a structured log with named placeholders.
    /// Expected: semantic properties (UserId, Action) + Akka metadata (ActorPath, Timestamp, Thread, LogSource) + {OriginalFormat}
    /// </summary>
    [Fact]
    public System.Threading.Tasks.Task StructuredLog_StateKeys()
    {
        _sink.Clear();

        Sys.Log.Info("User {UserId} performed {Action}", 12345, "Login");

        AwaitCondition(() => _sink.Entries.Any(e => e.Message.Contains("12345")));

        var entry = _sink.Entries.First(e => e.Message.Contains("12345"));

        // Snapshot the state keys and value types (scrub volatile values)
        var snapshot = new
        {
            entry.LogLevel,
            MessageContainsValue = entry.Message.Contains("12345"),
            StateKeys = entry.State.Keys.OrderBy(k => k).ToArray(),
            StateValueTypes = entry.State
                .OrderBy(kvp => kvp.Key)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.GetType().Name ?? "null"),
            HasActorPath = entry.State.ContainsKey("ActorPath"),
            HasTimestamp = entry.State.ContainsKey("Timestamp"),
            HasThread = entry.State.ContainsKey("Thread"),
            HasLogSource = entry.State.ContainsKey("LogSource"),
            HasOriginalFormat = entry.State.ContainsKey("{OriginalFormat}"),
            OriginalFormat = entry.State.GetValueOrDefault("{OriginalFormat}")?.ToString(),
            HasSemanticProperty_UserId = entry.State.ContainsKey("UserId"),
            HasSemanticProperty_Action = entry.State.ContainsKey("Action"),
            UserId = entry.State.GetValueOrDefault("UserId"),
            Action = entry.State.GetValueOrDefault("Action"),
        };

        return Verifier.Verify(snapshot);
    }

    /// <summary>
    /// Snapshot the state dictionary for a plain string log (no template placeholders).
    /// Expected: Akka metadata (ActorPath, Timestamp, Thread, LogSource) + {OriginalFormat}
    /// This is the scenario that regressed — plain string logs must still carry metadata.
    /// </summary>
    [Fact]
    public System.Threading.Tasks.Task PlainStringLog_StateKeys()
    {
        _sink.Clear();

        Sys.Log.Info("Server started successfully");

        AwaitCondition(() => _sink.Entries.Any(e => e.Message.Contains("Server started successfully")));

        var entry = _sink.Entries.First(e => e.Message.Contains("Server started successfully"));

        var snapshot = new
        {
            entry.LogLevel,
            MessageContainsValue = entry.Message.Contains("Server started successfully"),
            StateKeys = entry.State.Keys.OrderBy(k => k).ToArray(),
            StateValueTypes = entry.State
                .OrderBy(kvp => kvp.Key)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.GetType().Name ?? "null"),
            HasActorPath = entry.State.ContainsKey("ActorPath"),
            HasTimestamp = entry.State.ContainsKey("Timestamp"),
            HasThread = entry.State.ContainsKey("Thread"),
            HasLogSource = entry.State.ContainsKey("LogSource"),
            HasOriginalFormat = entry.State.ContainsKey("{OriginalFormat}"),
            OriginalFormat = entry.State.GetValueOrDefault("{OriginalFormat}")?.ToString(),
        };

        return Verifier.Verify(snapshot);
    }

    /// <summary>
    /// Snapshot the state dictionary for a log with positional placeholders.
    /// Expected: semantic properties (0) + Akka metadata + {OriginalFormat}
    /// </summary>
    [Fact]
    public System.Threading.Tasks.Task PositionalPlaceholderLog_StateKeys()
    {
        _sink.Clear();

        Sys.Log.Info("User {0} logged in", 99);

        AwaitCondition(() => _sink.Entries.Any(e => e.Message.Contains("99")));

        var entry = _sink.Entries.First(e => e.Message.Contains("99"));

        var snapshot = new
        {
            entry.LogLevel,
            MessageContainsValue = entry.Message.Contains("99"),
            StateKeys = entry.State.Keys.OrderBy(k => k).ToArray(),
            StateValueTypes = entry.State
                .OrderBy(kvp => kvp.Key)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.GetType().Name ?? "null"),
            HasActorPath = entry.State.ContainsKey("ActorPath"),
            HasTimestamp = entry.State.ContainsKey("Timestamp"),
            HasThread = entry.State.ContainsKey("Thread"),
            HasLogSource = entry.State.ContainsKey("LogSource"),
            HasOriginalFormat = entry.State.ContainsKey("{OriginalFormat}"),
            OriginalFormat = entry.State.GetValueOrDefault("{OriginalFormat}")?.ToString(),
            HasSemanticProperty_0 = entry.State.ContainsKey("0"),
            Value_0 = entry.State.GetValueOrDefault("0"),
        };

        return Verifier.Verify(snapshot);
    }
}
