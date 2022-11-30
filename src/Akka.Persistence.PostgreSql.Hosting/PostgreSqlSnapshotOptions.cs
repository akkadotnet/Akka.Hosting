// -----------------------------------------------------------------------
//  <copyright file="PostgreSqlSnapshotOptions.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using System;
using System.Text;
using Akka.Configuration;
using Akka.Persistence.Hosting;

#nullable enable
namespace Akka.Persistence.PostgreSql.Hosting
{
    public sealed class PostgreSqlSnapshotOptions: SqlSnapshotOptions
    {
        public PostgreSqlSnapshotOptions(bool isDefaultPlugin, string identifier = "postgresql") : base(isDefaultPlugin)
        {
            Identifier = identifier;
        }
        
        /// <summary>
        ///     <para>
        ///         The plugin identifier for this persistence plugin
        ///     </para>
        ///     <b>Default</b>: "postgresql"
        /// </summary>
        public override string Identifier { get; set; }
        
        /// <summary>
        ///     <para>
        ///         PostgreSQL schema name to table corresponding with persistent snapshot store.
        ///     </para>
        ///     <b>Default</b>: "public"
        /// </summary>
        public override string SchemaName { get; set; } = "public";
        
        /// <summary>
        ///     <para>
        ///         PostgreSQL table corresponding with persistent snapshot store.
        ///     </para>
        ///     <b>Default</b>: "snapshot_store"
        /// </summary>
        public override string TableName { get; set; } = "snapshot_store";
        
        /// <summary>
        ///     Uses the CommandBehavior.SequentialAccess when creating the command, providing a performance
        ///     improvement for reading large BLOBS.
        ///     <b>Default</b>: false
        /// </summary>
        public override bool SequentialAccess { get; set; } = false;

        /// <summary>
        ///     <para>
        ///         Postgres data type for the payload column
        ///     </para>
        ///     <b>Default</b>: <see cref="StoredAsType.ByteA"/>
        /// </summary>
        public StoredAsType StoredAs { get; set; } = StoredAsType.ByteA;

        public override Config DefaultConfig => PostgreSqlJournalOptions.Default;

        protected override StringBuilder Build(StringBuilder sb)
        {
            sb.AppendLine("class = \"Akka.Persistence.PostgreSql.Snapshot.PostgreSqlSnapshotStore, Akka.Persistence.PostgreSql\"");
            sb.AppendLine("plugin-dispatcher = \"akka.actor.default-dispatcher\"");
            sb.AppendLine($"stored-as = {StoredAs.ToHocon()}");

            return base.Build(sb);
        }
    }
}