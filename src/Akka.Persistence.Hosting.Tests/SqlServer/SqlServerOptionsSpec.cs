// -----------------------------------------------------------------------
//  <copyright file="SqlServerOptionsSpec.cs" company="Akka.NET Project">
//      Copyright (C) 2009-2022 Lightbend Inc. <http://www.lightbend.com>
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using System;
using Akka.Persistence.Query.Sql;
using Akka.Persistence.SqlServer;
using Akka.Persistence.SqlServer.Hosting;
using FluentAssertions;
using FluentAssertions.Extensions;
using Xunit;

namespace Akka.Persistence.Hosting.Tests.SqlServer;

public class SqlServerOptionsSpec
{
    #region Journal unit tests

    [Fact(DisplayName = "Empty SqlServerJournalOptions should generate empty config")]
    public void EmptyJournalOptionsTest()
    {
        var options = new SqlServerJournalOptions();
        var config = options.ToConfig();

        config.GetString("akka.persistence.journal.plugin").Should().Be("akka.persistence.journal.sql-server");
        config.HasPath("akka.persistence.query.journal.sql.refresh-interval").Should().BeFalse();
        config.HasPath("akka.persistence.journal.sql-server").Should().BeFalse();
    }

    [Fact(DisplayName = "Empty SqlServerJournalOptions with default fallback should return default config")]
    public void DefaultJournalOptionsTest()
    {
        var options = new SqlServerJournalOptions();
        var baseConfig = options.ToConfig()
            .WithFallback(SqlServerPersistence.DefaultConfiguration())
            .WithFallback(SqlReadJournal.DefaultConfiguration());

        baseConfig.GetString("akka.persistence.journal.plugin").Should().Be("akka.persistence.journal.sql-server");
        
        baseConfig.GetTimeSpan("akka.persistence.query.journal.sql.refresh-interval").Should()
            .Be(3.Seconds());
        
        var config = baseConfig.GetConfig("akka.persistence.journal.sql-server");
        config.Should().NotBeNull();
        config.GetString("connection-string").Should().BeEmpty();
        config.GetTimeSpan("connection-timeout").Should().Be(30.Seconds());
        config.GetString("schema-name").Should().Be("dbo");
        config.GetString("table-name").Should().Be("EventJournal");
        config.GetBoolean("auto-initialize").Should().BeFalse();
        config.GetString("metadata-table-name").Should().Be("Metadata");
        config.GetBoolean("sequential-access").Should().BeTrue();
        config.GetBoolean("use-constant-parameter-size").Should().BeFalse();
    }
    
    [Fact(DisplayName = "SqlServerJournalOptions should generate proper config")]
    public void JournalOptionsTest()
    {
        var options = new SqlServerJournalOptions
        {
            AutoInitialize = true,
            ConnectionString = "testConnection",
            ConnectionTimeout = 1.Seconds(),
            MetadataTableName = "testMetadata",
            QueryRefreshInterval = 2.Seconds(),
            SchemaName = "testSchema",
            SequentialAccess = false,
            TableName = "testTable",
            UseConstantParameterSize = true
        };
        var baseConfig = options.ToConfig()
            .WithFallback(SqlServerPersistence.DefaultConfiguration())
            .WithFallback(SqlReadJournal.DefaultConfiguration());
        
        baseConfig.GetString("akka.persistence.journal.plugin").Should().Be("akka.persistence.journal.sql-server");

        baseConfig.GetTimeSpan("akka.persistence.query.journal.sql.refresh-interval").Should()
            .Be(options.QueryRefreshInterval.Value);
        
        var config = baseConfig.GetConfig("akka.persistence.journal.sql-server");
        config.Should().NotBeNull();
        config.GetString("connection-string").Should().Be(options.ConnectionString);
        config.GetTimeSpan("connection-timeout").Should().Be(options.ConnectionTimeout.Value);
        config.GetString("schema-name").Should().Be(options.SchemaName);
        config.GetString("table-name").Should().Be(options.TableName);
        config.GetBoolean("auto-initialize").Should().Be(options.AutoInitialize.Value);
        config.GetString("metadata-table-name").Should().Be(options.MetadataTableName);
        config.GetBoolean("sequential-access").Should().Be(options.SequentialAccess.Value);
        config.GetBoolean("use-constant-parameter-size").Should().Be(options.UseConstantParameterSize.Value);
    }    

    #endregion

    #region Snapshot unit tests

    [Fact(DisplayName = "Empty SqlServerSnapshotOptions should generate empty config")]
    public void EmptySnapshotOptionsTest()
    {
        var options = new SqlServerSnapshotOptions();
        var config = options.ToConfig();

        config.GetString("akka.persistence.snapshot-store.plugin").Should().Be("akka.persistence.snapshot-store.sql-server");
        config.HasPath("akka.persistence.snapshot-store.sql-server").Should().BeFalse();
    }

    [Fact(DisplayName = "Empty SqlServerSnapshotOptions with default fallback should return default config")]
    public void DefaultSnapshotOptionsTest()
    {
        var options = new SqlServerSnapshotOptions();
        var baseConfig = options.ToConfig()
            .WithFallback(SqlServerPersistence.DefaultConfiguration());

        baseConfig.GetString("akka.persistence.snapshot-store.plugin").Should().Be("akka.persistence.snapshot-store.sql-server");
        
        var config = baseConfig.GetConfig("akka.persistence.snapshot-store.sql-server");
        config.Should().NotBeNull();
        config.GetString("connection-string").Should().BeEmpty();
        config.GetTimeSpan("connection-timeout").Should().Be(30.Seconds());
        config.GetString("schema-name").Should().Be("dbo");
        config.GetString("table-name").Should().Be("SnapshotStore");
        config.GetBoolean("auto-initialize").Should().BeFalse();
        config.GetBoolean("sequential-access").Should().BeTrue();
        config.GetBoolean("use-constant-parameter-size").Should().BeFalse();
    }
    
    [Fact(DisplayName = "SqlServerSnapshotOptions should generate proper config")]
    public void JournalSnapshotTest()
    {
        var options = new SqlServerSnapshotOptions
        {
            AutoInitialize = true,
            ConnectionString = "testConnection",
            ConnectionTimeout = 1.Seconds(),
            SchemaName = "testSchema",
            SequentialAccess = false,
            TableName = "testTable",
            UseConstantParameterSize = true
        };
        var baseConfig = options.ToConfig()
            .WithFallback(SqlServerPersistence.DefaultConfiguration());
        
        baseConfig.GetString("akka.persistence.snapshot-store.plugin").Should().Be("akka.persistence.snapshot-store.sql-server");

        var config = baseConfig.GetConfig("akka.persistence.snapshot-store.sql-server");
        config.Should().NotBeNull();
        config.GetString("connection-string").Should().Be(options.ConnectionString);
        config.GetTimeSpan("connection-timeout").Should().Be(options.ConnectionTimeout.Value);
        config.GetString("schema-name").Should().Be(options.SchemaName);
        config.GetString("table-name").Should().Be(options.TableName);
        config.GetBoolean("auto-initialize").Should().Be(options.AutoInitialize.Value);
        config.GetBoolean("sequential-access").Should().Be(options.SequentialAccess.Value);
        config.GetBoolean("use-constant-parameter-size").Should().Be(options.UseConstantParameterSize.Value);
    }

    #endregion
}