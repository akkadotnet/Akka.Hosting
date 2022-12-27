﻿// -----------------------------------------------------------------------
//  <copyright file="LoggerConfigBuilder.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Akka.Configuration;
using Akka.Dispatch;
using Akka.Event;

namespace Akka.Hosting
{
    public sealed class LoggerConfigBuilder
    {
        private readonly List<Type> _loggers = new List<Type> { typeof(DefaultLogger) };
        internal AkkaConfigurationBuilder Builder { get; }

        internal LoggerConfigBuilder(AkkaConfigurationBuilder builder)
        {
            Builder = builder;
        }

        /// <summary>
        /// <para>
        /// Log level used by the configured loggers.
        /// </para>
        /// Defaults to <c>LogLevel.InfoLevel</c>
        /// </summary>
        public LogLevel LogLevel { get; set; } = LogLevel.InfoLevel;
        
        /// <summary>
        /// <para>
        /// Log the complete configuration at INFO level when the actor system is started.
        /// This is useful when you are uncertain of what configuration is being used by the ActorSystem.
        /// </para>
        /// Defaults to false.
        /// </summary>
        public bool LogConfigOnStart { get; set; } = false;

        public DeadLetterOptions? DeadLetterOptions { get; set; }

        public DebugOptions? DebugOptions { get; set; }

        /// <summary>
        /// Clear all loggers currently registered.
        /// </summary>
        /// <returns>This <see cref="LoggerConfigBuilder"/> instance</returns>
        public LoggerConfigBuilder ClearLoggers()
        {
            _loggers.Clear();
            return this;
        }

        /// <summary>
        /// Register a logger
        /// </summary>
        /// <returns>This <see cref="LoggerConfigBuilder"/> instance</returns>
        public LoggerConfigBuilder AddLogger<T>() where T: IRequiresMessageQueue<ILoggerMessageQueueSemantics>
        {
            var logger = typeof(T);
            _loggers.Add(logger);
            return this;
        }

        /// <summary>
        /// INTERNAL API
        ///
        /// Used by logger extensions that needed to perform specific tasks before registering a logger type,
        /// such as setting up a Setup object with the builder
        /// </summary>
        /// <param name="logger">The logger <see cref="Type"/></param>
        internal void AddLogger(Type logger)
        {
            _loggers.Add(logger);
        }
        
        internal Config ToConfig()
        {
            var sb = new StringBuilder()
                .Append("akka.loglevel=").AppendLine(ParseLogLevel(LogLevel))
                .Append("akka.loggers=[").Append(string.Join(",", _loggers.Select(t => $"\"{t.AssemblyQualifiedName}\""))).AppendLine("]")
                .Append("akka.log-config-on-start=").AppendLine(LogConfigOnStart ? "true" : "false");
            if (DebugOptions is { })
                sb.AppendLine(DebugOptions.ToString());
            if (DeadLetterOptions is { })
                sb.AppendLine(DeadLetterOptions.ToString());
            
            return ConfigurationFactory.ParseString(sb.ToString());
        }

        private string ParseLogLevel(LogLevel logLevel)
            => logLevel switch
            {
                LogLevel.DebugLevel => "Debug",
                LogLevel.InfoLevel => "Info",
                LogLevel.WarningLevel => "Warning",
                LogLevel.ErrorLevel => "Error",
                _ => throw new ConfigurationException($"Unknown {nameof(LogLevel)} enum value: {logLevel}")
            };
    }
}