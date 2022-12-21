// -----------------------------------------------------------------------
//  <copyright file="SqlServerOptionsSpec.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using System;
using System.IO;
using System.Text;
using Akka.Configuration;
using Akka.Persistence.Query.Sql;
using Akka.Persistence.SqlServer;
using Akka.Persistence.SqlServer.Hosting;
using Akka.Util;
using FluentAssertions;
using FluentAssertions.Extensions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Akka.Persistence.Hosting.Tests.SqlServer;

public class SqlServerOptionsSpec
{
    #region Journal unit tests

    [Fact(DisplayName = "SqlServerJournalOptions as default plugin should generate plugin setting")]
    public void DefaultPluginJournalOptionsTest()
    {
        var options = new SqlServerJournalOptions(true);
        var config = options.ToConfig();

        config.GetString("akka.persistence.journal.plugin").Should().Be("akka.persistence.journal.sql-server");
        config.HasPath("akka.persistence.journal.sql-server").Should().BeTrue();
    }

    [Fact(DisplayName = "Empty SqlServerJournalOptions should equal empty config with default fallback")]
    public void DefaultJournalOptionsTest()
    {
        var options = new SqlServerJournalOptions(false);
        var emptyRootConfig = options.ToConfig().WithFallback(options.DefaultConfig);
        var baseRootConfig = Config.Empty
            .WithFallback(SqlServerPersistence.DefaultConfiguration())
            .WithFallback(SqlReadJournal.DefaultConfiguration());
        
        AssertString(emptyRootConfig, baseRootConfig, "akka.persistence.journal.plugin");
        AssertTimespan(emptyRootConfig, baseRootConfig, "akka.persistence.query.journal.sql.refresh-interval");
        
        var config = emptyRootConfig.GetConfig("akka.persistence.journal.sql-server");
        var baseConfig = baseRootConfig.GetConfig("akka.persistence.journal.sql-server");
        config.Should().NotBeNull();
        baseConfig.Should().NotBeNull();

        AssertJournalConfig(config, baseConfig);
    }
    
    [Fact(DisplayName = "Empty SqlServerJournalOptions with custom identifier should equal empty config with default fallback")]
    public void CustomIdJournalOptionsTest()
    {
        var options = new SqlServerJournalOptions(false, "custom");
        var emptyRootConfig = options.ToConfig().WithFallback(options.DefaultConfig);
        var baseRootConfig = Config.Empty
            .WithFallback(SqlServerPersistence.DefaultConfiguration())
            .WithFallback(SqlReadJournal.DefaultConfiguration());
        
        AssertString(emptyRootConfig, baseRootConfig, "akka.persistence.journal.plugin");
        AssertTimespan(emptyRootConfig, baseRootConfig, "akka.persistence.query.journal.sql.refresh-interval");
        
        var config = emptyRootConfig.GetConfig("akka.persistence.journal.custom");
        var baseConfig = baseRootConfig.GetConfig("akka.persistence.journal.sql-server");
        config.Should().NotBeNull();
        baseConfig.Should().NotBeNull();

        AssertJournalConfig(config, baseConfig);
    }
    
