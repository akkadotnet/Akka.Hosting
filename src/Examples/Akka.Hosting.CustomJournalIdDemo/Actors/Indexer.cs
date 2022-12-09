using System.Collections.Immutable;
using Akka.Actor;
using Akka.Event;
using Akka.Hosting.CustomJournalIdDemo.Messages;
using Akka.Persistence.Query;
using Akka.Persistence.Query.Sql;
using Akka.Streams;
using Akka.Streams.Dsl;
using Directive = Akka.Streams.Supervision.Directive;

namespace Akka.Hosting.CustomJournalIdDemo.Actors;

public sealed class Indexer : ReceiveActor
{
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly IActorRef _userActionsShardRegion;

    private Dictionary<string, UserDescriptor> _users = new Dictionary<string, UserDescriptor>();

    public Indexer(IActorRef userActionsShardRegion)
    {
        _userActionsShardRegion = userActionsShardRegion;

        Receive<UserDescriptor>(d =>
        {
            _log.Info("Found {0}", d);
            _users[d.UserId] = d;
        });

        Receive<FetchUsers>(f => { Sender.Tell(_users.Values.ToImmutableList()); });

        Receive<string>(s => { _log.Info("Recorded completion of the stream"); });

        Receive<UserCreatedEvent>(e =>
        {
            _userActionsShardRegion.Ask<UserDescriptor>(new FetchUser(e.UserId), TimeSpan.FromSeconds(1)).PipeTo(Self);
        });
    }


    protected override void PreStart()
    {
        FetchIds();
    }

    private void FetchIds()
    {
        var readJournal = Context.System.ReadJournalFor<SqlReadJournal>(SqlReadJournal.Identifier);
        readJournal.AllEvents()
            .Where(e => e.Event is UserCreatedEvent)
            .Select(uc => (UserCreatedEvent)uc.Event)
            .WithAttributes(ActorAttributes.CreateSupervisionStrategy(e => Directive.Restart))
            .RunWith(Sink.ActorRef<UserCreatedEvent>(Self, "complete"), Context.Materializer());
    }
}