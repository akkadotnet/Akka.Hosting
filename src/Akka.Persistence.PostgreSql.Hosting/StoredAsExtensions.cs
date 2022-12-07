// -----------------------------------------------------------------------
//  <copyright file="StoredAsExtensions.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using System;

namespace Akka.Persistence.PostgreSql.Hosting
{
    public static class StoredAsExtensions
    {
        internal static string ToHocon(this StoredAsType storedAsType)
            => storedAsType switch
            {
                StoredAsType.ByteA => "bytea",
                StoredAsType.Json => "json",
                StoredAsType.JsonB => "jsonb",
                _ => throw new ArgumentOutOfRangeException(nameof(storedAsType), storedAsType, "Invalid StoredAsType defined.")
            };
    }
}