    [Fact(DisplayName = "SqlServerJournalOptions should generate proper config")]
    public void JournalOptionsTest()
    {
        var options = new SqlServerJournalOptions(true)
        {
            Identifier = "custom",
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
        options.AdapterBuilder.AddWriteEventAdapter<EventAdapterSpecs.EventMapper1>("mapper1", new [] { typeof(EventAdapterSpecs.Event1) });
        options.AdapterBuilder.AddReadEventAdapter<EventAdapterSpecs.ReadAdapter>("reader1", new [] { typeof(EventAdapterSpecs.Event1) });
        options.AdapterBuilder.AddEventAdapter<EventAdapterSpecs.ComboAdapter>("combo", boundTypes: new [] { typeof(EventAdapterSpecs.Event2) });
        options.AdapterBuilder.AddWriteEventAdapter<EventAdapterSpecs.Tagger>("tagger", boundTypes: new [] { typeof(EventAdapterSpecs.Event1), typeof(EventAdapterSpecs.Event2) });
        
        var baseConfig = options.ToConfig();
        
        baseConfig.GetString("akka.persistence.journal.plugin").Should().Be("akka.persistence.journal.custom");

        baseConfig.GetTimeSpan("akka.persistence.query.journal.sql.refresh-interval").Should()
            .Be(options.QueryRefreshInterval);
        
        var config = baseConfig.GetConfig("akka.persistence.journal.custom");
        config.Should().NotBeNull();
        config.GetString("connection-string").Should().Be(options.ConnectionString);
        config.GetTimeSpan("connection-timeout").Should().Be(options.ConnectionTimeout);
        config.GetString("schema-name").Should().Be(options.SchemaName);
        config.GetString("table-name").Should().Be(options.TableName);
        config.GetBoolean("auto-initialize").Should().Be(options.AutoInitialize);
        config.GetString("metadata-table-name").Should().Be(options.MetadataTableName);
        config.GetBoolean("sequential-access").Should().Be(options.SequentialAccess);
        config.GetBoolean("use-constant-parameter-size").Should().Be(options.UseConstantParameterSize);
        
        config.GetStringList($"event-adapter-bindings.\"{typeof(EventAdapterSpecs.Event1).TypeQualifiedName()}\"").Should()
            .BeEquivalentTo("mapper1", "reader1", "tagger");
        config.GetStringList($"event-adapter-bindings.\"{typeof(EventAdapterSpecs.Event2).TypeQualifiedName()}\"").Should()
            .BeEquivalentTo("combo", "tagger");
        
        config.GetString("event-adapters.mapper1").Should().Be(typeof(EventAdapterSpecs.EventMapper1).TypeQualifiedName());
        config.GetString("event-adapters.reader1").Should().Be(typeof(EventAdapterSpecs.ReadAdapter).TypeQualifiedName());
        config.GetString("event-adapters.combo").Should().Be(typeof(EventAdapterSpecs.ComboAdapter).TypeQualifiedName());
        config.GetString("event-adapters.tagger").Should().Be(typeof(EventAdapterSpecs.Tagger).TypeQualifiedName());
    }

    [Fact(DisplayName = "SqlServerJournalOptions should be bindable to IConfiguration")]
    // ReSharper disable once InconsistentNaming
    public void JournalOptionsIConfigurationBindingTest()
    {
        const string json = @"
{
  ""Logging"": {
    ""LogLevel"": {
      ""Default"": ""Information"",
      ""Microsoft.AspNetCore"": ""Warning""
    }
  },
  ""Akka"": {
    ""JournalOptions"": {
      ""UseConstantParameterSize"": true,
      ""QueryRefreshInterval"": ""00:00:05"",

      ""ConnectionString"": ""Server=localhost,1533;Database=Akka;User Id=sa;"",
      ""ConnectionTimeout"": ""00:00:55"",
      ""SchemaName"": ""schema"",
      ""TableName"" : ""journal"",
      ""MetadataTableName"": ""meta"",
      ""SequentialAccess"": false,

      ""IsDefaultPlugin"": false,
      ""Identifier"": ""custom"",
      ""AutoInitialize"": true,
      ""Serializer"": ""hyperion""
    }
  }
}";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var jsonConfig = new ConfigurationBuilder().AddJsonStream(stream).Build();
        
        var options = jsonConfig.GetSection("Akka:JournalOptions").Get<SqlServerJournalOptions>();
        options.IsDefaultPlugin.Should().BeFalse();
        options.Identifier.Should().Be("custom");
        options.AutoInitialize.Should().BeTrue();
        options.Serializer.Should().Be("hyperion");
        options.ConnectionString.Should().Be("Server=localhost,1533;Database=Akka;User Id=sa;");
        options.ConnectionTimeout.Should().Be(55.Seconds());
        options.SchemaName.Should().Be("schema");
        options.TableName.Should().Be("journal");
        options.MetadataTableName.Should().Be("meta");
        options.SequentialAccess.Should().BeFalse();

        options.UseConstantParameterSize.Should().BeTrue();
        options.QueryRefreshInterval.Should().Be(5.Seconds());
    }
    #endregion

    #region Snapshot unit tests

    [Fact(DisplayName = "SqlServerSnapshotOptions as default plugin should generate plugin setting")]
    public void DefaultPluginSnapshotOptionsTest()
    {
        var options = new SqlServerSnapshotOptions(true);
        var config = options.ToConfig();

        config.GetString("akka.persistence.snapshot-store.plugin").Should().Be("akka.persistence.snapshot-store.sql-server");
        config.HasPath("akka.persistence.snapshot-store.sql-server").Should().BeTrue();
    }

    [Fact(DisplayName = "Empty SqlServerSnapshotOptions should equal empty config with default fallback")]
    public void DefaultSnapshotOptionsTest()
    {
        var options = new SqlServerSnapshotOptions(false);
        var emptyRootConfig = options.ToConfig();
        var baseRootConfig = Config.Empty
            .WithFallback(SqlServerPersistence.DefaultConfiguration());
        
        AssertString(emptyRootConfig, baseRootConfig, "akka.persistence.snapshot-store.plugin");
        
        var config = emptyRootConfig.GetConfig("akka.persistence.snapshot-store.sql-server");
        var baseConfig = baseRootConfig.GetConfig("akka.persistence.snapshot-store.sql-server");
        config.Should().NotBeNull();
        baseConfig.Should().NotBeNull();
        
        AssertSnapshotConfig(config, baseConfig);
    }
    
    [Fact(DisplayName = "Empty SqlServerSnapshotOptions with custom identifier should equal empty config with default fallback")]
    public void CustomIdSnapshotOptionsTest()
    {
        var options = new SqlServerSnapshotOptions(false, "custom");
        var emptyRootConfig = options.ToConfig();
        var baseRootConfig = Config.Empty
            .WithFallback(SqlServerPersistence.DefaultConfiguration());
        
        AssertString(emptyRootConfig, baseRootConfig, "akka.persistence.snapshot-store.plugin");
        AssertTimespan(emptyRootConfig, baseRootConfig, "akka.persistence.query.snapshot-store.sql.refresh-interval");
        
        var config = emptyRootConfig.GetConfig("akka.persistence.snapshot-store.custom");
        var baseConfig = baseRootConfig.GetConfig("akka.persistence.snapshot-store.sql-server");
        config.Should().NotBeNull();
        baseConfig.Should().NotBeNull();

        AssertSnapshotConfig(config, baseConfig);
    }
    
    [Fact(DisplayName = "SqlServerSnapshotOptions should generate proper config")]
    public void SnapshotOptionsTest()
    {
        var options = new SqlServerSnapshotOptions(true)
        {
            Identifier = "custom",
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
        
        baseConfig.GetString("akka.persistence.snapshot-store.plugin").Should().Be("akka.persistence.snapshot-store.custom");

        var config = baseConfig.GetConfig("akka.persistence.snapshot-store.custom");
        config.Should().NotBeNull();
        config.GetString("connection-string").Should().Be(options.ConnectionString);
        config.GetTimeSpan("connection-timeout").Should().Be(options.ConnectionTimeout);
        config.GetString("schema-name").Should().Be(options.SchemaName);
        config.GetString("table-name").Should().Be(options.TableName);
        config.GetBoolean("auto-initialize").Should().Be(options.AutoInitialize);
        config.GetBoolean("sequential-access").Should().Be(options.SequentialAccess);
        config.GetBoolean("use-constant-parameter-size").Should().Be(options.UseConstantParameterSize);
    }

    [Fact(DisplayName = "SqlServerSnapshotOptions should be bindable to IConfiguration")]
    public void SnapshotOptionsIConfigurationBindingTest()
    {
        const string json = @"
{
  ""Logging"": {
    ""LogLevel"": {
      ""Default"": ""Information"",
      ""Microsoft.AspNetCore"": ""Warning""
    }
  },
  ""Akka"": {
    ""SnapshotOptions"": {
      ""UseConstantParameterSize"": true,
      ""QueryRefreshInterval"": ""00:00:05.000"",

      ""ConnectionString"": ""Server=localhost,1533;Database=Akka;User Id=sa;"",
      ""ConnectionTimeout"": ""00:00:55"",
      ""SchemaName"": ""schema"",
      ""TableName"" : ""snapshot"",
      ""SequentialAccess"": false,

      ""IsDefaultPlugin"": false,
      ""Identifier"": ""custom"",
      ""AutoInitialize"": true,
      ""Serializer"": ""hyperion""
    }
  }
}";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var jsonConfig = new ConfigurationBuilder().AddJsonStream(stream).Build();
        
        var options = jsonConfig.GetSection("Akka:SnapshotOptions").Get<SqlServerSnapshotOptions>();
        options.IsDefaultPlugin.Should().BeFalse();
        options.Identifier.Should().Be("custom");
        options.AutoInitialize.Should().BeTrue();
        options.Serializer.Should().Be("hyperion");
        options.ConnectionString.Should().Be("Server=localhost,1533;Database=Akka;User Id=sa;");
        options.ConnectionTimeout.Should().Be(55.Seconds());
        options.SchemaName.Should().Be("schema");
        options.TableName.Should().Be("snapshot");
        options.SequentialAccess.Should().BeFalse();

        options.UseConstantParameterSize.Should().BeTrue();
    }
    #endregion

    private static void AssertJournalConfig(Config underTest, Config reference)
    {
        AssertString(underTest, reference, "class");
        AssertString(underTest, reference, "plugin-dispatcher");
        AssertString(underTest, reference, "connection-string");
        AssertTimespan(underTest, reference, "connection-timeout");
        AssertString(underTest, reference, "schema-name");
        AssertString(underTest, reference, "table-name");
        AssertBoolean(underTest, reference, "auto-initialize");
        AssertString(underTest, reference, "timestamp-provider");
        AssertString(underTest, reference, "metadata-table-name");
        AssertBoolean(underTest, reference, "sequential-access");
        AssertBoolean(underTest, reference, "use-constant-parameter-size");
    }

    private static void AssertSnapshotConfig(Config underTest, Config reference)
    {
        AssertString(underTest, reference, "class");
        AssertString(underTest, reference, "plugin-dispatcher");
        AssertString(underTest, reference, "connection-string");
        AssertTimespan(underTest, reference, "connection-timeout");
        AssertString(underTest, reference, "schema-name");
        AssertString(underTest, reference, "table-name");
        AssertBoolean(underTest, reference, "auto-initialize");
        AssertBoolean(underTest, reference, "sequential-access");
        AssertBoolean(underTest, reference, "use-constant-parameter-size");
    }
    
    private static void AssertString(Config underTest, Config reference, string hoconPath)
    {
        underTest.GetString(hoconPath).Should().Be(reference.GetString(hoconPath));
    }
    private static void AssertTimespan(Config underTest, Config reference, string hoconPath)
    {
        underTest.GetTimeSpan(hoconPath).Should().Be(reference.GetTimeSpan(hoconPath));
    }
    private static void AssertBoolean(Config underTest, Config reference, string hoconPath)
    {
        underTest.GetBoolean(hoconPath).Should().Be(reference.GetBoolean(hoconPath));
    }
}