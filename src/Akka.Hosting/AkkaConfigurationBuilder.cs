using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Actor.Setup;
using Akka.Configuration;
using Akka.DependencyInjection;
using Akka.Serialization;
using Akka.Util;
using Microsoft.Extensions.DependencyInjection;

namespace Akka.Hosting
{
    /// <summary>
    /// Delegate used to configure how to merge new HOCON in with the previous HOCON
    /// that has already been added to the <see cref="AkkaConfigurationBuilder"/>.
    /// </summary>
    public delegate Config HoconConfigurator(Config currentConfig, Config configToAdd);

    /// <summary>
    /// Describes how to add a new <see cref="Config"/> section to our existing HOCON.
    /// </summary>
    public enum HoconAddMode
    {
        /// <summary>
        /// Appends this HOCON to the back as a fallback.
        /// </summary>
        Append,

        /// <summary>
        /// Prepend this HOCON to the front, overriding current values without deleting them.
        /// </summary>
        Prepend,

        /// <summary>
        /// Replace all current HOCON with this HOCON instead.
        /// </summary>
        /// <remarks>
        /// WARNING: this is a destructive action. If you are writing a plugin or extension, never
        /// call a method with this value directly. Always allow the user to choose. 
        /// </remarks>
        Replace
    }

    /// <summary>
    /// Delegate used to instantiate <see cref="IActorRef"/>s once the <see cref="ActorSystem"/> has booted.
    /// </summary>
    public delegate Task ActorStarter(ActorSystem system, ActorRegistry registry);

    /// <summary>
    /// Used to help populate a <see cref="SerializationSetup"/> upon starting the <see cref="ActorSystem"/>,
    /// if any are added to the builder;
    /// </summary>
    internal sealed class SerializerRegistration
    {
        public SerializerRegistration(string id, ImmutableHashSet<Type> typeBindings, Func<ExtendedActorSystem, Serializer> serializerFactory)
        {
            Id = id;
            TypeBindings = typeBindings;
            SerializerFactory = serializerFactory;
        }

        public string Id { get; }
        
        public Func<ExtendedActorSystem, Serializer> SerializerFactory { get; }
        
        public ImmutableHashSet<Type> TypeBindings { get; }
    }

    public sealed class AkkaConfigurationBuilder
    {
        private readonly string _actorSystemName;
        private readonly IServiceCollection _serviceCollection;
        private readonly HashSet<SerializerRegistration> _serializers = new HashSet<SerializerRegistration>();
        private readonly HashSet<Setup> _setups = new HashSet<Setup>();

        /// <summary>
        /// The currently configured <see cref="ProviderSelection"/>.
        /// </summary>
        public Option<ProviderSelection> ActorRefProvider { get; private set; } = Option<ProviderSelection>.None;

        /// <summary>
        /// The current HOCON configuration.
        /// </summary>
        public Option<Config> Configuration { get; private set; } = Option<Config>.None;

        /// <summary>
        /// INTERNAL API.
        ///
        /// Used to hold a reference to the <see cref="Sys"/> being started.
        /// </summary>
        internal Option<ActorSystem> Sys { get; set; } = Option<ActorSystem>.None;

        private readonly HashSet<ActorStarter> _actorStarters = new HashSet<ActorStarter>();
        private bool _complete = false;

        public AkkaConfigurationBuilder(IServiceCollection serviceCollection, string actorSystemName)
        {
            _serviceCollection = serviceCollection;
            _actorSystemName = actorSystemName;
        }

        internal AkkaConfigurationBuilder AddSetup(Setup setup)
        {
            if (_complete) return this;

            // we will recreate our own BootstrapSetup later - just extract the parts for now.
            if (setup is BootstrapSetup bootstrapSetup)
            {
                if (bootstrapSetup.Config.HasValue)
                    Configuration = Configuration.HasValue
                        ? Configuration.FlatSelect<Config>(c => bootstrapSetup.Config.Value.WithFallback(c))
                        : bootstrapSetup.Config;
                ActorRefProvider = bootstrapSetup.ActorRefProvider;
                return this;
            }

            // don't apply the diSetup
            if (setup is DependencyResolverSetup)
            {
                return this;
            }

            _setups.Add(setup);
            return this;
        }

