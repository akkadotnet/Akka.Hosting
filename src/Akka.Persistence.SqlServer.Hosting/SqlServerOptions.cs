// -----------------------------------------------------------------------
//  <copyright file="SqlServerJournalOptions.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using System;
using System.Text;
using Akka.Configuration;
using Akka.Hosting;

namespace Akka.Persistence.SqlServer.Hosting
{
    public sealed class SqlServerJournalOptions
    {
        /// <summary>
        ///     Connection string used for database access
        /// </summary>
        public string ConnectionString { get; set; }
        
        /// <summary>
        ///     <para>
        ///         SQL commands timeout.
        ///     </para>
        ///     <b>Default</b>: 30 seconds
        /// </summary>
        public TimeSpan? ConnectionTimeout { get; set; }
        
        /// <summary>
        ///     <para>
        ///         SQL server schema name to table corresponding with persistent journal.
        ///     </para>
        ///     <b>Default</b>: "dbo"
        /// </summary>
        public string SchemaName { get; set; }
        
        /// <summary>
        ///     <para>
        ///         SQL server table corresponding with persistent journal.
        ///     </para>
        ///     <b>Default</b>: "EventJournal"
        /// </summary>
        public string TableName { get; set; }
        
        /// <summary>
        ///     <para>
        ///         Should corresponding journal table be initialized automatically.
        ///     </para>
        ///     <b>Default</b>: false
        /// </summary>
        public bool? AutoInitialize { get; set; }
        
        /// <summary>
        ///     <para>
        ///         SQL server table corresponding with persistent journal metadata.
        ///     </para>
        ///     <b>Default</b>: "Metadata"
        /// </summary>
        public string MetadataTableName { get; set; }
        
        /// <summary>
        ///     <para>
        ///         Uses the CommandBehavior.SequentialAccess when creating the command, providing a performance
        ///         improvement for reading large BLOBS.
        ///     </para>
        ///     <b>Default</b>: true
        /// </summary>
        public bool? SequentialAccess { get; set; }
        
        /// <summary>
        ///     <para>
        ///         By default, string parameter size in ADO.NET queries are set dynamically based on current parameter
        ///         value size.
        ///         If this parameter set to true, column sizes are loaded on journal startup from database schema, and 
        ///         string parameters have constant size which equals to corresponding column size.
        ///     </para>
        ///     <b>Default</b>: false
        /// </summary>
        public bool? UseConstantParameterSize { get; set; }
        
        /// <summary>
        /// <para>
        ///     The SQL write journal is notifying the query side as soon as things
        ///     are persisted, but for efficiency reasons the query side retrieves the events 
        ///     in batches that sometimes can be delayed up to the configured <see cref="QueryRefreshInterval"/>.
        /// </para>
        ///     <b>Default</b>: 3 seconds
        /// </summary>
        public TimeSpan? QueryRefreshInterval { get; set; }
        
        internal Config ToConfig()
        {
            var sb = new StringBuilder()
                .AppendLine("akka.persistence.journal.plugin = \"akka.persistence.journal.sql-server\"");
            
            if (QueryRefreshInterval != null)
                sb.AppendFormat("akka.persistence.query.journal.sql.refresh-interval = {0}\n", QueryRefreshInterval.ToHocon());

            var innerSb = new StringBuilder();
            if (ConnectionString != null)
                innerSb.AppendFormat("connection-string = {0}\n", ConnectionString.ToHocon());

            if (ConnectionTimeout != null)
                innerSb.AppendFormat("connection-timeout = {0}\n", ConnectionTimeout.ToHocon());

            if (SchemaName != null)
                innerSb.AppendFormat("schema-name = {0}\n", SchemaName.ToHocon());
            
            if (TableName != null)
                innerSb.AppendFormat("table-name = {0}\n", TableName.ToHocon());
            
            if (AutoInitialize != null)
                innerSb.AppendFormat("auto-initialize = {0}\n", AutoInitialize.ToHocon());
        
            if (MetadataTableName != null)
                innerSb.AppendFormat("metadata-table-name = {0}\n", MetadataTableName.ToHocon());
            
            if (SequentialAccess != null)
                innerSb.AppendFormat("sequential-access = {0}\n", SequentialAccess.ToHocon());
            
            if(UseConstantParameterSize != null)
                innerSb.AppendFormat("use-constant-parameter-size = {0}\n", UseConstantParameterSize.ToHocon());

            if (innerSb.Length > 0)
            {
                sb.AppendLine("akka.persistence.journal.sql-server {")
                    .Append(innerSb)
                    .AppendLine("}");
            }

            return sb.ToString();
        }
    }

    public sealed class SqlServerSnapshotOptions
    {
        /// <summary>
        ///     Connection string used for database access.
        /// </summary>
        public string ConnectionString { get; set; }
        
        /// <summary>
        ///     SQL commands timeout.
        ///     <b>Default</b>: 30 seconds
        /// </summary>
        public TimeSpan? ConnectionTimeout { get; set; }
        
        /// <summary>
        ///     SQL server schema name to table corresponding with persistent snapshot store.
        ///     <b>Default</b>: "dbo"
        /// </summary>
        public string SchemaName { get; set; }
        
        /// <summary>
        ///     SQL server table corresponding with persistent snapshot store.
        ///     <b>Default</b>: "EventJournal"
        /// </summary>
        public string TableName { get; set; }
        
        /// <summary>
        ///     Should corresponding snapshot store table be initialized automatically.
        ///     <b>Default</b>: false
        /// </summary>
        public bool? AutoInitialize { get; set; }
        
        /// <summary>
        ///     Uses the CommandBehavior.SequentialAccess when creating the command, providing a performance
        ///     improvement for reading large BLOBS.
        ///     <b>Default</b>: true
        /// </summary>
        public bool? SequentialAccess { get; set; }
        
        /// <summary>
        ///     By default, string parameter size in ADO.NET queries are set dynamically based on current parameter
        ///     value size.
        ///     If this parameter set to true, column sizes are loaded on journal startup from database schema, and 
        ///     string parameters have constant size which equals to corresponding column size.
        ///     <b>Default</b>: false
        /// </summary>
        public bool? UseConstantParameterSize { get; set; }

        internal Config ToConfig()
        {
            var sb = new StringBuilder()
                .AppendLine("akka.persistence.snapshot-store.plugin = \"akka.persistence.snapshot-store.sql-server\"");
            
            var innerSb = new StringBuilder();
            if (ConnectionString != null)
                innerSb.AppendFormat("connection-string = {0}\n", ConnectionString.ToHocon());

            if (ConnectionTimeout != null)
                innerSb.AppendFormat("connection-timeout = {0}\n", ConnectionTimeout.ToHocon());

            if (SchemaName != null)
                innerSb.AppendFormat("schema-name = {0}\n", SchemaName.ToHocon());
            
            if (TableName != null)
                innerSb.AppendFormat("table-name = {0}\n", TableName.ToHocon());
            
            if (AutoInitialize != null)
                innerSb.AppendFormat("auto-initialize = {0}\n", AutoInitialize.ToHocon());
        
            if (SequentialAccess != null)
                innerSb.AppendFormat("sequential-access = {0}\n", SequentialAccess.ToHocon());
            
            if(UseConstantParameterSize != null)
                innerSb.AppendFormat("use-constant-parameter-size = {0}\n", UseConstantParameterSize.ToHocon());

            if (innerSb.Length > 0)
            {
                sb.AppendLine("akka.persistence.snapshot-store.sql-server {")
                    .Append(innerSb)
                    .AppendLine("}");
            }

            return sb.ToString();
        }        
    }
}