using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Akka.Actor;

namespace Akka.Hosting
{
    /// <summary>
    /// INTERNAL API
    /// </summary>
    public class ActorRegistryExtension : ExtensionIdProvider<ActorRegistry>
    {
        public override ActorRegistry CreateExtension(ExtendedActorSystem system)
        {
            return new ActorRegistry();
        }
    }

    /// <summary>
    /// Mutable, but thread-safe <see cref="ActorRegistry"/>.
    /// </summary>
    /// <remarks>
    /// Should only be used for top-level actors that need to be accessed from inside or outside the <see cref="ActorSystem"/>.
    ///
    /// If you are adding every single actor in your <see cref="ActorSystem"/> to the registry you are definitely using it wrong.
    /// </remarks>
    public class ActorRegistry : IActorRegistry, IExtension
    {
        private readonly ConcurrentDictionary<Type, IActorRef> _actorRegistrations =
            new ConcurrentDictionary<Type, IActorRef>();

        /// <summary>
        /// Attempts to register an actor with the registry.
        /// </summary>
        /// <param name="actor">The bound <see cref="IActorRef"/>, if any. Is set to <see cref="ActorRefs.Nobody"/> if key is not found.</param>
        /// <param name="overwrite">If <c>true</c>, allows overwriting of a previous actor with the same key. Defaults to <c>false</c>.</param>
        /// <returns><c>true</c> if the actor was set to this key in the registry, <c>false</c> otherwise.</returns>
        public bool TryRegister<TKey>(IActorRef actor, bool overwrite = false)
        {
            return TryRegister(typeof(TKey), actor, overwrite);
        }

        /// <summary>
        /// Attempts to register an actor with the registry.
        /// </summary>
        /// <param name="key">The key for a particular actor.</param>
        /// <param name="actor">The bound <see cref="IActorRef"/>, if any. Is set to <see cref="ActorRefs.Nobody"/> if key is not found.</param>
        /// <param name="overwrite">If <c>true</c>, allows overwriting of a previous actor with the same key. Defaults to <c>false</c>.</param>
        /// <returns><c>true</c> if the actor was set to this key in the registry, <c>false</c> otherwise.</returns>
        public bool TryRegister(Type key, IActorRef actor, bool overwrite = false)
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
        /// <param name="actor">The bound <see cref="IActorRef"/>, if any. Is set to <see cref="ActorRefs.Nobody"/> if key is not found.</param>
        /// <returns><c>true</c> if an actor with this key exists, <c>false</c> otherwise.</returns>
        public bool TryGet<TKey>(out IActorRef actor)
        {
            return TryGet(typeof(TKey), out actor);
        }

        /// <summary>
        /// Try to retrieve an <see cref="IActorRef"/> with the given <see cref="TKey"/>.
        /// </summary>
        /// <param name="key">The key for a particular actor.</param>
        /// <param name="actor">The bound <see cref="IActorRef"/>, if any. Is set to <see cref="ActorRefs.Nobody"/> if key is not found.</param>
        /// <returns><c>true</c> if an actor with this key exists, <c>false</c> otherwise.</returns>
        public bool TryGet(Type key, out IActorRef actor)
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
        /// Fetches the <see cref="IActorRef"/> by key.
        /// </summary>
        /// <typeparam name="TKey">The key type to retrieve this actor.</typeparam>
        /// <returns>If found, the underlying <see cref="IActorRef"/>.
        /// If not found, returns <see cref="ActorRefs.Nobody"/>.</returns>
        public IActorRef Get<TKey>()
        {
            if (TryGet<TKey>(out var actor))
                return actor;
            return ActorRefs.Nobody;
        }

        /// <summary>
        /// Allows enumerated access to the collection of all registered actors.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<KeyValuePair<Type, IActorRef>> GetEnumerator()
        {
            return _actorRegistrations.GetEnumerator();
        }
        /// <summary>
        /// Allows enumerated access to the collection of all registered actors.
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public static IActorRegistry For(ActorSystem actorSystem)
        {
            return actorSystem.WithExtension<ActorRegistry, ActorRegistryExtension>();
        }
        }
    
    /// <summary>
    /// Represents a read-only collection of <see cref="IActorRef"/> instances keyed by the actor name.
    /// </summary>
    public interface IReadOnlyActorRegistry : IEnumerable<KeyValuePair<Type, IActorRef>>
    {
        /// <summary>
        /// Try to retrieve an <see cref="IActorRef"/> with the given <see cref="TKey"/>.
        /// </summary>
        /// <param name="actor">The bound <see cref="IActorRef"/>, if any. Is set to <see cref="ActorRefs.Nobody"/> if key is not found.</param>
        /// <returns><c>true</c> if an actor with this key exists, <c>false</c> otherwise.</returns>
        bool TryGet<TKey>(out IActorRef actor);
        
        /// <summary>
        /// Try to retrieve an <see cref="IActorRef"/> with the given <see cref="TKey"/>.
        /// </summary>
        /// <param name="key">The key for a particular actor.</param>
        /// <param name="actor">The bound <see cref="IActorRef"/>, if any. Is set to <see cref="ActorRefs.Nobody"/> if key is not found.</param>
        /// <returns><c>true</c> if an actor with this key exists, <c>false</c> otherwise.</returns>
        bool TryGet(Type key, out IActorRef actor);
        
        /// <summary>
        /// Fetches the <see cref="IActorRef"/> by key.
        /// </summary>
        /// <typeparam name="TKey">The key type to retrieve this actor.</typeparam>
        /// <returns>If found, the underlying <see cref="IActorRef"/>.
        /// If not found, returns <see cref="ActorRefs.Nobody"/>.</returns>
        IActorRef Get<TKey>();
    }

    /// <summary>
    /// An abstraction to allow <see cref="IActorRef"/> instances to be injected to non-Akka classes (such as controllers and SignalR Hubs).
    /// </summary>
    /// <remarks>
    /// Should only be used for top-level actors that need to be accessed from inside or outside the <see cref="ActorSystem"/>.
    ///
    /// If you are adding every single actor in your <see cref="ActorSystem"/> to the registry you are definitely using it wrong.
    /// </remarks>
    public interface IActorRegistry: IReadOnlyActorRegistry
    {
        /// <summary>
        /// Attempts to register an actor with the registry.
        /// </summary>
        /// <param name="actor">The bound <see cref="IActorRef"/>, if any. Is set to <see cref="ActorRefs.Nobody"/> if key is not found.</param>
        /// <param name="overwrite">If <c>true</c>, allows overwriting of a previous actor with the same key. Defaults to <c>false</c>.</param>
        /// <returns><c>true</c> if the actor was set to this key in the registry, <c>false</c> otherwise.</returns>
        bool TryRegister<TKey>(IActorRef actor, bool overwrite = false);

        /// <summary>
        /// Attempts to register an actor with the registry.
        /// </summary>
        /// <param name="key">The key for a particular actor.</param>
        /// <param name="actor">The bound <see cref="IActorRef"/>, if any. Is set to <see cref="ActorRefs.Nobody"/> if key is not found.</param>
        /// <param name="overwrite">If <c>true</c>, allows overwriting of a previous actor with the same key. Defaults to <c>false</c>.</param>
        /// <returns><c>true</c> if the actor was set to this key in the registry, <c>false</c> otherwise.</returns>
        bool TryRegister(Type key, IActorRef actor, bool overwrite = false);
    }
}