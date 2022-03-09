namespace Akka.Hosting.SqlSharding.Messages;

/// <summary>
/// Marker interface for all user-related events and messages
/// </summary>
public interface IWithUserId
{
    string UserId { get; }
}