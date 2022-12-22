// -----------------------------------------------------------------------
//  <copyright file="PostgreSqlOptionsSpec.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using System.IO;
using System.Text;
using Akka.Configuration;
using Akka.Persistence.PostgreSql;
using Akka.Persistence.PostgreSql.Hosting;
using Akka.Persistence.Query.Sql;
using Akka.Util;
using FluentAssertions;
using FluentAssertions.Extensions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Akka.Persistence.Hosting.Tests.PostgreSql;

public class PostgreSqlOptionsSpec
{
    #region Journal unit tests

    [Fact(DisplayName = "PostgreSqlJournalOptions as default plugin should generate plugin setting")]
    public void DefaultPluginJournalOptionsTest()
    {
        var options = new PostgreSqlJournalOptions(true);
        var config = options.ToConfig();

        config.GetString("akka.persistence.journal.plugin").Should().Be("akka.persistence.journal.postgresql");
        config.HasPath("akka.persistence.journal.postgresql").Should().BeTrue();
    }

    [Fact(DisplayName = "Empty PostgreSqlJournalOptions should equal empty config with default fallback")]
    public void DefaultJournalOptionsTest()
    {
        var options = new PostgreSqlJournalOptions(false);
        var emptyRootConfig = options.ToConfig().WithFallback(options.DefaultConfig);
        var baseRootConfig = Config.Empty
            .WithFallback(PostgreSqlPersistence.DefaultConfiguration());
        
        AssertString(emptyRootConfig, baseRootConfig, "akka.persistence.journal.plugin");
        
        var config = emptyRootConfig.GetConfig("akka.persistence.journal.postgresql");
        var baseConfig = baseRootConfig.GetConfig("akka.persistence.journal.postgresql");
        config.Should().NotBeNull();
        baseConfig.Should().NotBeNull();

        AssertJournalConfig(config, baseConfig);
    }
    
    [Fact(DisplayName = "Empty PostgreSqlJournalOptions with custom identifier should equal empty config with default fallback")]
    public void CustomIdJournalOptionsTest()
    {
        var options = new PostgreSqlJournalOptions(false, "custom");
        var emptyRootConfig = options.ToConfig().WithFallback(options.DefaultConfig);
        var baseRootConfig = Config.Empty
            .WithFallback(PostgreSqlPersistence.DefaultConfiguration());
        
        AssertString(emptyRootConfig, baseRootConfig, "akka.persistence.journal.plugin");
        
        var config = emptyRootConfig.GetConfig("akka.persistence.journal.custom");
        var baseConfig = baseRootConfig.GetConfig("akka.persistence.journal.postgresql");
        config.Should().NotBeNull();
        baseConfig.Should().NotBeNull();

        AssertJournalConfig(config, baseConfig);
    }
    
