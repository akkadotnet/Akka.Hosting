// -----------------------------------------------------------------------
//  <copyright file="SqlServerJournalOptions.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using System;
using System.Text;
using Akka.Configuration;
using Akka.Hosting;
using Akka.Persistence.Hosting;

#nullable enable
namespace Akka.Persistence.SqlServer.Hosting
{
    public sealed class SqlServerJournalOptions: SqlJournalOptions
    {
        internal static readonly Config Default = SqlServerPersistence.DefaultConfiguration();
        
        public SqlServerJournalOptions(bool isDefaultPlugin, string identifier = "sql-server") : base(isDefaultPlugin)
        {
            Identifier = identifier;
        }
        
        /// <summary>
        ///     <para>
        ///         The plugin identifier for this persistence plugin
        ///     </para>
        ///     <b>Default</b>: "sql-server"
        /// </summary>
        public override string Identifier { get; set; }
        
        /// <summary>
        ///     <para>
        ///         SQL schema name to table corresponding with persistent journal.
        ///     </para>
        ///     <b>Default</b>: "dbo"
        /// </summary>
        public override string SchemaName { get; set; } = "dbo";
        
        /// <summary>
        ///     <para>
        ///         SQL server table corresponding with persistent journal.
        ///     </para>
        ///     <b>Default</b>: "EventJournal"
        /// </summary>
        public override string TableName { get; set; } = "EventJournal";
        
        /// <summary>
        ///     <para>
        ///         SQL server table corresponding with persistent journal metadata.
        ///     </para>
        ///     <b>Default</b>: "Metadata"
        /// </summary>
        public override string MetadataTableName { get; set; } = "Metadata";

        /// <summary>
        ///     <para>
        ///         Uses the CommandBehavior.SequentialAccess when creating DB commands, providing a performance
        ///         improvement for reading large BLOBS.
        ///     </para>
        ///     <b>Default</b>: true
        /// </summary>
        public override bool SequentialAccess { get; set; } = true;
        
        /// <summary>
        ///     <para>
        ///         By default, string parameter size in ADO.NET queries are set dynamically based on current parameter
        ///         value size.
        ///         If this parameter set to true, column sizes are loaded on journal startup from database schema, and 
        ///         string parameters have constant size which equals to corresponding column size.
        ///     </para>
        ///     <b>Default</b>: false
        /// </summary>
        public bool UseConstantParameterSize { get; set; } = false;
        
        /// <summary>
        /// <para>
        ///     The SQL write journal is notifying the query side as soon as things
        ///     are persisted, but for efficiency reasons the query side retrieves the events 
        ///     in batches that sometimes can be delayed up to the configured <see cref="QueryRefreshInterval"/>.
        /// </para>
        ///     <b>Default</b>: 3 seconds
        /// </summary>
        public TimeSpan QueryRefreshInterval { get; set; } = TimeSpan.FromSeconds(3);

        public override Config DefaultConfig => Default;

        protected override StringBuilder Build(StringBuilder sb)
        {
            sb.AppendLine("class = \"Akka.Persistence.SqlServer.Journal.SqlServerJournal, Akka.Persistence.SqlServer\"");
            sb.AppendLine("plugin-dispatcher = \"akka.actor.default-dispatcher\"");
            sb.AppendLine("timestamp-provider = \"Akka.Persistence.Sql.Common.Journal.DefaultTimestampProvider, Akka.Persistence.Sql.Common\"");
            sb.AppendLine($"use-constant-parameter-size = {UseConstantParameterSize.ToHocon()}");
            
            sb = base.Build(sb);
            sb.AppendLine($"akka.persistence.query.journal.sql.refresh-interval = {QueryRefreshInterval.ToHocon()}");
            
            return sb;
        }
    }

    public sealed class SqlServerSnapshotOptions: SqlSnapshotOptions
    {
        public SqlServerSnapshotOptions(bool isDefault, string identifier = "sql-server") : base(isDefault)
        {
            Identifier = identifier;
        }
        
        /// <summary>
        ///     <para>
        ///         The plugin identifier for this persistence plugin
        ///     </para>
        ///     <b>Default</b>: "sql-server"
        /// </summary>
        public override string Identifier { get; set; }
        
        /// <summary>
        ///     <para>
        ///         SQL server schema name to table corresponding with persistent snapshot store.
        ///     </para>
        ///     <b>Default</b>: "dbo"
        /// </summary>
        public override string SchemaName { get; set; } = "dbo";
        
        /// <summary>
        ///     <para>
        ///         SQL server table corresponding with persistent snapshot store.
        ///     </para>
        ///     <b>Default</b>: "SnapshotStore"
        /// </summary>
        public override string TableName { get; set; } = "SnapshotStore";
        
        /// <summary>
        ///     Uses the CommandBehavior.SequentialAccess when creating the command, providing a performance
        ///     improvement for reading large BLOBS.
        ///     <b>Default</b>: true
        /// </summary>
        public override bool SequentialAccess { get; set; } = true;

        /// <summary>
        ///     <para>
        ///         By default, string parameter size in ADO.NET queries are set dynamically based on current parameter
        ///         value size. If this parameter set to true, column sizes are loaded on journal startup from database schema, and 
        ///         string parameters have constant size which equals to corresponding column size.
        ///     </para>
        ///     <b>Default</b>: false
        /// </summary>
        public bool UseConstantParameterSize { get; set; } = false;

        public override Config DefaultConfig => SqlServerJournalOptions.Default;

        protected override StringBuilder Build(StringBuilder sb)
        {
            sb.AppendLine("class = \"Akka.Persistence.SqlServer.Snapshot.SqlServerSnapshotStore, Akka.Persistence.SqlServer\"");
            sb.AppendLine("plugin-dispatcher = \"akka.actor.default-dispatcher\"");
            sb.AppendLine($"use-constant-parameter-size = {UseConstantParameterSize.ToHocon()}");
            
            return base.Build(sb);
        }
    }
}