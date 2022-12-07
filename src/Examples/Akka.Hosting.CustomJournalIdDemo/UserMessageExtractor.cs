using Akka.Cluster.Sharding;
using Akka.Hosting.CustomJournalIdDemo.Messages;

namespace Akka.Hosting.CustomJournalIdDemo;

public sealed class UserMessageExtractor : HashCodeMessageExtractor
{
    public UserMessageExtractor() : base(30)
    {
    }
    
    public UserMessageExtractor(int maxNumberOfShards) : base(maxNumberOfShards)
    {
    }

    public override string EntityId(object message)
    {
        if (message is IWithUserId userId)
        {
            return userId.UserId;
        }

        return null!;
    }
}