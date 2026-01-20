// -----------------------------------------------------------------------
//  <copyright file="LoggerFactoryLogger.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Akka.Actor;
using Akka.Configuration;
using Akka.Dispatch;
using Akka.Event;
using Microsoft.Extensions.Logging;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Akka.Hosting.Logging
{
    public class LoggerFactoryLogger: ActorBase, IRequiresMessageQueue<ILoggerMessageQueueSemantics>
    {
        /// <summary>
        /// only used when we're shutting down / spinning up
        /// </summary>
        protected readonly ILoggingAdapter InternalLogger = Akka.Event.Logging.GetLogger(Context.System.EventStream, nameof(LoggerFactoryLogger));
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<ActorSystem> _akkaLogger;

        public LoggerFactoryLogger()
        {
            var setup = Context.System.Settings.Setup.Get<LoggerFactorySetup>();
            if (!setup.HasValue) 
                throw new ConfigurationException(
                    $"Could not start {nameof(LoggerFactoryLogger)}, the required setup class " +
                    $"{nameof(LoggerFactorySetup)} could not be found. Have you added this to the ActorSystem setup?");
            _loggerFactory = setup.Value.LoggerFactory;
            _akkaLogger = _loggerFactory.CreateLogger<ActorSystem>();
        }

        protected override void PostStop()
        {
            InternalLogger.Info($"{nameof(LoggerFactoryLogger)} stopped");
        }

        protected override bool Receive(object message)
        {
            switch (message)
            { 
                case InitializeLogger _:
                    InternalLogger.Info($"{nameof(LoggerFactoryLogger)} started");
                    Sender.Tell(new LoggerInitialized());
                    return true;
                
                case LogEvent logEvent:
                    Log(logEvent, Sender.Path);
                    return true;
                
                default:
                    return false;
            }
        }
        
        protected virtual void Log(LogEvent log, ActorPath path)
        {
            var logLevel = GetLogLevel(log.LogLevel());

            // Create state dictionary with structured properties from the log message
            var state = new Dictionary<string, object>();

            // Use semantic logging to extract structured properties
            if (log.TryGetProperties(out var properties) && properties is not null)
            {
                // Add structured properties from the message template
                foreach (var prop in properties)
                {
                    state[prop.Key] = prop.Value;
                }
            }
            
            // Add Akka metadata properties
            state["ActorPath"] = path.ToString();
            state["Timestamp"] = log.Timestamp;
            state["Thread"] = log.Thread.ManagedThreadId;
            state["LogSource"] = log.LogSource;

            // Add {OriginalFormat} key per MEL convention for structured logging
            // This allows MEL sinks to recognize and preserve the message template
            state["{OriginalFormat}"] = log.GetTemplate();

            // Log with structured state
            // Use LogMessage.ToString() which applies the correct formatter (SemanticLogMessageFormatter)
            // instead of string.Format() which only works with positional {0} placeholders
            _akkaLogger.Log(logLevel, new EventId(), state, log.Cause,
                (s, ex) => log.Message is LogMessage logMsg ? logMsg.ToString() ?? string.Empty : log.Message?.ToString() ?? string.Empty);
        }

        private static LogLevel GetLogLevel(Event.LogLevel level)
        {
            return level switch
            {
                Event.LogLevel.DebugLevel => LogLevel.Debug,
                Event.LogLevel.InfoLevel => LogLevel.Information,
                Event.LogLevel.WarningLevel => LogLevel.Warning,
                Event.LogLevel.ErrorLevel => LogLevel.Error,
                _ => LogLevel.Error
            };
        }
    }
}