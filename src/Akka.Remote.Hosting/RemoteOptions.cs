// -----------------------------------------------------------------------
//  <copyright file="AkkaRemoteOptions.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Text;

namespace Akka.Remote.Hosting
{
    public sealed class RemoteOptions
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

        public override string ToString()
        {
            var sb = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(HostName))
                sb.AppendFormat("hostname = {0}\n", HostName);
            if (Port != null)
                sb.AppendFormat("port = {0}\n", Port);
            if(!string.IsNullOrWhiteSpace(PublicHostName))
                sb.AppendFormat("public-hostname = {0}\n", PublicHostName);
            if(PublicPort != null)
                sb.AppendFormat("public-port = {0}\n", PublicPort);

            if (sb.Length == 0) 
                return string.Empty;
            
            sb.Insert(0, "akka.remote.dot-netty.tcp {\n");
            sb.Append("}");
            return sb.ToString();
        }
    }
}