using System.Text;
using Akka.Actor;
using Akka.Hosting;

namespace Akka.Remote.Hosting
{
    public static class AkkaRemoteHostingExtensions
    {
        private static AkkaConfigurationBuilder BuildRemoteHocon(
            this AkkaConfigurationBuilder builder,
            string hostname = null,
            int? port = null,
            string publicHostname = null,
            int? publicPort = null)
        {
            var sb = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(hostname))
                sb.AppendFormat("hostname = {0}\n", hostname);
            if (port != null)
                sb.AppendFormat("port = {0}\n", port);
            if(!string.IsNullOrWhiteSpace(publicHostname))
                sb.AppendFormat("public-hostname = {0}\n", publicHostname);
            if(publicPort != null)
                sb.AppendFormat("public-port = {0}\n", publicPort);

            if (sb.Length == 0) 
                return builder;
            
            sb.Insert(0, "akka.remote.dot-netty.tcp {\n");
            sb.Append("}");

            // prepend the remoting configuration to the front
            return builder.AddHocon(sb.ToString(), HoconAddMode.Prepend);
        }

        /// <summary>
        /// Adds Akka.Remote support to this <see cref="ActorSystem"/>.
        /// </summary>
        /// <param name="builder">A configuration delegate.</param>
        /// <param name="hostname">The hostname to bind Akka.Remote upon.</param>
        /// <param name="port">The port to bind Akka.Remote upon.</param>
        /// <param name="publicHostname">Optional. If using hostname aliasing, this is the host we will advertise.</param>
        /// <param name="publicPort">Optional. If using port aliasing, this is the port we will advertise.</param>
        /// <returns>The same <see cref="AkkaConfigurationBuilder"/> instance originally passed in.</returns>
        public static AkkaConfigurationBuilder WithRemoting(
            this AkkaConfigurationBuilder builder,
            string hostname = null,
            int? port = null,
            string publicHostname = null,
            int? publicPort = null)
        {
            var hoconBuilder = BuildRemoteHocon(builder, hostname, port, publicHostname, publicPort);
        
            if (builder.ActorRefProvider.HasValue)
            {
                switch (builder.ActorRefProvider.Value)
                {
                    case ProviderSelection.Cluster _:
                    case ProviderSelection.Remote _:
                    case ProviderSelection.Custom _:
                        return hoconBuilder; // no-op
                }
            }

            return hoconBuilder.WithActorRefProvider(ProviderSelection.Remote.Instance);
        }
    }
}