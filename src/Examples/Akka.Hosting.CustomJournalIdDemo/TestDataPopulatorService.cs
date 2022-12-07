using Akka.Actor;
using Akka.Hosting.CustomJournalIdDemo.Actors;
using Akka.Hosting.CustomJournalIdDemo.Messages;

namespace Akka.Hosting.CustomJournalIdDemo;

public sealed class TestDataPopulatorService : IHostedService
{
    private readonly ActorSystem _system;
    private ICancelable? _cancelable;

    public TestDataPopulatorService(ActorSystem system)
    {
        _system = system;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cancelable = _system.Scheduler.Advanced.ScheduleRepeatedlyCancelable(TimeSpan.Zero, TimeSpan.FromSeconds(1), () =>
        {
            var entityRegion = ActorRegistry.For(_system).Get<UserActionsEntity>();
            var user = UserGenerator.CreateRandom();
            entityRegion.Tell(new CreateUser(user));
        });

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _cancelable?.Cancel();

        return Task.CompletedTask;
    }
}