using Akka.Actor;
using Akka.Hosting;
using Akka.Util;

namespace Akka.Remote.Hosting;

public static class AkkaRemoteHostingExtensions
{
    private static AkkaConfigurationBuilder BuildRemoteHocon(this AkkaConfigurationBuilder builder, string hostname, int port, string? publicHostname = null, int? publicPort = null)
    {
        var config = $@"
            akka.remote.dot-netty.tcp.hostname = ""{hostname}""
            akka.remote.dot-netty.tcp.public-hostname = ""{publicHostname ?? hostname}""
            akka.remote.dot-netty.tcp.port = {port}
            akka.remote.dot-netty.tcp.public-port = {publicPort ?? port}
        ";

        // prepend the remoting configuration to the front
        return builder.AddHocon(config, HoconAddMode.Prepend);
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
    public static AkkaConfigurationBuilder WithRemoting(this AkkaConfigurationBuilder builder, string hostname, int port, string? publicHostname = null, int? publicPort = null)
    {
        var hoconBuilder = BuildRemoteHocon(builder, hostname, port);
        
        if (builder.ActorRefProvider.HasValue)
        {
            switch (builder.ActorRefProvider.Value)
            {
                case ProviderSelection.Cluster _:
                case ProviderSelection.Remote _:
                    return hoconBuilder; // no-op
            }
        }

        return hoconBuilder.WithActorRefProvider(ProviderSelection.Remote.Instance);
    }
}