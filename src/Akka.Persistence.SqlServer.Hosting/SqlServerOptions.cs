// -----------------------------------------------------------------------
//  <copyright file="SqlServerJournalOptions.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using System;
using System.Data;
using System.Text;
using Akka.Configuration;
using Akka.Hosting;
using Akka.Persistence.Hosting;

#nullable enable
namespace Akka.Persistence.SqlServer.Hosting
{
    /// <summary>
    ///     Akka.Hosting options class to set up Microsoft SqlServer persistence journal.
    /// </summary>
    public sealed class SqlServerJournalOptions: SqlJournalOptions
    {
        private static readonly Config Default = SqlServerPersistence.DefaultConfiguration()
            .GetConfig(SqlServerJournalSettings.ConfigPath);

        /// <summary>
        ///     Create a new instance of <see cref="SqlServerJournalOptions"/>
        /// </summary>
        public SqlServerJournalOptions() : this(true)
        {
        }
        
        /// <summary>
        ///     Create a new instance of <see cref="SqlServerJournalOptions"/>
        /// </summary>
        /// <param name="isDefaultPlugin">
        ///     Indicates if this journal configuration should be the default configuration for all persistence
        /// </param>
        /// <param name="identifier">
        ///     The journal configuration identifier. <i>Default</i>: "sql-server"
        /// </param>
        public SqlServerJournalOptions(bool isDefaultPlugin, string identifier = "sql-server") : base(isDefaultPlugin)
        {
            Identifier = identifier;
        }
        
        /// <summary>
        ///     <para>
        ///         The plugin identifier for this persistence plugin
        ///     </para>
        ///     <b>Default</b>: <c>"sql-server"</c>
        /// </summary>
        public override string Identifier { get; set; }
        
        /// <summary>
        ///     <para>
        ///         SQL schema name to table corresponding with persistent journal.
        ///     </para>
        ///     <b>Default</b>: <c>"dbo"</c>
        /// </summary>
        public override string SchemaName { get; set; } = "dbo";
        
        /// <summary>
        ///     <para>
        ///         SQL server table corresponding with persistent journal.
        ///     </para>
        ///     <b>Default</b>: <c>"EventJournal"</c>
        /// </summary>
        public override string TableName { get; set; } = "EventJournal";
        
        /// <summary>
        ///     <para>
        ///         SQL server table corresponding with persistent journal metadata.
        ///     </para>
        ///     <b>Default</b>: <c>"Metadata"</c>
        /// </summary>
        public override string MetadataTableName { get; set; } = "Metadata";

        /// <summary>
        ///     <para>
        ///         Uses the CommandBehavior.SequentialAccess when creating DB commands, providing a performance
        ///         improvement for reading large BLOBS.
        ///     </para>
        ///     <b>Default</b>: <c>true</c>
        /// </summary>
        public override bool SequentialAccess { get; set; } = true;
        
        /// <summary>
        ///     <para>
        ///         By default, string parameter size in ADO.NET queries are set dynamically based on current parameter
        ///         value size.
        ///         If this parameter set to true, column sizes are loaded on journal startup from database schema, and 
        ///         string parameters have constant size which equals to corresponding column size.
        ///     </para>
        ///     <b>Default</b>: <c>false</c>
        /// </summary>
        public bool UseConstantParameterSize { get; set; } = false;
        
        /// <summary>
        ///     <para>
        ///         The SQL write journal is notifying the query side as soon as things
        ///         are persisted, but for efficiency reasons the query side retrieves the events 
        ///         in batches that sometimes can be delayed up to the configured <see cref="QueryRefreshInterval"/>.
        ///     </para>
        ///     <b>Default</b>: 3 seconds
        /// </summary>
        public TimeSpan QueryRefreshInterval { get; set; } = TimeSpan.FromSeconds(3);

        /// <inheritdoc/>
        public override IsolationLevel ReadIsolationLevel { get; set; } = IsolationLevel.Unspecified;

