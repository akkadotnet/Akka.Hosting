﻿//-----------------------------------------------------------------------
// <copyright file="EventFilterTestBase.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2021 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2021 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System.Threading.Tasks;
using Akka.Configuration;
using Akka.Event;

namespace Akka.Hosting.TestKit.Tests.TestEventListenerTests
{
    public abstract class EventFilterTestBase : TestKit
    {
        /// <summary>
        /// Used to signal that the test was successful and that we should ensure no more messages were logged
        /// </summary>
        protected bool TestSuccessful;

        protected EventFilterTestBase(string config)
        {
            Config = $"akka.loggers = [\"{typeof(ForwardAllEventsTestEventListener).AssemblyQualifiedName}\"], {(string.IsNullOrEmpty(config) ? "" : config)}";
        }

        protected override Config Config { get; }

        protected abstract void SendRawLogEventMessage(object message);

        protected override async Task BeforeTestStart()
        {
            await base.BeforeTestStart();
            
            //We send a ForwardAllEventsTo containing message to the TestEventListenerToForwarder logger (configured as a logger above).
            //It should respond with an "OK" message when it has received the message.
            var initLoggerMessage = new ForwardAllEventsTestEventListener.ForwardAllEventsTo(TestActor);
            // ReSharper disable once DoNotCallOverridableMethodsInConstructor
            SendRawLogEventMessage(initLoggerMessage);
            ExpectMsg("OK");
            //From now on we know that all messages will be forwarded to TestActor
        }

        protected override async Task AfterAllAsync()
        {
            //After every test we make sure no uncatched messages have been logged
            if(TestSuccessful)
            {
                EnsureNoMoreLoggedMessages();
            }
            await base.AfterAllAsync();
        }

        private void EnsureNoMoreLoggedMessages()
        {
            //We log a Finished message. When it arrives to TestActor we know no other message has been logged.
            //If we receive something else it means another message was logged, and ExpectMsg will fail
            const string message = "<<Finished>>";
            SendRawLogEventMessage(message);
            ExpectMsg<LogEvent>(err => (string) err.Message == message,hint: "message to be \"" + message + "\"");
        }

    }
}

