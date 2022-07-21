// -----------------------------------------------------------------------
//  <copyright file="LoggingExtensions.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using System;
using Akka.Event;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Akka.Hosting.Logging
{
    public static class LoggingExtensions
    {
        /// <summary>
        /// Fluent interface to configure the Akka.NET logger system
        /// </summary>
        /// <param name="builder">The <see cref="AkkaConfigurationBuilder"/> being configured</param>
        /// <param name="configurator">An action that can be used to modify the logging configuration</param>
        /// <returns>The original <see cref="AkkaConfigurationBuilder"/> instance</returns>
        public static AkkaConfigurationBuilder ConfigureLoggers(this AkkaConfigurationBuilder builder, Action<LoggerSetup> configurator)
        {
            var setup = new LoggerSetup(builder);
            configurator(setup);
            return builder.AddHoconConfiguration(setup.ToConfig(), HoconAddMode.Prepend);
        }
        
        /// <summary>
        /// Add the default Akka.NET logger that sinks all log events to the console
        /// </summary>
        /// <param name="setup">The <see cref="LoggerSetup"/> instance </param>
        /// <returns>the original <see cref="LoggerSetup"/> used to configure the logger system</returns>
        public static LoggerSetup AddDefaultLogger(this LoggerSetup setup)
        {
            setup.AddLogger<DefaultLogger>();
            return setup;
        }
        
        /// <summary>
        /// Add the <see cref="ILoggerFactory"/> logger that sinks all log events to the default
        /// <see cref="ILoggerFactory"/> instance registered in the host <see cref="ServiceProvider"/>
        /// </summary>
        /// <param name="setup">The <see cref="LoggerSetup"/> instance </param>
        /// <returns>the original <see cref="LoggerSetup"/> used to configure the logger system</returns>
        public static LoggerSetup AddLoggerFactory(this LoggerSetup setup)
        {
            setup.AddLogger(typeof(LoggerFactoryLogger));
            return setup;
        }
        
        /// <summary>
        /// Add the <see cref="ILoggerFactory"/> logger that sinks all log events to the provided <see cref="ILoggerFactory"/>
        /// </summary>
        /// <param name="setup">The <see cref="LoggerSetup"/> instance </param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> instance to be used as the log sink</param>
        /// <returns>the original <see cref="LoggerSetup"/> used to configure the logger system</returns>
        public static LoggerSetup AddLoggerFactory(this LoggerSetup setup, ILoggerFactory loggerFactory)
        {
            var builder = setup.Builder;
            builder.AddSetup(new LoggerFactorySetup(loggerFactory));
            setup.AddLogger(typeof(LoggerFactoryLogger));
            return setup;
        }
        
    }
}