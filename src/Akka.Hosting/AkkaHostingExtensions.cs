using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Actor.Setup;
using Akka.Configuration;
using Akka.DependencyInjection;
using Akka.Util;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ServiceProvider = Microsoft.Extensions.DependencyInjection.ServiceProvider;

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
    public delegate Task ActorStarter(ActorSystem system);
    
    public sealed class AkkaConfigurationBuilder
    {
        private readonly string _actorSystemName;
        private readonly IServiceCollection _serviceCollection;
        private readonly HashSet<Setup> _setups = new HashSet<Setup>();
        private Option<ProviderSelection> _selection = Option<ProviderSelection>.None;
        private Option<Config> _configuration = Option<Config>.None;
        private ActorStarter _actorStarter = system => Task.CompletedTask;
        private bool _complete = false;

        public AkkaConfigurationBuilder(IServiceCollection serviceCollection, string actorSystemName)
        {
            _serviceCollection = serviceCollection;
            _actorSystemName = actorSystemName;
        }

        public AkkaConfigurationBuilder AddSetup(Setup setup)
        {
            if (_complete) return this;
            
            // we will recreate our own BootstrapSetup later - just extract the parts for now.
            if (setup is BootstrapSetup bootstrapSetup)
            {
                if (bootstrapSetup.Config.HasValue)
                    _configuration = _configuration.HasValue
                        ? _configuration.FlatSelect<Config>(c => bootstrapSetup.Config.Value.WithFallback(c))
                        : bootstrapSetup.Config;
                _selection = bootstrapSetup.ActorRefProvider;
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

        public AkkaConfigurationBuilder WithActorRefProvider(ProviderSelection provider)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));
            if (_complete) return this;
            _selection = provider;
            return this;
        }

        public AkkaConfigurationBuilder AddHoconConfiguration(HoconConfigurator configurator, Config newHocon)
        {
            if (newHocon == null)
                throw new ArgumentNullException(nameof(newHocon));
            if (configurator == null)
                throw new ArgumentNullException(nameof(configurator));
            
            if (_complete) return this;
            _configuration = configurator(_configuration.GetOrElse(Config.Empty), newHocon);
            return this;
        }

        public AkkaConfigurationBuilder AddHoconConfiguration(Config newHocon,
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

        public AkkaConfigurationBuilder StartActors(ActorStarter starter)
        {
            if (_complete) return this;
            _actorStarter = starter;
            return this;
        }

        internal void Build()
        {
            if (!_complete)
            {
                _complete = true;
                _serviceCollection.AddSingleton<AkkaConfigurationBuilder>();
                
                // start the IHostedService which will run Akka.NET
                _serviceCollection.AddHostedService<AkkaHostedService>();
            }
        }

        internal async Task<ActorSystem> StartAsync()
        {
            if (!_complete)
                throw new InvalidOperationException("Cannot start ActorSystem - Builder is not marked as complete.");

            /*
             * Build setups
             */
            var sp = _serviceCollection.BuildServiceProvider();
            var diSetup = DependencyResolverSetup.Create(sp);
            var bootstrapSetup = BootstrapSetup.Create().WithConfig(_configuration.GetOrElse(Config.Empty));
            if (_selection.HasValue) // only set the provider when explicitly required
            {
                bootstrapSetup = bootstrapSetup.WithActorRefProvider(_selection.Value);
            }

            var actorSystemSetup = bootstrapSetup.And(diSetup);
            foreach (var setup in _setups)
            {
                actorSystemSetup = actorSystemSetup.And(setup);
            }
            
            /*
             * Start ActorSystem
             */
            var actorSystem = ActorSystem.Create(_actorSystemName, actorSystemSetup);
            
            // register as singleton - not interested in supporting multi-ActorSystem use cases
            _serviceCollection.AddSingleton<ActorSystem>(actorSystem);

            
            /*
             * Start Actors
             */
            await _actorStarter(actorSystem).ConfigureAwait(false);

            return actorSystem;
        }
    }
    
    /// <summary>
    /// Extension methods for configuring Akka.NET inside a Microsoft.Extensions.Hosting setup.
    /// </summary>
    public static class AkkaHostingExtensions
    {
        /// <summary>
        /// Registers an <see cref="ActorSystem"/> to this instance and creates a
        /// <see cref="AkkaConfigurationBuilder"/> that can be used to configure its
        /// behavior and ActorSystem spawning.
        /// </summary>
        /// <param name="services">The service collection to which we are binding Akka.NET.</param>
        /// <param name="actorSystemName">The name of the <see cref="ActorSystem"/> that will be instantiated.</param>
        /// <param name="builder">A configuration delegate.</param>
        /// <returns>The <see cref="IServiceCollection"/> instance.</returns>
        /// <remarks>
        /// Starts a background <see cref="IHostedService"/> that runs the <see cref="ActorSystem"/>
        /// and manages its lifecycle in accordance with Akka.NET best practices.
        /// </remarks>
        public static IServiceCollection AddAkka(this IServiceCollection services, string actorSystemName, Action<AkkaConfigurationBuilder> builder)
        {
            var b = new AkkaConfigurationBuilder(services, actorSystemName);
            builder(b);
            
            // registers the hosted services and begins execution
            b.Build();

            return services;
        }
        
        /// <summary>
        /// Registers an <see cref="ActorSystem"/> to this instance and creates a
        /// <see cref="AkkaConfigurationBuilder"/> that can be used to configure its
        /// behavior and ActorSystem spawning.
        /// </summary>
        /// <param name="services">The service collection to which we are binding Akka.NET.</param>
        /// <param name="actorSystemName">The name of the <see cref="ActorSystem"/> that will be instantiated.</param>
        /// <param name="builder">A configuration delegate that accepts an <see cref="IServiceProvider"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/> instance.</returns>
        /// <remarks>
        /// Starts a background <see cref="IHostedService"/> that runs the <see cref="ActorSystem"/>
        /// and manages its lifecycle in accordance with Akka.NET best practices.
        /// </remarks>
        public static IServiceCollection AddAkka(this IServiceCollection services, string actorSystemName, Action<AkkaConfigurationBuilder, ServiceProvider> builder)
        {
            var b = new AkkaConfigurationBuilder(services, actorSystemName);
            var sp = services.BuildServiceProvider();
            builder(b, sp);
            
            // registers the hosted services and begins execution
            b.Build();

            return services;
        }

        /// <summary>
        /// Adds a new <see cref="Setup"/> to this builder.
        /// </summary>
        /// <param name="builder">The builder instance being configured.</param>
        /// <param name="setup">A new <see cref="Setup"/> instance.</param>
        /// <returns>The same <see cref="AkkaConfigurationBuilder"/> instance originally passed in.</returns>
        public static AkkaConfigurationBuilder AddSetup(this AkkaConfigurationBuilder builder, Setup setup)
        {
            return builder.AddSetup(setup);
        }

        /// <summary>
        /// Adds a <see cref="Config"/> element to the <see cref="ActorSystem"/> being configured.
        /// </summary>
        /// <param name="builder">The builder instance being configured.</param>
        /// <param name="hocon">The HOCON to add.</param>
        /// <param name="addMode">The <see cref="HoconAddMode"/> - defaults to appending this HOCON as a fallback.</param>
        /// <returns>The same <see cref="AkkaConfigurationBuilder"/> instance originally passed in.</returns>
        public static AkkaConfigurationBuilder AddHocon(this AkkaConfigurationBuilder builder, Config hocon,
            HoconAddMode addMode = HoconAddMode.Append)
        {
            return builder.AddHoconConfiguration(hocon, addMode);
        }

        /// <summary>
        /// Configures the <see cref="ProviderSelection"/> for this <see cref="ActorSystem"/>. Can be used to
        /// configure whether or not Akka, Akka.Remote, or Akka.Cluster starts.
        /// </summary>
        /// <param name="builder">The builder instance being configured.</param>
        /// <param name="providerSelection">A <see cref="ProviderSelection"/>.</param>
        /// <returns>The same <see cref="AkkaConfigurationBuilder"/> instance originally passed in.</returns>
        public static AkkaConfigurationBuilder WithActorRefProvider(this AkkaConfigurationBuilder builder,
            ProviderSelection providerSelection)
        {
            return builder.WithActorRefProvider(providerSelection);
        }

        /// <summary>
        /// Adds a <see cref="ActorStarter"/> delegate that will be used exactly once to instantiate
        /// actors once the <see cref="ActorSystem"/> is started in this process. 
        /// </summary>
        /// <param name="builder">The builder instance being configured.</param>
        /// <param name="actorStarter">A <see cref="ActorStarter"/> delegate
        /// for configuring and starting actors.</param>
        /// <returns>The same <see cref="AkkaConfigurationBuilder"/> instance originally passed in.</returns>
        public static AkkaConfigurationBuilder WithActors(this AkkaConfigurationBuilder builder, ActorStarter actorStarter)
        {
            return builder.StartActors(actorStarter);
        }
        
    }
}
