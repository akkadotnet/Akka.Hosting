// -----------------------------------------------------------------------
//  <copyright file="AkkaRemoteOptions.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using System;
using System.Text;
using Akka.Configuration;

namespace Akka.Remote.Hosting
{
    public sealed class RemoteOptions
    {
        public string? HostName { get; set; }
        public int? Port { get; set; }
        public string? PublicHostName { get; set; }
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