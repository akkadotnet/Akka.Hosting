// -----------------------------------------------------------------------
//  <copyright file="JournalOptions.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Akka.Annotations;
using Akka.Configuration;
using Akka.Hosting;
using Akka.Persistence.Journal;

#nullable enable
namespace Akka.Persistence.Hosting
{
    /// <summary>
    /// Base class for all journal options class. If you're writing an options class for SQL plugins, use
    /// <see cref="SqlJournalOptions"/> instead.
    /// </summary>
    public abstract class JournalOptions
    {
        protected JournalOptions(bool isDefault)
        {
            IsDefaultPlugin = isDefault;
        }
        
        public bool IsDefaultPlugin { get; set; }
        
        /// <summary>
        /// <b>NOTE</b> If you're implementing an option class for new Akka.Hosting persistence, you need to override
        /// this property and provide a default value. The default value have to be the default journal configuration
        /// identifier name (i.e. "sql-server" or "postgresql"
        /// </summary>
        public abstract string Identifier { get; set; }
        
        /// <summary>
        ///     <para>
        ///         Should corresponding journal be initialized automatically, if applicable.
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
        
        /// <summary>
        ///     <para>
        ///         The default configuration for this journal. This must be the actual configuration block for this journal.
        ///     </para>
        ///     Example:
        ///     <code>
        ///         protected override Config InternalDefaultConfig = PostgreSqlPersistence.DefaultConfiguration()
        ///             .GetConfig("akka.persistence.journal.postgresql");
        ///     </code>
        /// </summary>
        protected abstract Config InternalDefaultConfig { get; }

        /// <summary>
        /// The default HOCON configuration for this specific snapshot store configuration identifier, normalized to the
        /// plugin identifier name
        /// </summary>
        public Config DefaultConfig => InternalDefaultConfig.MoveTo(PluginId);
        
        /// <summary>
        /// The journal adapter builder, use this builder to add custom journal
        /// <see cref="IEventAdapter"/>, <see cref="IWriteEventAdapter"/>, or <see cref="IReadEventAdapter"/>
        /// </summary>
        public AkkaPersistenceJournalBuilder AdapterBuilder { get; set; } = new ("", null!);

        private List<AdapterOptions>? _adapters;
        /// <summary>
        /// Advanced optional property, used to configure adapters using <c>IConfiguration</c> binding.
        /// </summary>
        public AdapterOptions[]? Adapters
        {
            get => _adapters?.ToArray();
            set
            {
                if (value is null) 
                    throw new Exception("Adapters assignment is compositional, multiple set is additive, it can not be set to null.");
                
                var writeType = typeof(IWriteEventAdapter);
                var readType = typeof(IReadEventAdapter);
                foreach (var adapter in value)
                {
                    // Adapter type validation
                    var type = Type.GetType(adapter.Type);
                    if (type is null)
                        throw new Exception($"Could not find adapter with Type {adapter.Type}");
                    
                    if (!(readType.IsAssignableFrom(type) || writeType.IsAssignableFrom(type)))
                        throw new Exception($"Type {adapter.Type} should implement {nameof(IWriteEventAdapter)}, {nameof(IReadEventAdapter)}, or {nameof(IEventAdapter)}");
                    
                    // Adapter name validation
                    if (string.IsNullOrEmpty(adapter.Name))
                        throw new Exception("Adapter must have a name");
                    
                    var illegalChars = adapter.Name.IsIllegalHoconKey();
                    if (illegalChars.Length > 0)
                        throw new Exception($"Invalid adapter name {adapter.Name}, contains illegal character(s) {string.Join(", ", illegalChars)}");
                    
                    // Store valid adapter
                    AdapterBuilder.Adapters[adapter.Name] = type;
                    
                    foreach (var typeName in adapter.TypeBindings)
                    {
                        // Type binding validation
                        var bindType = Type.GetType(typeName);
                        if (bindType is null)
                            throw new Exception($"Could not find Type {typeName} to bind to adapter {adapter.Name}");

                        // Assign bindings
                        if (!AdapterBuilder.Bindings.ContainsKey(bindType))
                            AdapterBuilder.Bindings[bindType] = new HashSet<string>();
                        AdapterBuilder.Bindings[bindType].Add(adapter.Name);
                    }
                }

                if (_adapters is null)
                    _adapters = value.ToList();
                else
                {
                    _adapters.AddRange(value);
                }
            } 
        }

        public string PluginId => $"akka.persistence.journal.{Identifier}";
        
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
            
            sb.Insert(0, $"{PluginId} {{{Environment.NewLine}");
            sb.AppendLine($"auto-initialize = {AutoInitialize.ToHocon()}");
            sb.AppendLine($"serializer = {Serializer.ToHocon()}");
            AdapterBuilder.AppendAdapters(sb);
            sb.AppendLine("}");
            
            if (IsDefaultPlugin)
                sb.AppendLine($"akka.persistence.journal.plugin = {PluginId}");

            return sb;
        }
        
        /// <summary>
        /// Transforms the journal options class into a HOCON <see cref="Config"/> instance 
        /// </summary>
        /// <returns>The <see cref="Config"/> equivalence of this options instance</returns>
        public Config ToConfig()
            => ToString();
        
        /// <summary>
        /// Transforms the journal options class into a HOCON string
        /// </summary>
        /// <returns>The HOCON string representation of this options instance</returns>
        public sealed override string ToString()
            => Build(new StringBuilder()).ToString();
    }
}