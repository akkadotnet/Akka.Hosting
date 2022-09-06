// -----------------------------------------------------------------------
//  <copyright file="HostingSpec_ExpectMsgFrom.cs" company="Akka.NET Project">
//      Copyright (C) 2009-2022 Lightbend Inc. <http://www.lightbend.com>
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;

namespace Akka.Hosting.TestKit
{
    public abstract partial class HostingSpec
    {
        /// <summary>
        /// Receive one message from the test actor and assert that it is of the specified type
        /// and was sent by the specified sender
        /// Wait time is bounded by the given duration if specified.
        /// If not specified, wait time is bounded by remaining time for execution of the innermost enclosing 'within'
        /// block, if inside a 'within' block; otherwise by the config value 
        /// "akka.test.single-expect-default".
        /// </summary>
        /// <typeparam name="T">TBD</typeparam>
        /// <param name="sender">TBD</param>
        /// <param name="duration">TBD</param>
        /// <param name="hint">TBD</param>
        /// <param name="cancellationToken"></param>
        /// <returns>TBD</returns>
        public T ExpectMsgFrom<T>(
            IActorRef sender,
            TimeSpan? duration = null,
            string hint = null,
            CancellationToken cancellationToken = default)
            => TestKit.ExpectMsgFrom<T>(sender, duration, hint, cancellationToken);

        public ValueTask<T> ExpectMsgFromAsync<T>(
            IActorRef sender,
            TimeSpan? duration = null,
            string hint = null,
            CancellationToken cancellationToken = default)
            => TestKit.ExpectMsgFromAsync<T>(sender, duration, hint, cancellationToken);

        /// <summary>
        /// Receive one message of the specified type from the test actor and assert that it
        /// equals the <paramref name="message"/> and was sent by the specified sender
        /// Wait time is bounded by the given duration if specified.
        /// If not specified, wait time is bounded by remaining time for execution of the innermost enclosing 'within'
        /// block, if inside a 'within' block; otherwise by the config value 
        /// "akka.test.single-expect-default".
        /// </summary>
        /// <typeparam name="T">TBD</typeparam>
        /// <param name="sender">TBD</param>
        /// <param name="message">TBD</param>
        /// <param name="timeout">TBD</param>
        /// <param name="hint">TBD</param>
        /// <param name="cancellationToken"></param>
        /// <returns>TBD</returns>
        public T ExpectMsgFrom<T>(
            IActorRef sender,
            T message,
            TimeSpan? timeout = null,
            string hint = null,
            CancellationToken cancellationToken = default)
            => TestKit.ExpectMsgFrom(sender, message, timeout, hint, cancellationToken);

        public ValueTask<T> ExpectMsgFromAsync<T>(
            IActorRef sender,
            T message,
            TimeSpan? timeout = null,
            string hint = null,
            CancellationToken cancellationToken = default)
            => TestKit.ExpectMsgFromAsync(sender, message, timeout, hint, cancellationToken);

        /// <summary>
        /// Receive one message of the specified type from the test actor and assert that the given
        /// predicate accepts it and was sent by the specified sender
        /// Wait time is bounded by the given duration if specified.
        /// If not specified, wait time is bounded by remaining time for execution of the innermost enclosing 'within'
        /// block, if inside a 'within' block; otherwise by the config value 
        /// "akka.test.single-expect-default".
        /// Use this variant to implement more complicated or conditional processing.
        /// </summary>
        /// <typeparam name="T">TBD</typeparam>
        /// <param name="sender">TBD</param>
        /// <param name="isMessage">TBD</param>
        /// <param name="timeout">TBD</param>
        /// <param name="hint">TBD</param>
        /// <param name="cancellationToken"></param>
        /// <returns>TBD</returns>
        public T ExpectMsgFrom<T>(
            IActorRef sender,
            Predicate<T> isMessage,
            TimeSpan? timeout = null,
            string hint = null,
            CancellationToken cancellationToken = default)
            => TestKit.ExpectMsgFrom(sender, isMessage, timeout, hint, cancellationToken);

        public ValueTask<T> ExpectMsgFromAsync<T>(
            IActorRef sender,
            Predicate<T> isMessage,
            TimeSpan? timeout = null,
            string hint = null,
            CancellationToken cancellationToken = default)
            => TestKit.ExpectMsgFromAsync(sender, isMessage, timeout, hint, cancellationToken);

