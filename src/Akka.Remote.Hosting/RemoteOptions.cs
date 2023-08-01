// -----------------------------------------------------------------------
//  <copyright file="AkkaRemoteOptions.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Akka.Configuration;
using Akka.Hosting;

namespace Akka.Remote.Hosting
{
    public class RemoteOptions
    {
        /// <summary>
        /// The hostname or ip to bind akka remoting to, <see cref="IPAddress.Any"/> is used if empty
        /// </summary>
        public string? HostName 
        {
            get => EnabledTransports.Count > 0 && EnabledTransports[0] is IWithHostNameAndPort opt
                ? opt.HostName
                : throw new ConfigurationException("Remote transport does not expose bindable host name and port");
            set
            {
                if(EnabledTransports.Count == 0 || EnabledTransports[0] is not IWithHostNameAndPort opt)
                    throw new ConfigurationException("Remote transport does not expose bindable host name and port");
                opt.HostName = value;
            } 
        }
        
        /// <summary>
        /// The default remote server port clients should connect to.
        /// Default is 2552, use 0 if you want a random available port.
        /// This port needs to be unique for each actor system on the same machine.
        /// </summary>
        public int? Port 
        {
            get => EnabledTransports.Count > 0 && EnabledTransports[0] is IWithHostNameAndPort opt
                ? opt.Port
                : throw new ConfigurationException("Remote transport does not expose bindable host name and port");
            set
            {
                if(EnabledTransports.Count == 0 || EnabledTransports[0] is not IWithHostNameAndPort opt)
                    throw new ConfigurationException("Remote transport does not expose bindable host name and port");
                opt.Port = value;
            } 
        }
        
        /// <summary>
        /// If this value is set, this becomes the public address for the actor system on this
        /// transport, which might be different than the physical ip address (hostname).
        /// This is designed to make it easy to support private / public addressing schemes
        /// </summary>
        public string? PublicHostName 
        {
            get => EnabledTransports.Count > 0 && EnabledTransports[0] is IWithPublicHostNameAndPort opt
                ? opt.PublicHostName
                : throw new ConfigurationException("Remote transport does not expose public host name and port");
            set
            {
                if(EnabledTransports.Count == 0 || EnabledTransports[0] is not IWithPublicHostNameAndPort opt)
                    throw new ConfigurationException("Remote transport does not expose public host name and port");
                opt.PublicHostName = value;
            } 
        }
        
        /// <summary>
        /// Similar to <see cref="PublicHostName"/>, this allows Akka.Remote users
        /// to alias the port they're listening on. The socket will actually listen on the
        /// <see cref="Port"/> property, but when connecting to other ActorSystems this node will advertise
        /// itself as being connected to the "public-port". This is helpful when working with 
        /// hosting environments that rely on address translation and port-forwarding, such as Docker.
        /// </summary>
        public int? PublicPort 
        {
            get => EnabledTransports.Count > 0 && EnabledTransports[0] is IWithPublicHostNameAndPort opt
                ? opt.PublicPort
                : throw new ConfigurationException("Remote transport does not expose public host name and port");
            set
            {
                if(EnabledTransports.Count == 0 || EnabledTransports[0] is not IWithPublicHostNameAndPort opt)
                    throw new ConfigurationException("Remote transport does not expose public host name and port");
                opt.PublicPort = value;
            } 
        }

        public List<RemoteTransportOptions> EnabledTransports { get; set; } = new ()
        {
            new DotNettyTcpTransportOptions()
        };

        internal void Build(AkkaConfigurationBuilder builder)
        {
            if (EnabledTransports is null || EnabledTransports.Count == 0)
                throw new ArgumentException("There must be at least one transport.", nameof(EnabledTransports));

            var transportIds = new List<string>();
            foreach (var transport in EnabledTransports)
            {
                transport.Build(builder);
                transportIds.Add(transport.TransportId.ToHocon());
            }

            builder.AddHocon($"akka.remote.enabled-transports = [{string.Join(", ", transportIds)}]", HoconAddMode.Prepend);
        }
    }