    [Fact(DisplayName = "PostgreSqlJournalOptions should generate proper config")]
    public void JournalOptionsTest()
    {
        var options = new PostgreSqlJournalOptions(true)
        {
            Identifier = "custom",
            AutoInitialize = true,
            ConnectionString = "testConnection",
            ConnectionTimeout = 1.Seconds(),
            MetadataTableName = "testMetadata",
            SchemaName = "testSchema",
            SequentialAccess = false,
            TableName = "testTable",
            StoredAs = StoredAsType.Json,
            UseBigIntIdentityForOrderingColumn = true
        };
        options.Adapters.AddWriteEventAdapter<EventAdapterSpecs.EventMapper1>("mapper1", new [] { typeof(EventAdapterSpecs.Event1) });
        options.Adapters.AddReadEventAdapter<EventAdapterSpecs.ReadAdapter>("reader1", new [] { typeof(EventAdapterSpecs.Event1) });
        options.Adapters.AddEventAdapter<EventAdapterSpecs.ComboAdapter>("combo", boundTypes: new [] { typeof(EventAdapterSpecs.Event2) });
        options.Adapters.AddWriteEventAdapter<EventAdapterSpecs.Tagger>("tagger", boundTypes: new [] { typeof(EventAdapterSpecs.Event1), typeof(EventAdapterSpecs.Event2) });
        
        var baseConfig = options.ToConfig();
        
        baseConfig.GetString("akka.persistence.journal.plugin").Should().Be("akka.persistence.journal.custom");

        var config = baseConfig.GetConfig("akka.persistence.journal.custom");
        config.Should().NotBeNull();
        config.GetString("connection-string").Should().Be(options.ConnectionString);
        config.GetTimeSpan("connection-timeout").Should().Be(options.ConnectionTimeout);
        config.GetString("schema-name").Should().Be(options.SchemaName);
        config.GetString("table-name").Should().Be(options.TableName);
        config.GetBoolean("auto-initialize").Should().Be(options.AutoInitialize);
        config.GetString("metadata-table-name").Should().Be(options.MetadataTableName);
        config.GetBoolean("sequential-access").Should().Be(options.SequentialAccess);
        config.GetString("stored-as").Should().Be(options.StoredAs.ToHocon());
        config.GetBoolean("use-bigint-identity-for-ordering-column").Should().Be(options.UseBigIntIdentityForOrderingColumn);
        
        config.GetStringList($"event-adapter-bindings.\"{typeof(EventAdapterSpecs.Event1).TypeQualifiedName()}\"").Should()
            .BeEquivalentTo("mapper1", "reader1", "tagger");
        config.GetStringList($"event-adapter-bindings.\"{typeof(EventAdapterSpecs.Event2).TypeQualifiedName()}\"").Should()
            .BeEquivalentTo("combo", "tagger");
        
        config.GetString("event-adapters.mapper1").Should().Be(typeof(EventAdapterSpecs.EventMapper1).TypeQualifiedName());
        config.GetString("event-adapters.reader1").Should().Be(typeof(EventAdapterSpecs.ReadAdapter).TypeQualifiedName());
        config.GetString("event-adapters.combo").Should().Be(typeof(EventAdapterSpecs.ComboAdapter).TypeQualifiedName());
        config.GetString("event-adapters.tagger").Should().Be(typeof(EventAdapterSpecs.Tagger).TypeQualifiedName());

    }

