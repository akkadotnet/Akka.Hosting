namespace Akka.Hosting.CustomJournalIdDemo.Messages;

public record CreateUser(UserDescriptor Descriptor) : IWithUserId
{
    public string UserId => Descriptor.UserId;
}

public enum ResponseKind
{
    Success,
    Failure,
    Unknown
}

public record CommandResponse(ResponseKind ResponseKind);

public sealed class FetchUsers
{
    public static readonly FetchUsers Instance = new FetchUsers();
    private FetchUsers(){}
}

public sealed class FetchUser : IWithUserId
{
    public FetchUser(string userId)
    {
        UserId = userId;
    }

    public string UserId { get; }
}