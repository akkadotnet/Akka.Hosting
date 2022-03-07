using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Actor.Setup;
using Akka.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Akka.Hosting
{
    public sealed class AkkaConfigurationBuilder
    {
        private IServiceCollection _serviceCollection;
        private readonly HashSet<Setup> _setups = new HashSet<Setup>();
        private ProviderSelection _selection = ProviderSelection.Local.Instance;
        private Config _configuration = Config.Empty;
        private Func<ActorSystem, Task> _actorStarter = system => Task.CompletedTask;

        public AkkaConfigurationBuilder(IServiceCollection serviceCollection)
        {
            _serviceCollection = serviceCollection;
        }

        public AkkaConfigurationBuilder AddSetup(Setup setup)
        {
            _setups.Add(setup);
            return this;
        }

        public AkkaConfigurationBuilder AddActorRefProvider(ProviderSelection provider)
        {
            _selection = provider;
            return this;
        }
        
        
    }
    
    public static class AkkaHostingExtensions
    {
        public
    }
}
