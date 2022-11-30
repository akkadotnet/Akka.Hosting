// -----------------------------------------------------------------------
//  <copyright file="Utils.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Akka.Persistence.Hosting
{
    public static class Utils
    {
        // This regex is conservative. Normally '.' and '/' is allowed in a HOCON literal, but we'll ban it
        // just because it'll make our life harder in the future.
        private static readonly Regex IllegalRegex = new (
            "([\u0020\u00A0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200A\u202F\u205F\u3000\u2028\u2029\uFEFF\u0009\u000A\u000B\u000C\u000D\u001C\u001D\u001E\u001F$\"{}\\[\\]:=,#`^?!@*&\\./])", 
            RegexOptions.Compiled);

        public static (bool, string[]) IsIllegalHoconKey(this string s)
        {
            var match = IllegalRegex.Match(s);
            if (!match.Success)
                return (false, Array.Empty<string>());

            var illegals = new List<string>();
            while (match.Success)
            {
                illegals.Add(match.Value);
                match = match.NextMatch();
            }
            return (true, illegals.Distinct().Select(c => char.IsWhiteSpace(c[0]) ? $"\\u{(int)c[0]:X4}" : c).ToArray());
        }
    }
}