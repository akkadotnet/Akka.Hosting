// -----------------------------------------------------------------------
//  <copyright file="TraceDemoService.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2024 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;
using Akka.Actor;
using Akka.Hosting;
using Akka.Hosting.OpenTelemetry.Demo.Actors;

namespace Akka.Hosting.OpenTelemetry.Demo;

/// <summary>
/// Background service that demonstrates OpenTelemetry trace correlation with Akka.NET actors.
/// Creates traced operations and sends messages to actors, verifying that logs from actors
/// are correlated with the originating trace.
/// </summary>
public sealed class TraceDemoService : BackgroundService
{
    private static readonly ActivitySource ActivitySource = new("Akka.Hosting.OpenTelemetry.Demo");

    private readonly ILogger<TraceDemoService> _logger;
    private readonly IRequiredActor<TraceDemoActor> _processorActor;
    private readonly IRequiredActor<ForwarderActor> _forwarderActor;
    private int _requestCounter;

    public TraceDemoService(
        ILogger<TraceDemoService> logger,
        IRequiredActor<TraceDemoActor> processorActor,
        IRequiredActor<ForwarderActor> forwarderActor)
    {
        _logger = logger;
        _processorActor = processorActor;
        _forwarderActor = forwarderActor;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait a moment for the actor system to fully initialize
        await Task.Delay(2000, stoppingToken);

        _logger.LogInformation("Starting trace correlation demo...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Create a traced operation
                using var activity = ActivitySource.StartActivity("ProcessMessage");

                var requestId = $"REQ-{++_requestCounter:D4}";
                var data = $"Sample data for request {requestId}";

                _logger.LogInformation("Sending request {RequestId} to actor within trace {TraceId}",
                    requestId, activity?.TraceId.ToString() ?? "none");

                // Send directly to processor
                var processor = await _processorActor.GetAsync(stoppingToken);
                var response = await processor.Ask<ProcessResponse>(
                    new ProcessRequest(requestId, data),
                    TimeSpan.FromSeconds(5),
                    stoppingToken);

                _logger.LogInformation("Received response for {RequestId}: {Result}",
                    response.RequestId, response.Result);

                // Now test forwarding through multiple actors
                using var forwardActivity = ActivitySource.StartActivity("ForwardMessage");

                var forwardRequestId = $"FWD-{_requestCounter:D4}";
                var forwardData = $"Forwarded data for request {forwardRequestId}";

                _logger.LogInformation("Sending forwarded request {RequestId} within trace {TraceId}",
                    forwardRequestId, forwardActivity?.TraceId.ToString() ?? "none");

                var forwarder = await _forwarderActor.GetAsync(stoppingToken);
                var forwardResponse = await forwarder.Ask<ProcessResponse>(
                    new ProcessRequest(forwardRequestId, forwardData),
                    TimeSpan.FromSeconds(5),
                    stoppingToken);

                _logger.LogInformation("Received forwarded response for {RequestId}: {Result}",
                    forwardResponse.RequestId, forwardResponse.Result);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during trace demo iteration");
            }

            // Wait before next iteration
            await Task.Delay(5000, stoppingToken);
        }

        _logger.LogInformation("Trace correlation demo stopped.");
    }
}
