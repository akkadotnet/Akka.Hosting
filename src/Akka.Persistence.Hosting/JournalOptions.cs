// -----------------------------------------------------------------------
//  <copyright file="JournalOptions.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using System;
using System.Text;
using Akka.Configuration;
using Akka.Hosting;

#nullable enable
namespace Akka.Persistence.Hosting
{
    public abstract class JournalOptions
    {
        private readonly bool _isDefault;
        
        protected JournalOptions(bool isDefault)
        {
            _isDefault = isDefault;
        }
        
        public abstract string Identifier { get; set; }
        
        /// <summary>
        ///     <para>
        ///         Should corresponding journal table be initialized automatically.
        ///     </para>
        ///     <b>Default</b>: false
        /// </summary>
        public bool AutoInitialize { get; set; } = false;
        
        /// <summary>
        ///     <para>
        ///         Default serializer used as manifest serializer when applicable and payload serializer when no
        ///         specific binding overrides are specified
        ///     </para>
        ///     <b>Default</b>: json
        /// </summary>
        public string Serializer { get; set; } = "json";
        
        public abstract Config DefaultConfig { get; }
        
        public AkkaPersistenceJournalBuilder Adapters { get; set; } = new ("", null!);
        
        /// <summary>
        /// The chain config builder.
        /// The top <see cref="JournalOptions.Build"/> caps the configuration with the outer `akka.persistence.journal.{id}` HOCON block.
        /// If you need to add more properties into this block, append them __BEFORE__ calling <c>base.Build()</c>.
        /// If you need to add more properties outside of this block, append them __AFTER__ calling <c>base.Build()</c>.
        /// </summary>
        /// <param name="sb"><see cref="StringBuilder"/> instance from lower chain</param>
        /// <returns>The fully built <see cref="StringBuilder"/> containing the `akka.persistence.journal.{id}` HOCON block.</returns>
        /// <exception cref="Exception">Thrown when <see cref="Identifier"/> is null or whitespace</exception>
        protected virtual StringBuilder Build(StringBuilder sb)
        {
            if(string.IsNullOrWhiteSpace(Identifier))
                throw new Exception($"Invalid {GetType()}, {nameof(Identifier)} is null or whitespace");

            var illegalChars = Identifier.IsIllegalHoconKey();
            if (illegalChars.Length > 0)
            {
                throw new Exception($"Invalid {GetType()}, {nameof(Identifier)} contains illegal character(s) {string.Join(", ", illegalChars)}");
            }
            
            sb.Insert(0, $"akka.persistence.journal.{Identifier.ToHocon()} {{{Environment.NewLine}");
            sb.AppendLine($"auto-initialize = {AutoInitialize.ToHocon()}");
            sb.AppendLine($"serializer = {Serializer.ToHocon()}");
            Adapters.AppendAdapters(sb);
            sb.AppendLine("}");
            
            if (_isDefault)
                sb.AppendLine($"akka.persistence.journal.plugin = akka.persistence.journal.{Identifier.ToHocon()}");

            return sb;
        }
        
        public Config ToConfig()
            => ToString();
        
        public sealed override string ToString()
            => Build(new StringBuilder()).ToString();
    }
}