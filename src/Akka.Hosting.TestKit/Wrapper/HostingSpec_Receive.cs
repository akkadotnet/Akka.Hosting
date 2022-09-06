// -----------------------------------------------------------------------
//  <copyright file="HostingSpec_Receive.cs" company="Akka.NET Project">
//      Copyright (C) 2009-2022 Lightbend Inc. <http://www.lightbend.com>
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Akka.TestKit;
using Akka.TestKit.Internal;

namespace Akka.Hosting.TestKit
{
    public abstract partial class HostingSpec
    {
        /// <summary>
        /// Receives messages until <paramref name="isMessage"/> returns <c>true</c>.
        /// Use it to ignore certain messages while waiting for a specific message.
        /// </summary>
        /// <param name="isMessage">The is message.</param>
        /// <param name="max">The maximum.</param>
        /// <param name="hint">The hint.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns the message that <paramref name="isMessage"/> matched</returns>
        public object FishForMessage(
            Predicate<object> isMessage,
            TimeSpan? max = null,
            string hint = "",
            CancellationToken cancellationToken = default)
            => TestKit.FishForMessage(isMessage, max, hint, cancellationToken);

        /// <inheritdoc cref="FishForMessage(Predicate{object}, TimeSpan?, string, CancellationToken)"/>
        public ValueTask<object> FishForMessageAsync(
            Predicate<object> isMessage, 
            TimeSpan? max = null,
            string hint = "",
            CancellationToken cancellationToken = default)
            => TestKit.FishForMessageAsync(isMessage, max, hint, cancellationToken);

        /// <summary>
        /// Receives messages until <paramref name="isMessage"/> returns <c>true</c>.
        /// Use it to ignore certain messages while waiting for a specific message.
        /// </summary>
        /// <typeparam name="T">The type of the expected message. Messages of other types are ignored.</typeparam>
        /// <param name="isMessage">The is message.</param>
        /// <param name="max">The maximum.</param>
        /// <param name="hint">The hint.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns the message that <paramref name="isMessage"/> matched</returns>
        public T FishForMessage<T>(
            Predicate<T> isMessage,
            TimeSpan? max = null,
            string hint = "",
            CancellationToken cancellationToken = default)
            => TestKit.FishForMessage(isMessage, max, hint, cancellationToken);

        /// <inheritdoc cref="FishForMessage{T}(Predicate{T}, TimeSpan?, string, CancellationToken)"/>
        public ValueTask<T> FishForMessageAsync<T>(
            Predicate<T> isMessage,
            TimeSpan? max = null,
            string hint = "",
            CancellationToken cancellationToken = default)
            => TestKit.FishForMessageAsync(isMessage, max, hint, cancellationToken);

        /// <summary>
        /// Receives messages until <paramref name="isMessage"/> returns <c>true</c>.
        /// Use it to ignore certain messages while waiting for a specific message.
        /// </summary>
        /// <typeparam name="T">The type of the expected message. Messages of other types are ignored.</typeparam>
        /// <param name="isMessage">The is message.</param>
        /// <param name="max">The maximum.</param>
        /// <param name="hint">The hint.</param>
        /// <param name="cancellationToken"></param>
        /// <param name="allMessages">If null then will be ignored. If not null then will be initially cleared, then filled with all the messages until <paramref name="isMessage"/> returns <c>true</c></param>
        /// <returns>Returns the message that <paramref name="isMessage"/> matched</returns>
        public T FishForMessage<T>(
            Predicate<T> isMessage,
            ArrayList allMessages,
            TimeSpan? max = null,
            string hint = "",
            CancellationToken cancellationToken = default)
            => TestKit.FishForMessage(isMessage, allMessages, max, hint, cancellationToken);

        /// <inheritdoc cref="FishForMessage{T}(Predicate{T}, ArrayList, TimeSpan?, string, CancellationToken)"/>
        public  ValueTask<T> FishForMessageAsync<T>(
            Predicate<T> isMessage,
            ArrayList allMessages, 
            TimeSpan? max = null,
            string hint = "",
            CancellationToken cancellationToken = default)
            => TestKit.FishForMessageAsync(isMessage, allMessages, max, hint, cancellationToken);

