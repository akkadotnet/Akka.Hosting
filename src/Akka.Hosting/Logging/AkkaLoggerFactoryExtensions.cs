// -----------------------------------------------------------------------
//  <copyright file="AkkaLoggerFactoryExtensions.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using Akka.Configuration;
using Akka.Event;
using Microsoft.Extensions.Logging;

namespace Akka.Hosting.Logging
{
    public static class AkkaLoggerFactoryExtensions
    {
        public static AkkaConfigurationBuilder WithLoggerFactory(this AkkaConfigurationBuilder builder)
        {
            return builder.AddHocon("akka.loggers = [\"Akka.Hosting.Logging.LoggerFactoryLogger, Akka.Hosting\"]");
        }
        
        public static AkkaConfigurationBuilder AddLoggerFactory(this AkkaConfigurationBuilder builder)
        {
            var loggers = builder.Configuration.HasValue
                ? builder.Configuration.Value.GetStringList("akka.loggers")
                : new List<string>();
            
            if(loggers.Count == 0)
                loggers.Add("Akka.Event.DefaultLogger");
            
            loggers.Add("Akka.Hosting.Logging.LoggerFactoryLogger, Akka.Hosting");
            return builder.AddHocon($"akka.loggers = [{string.Join(", ", loggers.Select(s => $"\"{s}\""))}]");
        }
        
    }
}