    public interface IWithHostNameAndPort
    {
        /// <summary>
        /// The hostname or ip to bind akka remoting to, <see cref="IPAddress.Any"/> is used if empty
        /// </summary>
        public string? HostName { get; set; }
        
        /// <summary>
        /// The default remote server port clients should connect to.
        /// Default is 2552, use 0 if you want a random available port.
        /// This port needs to be unique for each actor system on the same machine.
        /// </summary>
        public int? Port { get; set; }
    }

    public interface IWithPublicHostNameAndPort: IWithHostNameAndPort
    {
        /// <summary>
        /// If this value is set, this becomes the public address for the actor system on this
        /// transport, which might be different than the physical ip address (hostname).
        /// This is designed to make it easy to support private / public addressing schemes
        /// </summary>
        public string? PublicHostName { get; set; }
        
        /// <summary>
        /// Similar to <see cref="PublicHostName"/>, this allows Akka.Remote users
        /// to alias the port they're listening on. The socket will actually listen on the
        /// <see cref="IWithHostNameAndPort.Port"/> property, but when connecting to other ActorSystems this node will advertise
        /// itself as being connected to the "public-port". This is helpful when working with 
        /// hosting environments that rely on address translation and port-forwarding, such as Docker.
        /// </summary>
        public int? PublicPort { get; set; }
    }
    
    public abstract class RemoteTransportOptions
    {
        public abstract string TransportId { get; }
        
        internal virtual void Build(AkkaConfigurationBuilder builder)
        {
            var sb = new StringBuilder();
            Build(sb);
            if (sb.Length > 0)
            {
                sb.Insert(0, "akka.remote {\n");
                sb.AppendLine("}");
                builder.AddHocon(sb.ToString(), HoconAddMode.Prepend);
            }
        }
        
        protected abstract void Build(StringBuilder builder);
    }
    
    public sealed class SslOptions
    {
        public bool? SuppressValidation { get; set; }
        public X509Certificate2? X509Certificate { get; set; }
        public SslCertificateOptions CertificateOptions { get; set; } = new ();

        internal void Build(StringBuilder builder)
        {
            var sb = new StringBuilder();
            
            if (SuppressValidation is not null)
                sb.AppendLine($"suppress-validation = {SuppressValidation.ToHocon()}");
            
            CertificateOptions.Build(sb);
            
            if(sb.Length == 0)
                return;
            
            sb.Insert(0, "ssl {");
            sb.AppendLine("}");
            builder.Append(sb);
        }
    }

    public sealed class SslCertificateOptions
    {
        public string? Path { get; set; }
        public string? Password { get; set; }
        public bool? UseThumbprintOverFile { get; set; }
        public string? Thumbprint { get; set; }
        public string? StoreName { get; set; }
        public string? StoreLocation { get; set; }
        
        internal void Build(StringBuilder builder)
        {
            var sb = new StringBuilder();
            
            if (!string.IsNullOrEmpty(Path))
                sb.AppendLine($"path = {Path.ToHocon()}");
            
            if (!string.IsNullOrEmpty(Password))
                sb.AppendLine($"password = {Password.ToHocon()}");
            
            if (UseThumbprintOverFile is not null)
                sb.AppendLine($"use-thumbprint-over-file = {UseThumbprintOverFile.ToHocon()}");
            
            if (!string.IsNullOrEmpty(Thumbprint))
                sb.AppendLine($"thumbprint = {Thumbprint.ToHocon()}");
            
            if (!string.IsNullOrEmpty(StoreName))
                sb.AppendLine($"store-name = {StoreName.ToHocon()}");
            
            if (!string.IsNullOrEmpty(StoreLocation))
                sb.AppendLine($"store-location = {StoreLocation.ToHocon()}");
            
            if (sb.Length == 0) 
                return;
            
            sb.Insert(0, "certificate {\n");
            sb.AppendLine("}");
            builder.Append(sb);
        }
    }
}