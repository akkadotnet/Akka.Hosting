// -----------------------------------------------------------------------
//  <copyright file="StringExtensions.cs" company="Akka.NET Project">
//      Copyright (C) 2009-2022 Lightbend Inc. <http://www.lightbend.com>
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using System;
using System.Globalization;
using System.Text.RegularExpressions;
using Akka.Configuration;

namespace Akka.Hosting
{
    public static class HoconExtensions
    {
        private static readonly Regex EscapeRegex = new Regex("[ \t:]{1}", RegexOptions.Compiled);
        
        public static string ToHocon(this string text)
        {
            if (text is null)
                throw new ConfigurationException("Value can not be null");
            
            return EscapeRegex.IsMatch(text) || text == string.Empty ? $"\"{text}\"" : text;
        }

        public static string ToHocon(this bool? value)
        {
            if (value is null)
                throw new ConfigurationException("Value can not be null");
            
            return value.Value ? "on" : "off";
        }

        public static string ToHocon(this bool value)
            => value ? "on" : "off";

        public static string ToHocon(this TimeSpan? value, bool allowInfinite = false, bool zeroIsInfinite = false)
        {
            if (value is null)
                throw new ConfigurationException("Value can not be null");
            return value.Value.ToHocon(allowInfinite, zeroIsInfinite);
        }

        public static string ToHocon(this TimeSpan value, bool allowInfinite = false, bool zeroIsInfinite = false)
        {
            if (!allowInfinite)
            {
                if ((zeroIsInfinite && value <= TimeSpan.Zero) || (!zeroIsInfinite && value < TimeSpan.Zero))
                    throw new ConfigurationException("Infinite value is not allowed");
            }

            if ((zeroIsInfinite && value <= TimeSpan.Zero) || (!zeroIsInfinite && value < TimeSpan.Zero))
                return "infinite";
            
            return value.TotalMilliseconds.ToString(CultureInfo.InvariantCulture);
        }
    }
}