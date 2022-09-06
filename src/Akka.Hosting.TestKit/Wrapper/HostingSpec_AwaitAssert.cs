// -----------------------------------------------------------------------
//  <copyright file="HostingSpec_AwaitAssert.cs" company="Akka.NET Project">
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
        /// <para>Await until the given assertion does not throw an exception or the timeout
        /// expires, whichever comes first. If the timeout expires the last exception
        /// is thrown.</para>
        /// <para>The action is called, and if it throws an exception the thread sleeps
        /// the specified interval before retrying.</para>
        /// <para>If no timeout is given, take it from the innermost enclosing `within`
        /// block.</para>
        /// <para>Note that the timeout is scaled using <see cref="Dilated" />,
        /// which uses the configuration entry "akka.test.timefactor".</para>
        /// </summary>
        /// <param name="assertion">The action.</param>
        /// <param name="duration">The timeout.</param>
        /// <param name="interval">The interval to wait between executing the assertion.</param>
        /// <param name="cancellationToken"></param>
        public void AwaitAssert(Action assertion, TimeSpan? duration = null, TimeSpan? interval = null,
            CancellationToken cancellationToken = default)
            => TestKit.AwaitAssert(assertion, duration, interval, cancellationToken);
        
        /// <inheritdoc cref="AwaitAssert(Action, TimeSpan?, TimeSpan?, CancellationToken)"/>
        public Task AwaitAssertAsync(Action assertion, TimeSpan? duration=null, TimeSpan? interval=null, CancellationToken cancellationToken = default)
            => TestKit.AwaitAssertAsync(assertion, duration, interval, cancellationToken);

        /// <summary>
        /// <para>Await until the given assertion does not throw an exception or the timeout
        /// expires, whichever comes first. If the timeout expires the last exception
        /// is thrown.</para>
        /// <para>The action is called, and if it throws an exception the thread sleeps
        /// the specified interval before retrying.</para>
        /// <para>If no timeout is given, take it from the innermost enclosing `within`
        /// block.</para>
        /// <para>Note that the timeout is scaled using <see cref="Dilated" />,
        /// which uses the configuration entry "akka.test.timefactor".</para>
        /// </summary>
        /// <param name="assertion">The action.</param>
        /// <param name="duration">The timeout.</param>
        /// <param name="interval">The interval to wait between executing the assertion.</param>
        /// <param name="cancellationToken"></param>
        public Task AwaitAssertAsync(Func<Task> assertion, TimeSpan? duration=null, TimeSpan? interval=null, CancellationToken cancellationToken = default)
            => TestKit.AwaitAssertAsync(assertion, duration, interval, cancellationToken);

    }
}