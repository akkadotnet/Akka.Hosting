using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Actor.Dsl;
using Akka.Actor.Setup;
using Akka.Configuration;
using Akka.DependencyInjection;
using Akka.Hosting.Logging;
using Akka.Serialization;
using Akka.Util;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
    public delegate Task ActorStarter(ActorSystem system, IActorRegistry registry);

    /// <summary>
    /// Used to help populate a <see cref="SerializationSetup"/> upon starting the <see cref="ActorSystem"/>,
    /// if any are added to the builder;
    /// </summary>
    internal sealed class SerializerRegistration
    {
        public SerializerRegistration(string id, ImmutableHashSet<Type> typeBindings,
            Func<ExtendedActorSystem, Serializer> serializerFactory)
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
        internal readonly string ActorSystemName;
        internal readonly IServiceCollection ServiceCollection;
        internal readonly HashSet<SerializerRegistration> Serializers = new HashSet<SerializerRegistration>();
        internal readonly HashSet<Setup> Setups = new HashSet<Setup>();

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
            ServiceCollection = serviceCollection;
            ActorSystemName = actorSystemName;
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

            Setups.Add(setup);
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

        private static ActorStarter ToAsyncStarter(Action<ActorSystem, IActorRegistry> nonAsyncStarter)
        {
            Task Starter(ActorSystem f, IActorRegistry registry)
            {
                nonAsyncStarter(f, registry);
                return Task.CompletedTask;
            }

            return Starter;
        }

        public AkkaConfigurationBuilder StartActors(Action<ActorSystem, IActorRegistry> starter)
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
            string serializerIdentifier, IEnumerable<Type> boundTypes,
            Func<ExtendedActorSystem, Serializer> serializerFactory)
        {
            var serializerRegistration = new SerializerRegistration(serializerIdentifier,
                boundTypes.ToImmutableHashSet(), serializerFactory);
            Serializers.Add(serializerRegistration);
            return this;
        }

        internal void Bind()
        {
            // register as singleton - not interested in supporting multi-Sys use cases
            ServiceCollection.AddSingleton<ActorSystem>(ActorSystemFactory());

            ServiceCollection.AddSingleton<ActorRegistry>(sp =>
            {
                return ActorRegistry.For(sp.GetRequiredService<ActorSystem>());
            });
            
            ServiceCollection.AddSingleton<IActorRegistry>(sp =>
            {
                return sp.GetRequiredService<ActorRegistry>();
            });
            
            ServiceCollection.AddSingleton<IReadOnlyActorRegistry>(sp =>
            {
                return sp.GetRequiredService<ActorRegistry>();
            });
        }

        private static Func<IServiceProvider, ActorSystem> ActorSystemFactory()
        {
            return sp =>
            {
                var config = sp.GetRequiredService<AkkaConfigurationBuilder>();

                /*
                 * Build setups
                 */
                
                // check to see if we need a LoggerSetup
                var hasLoggerSetup = config.Setups.Any(c => c is LoggerFactorySetup);
                if (!hasLoggerSetup)
                {
                    var logger = sp.GetService<ILoggerFactory>();
                    
                    // on the off-chance that we're not running with ILogger support enabled
                    // (should be a rare case that only comes up during testing)
                    if (logger != null) 
                    {
                        var loggerSetup = new LoggerFactorySetup(logger);
                        config.AddSetup(loggerSetup);
                    }
                }
                
                var diSetup = DependencyResolverSetup.Create(sp);
                var bootstrapSetup = BootstrapSetup.Create().WithConfig(config.Configuration.GetOrElse(Config.Empty));
                if (config.ActorRefProvider.HasValue) // only set the provider when explicitly required
                {
                    bootstrapSetup = bootstrapSetup.WithActorRefProvider(config.ActorRefProvider.Value);
                }

                var actorSystemSetup = bootstrapSetup.And(diSetup);
                foreach (var setup in config.Setups)
                {
                    actorSystemSetup = actorSystemSetup.And(setup);
                }

                /* check to see if we have any custom serializers that need to be registered */
                if (config.Serializers.Count > 0)
                {
                    var serializationSetup = SerializationSetup.Create(system =>
                        config.Serializers
                            .Select(r =>
                                SerializerDetails.Create(r.Id, r.SerializerFactory(system), r.TypeBindings))
                            .ToImmutableHashSet());

                    actorSystemSetup = actorSystemSetup.And(serializationSetup);
                }

                /*
                 * Start ActorSystem
                 */
                var sys = ActorSystem.Create(config.ActorSystemName, actorSystemSetup);

                return sys;
            };
        }

        internal Task<ActorSystem> StartAsync(IServiceProvider sp)
        {
            return StartAsync(sp.GetRequiredService<ActorSystem>());
        }

        internal async Task<ActorSystem> StartAsync(ActorSystem sys)
        {
            if (_complete) return sys;
            _complete = true;

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