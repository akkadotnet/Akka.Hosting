using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
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
    /// Used to implement "wait for actor" mechanics
    /// </summary>
    internal sealed class WaitForActorRegistration : IEquatable<WaitForActorRegistration>
    {
        public WaitForActorRegistration(Type key, TaskCompletionSource<IActorRef> waiter)
        {
            Key = key;
            Waiter = waiter;
        }

        public Type Key { get; }
        
        public TaskCompletionSource<IActorRef> Waiter { get; }

        public CancellationTokenRegistration CancellationRegistration { get; set; }

        public bool Equals(WaitForActorRegistration other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Key == other.Key;
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || obj is WaitForActorRegistration other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Key.GetHashCode();
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
    public class ActorRegistry : IExtension
    {
        private readonly ConcurrentDictionary<Type, IActorRef> _actorRegistrations =
            new ConcurrentDictionary<Type, IActorRef>();

        /// <summary>
        /// In the event that an actor is not available yet, typically during the very beginning of ActorSystem startup,
        /// we can wait on that actor becoming available.
        /// </summary>
        /// <remarks>
        /// Have to store a collection of <see cref="WaitForActorRegistration"/>s here so each waiter gets its own cancellation token.
        /// </remarks>
        private readonly ConcurrentDictionary<Type, ImmutableHashSet<WaitForActorRegistration>> _actorWaiters =
            new ConcurrentDictionary<Type, ImmutableHashSet<WaitForActorRegistration>>();

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
            
            NotifyWaiters(key, _actorRegistrations[key]);
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
        /// Try to retrieve an <see cref="IActorRef"/> with the given type.
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

        public async Task<IActorRef> GetAsync<TKey>(CancellationToken ct)
        {
            return await GetAsync(typeof(TKey), ct);
        }

        public async Task<IActorRef> GetAsync(Type key, CancellationToken ct)
        {
            // try to get the populated actor first, if available
            if (TryGet(key, out var storedActor))
            {
                return storedActor;
            }

            var tcs = new TaskCompletionSource<IActorRef>(TaskCreationOptions.RunContinuationsAsynchronously);
            var waitingRegistration = new WaitForActorRegistration(key, tcs);
            var registration = ct.Register(CancelWaiter(key, ct, waitingRegistration), _actorWaiters);
            waitingRegistration.CancellationRegistration = registration;

            _actorWaiters.AddOrUpdate(key,
                type => ImmutableHashSet<WaitForActorRegistration>.Empty.Add(waitingRegistration),
                (type, set) => set.Add(waitingRegistration));
            

            return await tcs.Task.ConfigureAwait(false);
        }

        private void NotifyWaiters(Type key, IActorRef value)
        {
            // remove the registrations and then iterate over them
            if (_actorWaiters.TryRemove(key, out var registrations))
            {
                foreach (var r in registrations)
                {
                    r.Waiter.TrySetResult(value);
                    r.CancellationRegistration.Dispose();
                }
            }
        }

        private static Action<object> CancelWaiter(Type key, CancellationToken ct, WaitForActorRegistration waitingRegistration)
        {
            return dict =>
            {
                // first step during timeout is to remove our registration
                var d = (ConcurrentDictionary<Type, ImmutableHashSet<WaitForActorRegistration>>)dict;
                var removed = false;
                while (!removed)
                {
                    if(d.TryGetValue(key, out var registrations))
                    {
                        var original = registrations;
                        registrations = registrations.Remove(waitingRegistration);
                        if (d.TryUpdate(key, registrations, original))
                            removed = true;
                    }
                    else // no key exists
                    {
                        removed = true;
                    }
                }
                
                // next, cancel the task
                waitingRegistration.Waiter.TrySetCanceled(ct);
            };
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

        public static ActorRegistry For(ActorSystem actorSystem)
        {
            return actorSystem.WithExtension<ActorRegistry, ActorRegistryExtension>();
        }
    }
}