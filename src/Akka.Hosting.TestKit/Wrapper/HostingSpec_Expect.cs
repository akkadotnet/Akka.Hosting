// -----------------------------------------------------------------------
//  <copyright file="HostingSpec_Expect.cs" company="Akka.NET Project">
//      Copyright (C) 2009-2022 Lightbend Inc. <http://www.lightbend.com>
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.TestKit;
using Akka.TestKit.Internal;
using Akka.Util;

namespace Akka.Hosting.TestKit
{
    public abstract partial class HostingSpec
    {
        /// <summary>
        /// Receive one message from the test actor and assert that it is of the specified type.
        /// Wait time is bounded by the given duration, if specified; otherwise
        /// wait time is bounded by remaining time for execution of the innermost enclosing 'within'
        /// block, if inside a 'within' block; otherwise by the config value 
        /// "akka.test.single-expect-default".
        /// </summary>
        /// <typeparam name="T">TBD</typeparam>
        /// <param name="duration">TBD</param>
        /// <param name="hint">TBD</param>
        /// <param name="cancellationToken"></param>
        /// <returns>TBD</returns>
        public T ExpectMsg<T>(
            TimeSpan? duration = null,
            string hint = null,
            CancellationToken cancellationToken = default)
            => TestKit.ExpectMsg<T>(duration, hint, cancellationToken);
        
        /// <inheritdoc cref="ExpectMsg{T}(TimeSpan?, string, CancellationToken)"/>
        public ValueTask<T> ExpectMsgAsync<T>(
            TimeSpan? duration = null, 
            string hint = null,
            CancellationToken cancellationToken = default)
            => TestKit.ExpectMsgAsync<T>(duration, hint, cancellationToken);

        /// <summary>
        /// Receive one message of the specified type from the test actor and assert that it
        /// equals the <paramref name="message"/>.
        /// Wait time is bounded by the given duration, if specified; otherwise
        /// wait time is bounded by remaining time for execution of the innermost enclosing 'within'
        /// block, if inside a 'within' block; otherwise by the config value 
        /// "akka.test.single-expect-default".
        /// </summary>
        /// <typeparam name="T">TBD</typeparam>
        /// <param name="message">TBD</param>
        /// <param name="timeout">TBD</param>
        /// <param name="hint">TBD</param>
        /// <param name="cancellationToken"></param>
        /// <returns>TBD</returns>
        public T ExpectMsg<T>(
            T message,
            TimeSpan? timeout = null,
            string hint = null,
            CancellationToken cancellationToken = default)
            => TestKit.ExpectMsg(message, timeout, hint, cancellationToken);

        /// <inheritdoc cref="ExpectMsg{T}(T, TimeSpan?, string, CancellationToken)"/>
        public ValueTask<T> ExpectMsgAsync<T>(
            T message,
            TimeSpan? timeout = null,
            string hint = null,
            CancellationToken cancellationToken = default)
            => TestKit.ExpectMsgAsync(message, timeout, hint, cancellationToken);

        /// <summary>
        /// Receive one message of the specified type from the test actor and assert that the given
        /// predicate accepts it.
        /// Use this variant to implement more complicated or conditional processing.
        /// 
        /// Wait time is bounded by the given duration, if specified; otherwise
        /// wait time is bounded by remaining time for execution of the innermost enclosing 'within'
        /// block, if inside a 'within' block; otherwise by the config value 
        /// "akka.test.single-expect-default".
        /// </summary>
        /// <typeparam name="T">TBD</typeparam>
        /// <param name="isMessage">TBD</param>
        /// <param name="timeout">TBD</param>
        /// <param name="hint">TBD</param>
        /// <param name="cancellationToken"></param>
        /// <returns>TBD</returns>
        public T ExpectMsg<T>(
            Predicate<T> isMessage,
            TimeSpan? timeout = null,
            string hint = null, 
            CancellationToken cancellationToken = default)
            => TestKit.ExpectMsg(isMessage, timeout, hint, cancellationToken);
        
        /// <inheritdoc cref="ExpectMsg{T}(Predicate{T}, TimeSpan?, string, CancellationToken)"/>
        public ValueTask<T> ExpectMsgAsync<T>(
            Predicate<T> isMessage,
            TimeSpan? timeout = null,
            string hint = null, 
            CancellationToken cancellationToken = default)
            => TestKit.ExpectMsgAsync(isMessage, timeout, hint, cancellationToken);


