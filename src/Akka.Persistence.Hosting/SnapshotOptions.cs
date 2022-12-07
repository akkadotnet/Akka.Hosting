// -----------------------------------------------------------------------
//  <copyright file="SnapshotOptions.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using System;
using System.Text;
using Akka.Configuration;
using Akka.Hosting;

namespace Akka.Persistence.Hosting
{
    /// <summary>
    /// Base class for all snapshot store options class. If you're writing an options class for SQL plugins, use
    /// <see cref="SqlSnapshotOptions"/> instead.
    /// </summary>
    public abstract class SnapshotOptions
    {
        private readonly bool _isDefault;
        
        protected SnapshotOptions(bool isDefault)
        {
            _isDefault = isDefault;
        }
        
        public abstract string Identifier { get; set; }
        
        /// <summary>
        ///     Should corresponding snapshot store table be initialized automatically.
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
        
        /// <summary>
        /// The default configuration for this journal. This must be the actual configuration block for this journal.
        /// Example:
        /// protected override Config InternalDefaultConfig = PostgreSqlPersistence.DefaultConfiguration()
        ///     .GetConfig("akka.persistence.snapshot-store.postgresql");
        /// </summary>
        protected abstract Config InternalDefaultConfig { get; }
        
        public Config DefaultConfig => InternalDefaultConfig.MoveTo($"akka.persistence.snapshot-store.{Identifier}");
        
        /// <summary>
        /// The chain config builder.
        /// The top <see cref="SnapshotOptions.Build"/> caps the configuration with the outer `akka.persistence.snapshot-store.{id}` HOCON block.
        /// If you need to add more properties into this block, append them __BEFORE__ calling <c>base.Build()</c>.
        /// If you need to add more properties outside of this block, append them __AFTER__ calling <c>base.Build()</c>.
        /// </summary>
        /// <param name="sb"><see cref="StringBuilder"/> instance from lower chain</param>
        /// <returns>The fully built <see cref="StringBuilder"/> containing the `akka.persistence.snapshot-store.{id}` HOCON block.</returns>
        /// <exception cref="Exception">Thrown when <see cref="Identifier"/> is null, contains only whitespaces, or contains illegal characters</exception>
        protected virtual StringBuilder Build(StringBuilder sb)
        {
            if(string.IsNullOrWhiteSpace(Identifier))
                throw new Exception($"Invalid {GetType()}, {nameof(Identifier)} is null or whitespace");
            
            var illegalChars = Identifier.IsIllegalHoconKey();
            if (illegalChars.Length > 0)
            {
                throw new Exception($"Invalid {GetType()}, {nameof(Identifier)} contains illegal character(s) {string.Join(", ", illegalChars)}");
            }
            
            sb.Insert(0, $"akka.persistence.snapshot-store.{Identifier} {{{Environment.NewLine}");
            sb.AppendLine($"serializer = {Serializer.ToHocon()}");
            sb.AppendLine($"auto-initialize = {AutoInitialize.ToHocon()}");
            sb.AppendLine("}");
            
            if (_isDefault)
                sb.AppendLine($"akka.persistence.snapshot-store.plugin = akka.persistence.snapshot-store.{Identifier}");
            
            return sb;
        }
        
        public Config ToConfig()
            => ToString();
        
        public sealed override string ToString()
            => Build(new StringBuilder()).ToString();
    }
}