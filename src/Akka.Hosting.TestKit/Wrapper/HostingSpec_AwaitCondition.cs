// -----------------------------------------------------------------------
//  <copyright file="HostingSpec_AwaitCondition.cs" company="Akka.NET Project">
//      Copyright (C) 2009-2022 Lightbend Inc. <http://www.lightbend.com>
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Akka.Hosting.TestKit
{
    public abstract partial class HostingSpec
    {
        /// <summary>
        /// <para>Await until the given condition evaluates to <c>true</c> or until a timeout</para>
        /// <para>The timeout is taken from the innermost enclosing `within`
        /// block (if inside a `within` block) or the value specified in config value "akka.test.single-expect-default". 
        /// The value is <see cref="Dilated(TimeSpan)">dilated</see>, i.e. scaled by the factor 
        /// specified in config value "akka.test.timefactor"..</para>
        /// <para>A call to <paramref name="conditionIsFulfilled"/> is done immediately, then the threads sleep
        /// for about a tenth of the timeout value, before it checks the condition again. This is repeated until
        /// timeout or the condition evaluates to <c>true</c>. To specify another interval, use the overload
        /// <see cref="AwaitCondition(System.Func{bool},System.Nullable{System.TimeSpan},System.Nullable{System.TimeSpan},string, CancellationToken)"/>
        /// </para>
        /// </summary>
        /// <param name="conditionIsFulfilled">The condition that must be fulfilled within the duration.</param>
        /// <param name="cancellationToken"></param>
        public void AwaitCondition(Func<bool> conditionIsFulfilled, CancellationToken cancellationToken = default)
            => TestKit.AwaitCondition(conditionIsFulfilled, cancellationToken);
        
        public Task AwaitConditionAsync(Func<Task<bool>> conditionIsFulfilled, CancellationToken cancellationToken = default)
            => TestKit.AwaitConditionAsync(conditionIsFulfilled, cancellationToken);

        /// <summary>
        /// <para>Await until the given condition evaluates to <c>true</c> or the timeout
        /// expires, whichever comes first.</para>
        /// <para>If no timeout is given, take it from the innermost enclosing `within`
        /// block (if inside a `within` block) or the value specified in config value "akka.test.single-expect-default". 
        /// The value is <see cref="Dilated(TimeSpan)">dilated</see>, i.e. scaled by the factor 
        /// specified in config value "akka.test.timefactor"..</para>
        /// <para>A call to <paramref name="conditionIsFulfilled"/> is done immediately, then the threads sleep
        /// for about a tenth of the timeout value, before it checks the condition again. This is repeated until
        /// timeout or the condition evaluates to <c>true</c>. To specify another interval, use the overload
        /// <see cref="AwaitCondition(System.Func{bool},System.Nullable{System.TimeSpan},System.Nullable{System.TimeSpan},string, CancellationToken)"/>
        /// </para>
        /// </summary>
        /// <param name="conditionIsFulfilled">The condition that must be fulfilled within the duration.</param>
        /// <param name="max">The maximum duration. If undefined, uses the remaining time 
        /// (if inside a `within` block) or the value specified in config value "akka.test.single-expect-default". 
        /// The value is <see cref="Dilated(TimeSpan)">dilated</see>, i.e. scaled by the factor 
        /// specified in config value "akka.test.timefactor".</param>
        /// <param name="cancellationToken"></param>
        public void AwaitCondition(Func<bool> conditionIsFulfilled, TimeSpan? max, CancellationToken cancellationToken = default)
            => TestKit.AwaitCondition(conditionIsFulfilled, max, cancellationToken);
        
        public Task AwaitConditionAsync(Func<Task<bool>> conditionIsFulfilled, TimeSpan? max, CancellationToken cancellationToken = default)
            => TestKit.AwaitConditionAsync(conditionIsFulfilled, max, cancellationToken);
        
        /// <summary>
        /// <para>Await until the given condition evaluates to <c>true</c> or the timeout
        /// expires, whichever comes first.</para>
        /// <para>If no timeout is given, take it from the innermost enclosing `within`
        /// block (if inside a `within` block) or the value specified in config value "akka.test.single-expect-default". 
        /// The value is <see cref="Dilated(TimeSpan)">dilated</see>, i.e. scaled by the factor 
        /// specified in config value "akka.test.timefactor"..</para>
        /// <para>A call to <paramref name="conditionIsFulfilled"/> is done immediately, then the threads sleep
        /// for about a tenth of the timeout value, before it checks the condition again. This is repeated until
        /// timeout or the condition evaluates to <c>true</c>. To specify another interval, use the overload
        /// <see cref="AwaitCondition(System.Func{bool},System.Nullable{System.TimeSpan},System.Nullable{System.TimeSpan},string, CancellationToken)"/>
        /// </para>
        /// </summary>
        /// <param name="conditionIsFulfilled">The condition that must be fulfilled within the duration.</param>
        /// <param name="max">The maximum duration. If undefined, uses the remaining time 
        /// (if inside a `within` block) or the value specified in config value "akka.test.single-expect-default". 
        /// The value is <see cref="Dilated(TimeSpan)">dilated</see>, i.e. scaled by the factor 
        /// specified in config value "akka.test.timefactor".</param>
        /// <param name="message">The message used if the timeout expires.</param>
        /// <param name="cancellationToken"></param>
        public void AwaitCondition(Func<bool> conditionIsFulfilled, TimeSpan? max, string message, CancellationToken cancellationToken = default)
            => TestKit.AwaitCondition(conditionIsFulfilled, max, message, cancellationToken);
        
        public Task AwaitConditionAsync(Func<Task<bool>> conditionIsFulfilled, TimeSpan? max, string message, CancellationToken cancellationToken = default)
            => TestKit.AwaitConditionAsync(conditionIsFulfilled, max, message, cancellationToken);

        /// <summary>
        /// <para>Await until the given condition evaluates to <c>true</c> or the timeout
        /// expires, whichever comes first.</para>
        /// <para>If no timeout is given, take it from the innermost enclosing `within`
        /// block.</para>
        /// <para>Note that the timeout is <see cref="Dilated(TimeSpan)">dilated</see>, i.e. scaled by the factor 
        /// specified in config value "akka.test.timefactor".</para>
        /// <para>The parameter <paramref name="interval"/> specifies the time between calls to <paramref name="conditionIsFulfilled"/>
        /// Between calls the thread sleeps. If <paramref name="interval"/> is undefined the thread only sleeps 
        /// one time, using the <paramref name="max"/> as duration, and then rechecks the condition and ultimately 
        /// succeeds or fails.</para>
        /// <para>To make sure that tests run as fast as possible, make sure you do not leave this value as undefined,
        /// instead set it to a relatively small value.</para>
        /// </summary>
        /// <param name="conditionIsFulfilled">The condition that must be fulfilled within the duration.</param>
        /// <param name="max">The maximum duration. If undefined, uses the remaining time 
        /// (if inside a `within` block) or the value specified in config value "akka.test.single-expect-default". 
        /// The value is <see cref="Dilated(TimeSpan)">dilated</see>, i.e. scaled by the factor 
        /// specified in config value "akka.test.timefactor".</param>
        /// <param name="interval">The time between calls to <paramref name="conditionIsFulfilled"/> to check
        /// if the condition is fulfilled. Between calls the thread sleeps. If undefined, negative or 
        /// <see cref="Timeout.InfiniteTimeSpan"/>the thread only sleeps one time, using the <paramref name="max"/>, 
        /// and then rechecks the condition and ultimately succeeds or fails.
        /// <para>To make sure that tests run as fast as possible, make sure you do not set this value as undefined,
        /// instead set it to a relatively small value.</para>
        /// </param>
        /// <param name="message">The message used if the timeout expires.</param>
        /// <param name="cancellationToken"></param>
        public void AwaitCondition(Func<bool> conditionIsFulfilled, TimeSpan? max, TimeSpan? interval, string message = null, CancellationToken cancellationToken = default)
            => TestKit.AwaitCondition(conditionIsFulfilled, max, interval, message, cancellationToken);
        
        public Task AwaitConditionAsync(Func<Task<bool>> conditionIsFulfilled, TimeSpan? max, TimeSpan? interval, string message = null, CancellationToken cancellationToken = default)
            => TestKit.AwaitConditionAsync(conditionIsFulfilled, max, interval, message, cancellationToken);

        /// <summary>
        /// <para>Await until the given condition evaluates to <c>true</c> or the timeout
        /// expires, whichever comes first. Returns <c>true</c> if the condition was fulfilled.</para>        
        /// <para>The parameter <paramref name="interval"/> specifies the time between calls to <paramref name="conditionIsFulfilled"/>
        /// Between calls the thread sleeps. If <paramref name="interval"/> is not specified or <c>null</c> 100 ms is used.</para>
        /// </summary>
        /// <param name="conditionIsFulfilled">The condition that must be fulfilled within the duration.</param>
        /// <param name="max">The maximum duration.</param>
        /// <param name="interval">Optional. The time between calls to <paramref name="conditionIsFulfilled"/> to check
        /// if the condition is fulfilled. Between calls the thread sleeps. If undefined, 100 ms is used
        /// </param>
        /// <param name="cancellationToken"></param>
        /// <returns>TBD</returns>
        public bool AwaitConditionNoThrow(Func<bool> conditionIsFulfilled, TimeSpan max, TimeSpan? interval = null,
            CancellationToken cancellationToken = default)
            => TestKit.AwaitConditionNoThrow(conditionIsFulfilled, max, interval, cancellationToken);
        
        public Task<bool> AwaitConditionNoThrowAsync(Func<Task<bool>> conditionIsFulfilled, TimeSpan max, TimeSpan? interval = null, CancellationToken cancellationToken = default)
            => TestKit.AwaitConditionNoThrowAsync(conditionIsFulfilled, max, interval, cancellationToken);

    }
}