        /// <summary>
        /// Receives messages until <paramref name="max"/>.
        ///
        /// Ignores all messages except for a message of type <typeparamref name="T"/>.
        /// Asserts that all messages are not of the of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type that the message is not supposed to be.</typeparam>
        /// <param name="max">Optional. The maximum wait duration. Defaults to <see cref="RemainingOrDefault"/> when unset.</param>
        /// <param name="cancellationToken"></param>
        public Task FishUntilMessageAsync<T>(TimeSpan? max = null, CancellationToken cancellationToken = default)
            => TestKit.FishUntilMessageAsync<T>(max, cancellationToken);

        /// <summary>
        /// Waits for a <paramref name="max"/> period of 'radio-silence' limited to a number of <paramref name="maxMessages"/>.
        /// Note: 'radio-silence' definition: period when no messages arrive at.
        /// </summary>
        /// <param name="max">A temporary period of 'radio-silence'.</param>
        /// <param name="maxMessages">The method asserts that <paramref name="maxMessages"/> is never reached.</param>
        /// <param name="cancellationToken"></param>
        /// If set to null then this method will loop for an infinite number of <paramref name="max"/> periods. 
        /// NOTE: If set to null and radio-silence is never reached then this method will never return.  
        /// <returns>Returns all the messages encountered before 'radio-silence' was reached.</returns>
        public Task<ArrayList> WaitForRadioSilenceAsync(TimeSpan? max = null, uint? maxMessages = null,
            CancellationToken cancellationToken = default)
            => TestKit.WaitForRadioSilenceAsync(max, maxMessages, cancellationToken);

        /// <summary>
        /// Receive one message from the internal queue of the TestActor.
        /// This method blocks the specified duration or until a message
        /// is received. If no message was received, <c>null</c> is returned.
        /// <remarks>This method does NOT automatically scale its Duration parameter using <see cref="Dilated(TimeSpan)" />!</remarks>
        /// </summary>
        /// <param name="max">The maximum duration to wait. 
        /// If <c>null</c> the config value "akka.test.single-expect-default" is used as timeout.
        /// If set to a negative value or <see cref="Timeout.InfiniteTimeSpan"/>, blocks forever.
        /// <remarks>This method does NOT automatically scale its Duration parameter using <see cref="Dilated(TimeSpan)" />!</remarks></param>
        /// <param name="cancellationToken"></param>
        /// <returns>The message if one was received; <c>null</c> otherwise</returns>
        public object ReceiveOne(TimeSpan? max = null, CancellationToken cancellationToken = default)
            => TestKit.ReceiveOne(max, cancellationToken);

        /// <inheritdoc cref="ReceiveOne(TimeSpan?, CancellationToken)"/>
        public ValueTask<object> ReceiveOneAsync(TimeSpan? max = null, CancellationToken cancellationToken = default)
            => TestKit.ReceiveOneAsync(max, cancellationToken);

        /// <summary>
        /// Receive one message from the internal queue of the TestActor within 
        /// the specified duration. The method blocks the specified duration.
        /// <remarks><b>Note!</b> that the returned <paramref name="envelope"/> 
        /// is a <see cref="MessageEnvelope"/> containing the sender and the message.</remarks>
        /// <remarks>This method does NOT automatically scale its Duration parameter using <see cref="Dilated(TimeSpan)" />!</remarks>
        /// </summary>
        /// <param name="envelope">The received envelope.</param>
        /// <param name="max">Optional: The maximum duration to wait. 
        ///     If <c>null</c> the config value "akka.test.single-expect-default" is used as timeout.
        ///     If set to a negative value or <see cref="Timeout.InfiniteTimeSpan"/>, blocks forever.
        ///     <remarks>This method does NOT automatically scale its Duration parameter using <see cref="Dilated(TimeSpan)" />!</remarks></param>
        /// <param name="cancellationToken"></param>
        /// <returns><c>True</c> if a message was received within the specified duration; <c>false</c> otherwise.</returns>
        public bool TryReceiveOne(
            out MessageEnvelope envelope,
            TimeSpan? max = null,
            CancellationToken cancellationToken = default)
            => TestKit.TryReceiveOne(out envelope, max, cancellationToken);