        /// <summary>
        /// Receive one message of the specified type from the test actor and assert that the given
        /// predicate accepts it and was sent by a sender that matches the <paramref name="isSender"/> predicate.
        /// Wait time is bounded by the given duration if specified.
        /// If not specified, wait time is bounded by remaining time for execution of the innermost enclosing 'within'
        /// block, if inside a 'within' block; otherwise by the config value 
        /// "akka.test.single-expect-default".
        /// Use this variant to implement more complicated or conditional processing.
        /// </summary>
        /// <typeparam name="T">TBD</typeparam>
        /// <param name="isSender">TBD</param>
        /// <param name="isMessage">TBD</param>
        /// <param name="timeout">TBD</param>
        /// <param name="hint">TBD</param>
        /// <param name="cancellationToken"></param>
        /// <returns>TBD</returns>
        public T ExpectMsgFrom<T>(
            Predicate<IActorRef> isSender, 
            Predicate<T> isMessage,
            TimeSpan? timeout = null,
            string hint = null,
            CancellationToken cancellationToken = default)
            => TestKit.ExpectMsgFrom(isSender, isMessage, timeout, hint, cancellationToken);

        public ValueTask<T> ExpectMsgFromAsync<T>(
            Predicate<IActorRef> isSender,
            Predicate<T> isMessage,
            TimeSpan? timeout = null,
            string hint = null,
            CancellationToken cancellationToken = default)
            => TestKit.ExpectMsgFromAsync(isSender, isMessage, timeout, hint, cancellationToken);

        /// <summary>
        /// Receive one message of the specified type from the test actor, verifies that the sender is the specified
        /// and calls the action that performs extra assertions.
        /// Wait time is bounded by the given duration if specified.
        /// If not specified, wait time is bounded by remaining time for execution of the innermost enclosing 'within'
        /// block, if inside a 'within' block; otherwise by the config value 
        /// "akka.test.single-expect-default".
        /// Use this variant to implement more complicated or conditional processing.
        /// </summary>
        /// <typeparam name="T">TBD</typeparam>
        /// <param name="sender">TBD</param>
        /// <param name="assertMessage">TBD</param>
        /// <param name="timeout">TBD</param>
        /// <param name="hint">TBD</param>
        /// <param name="cancellationToken"></param>
        /// <returns>TBD</returns>
        public T ExpectMsgFrom<T>(
            IActorRef sender,
            Action<T> assertMessage,
            TimeSpan? timeout = null,
            string hint = null,
            CancellationToken cancellationToken = default)
            => TestKit.ExpectMsgFrom(sender, assertMessage, timeout, hint, cancellationToken);

        public ValueTask<T> ExpectMsgFromAsync<T>(
            IActorRef sender,
            Action<T> assertMessage,
            TimeSpan? timeout = null,
            string hint = null,
            CancellationToken cancellationToken = default)
            => TestKit.ExpectMsgFromAsync(sender, assertMessage, timeout, hint, cancellationToken);

        /// <summary>
        /// Receive one message of the specified type from the test actor and calls the 
        /// action that performs extra assertions.
        /// Wait time is bounded by the given duration if specified.
        /// If not specified, wait time is bounded by remaining time for execution of the innermost enclosing 'within'
        /// block, if inside a 'within' block; otherwise by the config value 
        /// "akka.test.single-expect-default".
        /// Use this variant to implement more complicated or conditional processing.
        /// </summary>
        /// <typeparam name="T">TBD</typeparam>
        /// <param name="assertSender">TBD</param>
        /// <param name="assertMessage">TBD</param>
        /// <param name="timeout">TBD</param>
        /// <param name="hint">TBD</param>
        /// <param name="cancellationToken"></param>
        /// <returns>TBD</returns>
        public T ExpectMsgFrom<T>(
            Action<IActorRef> assertSender, 
            Action<T> assertMessage,
            TimeSpan? timeout = null,
            string hint = null,
            CancellationToken cancellationToken = default)
            => TestKit.ExpectMsgFrom(assertSender, assertMessage, timeout, hint, cancellationToken);
        
        public ValueTask<T> ExpectMsgFromAsync<T>(
            Action<IActorRef> assertSender, 
            Action<T> assertMessage,
            TimeSpan? timeout = null,
            string hint = null,
            CancellationToken cancellationToken = default)
            => TestKit.ExpectMsgFromAsync(assertSender, assertMessage, timeout, hint, cancellationToken);
    }
}