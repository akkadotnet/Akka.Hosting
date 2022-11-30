// -----------------------------------------------------------------------
//  <copyright file="SqlSnapshotOptions.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using System;
using System.Text;
using Akka.Hosting;

#nullable enable
namespace Akka.Persistence.Hosting
{
    public abstract class SqlSnapshotOptions: SnapshotOptions
    {
        protected SqlSnapshotOptions(bool isDefault): base(isDefault)
        {
        }

        /// <summary>
        ///     Connection string used for database access.
        /// </summary>
        public string ConnectionString { get; set; } = "";

        /// <summary>
        ///     SQL commands timeout.
        ///     <b>Default</b>: 30 seconds
        /// </summary>
        public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(30);

        public abstract string SchemaName { get; set; }

        public abstract string TableName { get; set; }

        /// <summary>
        ///     Uses the CommandBehavior.SequentialAccess when creating the command, providing a performance
        ///     improvement for reading large BLOBS.
        ///     <b>Default</b>: false
        /// </summary>
        public abstract bool SequentialAccess { get; set; }

        protected override StringBuilder Build(StringBuilder sb)
        {
            sb.AppendLine($"connection-string = {ConnectionString.ToHocon()}");
            sb.AppendLine($"connection-timeout = {ConnectionTimeout.ToHocon()}");
            sb.AppendLine($"schema-name = {SchemaName.ToHocon()}");
            sb.AppendLine($"table-name = {TableName.ToHocon()}");
            sb.AppendLine($"sequential-access = {SequentialAccess.ToHocon()}");

            return base.Build(sb);
        }

    }
}