        /// <inheritdoc cref="TryReceiveOne(out MessageEnvelope, TimeSpan?, CancellationToken)"/>
        public ValueTask<(bool success, MessageEnvelope envelope)> TryReceiveOneAsync(
            TimeSpan? max,
            CancellationToken cancellationToken = default)
            => TestKit.TryReceiveOneAsync(max, cancellationToken);

        #region Peek methods

        /// <summary>
        /// Peek one message from the head of the internal queue of the TestActor.
        /// This method blocks the specified duration or until a message
        /// is received. If no message was received, <c>null</c> is returned.
        /// <remarks>This method does NOT automatically scale its Duration parameter using <see cref="Dilated(TimeSpan)" />!</remarks>
        /// </summary>
        /// <param name="max">The maximum duration to wait. 
        /// If <c>null</c> the config value "akka.test.single-expect-default" is used as timeout.
        /// If set to a negative value or <see cref="Timeout.InfiniteTimeSpan"/>, blocks forever.
        /// <remarks>This method does NOT automatically scale its Duration parameter using <see cref="Dilated(TimeSpan)" />!</remarks></param>
        /// <param name="cancellationToken"></param>
        /// <returns>The message if one was received; <c>null</c> otherwise</returns>
        public object PeekOne(TimeSpan? max = null, CancellationToken cancellationToken = default)
            => TestKit.PeekOne(max, cancellationToken);

        /// <inheritdoc cref="PeekOne(TimeSpan?, CancellationToken)"/>
        public ValueTask<object> PeekOneAsync(TimeSpan? max = null, CancellationToken cancellationToken = default)
            => TestKit.PeekOneAsync(max, cancellationToken);

        /// <summary>
        /// Peek one message from the head of the internal queue of the TestActor.
        /// This method blocks until cancelled. 
        /// </summary>
        /// <param name="cancellationToken">A token used to cancel the operation</param>
        /// <returns>The message if one was received; <c>null</c> otherwise</returns>
        public object PeekOne(CancellationToken cancellationToken)
            => TestKit.PeekOne(cancellationToken);

        /// <inheritdoc cref="PeekOne(CancellationToken)"/>
        public ValueTask<object> PeekOneAsync(CancellationToken cancellationToken)
            => TestKit.PeekOneAsync(cancellationToken);

        /// <summary>
        /// Peek one message from the head of the internal queue of the TestActor within 
        /// the specified duration.
        /// <para><c>True</c> is returned if a message existed, and the message 
        /// is returned in <paramref name="envelope" />. The method blocks the 
        /// specified duration, and can be cancelled using the 
        /// <paramref name="cancellationToken" />.
        /// </para> 
        /// <remarks>This method does NOT automatically scale its duration parameter using <see cref="Dilated(TimeSpan)" />!</remarks>
        /// </summary>
        /// <param name="envelope">The received envelope.</param>
        /// <param name="max">The maximum duration to wait. 
        ///     If <c>null</c> the config value "akka.test.single-expect-default" is used as timeout.
        ///     If set to <see cref="Timeout.InfiniteTimeSpan"/>, blocks forever (or until cancelled).
        ///     <remarks>This method does NOT automatically scale its Duration parameter using <see cref="Dilated(TimeSpan)" />!</remarks>
        /// </param>
        /// <param name="cancellationToken">A token used to cancel the operation.</param>
        /// <returns><c>True</c> if a message was received within the specified duration; <c>false</c> otherwise.</returns>
        public bool TryPeekOne(out MessageEnvelope envelope, TimeSpan? max, CancellationToken cancellationToken)
            => TestKit.TryPeekOne(out envelope, max, cancellationToken);

        /// <inheritdoc cref="TryPeekOne(out MessageEnvelope, TimeSpan?, CancellationToken)"/>
        public ValueTask<(bool success, MessageEnvelope envelope)> TryPeekOneAsync(TimeSpan? max, CancellationToken cancellationToken)
            => TestKit.TryPeekOneAsync(max, cancellationToken);

        #endregion

