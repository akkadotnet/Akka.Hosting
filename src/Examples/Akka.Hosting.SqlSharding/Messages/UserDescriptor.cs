namespace Akka.Hosting.SqlSharding.Messages;

public record UserDescriptor(string UserId, string UserName) : IWithUserId
{
    public static readonly UserDescriptor Empty = new UserDescriptor(string.Empty, string.Empty);
}