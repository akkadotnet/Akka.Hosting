using Akka.TestKit;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Akka.Hosting.TestKit.Tests;

using System;
using System.Linq;
using System.Threading.Tasks;
using Actor;
using Hosting;
using Akka.Hosting.TestKit;
using Xunit;

file sealed class StartupPinger(IRequiredActor<TestProbe> testActorReq) : ReceiveActor
{
    protected override void PreStart()
    {
        var testActor = testActorReq.GetAsync().GetAwaiter().GetResult();
        testActor.Tell("startup-ping");
    }
}

file sealed class HostedTestKitRunner : TestKit, IAsyncLifetime  // TestKit already implements IAsyncLifetime, but we expose it here for clarity
{
    public HostedTestKitRunner(ITestOutputHelper output) 
        : base($"{Guid.NewGuid():N}", startupTimeout: TimeSpan.FromSeconds(20), output: output, logLevel: LogLevel.Error)
    {
    }

    protected override void ConfigureAkka(AkkaConfigurationBuilder builder, IServiceProvider provider)
    {
        builder.WithActors((system, _, resolver) =>
        {
            system.ActorOf(resolver.Props<StartupPinger>(), "pinger");
        });
    }

    // Optional convenience wrappers so the test code reads cleanly
    public Task StartAsync() => InitializeAsync();
    public Task StopAsync()  => DisposeAsync();

    public Task ExpectStartupAsync(TimeSpan? timeout = null)
        => ExpectMsgAsync("startup-ping", timeout ?? TimeSpan.FromSeconds(5)).AsTask();
}

public class TestActorStartupDeadlockSpec
{
    private readonly ITestOutputHelper _output;

    public TestActorStartupDeadlockSpec(ITestOutputHelper output)
    {
        _output = output;
    }
    
    // Give the overall test plenty of time; we enforce our own short, per-step timeouts below.
    [Fact(Timeout = 20000)]
    public async Task parallel_host_start_should_not_deadlock()
    {
        // Per-runner bounded timeouts
        var startTimeout   = TimeSpan.FromSeconds(7); // pre-fix should trip this quickly
        var expectTimeout  = TimeSpan.FromSeconds(5);
        var stopTimeout    = TimeSpan.FromSeconds(5);
        var concurrentHosts = Environment.ProcessorCount * 2;

        // Spin up N independent hosts concurrently inside the same theory
        var runners = Enumerable.Range(0, concurrentHosts)
                                .Select(_ => Task.Run(RunOneAsync))
                                .ToArray();

        await Task.WhenAll(runners);
        return;

        async Task RunOneAsync()
        {
            var kit = new HostedTestKitRunner(_output);

            // --- START (bounded) ---
            var startTask = kit.StartAsync();
            var startDone = await Task.WhenAny(startTask, Task.Delay(startTimeout)).ConfigureAwait(false);
            if (startDone != startTask)
            {
                // Fail fast with a clear message rather than timing out the entire test method
                Assert.Fail(
                    $"Host did not start within {startTimeout}. " +
                    "This indicates the known startup deadlock (TestKit initialized inline on the startup thread).");
            }
            // propagate any exception from StartAsync (e.g., watchdog TimeoutException)
            await startTask.ConfigureAwait(false);

            try
            {
                // --- EXPECT (bounded) ---
                var expectTask = kit.ExpectStartupAsync(expectTimeout);
                var expectDone = await Task.WhenAny(expectTask, Task.Delay(expectTimeout)).ConfigureAwait(false);
                if (expectDone != expectTask)
                    Assert.Fail($"Did not receive startup ping within {expectTimeout}.");

                await expectTask.ConfigureAwait(false);
            }
            finally
            {
                // --- STOP (bounded) ---
                var stopTask = kit.StopAsync();
                var stopDone = await Task.WhenAny(stopTask, Task.Delay(stopTimeout)).ConfigureAwait(false);
                if (stopDone == stopTask)
                    await stopTask.ConfigureAwait(false);
                // else: swallow to avoid hanging the test process on shutdown in pre-fix scenarios
            }
        }
    }
}
