using System;
using System.IO;
using Akka.Actor;
using Akka.Actor.Setup;
using Akka.Configuration;
using Akka.Util;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ServiceProvider = Microsoft.Extensions.DependencyInjection.ServiceProvider;

namespace Akka.Hosting
{
    /// <summary>
    /// Extension methods for configuring Akka.NET inside a Microsoft.Extensions.Hosting setup.
    /// </summary>
    public static class AkkaHostingExtensions
    {
        /// <summary>
        /// Registers an <see cref="ActorSystem"/> to this instance and creates a
        /// <see cref="AkkaConfigurationBuilder"/> that can be used to configure its
        /// behavior and Sys spawning.
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
            return AddAkka(services, actorSystemName, (configurationBuilder, provider) =>
            {
                builder(configurationBuilder);
            });
        }
        
        /// <summary>
        /// Registers an <see cref="ActorSystem"/> to this instance and creates a
        /// <see cref="AkkaConfigurationBuilder"/> that can be used to configure its
        /// behavior and Sys spawning.
        /// </summary>
        /// <param name="services">The service collection to which we are binding Akka.NET.</param>
        /// <param name="actorSystemName">The name of the <see cref="ActorSystem"/> that will be instantiated.</param>
        /// <param name="builder">A configuration delegate that accepts an <see cref="IServiceProvider"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/> instance.</returns>
        /// <remarks>
        /// Starts a background <see cref="IHostedService"/> that runs the <see cref="ActorSystem"/>
        /// and manages its lifecycle in accordance with Akka.NET best practices.
        /// </remarks>
        public static IServiceCollection AddAkka(this IServiceCollection services, string actorSystemName, Action<AkkaConfigurationBuilder, IServiceProvider> builder)
        {
            var b = new AkkaConfigurationBuilder(services, actorSystemName);
            services.AddSingleton<AkkaConfigurationBuilder>(sp =>
            {
                builder(b, sp);
                return b;
            });
            
            // registers the hosted services and begins execution
            b.Bind();
            
            // start the IHostedService which will run Akka.NET
            services.AddHostedService<AkkaHostedService>();

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
        /// Automatically loads the given HOCON file from <see cref="hoconFilePath"/>
        /// and inserts it into the <see cref="ActorSystem"/>s' configuration.
        /// </summary>
        /// <param name="builder">The builder instance being configured.</param>
        /// <param name="hoconFilePath">The path to the HOCON file. Can be relative or absolute.</param>
        /// <param name="addMode">The <see cref="HoconAddMode"/> - defaults to appending this HOCON as a fallback.</param>
        /// <returns>The same <see cref="AkkaConfigurationBuilder"/> instance originally passed in.</returns>
        public static AkkaConfigurationBuilder AddHoconFile(this AkkaConfigurationBuilder builder, string hoconFilePath,
            HoconAddMode addMode = HoconAddMode.Append)
        {
            var hoconText = ConfigurationFactory.ParseString(File.ReadAllText(hoconFilePath));
            return AddHocon(builder, hoconText, addMode);
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
        public static AkkaConfigurationBuilder WithActors(this AkkaConfigurationBuilder builder, Action<ActorSystem, IActorRegistry> actorStarter)
        {
            return builder.StartActors(actorStarter);
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