        /// <inheritdoc/>
        public override IsolationLevel WriteIsolationLevel { get; set; } = IsolationLevel.Unspecified;

        protected override Config InternalDefaultConfig => Default;

        protected override StringBuilder Build(StringBuilder sb)
        {
            sb.AppendLine($"use-constant-parameter-size = {UseConstantParameterSize.ToHocon()}");
            
            sb = base.Build(sb);
            sb.AppendLine($"akka.persistence.query.journal.sql.refresh-interval = {QueryRefreshInterval.ToHocon()}");
            
            return sb;
        }
    }

    /// <summary>
    /// Akka.Hosting options class to set up Microsoft SqlServer persistence snapshot store.
    /// </summary>
    public sealed class SqlServerSnapshotOptions: SqlSnapshotOptions
    {
        private static readonly Config Default = SqlServerPersistence.DefaultConfiguration()
            .GetConfig(SqlServerSnapshotSettings.ConfigPath);

        /// <summary>
        ///     Create a new instance of <see cref="SqlServerSnapshotOptions"/>
        /// </summary>
        public SqlServerSnapshotOptions() : this(true)
        {
        }
        
        /// <summary>
        ///     Create a new instance of <see cref="SqlServerSnapshotOptions"/>
        /// </summary>
        /// <param name="isDefaultPlugin">
        ///     Indicates if this snapshot store configuration should be the default configuration for all persistence
        /// </param>
        /// <param name="identifier">
        ///     The snapshot store configuration identifier. <i>Default</i>: "sql-server"
        /// </param>
        public SqlServerSnapshotOptions(bool isDefaultPlugin, string identifier = "sql-server") : base(isDefaultPlugin)
        {
            Identifier = identifier;
        }
        
        /// <summary>
        ///     <para>
        ///         The plugin identifier for this persistence plugin
        ///     </para>
        ///     <b>Default</b>: <c>"sql-server"</c>
        /// </summary>
        public override string Identifier { get; set; }
        
        /// <summary>
        ///     <para>
        ///         SQL server schema name to table corresponding with persistent snapshot store.
        ///     </para>
        ///     <b>Default</b>: <c>"dbo"</c>
        /// </summary>
        public override string SchemaName { get; set; } = "dbo";
        
        /// <summary>
        ///     <para>
        ///         SQL server table corresponding with persistent snapshot store.
        ///     </para>
        ///     <b>Default</b>: <c>"SnapshotStore"</c>
        /// </summary>
        public override string TableName { get; set; } = "SnapshotStore";
        
        /// <summary>
        ///     Uses the CommandBehavior.SequentialAccess when creating the command, providing a performance
        ///     improvement for reading large BLOBS.
        ///     <b>Default</b>: <c>true</c>
        /// </summary>
        public override bool SequentialAccess { get; set; } = true;

        /// <summary>
        ///     <para>
        ///         By default, string parameter size in ADO.NET queries are set dynamically based on current parameter
        ///         value size. If this parameter set to true, column sizes are loaded on journal startup from database
        ///         schema, and string parameters have constant size which equals to corresponding column size.
        ///     </para>
        ///     <b>Default</b>: <c>false</c>
        /// </summary>
        public bool UseConstantParameterSize { get; set; } = false;

        /// <inheritdoc/>
        public override IsolationLevel ReadIsolationLevel { get; set; } = IsolationLevel.Unspecified;

        /// <inheritdoc/>
        public override IsolationLevel WriteIsolationLevel { get; set; } = IsolationLevel.Unspecified;

        protected override Config InternalDefaultConfig => Default;

        protected override StringBuilder Build(StringBuilder sb)
        {
            sb.AppendLine("class = \"Akka.Persistence.SqlServer.Snapshot.SqlServerSnapshotStore, Akka.Persistence.SqlServer\"");
            sb.AppendLine("plugin-dispatcher = \"akka.actor.default-dispatcher\"");
            sb.AppendLine($"use-constant-parameter-size = {UseConstantParameterSize.ToHocon()}");
            
            return base.Build(sb);
        }
    }
}