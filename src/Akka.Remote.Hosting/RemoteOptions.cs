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
using Akka.Remote.Transport.DotNetty;

namespace Akka.Remote.Hosting
{
    public class RemoteOptions
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
        
        /// <summary>
        /// If this value is set, this becomes the public address for the actor system on this
        /// transport, which might be different than the physical ip address (hostname).
        /// This is designed to make it easy to support private / public addressing schemes
        /// </summary>
        public string? PublicHostName { get; set; }
        
        /// <summary>
        /// Similar to <see cref="PublicHostName"/>, this allows Akka.Remote users
        /// to alias the port they're listening on. The socket will actually listen on the
        /// <see cref="Port"/> property, but when connecting to other ActorSystems this node will advertise
        /// itself as being connected to the "public-port". This is helpful when working with 
        /// hosting environments that rely on address translation and port-forwarding, such as Docker.
        /// </summary>
        public int? PublicPort { get; set; }

        public bool? EnableSsl { get; set; }
    
        public SslOptions Ssl { get; set; } = new ();
        
        internal void Build(AkkaConfigurationBuilder builder)
        {
            var sb = new StringBuilder();
            Build(sb);

            if (sb.Length > 0)
                builder.AddHocon(sb.ToString(), HoconAddMode.Prepend);
            
            if (EnableSsl is false || Ssl.X509Certificate == null) 
                return;
        
            var suppressValidation = Ssl.SuppressValidation ?? false;
            builder.AddSetup(new DotNettySslSetup(Ssl.X509Certificate, suppressValidation));
        }
    
        private void Build(StringBuilder builder)
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
        
            sb.Insert(0, "akka.remote.dot-netty.tcp {\n");
            sb.Append("}");
            builder.Append(sb);
        }
        
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