        /// <summary>
        /// Receive one message of the specified type from the test actor and calls the 
        /// action that performs extra assertions.
        /// Use this variant to implement more complicated or conditional processing.
        /// 
        /// Wait time is bounded by the given duration, if specified; otherwise
        /// wait time is bounded by remaining time for execution of the innermost enclosing 'within'
        /// block, if inside a 'within' block; otherwise by the config value 
        /// "akka.test.single-expect-default".
        /// </summary>
        /// <typeparam name="T">TBD</typeparam>
        /// <param name="assert">TBD</param>
        /// <param name="timeout">TBD</param>
        /// <param name="hint">TBD</param>
        /// <param name="cancellationToken"></param>
        /// <returns>TBD</returns>
        public T ExpectMsg<T>(
            Action<T> assert,
            TimeSpan? timeout = null,
            string hint = null,
            CancellationToken cancellationToken = default)
            => TestKit.ExpectMsg(assert, timeout, hint, cancellationToken);

        /// <inheritdoc cref="ExpectMsg{T}(Action{T}, TimeSpan?, string, CancellationToken)"/>
        public ValueTask<T> ExpectMsgAsync<T>(
            Action<T> assert,
            TimeSpan? timeout = null,
            string hint = null,
            CancellationToken cancellationToken = default)
            => TestKit.ExpectMsgAsync(assert, timeout, hint, cancellationToken);

        /// <summary>
        /// Receive one message of the specified type from the test actor and assert that the given
        /// predicate accepts it.
        /// Use this variant to implement more complicated or conditional processing.
        /// 
        /// Wait time is bounded by the given duration, if specified; otherwise
        /// wait time is bounded by remaining time for execution of the innermost enclosing 'within'
        /// block, if inside a 'within' block; otherwise by the config value 
        /// "akka.test.single-expect-default".
        /// </summary>
        /// <typeparam name="T">TBD</typeparam>
        /// <param name="isMessageAndSender">TBD</param>
        /// <param name="timeout">TBD</param>
        /// <param name="hint">TBD</param>
        /// <param name="cancellationToken"></param>
        /// <returns>TBD</returns>
        public T ExpectMsg<T>(
            Func<T, IActorRef, bool> isMessageAndSender, 
            TimeSpan? timeout = null,
            string hint = null,
            CancellationToken cancellationToken = default)
            => TestKit.ExpectMsg(isMessageAndSender, timeout, hint, cancellationToken);

        /// <inheritdoc cref="ExpectMsg{T}(Func{T, IActorRef, bool}, TimeSpan?, string, CancellationToken)"/>
        public ValueTask<T> ExpectMsgAsync<T>(
            Func<T, IActorRef, bool> isMessageAndSender, 
            TimeSpan? timeout = null,
            string hint = null,
            CancellationToken cancellationToken = default)
            => TestKit.ExpectMsgAsync(isMessageAndSender, timeout, hint, cancellationToken);

        /// <summary>
        /// Receive one message of the specified type from the test actor calls the 
        /// action that performs extra assertions.
        /// Use this variant to implement more complicated or conditional processing.
        /// 
        /// Wait time is bounded by the given duration, if specified; otherwise
        /// wait time is bounded by remaining time for execution of the innermost enclosing 'within'
        /// block, if inside a 'within' block; otherwise by the config value 
        /// "akka.test.single-expect-default".
        /// </summary>
        /// <typeparam name="T">TBD</typeparam>
        /// <param name="assertMessageAndSender">TBD</param>
        /// <param name="timeout">TBD</param>
        /// <param name="hint">TBD</param>
        /// <param name="cancellationToken"></param>
        /// <returns>TBD</returns>
        public T ExpectMsg<T>(
            Action<T, IActorRef> assertMessageAndSender, 
            TimeSpan? timeout = null,
            string hint = null,
            CancellationToken cancellationToken = default)
            => TestKit.ExpectMsg(assertMessageAndSender, timeout, hint, cancellationToken);
        
