using System.Collections.Immutable;
using Akka.Actor;
using Akka.Event;
using Akka.Hosting.SqlSharding.Messages;
using Akka.Persistence.Query;
using Akka.Persistence.Query.Sql;
using Akka.Streams;
using Akka.Streams.Dsl;
using Directive = Akka.Streams.Supervision.Directive;

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

        Receive<FetchUsers>(f => { Sender.Tell(_users.ToImmutableList()); });

        Receive<string>(s => { _log.Info("Recorded completion of the stream"); });
    }


    protected override void PreStart()
    {
        var readJournal = Context.System.ReadJournalFor<SqlReadJournal>(SqlReadJournal.Identifier);
        readJournal.PersistenceIds()
            .SelectAsync(10, async c =>
            {
                var attempts = 5;
                while (attempts > 0)
                    try
                    {
                        return await _userActionsShardRegion.Ask<UserDescriptor>(new FetchUser(c),
                            TimeSpan.FromSeconds(5));
                    }
                    catch
                    {
                        if (attempts == 0)
                            throw;
                        attempts--;
                    }

                return UserDescriptor.Empty;
            })
            .WithAttributes(ActorAttributes.CreateSupervisionStrategy(e => Directive.Restart))
            .RunWith(Sink.ActorRef<UserDescriptor>(Self, "complete"), Context.Materializer());
    }
}