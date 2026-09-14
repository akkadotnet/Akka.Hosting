// -----------------------------------------------------------------------
//  <copyright file="TestKit.Hooks.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using System.Threading.Tasks;

namespace Akka.Hosting.TestKit
{
    /// <summary>
    /// Per-test lifecycle hooks for the xUnit v3 test kit. Kept in a separate file (not compiled
    /// into the xUnit2 variant) so the xUnit2 partial can override these same-named members to
    /// install its own per-test ambient state (see #764).
    /// </summary>
    public abstract partial class TestKit
    {
        protected virtual Task BeforeTestStart()
        {
            return Task.CompletedTask;
        }

        protected virtual Task AfterAllAsync()
        {
            return Task.CompletedTask;
        }
    }
}