        /// <inheritdoc cref="ExpectMsg{T}(Action{T, IActorRef}, TimeSpan?, string, CancellationToken)"/>
        public ValueTask<T> ExpectMsgAsync<T>(
            Action<T, IActorRef> assertMessageAndSender,
            TimeSpan? timeout = null,
            string hint = null,
            CancellationToken cancellationToken = default)
            => TestKit.ExpectMsgAsync(assertMessageAndSender, timeout, hint, cancellationToken);


        /// <summary>
        /// Receive one message from the test actor and assert that it is equal to the expected value,
        /// according to the specified comparer function.
        /// 
        /// Wait time is bounded by remaining time for execution of the innermost enclosing 'within'
        /// block, if inside a 'within' block; otherwise by the config value 
        /// "akka.test.single-expect-default".
        /// </summary>
        /// <typeparam name="T">TBD</typeparam>
        /// <param name="expected">TBD</param>
        /// <param name="comparer">TBD</param>
        /// <param name="timeout">TBD</param>
        /// <param name="hint">TBD</param>
        /// <param name="cancellationToken"></param>
        /// <returns>TBD</returns>
        public T ExpectMsg<T>(
            T expected,
            Func<T, T, bool> comparer,
            TimeSpan? timeout = null,
            string hint = null,
            CancellationToken cancellationToken = default)
            => TestKit.ExpectMsg(expected, comparer, timeout, hint, cancellationToken);
        
        /// <inheritdoc cref="ExpectMsg{T}(T, Func{T, T, bool}, TimeSpan?, string, CancellationToken)"/>
        public ValueTask<T> ExpectMsgAsync<T>(
            T expected,
            Func<T, T, bool> comparer,
            TimeSpan? timeout = null,
            string hint = null,
            CancellationToken cancellationToken = default)
            => TestKit.ExpectMsgAsync(expected, comparer, timeout, hint, cancellationToken);

        /// <summary>
        /// Receive one message from the test actor and assert that it is the Terminated message of the given ActorRef.
        /// 
        /// Wait time is bounded by the given duration, if specified; otherwise
        /// wait time is bounded by remaining time for execution of the innermost enclosing 'within'
        /// block, if inside a 'within' block; otherwise by the config value 
        /// "akka.test.single-expect-default".
        /// </summary>
        /// <param name="target">TBD</param>
        /// <param name="timeout">TBD</param>
        /// <param name="hint">TBD</param>
        /// <param name="cancellationToken"></param>
        /// <returns>TBD</returns>
        public Terminated ExpectTerminated(
            IActorRef target,
            TimeSpan? timeout = null,
            string hint = null,
            CancellationToken cancellationToken = default)
            => TestKit.ExpectTerminated(target, timeout, hint, cancellationToken);
        
        /// <inheritdoc cref="ExpectTerminated(IActorRef, TimeSpan?, string, CancellationToken)"/>
        public ValueTask<Terminated> ExpectTerminatedAsync(
            IActorRef target,
            TimeSpan? timeout = null,
            string hint = null,
            CancellationToken cancellationToken = default)
            => TestKit.ExpectTerminatedAsync(target, timeout, hint, cancellationToken);

        /// <summary>
        /// Assert that no message is received.
        /// 
        /// Wait time is bounded by remaining time for execution of the innermost enclosing 'within'
        /// block, if inside a 'within' block; otherwise by the config value 
        /// "akka.test.single-expect-default".
        /// </summary>
        public void ExpectNoMsg(CancellationToken cancellationToken = default)
            => TestKit.ExpectNoMsg(cancellationToken);
        
        /// <inheritdoc cref="ExpectNoMsg(CancellationToken)"/>
        public ValueTask ExpectNoMsgAsync(CancellationToken cancellationToken = default)
            => TestKit.ExpectNoMsgAsync(cancellationToken);

        /// <summary>
        /// Assert that no message is received for the specified time.
        /// </summary>
        /// <param name="duration">TBD</param>
        /// <param name="cancellationToken"></param>
        public void ExpectNoMsg(TimeSpan duration, CancellationToken cancellationToken = default)
            => TestKit.ExpectNoMsg(duration, cancellationToken);
        
        /// <inheritdoc cref="ExpectNoMsg(TimeSpan, CancellationToken)"/>
        public ValueTask ExpectNoMsgAsync(TimeSpan duration, CancellationToken cancellationToken = default)
            => TestKit.ExpectNoMsgAsync(duration, cancellationToken);

