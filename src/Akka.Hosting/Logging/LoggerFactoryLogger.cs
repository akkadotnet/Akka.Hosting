// -----------------------------------------------------------------------
//  <copyright file="LoggerFactoryLogger.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
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

            // Try to get ActivityContext for OpenTelemetry trace correlation
            // This captures trace context that was active when the log was created,
            // solving the problem that Activity.Current doesn't flow across mailbox boundaries
            var activityContext = TryGetActivityContext(log);

            // Use semantic logging to extract structured properties
            if (log.TryGetProperties(out var properties) && properties is not null)
            {
                var formattedMessage = FormatMessage(log.GetTemplate(), log.GetParameters().ToArray());

                // Use AkkaLogState to include trace context with structured properties
                var state = new AkkaLogState(
                    activityContext,
                    properties,
                    path.ToString(),
                    log.Timestamp,
                    log.Thread.ManagedThreadId,
                    log.LogSource,
                    log.GetTemplate(),
                    formattedMessage);

                // Log with structured state including trace context
                _akkaLogger.Log(logLevel, new EventId(), state, log.Cause,
                    (s, ex) => s.ToString());
            }
            else
            {
                // Fallback for non-structured messages (plain strings)
                // Still include trace context if available
                if (activityContext.TraceId != default)
                {
                    var state = new AkkaLogState(activityContext, log.ToString());
                    _akkaLogger.Log(logLevel, new EventId(), state, log.Cause,
                        (s, ex) => s.ToString());
                }
                else
                {
                    _akkaLogger.Log<LogEvent>(logLevel, new EventId(), log, log.Cause,
                        (@event, exception) => @event.ToString());
                }
            }
        }

        /// <summary>
        /// Attempts to extract the ActivityContext from a LogEvent.
        /// Uses reflection to maintain compatibility with older Akka.NET versions
        /// that don't have the ActivityContext property.
        /// </summary>
        private static ActivityContext TryGetActivityContext(LogEvent log)
        {
            // Try to get ActivityContext via the property added in Akka.NET 1.5.59
            // Use reflection for backwards compatibility with older versions
            try
            {
                var activityContextProperty = log.GetType().GetProperty("ActivityContext");
                if (activityContextProperty != null)
                {
                    var value = activityContextProperty.GetValue(log);
                    if (value is ActivityContext context)
                    {
                        return context;
                    }
                }
            }
            catch
            {
                // Ignore reflection errors - just return default
            }

            return default;
        }

        private static string FormatMessage(string template, object[] args)
        {
            try
            {
                return args.Length == 0 ? template : string.Format(template, args);
            }
            catch
            {
                // If formatting fails, return the template as-is
                return template;
            }
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