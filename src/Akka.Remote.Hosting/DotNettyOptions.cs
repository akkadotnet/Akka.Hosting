// -----------------------------------------------------------------------
//  <copyright file="DotNettyOptions.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2023 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using System.Text;
using Akka.Configuration;
using Akka.Hosting;
using Akka.Remote.Transport.DotNetty;

namespace Akka.Remote.Hosting;

internal enum DotNettyTransportProtocol
{
    Udp,
    Tcp,
}
    
public abstract class DotNettyTransportOptions : RemoteTransportOptions, IWithPublicHostNameAndPort
{
    public override string TransportId => $"akka.remote.dot-netty.{TransportProtocol.ToString().ToLowerInvariant()}";

    /// <inheritdoc cref="IWithHostNameAndPort.HostName"/>
    public string? HostName { get; set; }
    
    /// <inheritdoc cref="IWithHostNameAndPort.Port"/>
    public int? Port { get; set; }
    
    /// <inheritdoc cref="IWithPublicHostNameAndPort.PublicHostName"/>
    public string? PublicHostName { get; set; }
    
    /// <inheritdoc cref="IWithPublicHostNameAndPort.PublicPort"/>
    public int? PublicPort { get; set; }
    
    internal abstract DotNettyTransportProtocol TransportProtocol { get; }
    
    public bool? EnableSsl { get; set; }
    
    public SslOptions Ssl { get; set; } = new ();
    
    internal override void Build(AkkaConfigurationBuilder builder)
    {
        base.Build(builder);
        
        if (EnableSsl is false || Ssl?.X509Certificate == null) 
            return;
        
        var suppressValidation = Ssl.SuppressValidation ?? false;
        builder.AddSetup(new DotNettySslSetup(Ssl.X509Certificate, suppressValidation));
    }
    
    protected override void Build(StringBuilder builder)
    {
        var sb = new StringBuilder();
        
        if (!string.IsNullOrWhiteSpace(HostName))
            sb.AppendLine($"hostname = {HostName.ToHocon()}");
        
        if (Port is not null)
            sb.AppendLine($"port = {Port}");
        
        if (!string.IsNullOrWhiteSpace(PublicHostName))
            sb.AppendLine($"public-hostname = {PublicHostName.ToHocon()}");
        
        if (PublicPort is not null)
            sb.AppendLine($"public-port = {PublicPort}");
        
        if (EnableSsl is not null)
        {
            sb.AppendLine($"enable-ssl = {EnableSsl.ToHocon()}");
            if (EnableSsl.Value)
            {
                if(Ssl is null)
                    throw new ConfigurationException("Ssl property need to be populated when EnableSsl is set to true.");
                
                Ssl.Build(sb);
            }
        }
        
        if(sb.Length == 0)
            return;
        
        sb.Insert(0, $"dot-netty.{TransportProtocol.ToString().ToLowerInvariant()} {{\n");
        sb.Append("}");
        builder.Append(sb);
    }
}

public sealed class DotNettyTcpTransportOptions : DotNettyTransportOptions
{
    internal override DotNettyTransportProtocol TransportProtocol => DotNettyTransportProtocol.Tcp;
}

public sealed class DotNettyUdpTransportOptions : DotNettyTransportOptions
{
    internal override DotNettyTransportProtocol TransportProtocol => DotNettyTransportProtocol.Udp;
}
