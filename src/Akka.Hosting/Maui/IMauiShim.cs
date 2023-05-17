// -----------------------------------------------------------------------
//  <copyright file="MauiServiceHandler.cs" company="Akka.NET Project">
//      Copyright (C) 2009-2023 Lightbend Inc. <http://www.lightbend.com>
//      Copyright (C) 2013-2023 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using System;
using Microsoft.Extensions.DependencyInjection;

namespace Akka.Hosting.Maui;

/// <summary>
/// INTERNAL API
/// </summary>
/// <remarks>
/// Used to work around https://github.com/akkadotnet/Akka.Hosting/issues/289
/// </remarks>
internal interface IMauiShim
{
   void BindAkkaService(IServiceCollection serviceCollection);
}

/// <summary>
/// INTERNAL API
/// </summary>
/// <remarks>
/// https://github.com/akkadotnet/Akka.Hosting/issues/289
/// </remarks>
internal static class MauiShimHolder
{
   public static IMauiShim? Shim { get; set; } = null;
}