using System.Collections.Immutable;
using Akka.Actor;
using Akka.Event;
using Akka.Hosting.SqlSharding.Messages;
using Akka.Persistence.Query;
using Akka.Persistence.Query.Sql;
using Akka.Streams;
using Akka.Streams.Dsl;

namespace Akka.Hosting.SqlSharding;

public sealed class Indexer : ReceiveActor
{
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly IActorRef _userActionsShardRegion;

    private HashSet<UserDescriptor> _users = new HashSet<UserDescriptor>();

    public Indexer(IActorRef userActionsShardRegion)
    {
        _userActionsShardRegion = userActionsShardRegion;

        Receive<UserDescriptor>(d =>
        {
            _log.Info("Found {0}", d);
            _users.Add(d);
        });

        Receive<FetchUsers>(f =>
        {
            Sender.Tell(_users.ToImmutableList());
        });
    }

   

    protected override void PreStart()
    {
        var readJournal = Context.System.ReadJournalFor<SqlReadJournal>(SqlReadJournal.Identifier);
        readJournal.PersistenceIds()
            .SelectAsync(10, c => _userActionsShardRegion.Ask<UserDescriptor>(new FetchUser(c),
                TimeSpan.FromSeconds(5)))
            .RunWith(Sink.ActorRef<UserDescriptor>(Self, "complete"), Context.Materializer());
    }
}