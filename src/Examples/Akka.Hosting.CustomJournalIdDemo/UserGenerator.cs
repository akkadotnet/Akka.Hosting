using Akka.Hosting.CustomJournalIdDemo.Messages;
using Akka.Util;

namespace Akka.Hosting.CustomJournalIdDemo;

public static class UserGenerator
{
    public static readonly string[] FirstNames = new[]
        { "Yoda", "Obi-Wan", "Darth Vader", "Leia", "Luke", "R2D2", "Han", "Chewbacca", "Jabba", "Ardbeg", "Lando" };

    public static readonly string[] LastNames = new[] { "Fat", "Kenobi", "Skywalker", "Solo", "Fett", "Calrissian" };
    
    private static T PickRandom<T>(T[] items) => items[ThreadLocalRandom.Current.Next(items.Length)];

    public static UserDescriptor CreateRandom()
    {
        var userName = $"{PickRandom(FirstNames)} {PickRandom(LastNames)} v{ThreadLocalRandom.Current.Next(0,100)}";
        var userId = MurmurHash.StringHash(userName);
        return new UserDescriptor(userId.ToString(), userName);
    }
}