        const string Json = @"
{
  ""Logging"": {
    ""LogLevel"": {
      ""Default"": ""Information"",
      ""Microsoft.AspNetCore"": ""Warning""
    }
  },
  ""Akka"": {
    ""JournalOptions"": {
      ""StoredAs"": ""JsonB"",
      ""UseBigIntIdentityForOrderingColumn"": true,

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
    
    [Fact(DisplayName = "PostgreSqlJournalOptions should be bindable to IConfiguration")]
    public void JournalOptionsIConfigurationBindingTest()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(Json));
        var jsonConfig = new ConfigurationBuilder().AddJsonStream(stream).Build();
        
        var options = jsonConfig.GetSection("Akka:JournalOptions").Get<PostgreSqlJournalOptions>();
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

        options.StoredAs.Should().Be(StoredAsType.JsonB);
        options.UseBigIntIdentityForOrderingColumn.Should().BeTrue();
    }
    
    #endregion

    #region Snapshot unit tests

    [Fact(DisplayName = "PostgreSqlSnapshotOptions as default plugin should generate plugin setting")]
    public void DefaultPluginSnapshotOptionsTest()
    {
        var options = new PostgreSqlSnapshotOptions(true);
        var config = options.ToConfig();

        config.GetString("akka.persistence.snapshot-store.plugin").Should().Be("akka.persistence.snapshot-store.postgresql");
        config.HasPath("akka.persistence.snapshot-store.postgresql").Should().BeTrue();
    }

    [Fact(DisplayName = "Empty PostgreSqlSnapshotOptions with default fallback should return default config")]
    public void DefaultSnapshotOptionsTest()
    {
        var options = new PostgreSqlSnapshotOptions(false);
        var emptyRootConfig = options.ToConfig().WithFallback(options.DefaultConfig);
        var baseRootConfig = Config.Empty
            .WithFallback(PostgreSqlPersistence.DefaultConfiguration());
        
        AssertString(emptyRootConfig, baseRootConfig, "akka.persistence.snapshot-store.plugin");
        
        var config = emptyRootConfig.GetConfig("akka.persistence.snapshot-store.postgresql");
        var baseConfig = baseRootConfig.GetConfig("akka.persistence.snapshot-store.postgresql");
        config.Should().NotBeNull();
        baseConfig.Should().NotBeNull();

        AssertSnapshotConfig(config, baseConfig);
    }
    
    [Fact(DisplayName = "Empty PostgreSqlSnapshotOptions with custom identifier should equal empty config with default fallback")]
    public void CustomIdSnapshotOptionsTest()
    {
        var options = new PostgreSqlSnapshotOptions(false, "custom");
        var emptyRootConfig = options.ToConfig().WithFallback(options.DefaultConfig);
        var baseRootConfig = Config.Empty
            .WithFallback(PostgreSqlPersistence.DefaultConfiguration());
        
        AssertString(emptyRootConfig, baseRootConfig, "akka.persistence.snapshot-store.plugin");
        
        var config = emptyRootConfig.GetConfig("akka.persistence.snapshot-store.custom");
        var baseConfig = baseRootConfig.GetConfig("akka.persistence.snapshot-store.postgresql");
        config.Should().NotBeNull();
        baseConfig.Should().NotBeNull();

        AssertSnapshotConfig(config, baseConfig);
    }
    
    [Fact(DisplayName = "PostgreSqlSnapshotOptions should generate proper config")]
    public void SnapshotOptionsTest()
    {
        var options = new PostgreSqlSnapshotOptions(true)
        {
            Identifier = "custom",
            AutoInitialize = true,
            ConnectionString = "testConnection",
            ConnectionTimeout = 1.Seconds(),
            SchemaName = "testSchema",
            SequentialAccess = false,
            TableName = "testTable",
            StoredAs = StoredAsType.Json,
        };
        var baseConfig = options.ToConfig()
            .WithFallback(PostgreSqlPersistence.DefaultConfiguration());
        
        baseConfig.GetString("akka.persistence.snapshot-store.plugin").Should().Be("akka.persistence.snapshot-store.custom");

        var config = baseConfig.GetConfig("akka.persistence.snapshot-store.custom");
        config.Should().NotBeNull();
        config.GetString("connection-string").Should().Be(options.ConnectionString);
        config.GetTimeSpan("connection-timeout").Should().Be(options.ConnectionTimeout);
        config.GetString("schema-name").Should().Be(options.SchemaName);
        config.GetString("table-name").Should().Be(options.TableName);
        config.GetBoolean("auto-initialize").Should().Be(options.AutoInitialize);
        config.GetBoolean("sequential-access").Should().Be(options.SequentialAccess);
        config.GetString("stored-as").Should().Be(options.StoredAs.ToHocon());
    }

    [Fact(DisplayName = "PostgreSqlSnapshotOptions should be bindable to IConfiguration")]
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
      ""StoredAs"": ""JsonB"",

      ""ConnectionString"": ""Server=localhost,1533;Database=Akka;User Id=sa;"",
      ""ConnectionTimeout"": ""00:00:55"",
      ""SchemaName"": ""schema"",
      ""TableName"" : ""snapshot"",
      ""SequentialAccess"": false,

      ""IsDefaultPlugin"": false,
      ""Identifier"": ""CustomSnapshot"",
      ""AutoInitialize"": true,
      ""Serializer"": ""hyperion""
    }
  }
}";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var jsonConfig = new ConfigurationBuilder().AddJsonStream(stream).Build();
        
        var options = jsonConfig.GetSection("Akka:SnapshotOptions").Get<PostgreSqlSnapshotOptions>();
        options.IsDefaultPlugin.Should().BeFalse();
        options.Identifier.Should().Be("CustomSnapshot");
        options.AutoInitialize.Should().BeTrue();
        options.Serializer.Should().Be("hyperion");
        options.ConnectionString.Should().Be("Server=localhost,1533;Database=Akka;User Id=sa;");
        options.ConnectionTimeout.Should().Be(55.Seconds());
        options.SchemaName.Should().Be("schema");
        options.TableName.Should().Be("snapshot");
        options.SequentialAccess.Should().BeFalse();

        options.StoredAs.Should().Be(StoredAsType.JsonB);
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
        AssertString(underTest, reference, "metadata-table-name");
        AssertBoolean(underTest, reference, "sequential-access");
        AssertString(underTest, reference, "stored-as");
        AssertBoolean(underTest, reference, "use-bigint-identity-for-ordering-column");
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