// -----------------------------------------------------------------------
//  <copyright file="Program.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2024 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;
using Akka.Actor;
using Akka.Hosting;
using Akka.Hosting.Logging;
using Akka.Hosting.OpenTelemetry.Demo;
using Akka.Hosting.OpenTelemetry.Demo.Actors;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;

var builder = Host.CreateApplicationBuilder(args);

// Add Aspire service defaults (service discovery + tracing)
builder.AddServiceDefaults();

// Configure OpenTelemetry logging with Akka trace correlation
builder.Logging.AddOpenTelemetry(options =>
{
    options.SetResourceBuilder(ResourceBuilder.CreateDefault()
        .AddService("akka-otel-demo"));

    // CRITICAL: Register Akka trace correlation processor FIRST (before exporters)
    // This extracts trace context from Akka log attributes and applies it to LogRecords
    options.AddAkkaTraceCorrelation();

    // Include formatted message for easier debugging
    options.IncludeFormattedMessage = true;
    options.IncludeScopes = true;

    options.AddOtlpExporter();
});

// Configure Akka.NET with LoggerFactoryLogger
builder.Services.AddAkka("TraceDemo", configBuilder =>
{
    configBuilder.ConfigureLoggers(setup =>
    {
        setup.ClearLoggers();
        setup.AddLoggerFactory();
    });

    // Register actors
    configBuilder.WithActors((system, registry, resolver) =>
    {
        var processor = system.ActorOf(Props.Create<TraceDemoActor>(), "processor");
        var forwarder = system.ActorOf(Props.Create(() => new ForwarderActor(processor)), "forwarder");

        registry.Register<TraceDemoActor>(processor);
        registry.Register<ForwarderActor>(forwarder);
    });
});

// Add background service to demonstrate trace correlation
builder.Services.AddHostedService<TraceDemoService>();

var app = builder.Build();
app.Run();
