namespace Akka.Hosting.SqlSharding.Messages;

public record UserCreatedEvent(UserDescriptor Descriptor, long Timestamp) : IWithUserId
{
    public string UserId => Descriptor.UserId;
}