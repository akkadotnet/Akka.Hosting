// -----------------------------------------------------------------------
//  <copyright file="AdapterOptions.cs" company="Akka.NET Project">
//      Copyright (C) 2009-2022 Lightbend Inc. <http://www.lightbend.com>
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

namespace Akka.Persistence.Hosting
{
    public class AdapterOptions: IEquatable<AdapterOptions>
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string[] TypeBindings { get; set; }

        public bool Equals(AdapterOptions other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Name == other.Name
                   && Type == other.Type
                   && TypeBindings.Length == other.TypeBindings.Length
                   && TypeBindings.All(t => other.TypeBindings.Contains(t));
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is AdapterOptions opt && Equals(opt);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Type != null ? Type.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (TypeBindings != null ? TypeBindings.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}