        internal AkkaConfigurationBuilder WithActorRefProvider(ProviderSelection provider)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));
            if (_complete) return this;
            ActorRefProvider = provider;
            return this;
        }

        internal AkkaConfigurationBuilder AddHoconConfiguration(HoconConfigurator configurator, Config newHocon)
        {
            if (newHocon == null)
                throw new ArgumentNullException(nameof(newHocon));
            if (configurator == null)
                throw new ArgumentNullException(nameof(configurator));

            if (_complete) return this;
            Configuration = configurator(Configuration.GetOrElse(Config.Empty), newHocon);
            return this;
        }

        internal AkkaConfigurationBuilder AddHoconConfiguration(Config newHocon,
            HoconAddMode addMode = HoconAddMode.Append)
        {
            switch (addMode)
            {
                case HoconAddMode.Append:
                    return AddHoconConfiguration((config, add) => config.WithFallback(add), newHocon);
                case HoconAddMode.Prepend:
                    return AddHoconConfiguration((config, add) => add.WithFallback(config), newHocon);
                case HoconAddMode.Replace:
                    return AddHoconConfiguration((config, add) => add.WithFallback(config), newHocon);
                default:
                    throw new ArgumentOutOfRangeException(nameof(addMode), addMode, null);
            }
        }

        private static ActorStarter ToAsyncStarter(Action<ActorSystem, ActorRegistry> nonAsyncStarter)
        {
            Task Starter(ActorSystem f, ActorRegistry registry)
            {
                nonAsyncStarter(f, registry);
                return Task.CompletedTask;
            }

            return Starter;
        }

        public AkkaConfigurationBuilder StartActors(Action<ActorSystem, ActorRegistry> starter)
        {
            if (_complete) return this;
            _actorStarters.Add(ToAsyncStarter(starter));
            return this;
        }

        public AkkaConfigurationBuilder StartActors(ActorStarter starter)
        {
            if (_complete) return this;
            _actorStarters.Add(starter);
            return this;
        }

        public AkkaConfigurationBuilder WithCustomSerializer(
            string serializerIdentifier, IEnumerable<Type> boundTypes, Func<ExtendedActorSystem, Serializer> serializerFactory)
        {
            var serializerRegistration = new SerializerRegistration(serializerIdentifier,
                boundTypes.ToImmutableHashSet(), serializerFactory);
            _serializers.Add(serializerRegistration);
            return this;
        }

        internal void Build()
        {
            if (!_complete)
            {
                _complete = true;
                _serviceCollection.AddSingleton<AkkaConfigurationBuilder>(this);

                // start the IHostedService which will run Akka.NET
                _serviceCollection.AddHostedService<AkkaHostedService>();

                /*
            * Build setups
            */
                var sp = _serviceCollection.BuildServiceProvider();
                var diSetup = DependencyResolverSetup.Create(sp);
                var bootstrapSetup = BootstrapSetup.Create().WithConfig(Configuration.GetOrElse(Config.Empty));
                if (ActorRefProvider.HasValue) // only set the provider when explicitly required
                {
                    bootstrapSetup = bootstrapSetup.WithActorRefProvider(ActorRefProvider.Value);
                }

                var actorSystemSetup = bootstrapSetup.And(diSetup);
                foreach (var setup in _setups)
                {
                    actorSystemSetup = actorSystemSetup.And(setup);
                }
                
                /* check to see if we have any custom serializers that need to be registered */
                if (_serializers.Count > 0)
                {
                    var serializationSetup = SerializationSetup.Create(system =>
                        _serializers
                            .Select(r => SerializerDetails.Create(r.Id, r.SerializerFactory(system), r.TypeBindings))
                            .ToImmutableHashSet());

                    actorSystemSetup = actorSystemSetup.And(serializationSetup);
                }

                /*
                 * Start ActorSystem
                 */
                var sys = ActorSystem.Create(_actorSystemName, actorSystemSetup);

                Sys = sys;

                // register as singleton - not interested in supporting multi-Sys use cases
                _serviceCollection.AddSingleton<ActorSystem>(sys);

                var actorRegistry = ActorRegistry.For(sys);
                _serviceCollection.AddSingleton(actorRegistry);
            }
        }

        internal Task<ActorSystem> StartAsync()
        {
            if (!_complete)
                throw new InvalidOperationException("Cannot start Sys - Builder is not marked as complete.");

            return StartAsync(Sys.Value);
        }

        internal async Task<ActorSystem> StartAsync(ActorSystem sys)
        {
            if (!_complete)
                throw new InvalidOperationException("Cannot start Sys - Builder is not marked as complete.");

            /*
             * Start Actors
             */

            var registry = ActorRegistry.For(sys);

            foreach (var starter in _actorStarters)
            {
                await starter(sys, registry).ConfigureAwait(false);
            }

            return sys;
        }
    }
}