// -----------------------------------------------------------------------
//  <copyright file="AkkaLoggerFactoryExtensions.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using System;
using Akka.Actor;
using Akka.Event;
using Microsoft.Extensions.Logging;

namespace Akka.Hosting.Logging
{
    public static class AkkaLoggerFactoryExtensions
    {
        public static AkkaConfigurationBuilder WithLoggerFactoryLogger(
            this AkkaConfigurationBuilder builder,
            ILoggerFactory loggerFactory,
            string timestampFormat = LoggerFactoryLogger.DefaultTimeStampFormat)
        {
            return builder.WithActors((system, registry) =>
            {
                var extSystem = (ExtendedActorSystem)system;
                var logger = extSystem.SystemActorOf(
                    props: Props.Create(() => new LoggerFactoryLogger(loggerFactory, timestampFormat)), 
                    name: "log-ILoggerLogger");
                logger.Ask<LoggerInitialized>(new InitializeLogger(system.EventStream), TimeSpan.FromSeconds(5));
            });
        }
    }
}