        /// <summary>
        /// Receive a series of messages until the function returns null or the overall
        /// maximum duration is elapsed or expected messages count is reached.
        /// Returns the sequence of messages.
        /// 
        /// Note that it is not an error to hit the `max` duration in this case.
        /// The max duration is scaled by <see cref="Dilated(TimeSpan)"/>
        /// </summary>
        /// <typeparam name="T">TBD</typeparam>
        /// <param name="max">TBD</param>
        /// <param name="filter">TBD</param>
        /// <param name="msgs">TBD</param>
        /// <param name="cancellationToken"></param>
        /// <returns>TBD</returns>
        public IReadOnlyList<T> ReceiveWhile<T>(
            TimeSpan? max,
            Func<object, T> filter,
            int msgs = int.MaxValue,
            CancellationToken cancellationToken = default)
            => TestKit.ReceiveWhile(max, filter, msgs, cancellationToken);

        /// <inheritdoc cref="ReceiveWhile{T}(TimeSpan?, Func{object, T}, int, CancellationToken)"/>
        public IAsyncEnumerable<T> ReceiveWhileAsync<T>(
            TimeSpan? max,
            Func<object, T> filter,
            int msgs = int.MaxValue,
            CancellationToken cancellationToken = default)
            => TestKit.ReceiveWhileAsync(max, filter, msgs, cancellationToken);
        
        /// <summary>
        /// Receive a series of messages until the function returns null or the idle 
        /// timeout is met or the overall maximum duration is elapsed or 
        /// expected messages count is reached.
        /// Returns the sequence of messages.
        /// 
        /// Note that it is not an error to hit the `max` duration in this case.
        /// The max duration is scaled by <see cref="Dilated(TimeSpan)"/>
        /// </summary>
        /// <typeparam name="T">TBD</typeparam>
        /// <param name="max">TBD</param>
        /// <param name="idle">TBD</param>
        /// <param name="filter">TBD</param>
        /// <param name="msgs">TBD</param>
        /// <param name="cancellationToken"></param>
        /// <returns>TBD</returns>
        public IReadOnlyList<T> ReceiveWhile<T>(
            TimeSpan? max,
            TimeSpan? idle,
            Func<object, T> filter, 
            int msgs = int.MaxValue,
            CancellationToken cancellationToken = default)
            => TestKit.ReceiveWhile(max, idle, filter, msgs, cancellationToken);

        /// <inheritdoc cref="ReceiveWhile{T}(TimeSpan?, TimeSpan?, Func{object, T}, int, CancellationToken)"/>
        public IAsyncEnumerable<T> ReceiveWhileAsync<T>(
            TimeSpan? max,
            TimeSpan? idle,
            Func<object, T> filter,
            int msgs = int.MaxValue,
            CancellationToken cancellationToken = default)
            => TestKit.ReceiveWhileAsync(max, idle, filter, msgs, cancellationToken);

        /// <summary>
        /// Receive a series of messages until the function returns null or the idle 
        /// timeout is met (disabled by default) or the overall
        /// maximum duration is elapsed or expected messages count is reached.
        /// Returns the sequence of messages.
        /// 
        /// Note that it is not an error to hit the `max` duration in this case.
        /// The max duration is scaled by <see cref="Dilated(TimeSpan)"/>
        /// </summary>
        /// <typeparam name="T">TBD</typeparam>
        /// <param name="filter">TBD</param>
        /// <param name="max">TBD</param>
        /// <param name="idle">TBD</param>
        /// <param name="msgs">TBD</param>
        /// <param name="cancellationToken"></param>
        /// <returns>TBD</returns>
        public IReadOnlyList<T> ReceiveWhile<T>(
            Func<object, T> filter,
            TimeSpan? max = null,
            TimeSpan? idle = null,
            int msgs = int.MaxValue,
            CancellationToken cancellationToken = default)
            => TestKit.ReceiveWhile(filter, max, idle, msgs, cancellationToken);

        /// <inheritdoc cref="ReceiveWhile{T}(Func{object, T}, TimeSpan?, TimeSpan?, int, CancellationToken)"/>
        public IAsyncEnumerable<T> ReceiveWhileAsync<T>(
            Func<object, T> filter,
            TimeSpan? max = null,
            TimeSpan? idle = null,
            int msgs = int.MaxValue,
            CancellationToken cancellationToken = default)
            => TestKit.ReceiveWhileAsync(filter, max, idle, msgs, cancellationToken);

