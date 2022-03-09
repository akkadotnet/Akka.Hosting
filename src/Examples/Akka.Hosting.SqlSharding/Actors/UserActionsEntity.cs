using Akka.Event;
using Akka.Actor;
using Akka.Hosting.SqlSharding.Messages;
using Akka.Persistence;

namespace Akka.Hosting.SqlSharding;

public class UserActionsEntity : ReceivePersistentActor
{
    public static Props Props(string userId)
    {
        return Actor.Props.Create(() => new UserActionsEntity(userId));
    }
    
    public UserActionsEntity(string persistenceId)
    {
        PersistenceId = persistenceId;
        CurrentState = UserDescriptor.Empty;
        
        Recover<UserCreatedEvent>(c =>
        {
            CurrentState = c.Descriptor;
            _log.Info("Recovered {0}", c);
        });

        Command<CreateUser>(user =>
        {
            var e = new UserCreatedEvent(user.Descriptor, DateTime.UtcNow.Ticks);
            Persist(e, evt =>
            {
                _log.Info("Persisted {0}", evt);
                CurrentState = evt.Descriptor;
                if(!Sender.IsNobody())
                    Sender.Tell(new CommandResponse(ResponseKind.Success));
            });
        });

        Command<FetchUser>(user =>
        {
            Sender.Tell(CurrentState);
        });
    }

    private readonly ILoggingAdapter _log = Context.GetLogger();

    public override string PersistenceId { get; }
    
    public UserDescriptor CurrentState { get; private set; }
}