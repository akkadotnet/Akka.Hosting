using System;
using System.IO;
using Akka.Actor;
using Akka.Actor.Setup;
using Akka.Configuration;
using Akka.DependencyInjection;
using Akka.Hosting.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
            
            if (Util.IsRunningInMaui)
            {
                // blow up Maui users who are about to footgun
                throw new PlatformNotSupportedException(
                    "Due to https://github.com/dotnet/maui/issues/2244, normal Akka.Hosting.AddAkka method will not work." +
                    "Instead, you need to install Akka.Hosting.Maui and use the AddAkkaMaui extension method instead.");
            }
            else
            {
                // start the IHostedService which will run Akka.NET
                services.AddHostedService<AkkaHostedService>();
            }

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
        public static AkkaConfigurationBuilder AddHocon(this AkkaConfigurationBuilder builder, Config hocon, HoconAddMode addMode)
        {
            return builder.AddHoconConfiguration(hocon, addMode);
        }

        /// <summary>
        ///     Converts an <see cref="IConfiguration"/> into HOCON <see cref="Config"/> instance and adds it to the
        ///     <see cref="ActorSystem"/> being configured.<br/>
        ///     <b>NOTES:</b><br/>
        ///     <list type="bullet">
        ///         <item>All variable name are automatically converted to lower case.</item>
        ///         <item>All "." (period) in the <see cref="IConfiguration"/> key will be treated as a HOCON object key separator</item>
        ///         <item>For environment variable configuration provider:
        ///             <list type="bullet">
        ///                 <item>"__" (double underline) will be converted to "." (period).</item>
        ///                 <item>"_" (single underline) will be converted to "-" (dash).</item>
        ///                 <item>If all keys are composed of integer parseable keys, the whole object is treated as an array</item>
        ///             </list>
        ///         </item>
        ///     </list>
        ///     Example:<br/>
        ///     JSON configuration:
        ///     <code>
        /// {
        ///     "akka.cluster": {
        ///         "roles": [ "front-end", "back-end" ],
        ///         "min-nr-of-members": 3,
        ///         "log-info": true
        ///     }
        /// }
        ///     </code>
        ///     and environment variables:
        ///     <code>
        /// AKKA__CLUSTER__ROLES__0=front-end
        /// AKKA__CLUSTER__ROLES__1=back-end
        /// AKKA__CLUSTER__MIN_NR_OF_MEMBERS=3
        /// AKKA__CLUSTER__LOG_INFO=true
        ///     </code>
        ///     is equivalent to HOCON configuration of:
        ///     <code>
        /// akka {
        ///     cluster {
        ///         roles: [ front-end, back-end ]
        ///         min-nr-of-members: 3
        ///         log-info: true
        ///     }
        /// }
        ///     </code>
        /// </summary>
        /// <param name="builder">The builder instance being configured.</param>
        /// <param name="configuration">The <see cref="IConfiguration"/> instance to be converted to HOCON <see cref="Config"/>.</param>
        /// <param name="addMode">The <see cref="HoconAddMode"/> - defaults to appending this HOCON as a fallback.</param>
        /// <returns>The same <see cref="AkkaConfigurationBuilder"/> instance originally passed in.</returns>
        public static AkkaConfigurationBuilder AddHocon(
            this AkkaConfigurationBuilder builder, 
            IConfiguration configuration,
            HoconAddMode addMode)
        {
            return builder.AddHoconConfiguration(configuration.ToHocon(), addMode);
        }
        
        /// <summary>
        /// Automatically loads the given HOCON file from <see cref="hoconFilePath"/>
        /// and inserts it into the <see cref="ActorSystem"/>s' configuration.
        /// </summary>
        /// <param name="builder">The builder instance being configured.</param>
        /// <param name="hoconFilePath">The path to the HOCON file. Can be relative or absolute.</param>
        /// <param name="addMode">The <see cref="HoconAddMode"/> - defaults to appending this HOCON as a fallback.</param>
        /// <returns>The same <see cref="AkkaConfigurationBuilder"/> instance originally passed in.</returns>
        public static AkkaConfigurationBuilder AddHoconFile(this AkkaConfigurationBuilder builder, string hoconFilePath, HoconAddMode addMode)
        {
            var hoconText = ConfigurationFactory.ParseString(File.ReadAllText(hoconFilePath));
            return AddHocon(builder, hoconText, addMode);
        }

        public static AkkaConfigurationBuilder WithActorAskTimeout(this AkkaConfigurationBuilder builder, TimeSpan timeout)
        {
            return AddHocon(builder, $"akka.actor.ask-timeout = {timeout.ToHocon(true, true)}", HoconAddMode.Prepend);
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
        /// <param name="actorStarter">A delegate for starting and configuring actors.</param>
        /// <returns>The same <see cref="AkkaConfigurationBuilder"/> instance originally passed in.</returns>
        /// <remarks>
        /// This method supports Akka.DependencyInjection directly by making the <see cref="ActorSystem"/>'s <see cref="IDependencyResolver"/> immediately available.
        /// </remarks>
        public static AkkaConfigurationBuilder WithActors(this AkkaConfigurationBuilder builder, Action<ActorSystem, IActorRegistry, IDependencyResolver> actorStarter)
        {
            return builder.StartActors(actorStarter);
        }
        
        /// <summary>
        /// Adds a <see cref="ActorStarter"/> delegate that will be used exactly once to instantiate
        /// actors once the <see cref="ActorSystem"/> is started in this process. 
        /// </summary>
        /// <param name="builder">The builder instance being configured.</param>
        /// <param name="actorStarter">A <see cref="ActorStarterWithResolver"/> delegate
        /// for configuring and starting actors.</param>
        /// <returns>The same <see cref="AkkaConfigurationBuilder"/> instance originally passed in.</returns>
        public static AkkaConfigurationBuilder WithActors(this AkkaConfigurationBuilder builder, ActorStarterWithResolver actorStarter)
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
