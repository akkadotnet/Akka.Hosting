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
        public const string DefaultTimeStampFormat = "yy/MM/dd-HH:mm:ss.ffff";
        private const string DefaultMessageFormat = "[{{Timestamp:{0}}}][{{LogSource}}][{{ActorPath}}][{{Thread:0000}}]: {{Message}}";
        private static readonly Event.LogLevel[] AllLogLevels = Enum.GetValues(typeof(Event.LogLevel)).Cast<Event.LogLevel>().ToArray();
        
        /// <summary>
        /// only used when we're shutting down / spinning up
        /// </summary>
        private readonly ILoggingAdapter _internalLogger = Akka.Event.Logging.GetLogger(Context.System.EventStream, nameof(LoggerFactoryLogger));
        private readonly ILoggerFactory _loggerFactory;
        private ILogger<ActorSystem> _akkaLogger;
        private readonly string _messageFormat;

        public LoggerFactoryLogger()
        {
            _messageFormat = string.Format(DefaultMessageFormat, DefaultTimeStampFormat);
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
            _internalLogger.Info($"{nameof(LoggerFactoryLogger)} stopped");
        }

        protected override bool Receive(object message)
        {
            switch (message)
            { 
                case InitializeLogger _:
                    _internalLogger.Info($"{nameof(LoggerFactoryLogger)} started");
                    Sender.Tell(new LoggerInitialized());
                    return true;
                
                case LogEvent logEvent:
                    Log(logEvent, Sender.Path);
                    return true;
                
                default:
                    return false;
            }
        }
        
        private void Log(LogEvent log, ActorPath path)
        {
            var message = GetMessage(log.Message);
            _akkaLogger.Log(GetLogLevel(log.LogLevel()), log.Cause, _messageFormat, GetArgs(log, path, message));
        }

        private static object[] GetArgs(LogEvent log, ActorPath path, object message)
            => new []{ log.Timestamp, log.LogSource, path, log.Thread.ManagedThreadId, message };

        private static object GetMessage(object obj)
        {
            try
            {
                return obj is LogMessage m ? string.Format(m.Format, m.Args) : obj;
            }
            catch (Exception ex)
            {
                // Formatting/ToString error handling
                var sb = new StringBuilder("Exception while recording log: ")
                    .Append(ex.Message)
                    .Append(' ');
                switch (obj)
                {
                    case LogMessage msg:
                        var args = msg.Args.Select(o =>
                        {
                            try
                            {
                                return o.ToString();
                            }
                            catch(Exception e)
                            {
                                return $"{o.GetType()}.ToString() throws {e.GetType()}: {e.Message}";
                            }
                        });
                        sb.Append($"Format: [{msg.Format}], Args: [{string.Join(",", args)}].");
                        break;
                    case string str:
                        sb.Append($"Message: [{str}].");
                        break;
                    default:
                        sb.Append($"Failed to invoke {obj.GetType()}.ToString().");
                        break;
                }

                sb.AppendLine(" Please take a look at the logging call where this occurred and fix your format string.");
                sb.Append(ex);
                return sb.ToString();
            }
        }

        private static LogLevel GetLogLevel(Event.LogLevel level)
        {
            switch (level)
            {
                case Event.LogLevel.DebugLevel:
                    return LogLevel.Debug;
                case Event.LogLevel.InfoLevel:
                    return LogLevel.Information;
                case Event.LogLevel.WarningLevel:
                    return LogLevel.Warning;
                case Event.LogLevel.ErrorLevel:
                    return LogLevel.Error;
                default:
                    // Should never reach this code path
                    return LogLevel.Error;
            }
        }
    }
}