        /// <summary>
        /// Receive a series of messages.
        /// It will continue to receive messages until the <paramref name="shouldContinue"/> predicate returns <c>false</c> or the idle 
        /// timeout is met (disabled by default) or the overall
        /// maximum duration is elapsed or expected messages count is reached.
        /// If a message that isn't of type <typeparamref name="T"/> the parameter <paramref name="shouldIgnoreOtherMessageTypes"/> 
        /// declares if the message should be ignored or not.
        /// <para>Returns the sequence of messages.</para>
        /// 
        /// Note that it is not an error to hit the `max` duration in this case.
        /// The max duration is scaled by <see cref="Dilated(TimeSpan)"/>
        /// </summary>
        /// <typeparam name="T">TBD</typeparam>
        /// <param name="shouldContinue">TBD</param>
        /// <param name="max">TBD</param>
        /// <param name="idle">TBD</param>
        /// <param name="msgs">TBD</param>
        /// <param name="shouldIgnoreOtherMessageTypes">TBD</param>
        /// <param name="cancellationToken"></param>
        /// <returns>TBD</returns>
        public IReadOnlyList<T> ReceiveWhile<T>(
            Predicate<T> shouldContinue,
            TimeSpan? max = null,
            TimeSpan? idle = null,
            int msgs = int.MaxValue,
            bool shouldIgnoreOtherMessageTypes = true,
            CancellationToken cancellationToken = default)
            => TestKit.ReceiveWhile(shouldContinue, max, idle, msgs, shouldIgnoreOtherMessageTypes, cancellationToken);

        /// <inheritdoc cref="ReceiveWhile{T}(Predicate{T}, TimeSpan?, TimeSpan?, int, bool, CancellationToken)"/>
        public IAsyncEnumerable<T> ReceiveWhileAsync<T>(
            Predicate<T> shouldContinue,
            TimeSpan? max = null,
            TimeSpan? idle = null,
            int msgs = int.MaxValue,
            bool shouldIgnoreOtherMessageTypes = true,
            CancellationToken cancellationToken = default)
            => TestKit.ReceiveWhileAsync(shouldContinue, max, idle, msgs, shouldIgnoreOtherMessageTypes, cancellationToken);

        /// <summary>
        /// Receive the specified number of messages using <see cref="RemainingOrDefault"/> as timeout.
        /// </summary>
        /// <param name="numberOfMessages">The number of messages.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>The received messages</returns>
        public IReadOnlyCollection<object> ReceiveN(
            int numberOfMessages,
            CancellationToken cancellationToken = default)
            => TestKit.ReceiveN(numberOfMessages, cancellationToken);

        /// <inheritdoc cref="ReceiveN(int, CancellationToken)"/>
        public  IAsyncEnumerable<object> ReceiveNAsync(
            int numberOfMessages,
            CancellationToken cancellationToken = default)
            => TestKit.ReceiveNAsync(numberOfMessages, cancellationToken);

        /// <summary>
        /// Receive the specified number of messages in a row before the given deadline.
        /// The deadline is scaled by "akka.test.timefactor" using <see cref="Dilated"/>.
        /// </summary>
        /// <param name="numberOfMessages">The number of messages.</param>
        /// <param name="max">The timeout scaled by "akka.test.timefactor" using <see cref="Dilated"/>.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>The received messages</returns>
        public IReadOnlyCollection<object> ReceiveN(
            int numberOfMessages,
            TimeSpan max,
            CancellationToken cancellationToken = default)
            => TestKit.ReceiveN(numberOfMessages, max, cancellationToken);

        /// <inheritdoc cref="ReceiveN(int, TimeSpan, CancellationToken)"/>
        public IAsyncEnumerable<object> ReceiveNAsync(
            int numberOfMessages,
            TimeSpan max,
            CancellationToken cancellationToken = default)
            => TestKit.ReceiveNAsync(numberOfMessages, max, cancellationToken);
    }
}