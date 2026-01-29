// -----------------------------------------------------------------------
//  <copyright file="TraceDemoActor.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2024 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using Akka.Actor;
using Akka.Event;

namespace Akka.Hosting.OpenTelemetry.Demo.Actors;

/// <summary>
/// Messages for the trace demo actor.
/// </summary>
public sealed record ProcessRequest(string RequestId, string Data);
public sealed record ProcessResponse(string RequestId, string Result);

/// <summary>
/// Actor that demonstrates OpenTelemetry trace correlation.
/// Logs are emitted with the trace context from the originating request,
/// even though Activity.Current doesn't flow across mailbox boundaries.
/// </summary>
public sealed class TraceDemoActor : ReceiveActor
{
    private readonly ILoggingAdapter _log = Context.GetLogger();

    public TraceDemoActor()
    {
        Receive<ProcessRequest>(HandleProcessRequest);
    }

    private void HandleProcessRequest(ProcessRequest request)
    {
        // This log will include the TraceId and SpanId from the originating request
        // even though we're on a different thread after crossing the mailbox boundary
        _log.Info("Processing request {RequestId} with data: {Data}", request.RequestId, request.Data);

        // Simulate some processing
        var result = $"Processed: {request.Data.ToUpperInvariant()}";

        _log.Info("Completed processing request {RequestId}, result: {Result}", request.RequestId, result);

        Sender.Tell(new ProcessResponse(request.RequestId, result));
    }
}

/// <summary>
/// Actor that forwards messages to demonstrate trace context flowing through actor chains.
/// </summary>
public sealed class ForwarderActor : ReceiveActor
{
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly IActorRef _target;

    public ForwarderActor(IActorRef target)
    {
        _target = target;

        Receive<ProcessRequest>(request =>
        {
            _log.Info("Forwarding request {RequestId} to processor", request.RequestId);
            _target.Forward(request);
        });
    }
}