        /// <summary>
        /// Assert that no message is received for the specified time in milliseconds.
        /// </summary>
        /// <param name="milliseconds">TBD</param>
        /// <param name="cancellationToken"></param>
        public void ExpectNoMsg(int milliseconds, CancellationToken cancellationToken = default)
            => TestKit.ExpectNoMsg(milliseconds, cancellationToken);
        
        /// <inheritdoc cref="ExpectNoMsg(int, CancellationToken)"/>
        public ValueTask ExpectNoMsgAsync(int milliseconds, CancellationToken cancellationToken = default)
            => TestKit.ExpectNoMsgAsync(milliseconds, cancellationToken);

        /// <summary>
        /// Receive a message from the test actor and assert that it equals 
        /// one of the given <paramref name="messages"/>. Wait time is bounded by 
        /// <see cref="RemainingOrDefault"/> as duration, with an assertion exception being thrown in case of timeout.
        /// </summary>
        /// <typeparam name="T">The type of the messages</typeparam>
        /// <param name="messages">The messages.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>The received messages in received order</returns>
        public T ExpectMsgAnyOf<T>(IEnumerable<T> messages, CancellationToken cancellationToken = default)
            => TestKit.ExpectMsgAnyOf(messages, cancellationToken);

        public ValueTask<T> ExpectMsgAnyOfAsync<T>(IEnumerable<T> messages, CancellationToken cancellationToken = default)
            => TestKit.ExpectMsgAnyOfAsync(messages, cancellationToken);

        /// <summary>
        /// Receive a number of messages from the test actor matching the given
        /// number of objects and assert that for each given object one is received
        /// which equals it and vice versa. This construct is useful when the order in
        /// which the objects are received is not fixed. Wait time is bounded by 
        /// <see cref="RemainingOrDefault"/> as duration, with an assertion exception being thrown in case of timeout.
        /// 
        /// <code>
        ///   dispatcher.Tell(SomeWork1())
        ///   dispatcher.Tell(SomeWork2())
        ///   ExpectMsgAllOf(TimeSpan.FromSeconds(1), Result1(), Result2())
        /// </code>
        /// </summary>
        /// <typeparam name="T">The type of the messages</typeparam>
        /// <param name="messages">The messages.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>The received messages in received order</returns>
        public IReadOnlyCollection<T> ExpectMsgAllOf<T>(
            IReadOnlyCollection<T> messages,
            CancellationToken cancellationToken = default)
            => TestKit.ExpectMsgAllOf(messages, cancellationToken);

        public IAsyncEnumerable<T> ExpectMsgAllOfAsync<T>(
            IReadOnlyCollection<T> messages,
            CancellationToken cancellationToken = default)
            => TestKit.ExpectMsgAllOfAsync(messages, cancellationToken);

        /// <summary>
        /// Receive a number of messages from the test actor matching the given
        /// number of objects and assert that for each given object one is received
        /// which equals it and vice versa. This construct is useful when the order in
        /// which the objects are received is not fixed. Wait time is bounded by the
        /// given duration, with an assertion exception being thrown in case of timeout.
        /// 
        /// <code>
        ///   dispatcher.Tell(SomeWork1())
        ///   dispatcher.Tell(SomeWork2())
        ///   ExpectMsgAllOf(TimeSpan.FromSeconds(1), Result1(), Result2())
        /// </code>
        /// The deadline is scaled by "akka.test.timefactor" using <see cref="Dilated"/>.
        /// </summary>
        /// <typeparam name="T">The type of the messages</typeparam>
        /// <param name="max">The deadline. The deadline is scaled by "akka.test.timefactor" using <see cref="Dilated"/>.</param>
        /// <param name="messages">The messages.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>The received messages in received order</returns>
        public IReadOnlyCollection<T> ExpectMsgAllOf<T>(
            TimeSpan max,
            IReadOnlyCollection<T> messages,
            CancellationToken cancellationToken = default)
            => TestKit.ExpectMsgAllOf(max, messages, cancellationToken);
        
        public IAsyncEnumerable<T> ExpectMsgAllOfAsync<T>(
            TimeSpan max,
            IReadOnlyCollection<T> messages,
            CancellationToken cancellationToken = default)
            => TestKit.ExpectMsgAllOfAsync(max, messages, cancellationToken);
    }
}