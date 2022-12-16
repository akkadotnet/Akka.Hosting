namespace Akka.Hosting.CustomJournalIdDemo.Messages;

/// <summary>
/// Marker interface for all user-related events and messages
/// </summary>
public interface IWithUserId
{
    string UserId { get; }
}