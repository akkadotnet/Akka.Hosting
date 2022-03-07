using System.Collections.Concurrent;
using System.Collections.Generic;
using Akka.Actor;

namespace Akka.Hosting;


/// <summary>
/// Default <see cref="ActorRegistry{TKey}"/> implementation designed
/// to work with <see cref="System.Enum"/> or constant <see cref="int"/>.
/// </summary>
public sealed class DefaultActorRegistry : ActorRegistry<int>{  }

/// <summary>
/// INTERNAL API
/// </summary>
/// <typeparam name="TKey">The type of key to use for the registry.</typeparam>
public class ActorRegistryExtension<TKey> : ExtensionIdProvider<ActorRegistry<TKey>> where TKey : struct
{
    public override ActorRegistry<TKey> CreateExtension(ExtendedActorSystem system)
    {
        return new ActorRegistry<TKey>();
    }
}

/// <summary>
/// Mutable, but thread-safe <see cref="ActorRegistry{TKey}"/>.
/// </summary>
/// <typeparam name="TKey">A actor type. For best practices, use an <see cref="System.Enum"/> or any other easily type-checked actor.</typeparam>
/// <remarks>
/// Should only be used for top-level actors that need to be accessed from inside or outside the <see cref="ActorSystem"/>.
///
/// If you are adding every single actor in your <see cref="ActorSystem"/> to the registry you are definitely using it wrong.
/// </remarks>
public class ActorRegistry<TKey> : IExtension where TKey : struct
{
    private readonly ConcurrentDictionary<TKey, IActorRef> _actorRegistrations = new ConcurrentDictionary<TKey, IActorRef>();

    /// <summary>
    /// Attempts to register an actor with the registry.
    /// </summary>
    /// <param name="key">The key for a particular actor.</param>
    /// <param name="actor">The bound <see cref="IActorRef"/>, if any. Is set to <see cref="ActorRefs.Nobody"/> if key is not found.</param>
    /// <param name="overwrite">If <c>true</c>, allows overwriting of a previous actor with the same key. Defaults to <c>false</c>.</param>
    /// <returns><c>true</c> if the actor was set to this key in the registry, <c>false</c> otherwise.</returns>
    public bool TryRegister(TKey key, IActorRef actor, bool overwrite = false)
    {
        if (!overwrite)
            return _actorRegistrations.TryAdd(key, actor);
        else
            _actorRegistrations[key] = actor;
        return true;
    }

    /// <summary>
    /// Try to retrieve an <see cref="IActorRef"/> with the given <see cref="TKey"/>.
    /// </summary>
    /// <param name="key">The key for a particular actor.</param>
    /// <param name="actor">The bound <see cref="IActorRef"/>, if any. Is set to <see cref="ActorRefs.Nobody"/> if key is not found.</param>
    /// <returns><c>true</c> if an actor with this key exists, <c>false</c> otherwise.</returns>
    public bool TryFetch(TKey key, out IActorRef actor)
    {
        if (_actorRegistrations.ContainsKey(key))
        {
            actor = _actorRegistrations[key];
            return true;
        }

        actor = ActorRefs.Nobody;
        return false;
    }

    /// <summary>
    /// Allows enumerated access to the collection of all registered actors.
    /// </summary>
    /// <returns></returns>
    public IEnumerator<KeyValuePair<TKey, IActorRef>> GetEnumerator()
    {
        return _actorRegistrations.GetEnumerator();
    }

    public static ActorRegistry<TKey> For(ActorSystem actorSystem)
    {
        return actorSystem.WithExtension<ActorRegistry<TKey>, ActorRegistryExtension<TKey>>();
    }
}