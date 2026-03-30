// -----------------------------------------------------------------------
//  <copyright file="LogStateSnapshotSpecs.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2025 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
/// Captures both the formatted message and the structured state dictionary
/// from ILogger.Log(), sanitizes volatile values (timestamps, thread IDs,
/// actor paths), and snapshots the result as plain text.
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

    [Fact]
    public System.Threading.Tasks.Task StructuredLog_StateSnapshot()
    {
        _sink.Clear();
        Sys.Log.Info("User {UserId} performed {Action}", 12345, "Login");
        AwaitCondition(() => _sink.Entries.Any(e => e.Message.Contains("12345")));
        var entry = _sink.Entries.First(e => e.Message.Contains("12345"));
        return Verifier.Verify(FormatEntry(entry));
    }

    [Fact]
    public System.Threading.Tasks.Task PlainStringLog_StateSnapshot()
    {
        _sink.Clear();
        Sys.Log.Info("Server started successfully");
        AwaitCondition(() => _sink.Entries.Any(e => e.Message.Contains("Server started successfully")));
        var entry = _sink.Entries.First(e => e.Message.Contains("Server started successfully"));
        return Verifier.Verify(FormatEntry(entry));
    }

    [Fact]
    public System.Threading.Tasks.Task PositionalPlaceholderLog_StateSnapshot()
    {
        _sink.Clear();
        Sys.Log.Info("User {0} logged in", 99);
        AwaitCondition(() => _sink.Entries.Any(e => e.Message.Contains("99")));
        var entry = _sink.Entries.First(e => e.Message.Contains("99"));
        return Verifier.Verify(FormatEntry(entry));
    }

    private static string FormatEntry(BugReproLogEntry entry)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"LogLevel: {entry.LogLevel}");
        sb.AppendLine($"Message: {SanitizeMessage(entry.Message)}");
        sb.AppendLine("State:");
        foreach (var kvp in entry.State.OrderBy(k => k.Key))
        {
            sb.AppendLine($"  [{kvp.Key}] = {SanitizeValue(kvp.Key, kvp.Value)}");
        }
        return sb.ToString();
    }

    private static string SanitizeMessage(string message)
    {
        // Replace [LEVEL][datetime][Thread NNNN][logSource] prefix with sanitized constants
        message = Regex.Replace(message,
            @"\[(DEBUG|INFO|WARNING|ERROR)\]",
            "[LEVEL]");
        message = Regex.Replace(message,
            @"\[\d{2}/\d{2}/\d{4} \d{2}:\d{2}:\d{2}\.\d{3}Z?\]",
            "[DateTime]");
        message = Regex.Replace(message,
            @"\[Thread \d+\]",
            "[Thread 0001]");
        message = Regex.Replace(message,
            @"\[akka://[^\]]+\]",
            "[ActorPath]");
        return message;
    }

    private static string SanitizeValue(string key, object? value)
    {
        return key switch
        {
            "ActorPath" => "akka://test/...",
            "Timestamp" => "DateTime",
            "Thread" => "1",
            "LogSource" => "LogSource",
            _ => value?.ToString() ?? "null"
        };
    }
}
