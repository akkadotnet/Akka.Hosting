// -----------------------------------------------------------------------
//  <copyright file="JournalOptionsSpec.cs" company="Akka.NET Project">
//      Copyright (C) 2009-2022 Lightbend Inc. <http://www.lightbend.com>
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using System;
using System.IO;
using System.Text;
using Akka.Configuration;
using Akka.Util;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;
using static FluentAssertions.FluentActions;

namespace Akka.Persistence.Hosting.Tests;

public class JournalOptionsSpec
{ 
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
      ""IsDefaultPlugin"": false,
      ""Identifier"": ""custom"",
      ""AutoInitialize"": true,
      ""Serializer"": ""hyperion"",
      ""Adapters"": [
        {
            ""Name"": ""mapper1"",
            ""Type"": ""Akka.Persistence.Hosting.Tests.EventAdapterSpecs+EventMapper1, Akka.Persistence.Hosting.Tests"",
            ""TypeBindings"": [
              ""Akka.Persistence.Hosting.Tests.EventAdapterSpecs+Event1, Akka.Persistence.Hosting.Tests""
            ]
        },
        {
            ""Name"": ""reader1"",
            ""Type"": ""Akka.Persistence.Hosting.Tests.EventAdapterSpecs+ReadAdapter, Akka.Persistence.Hosting.Tests"",
            ""TypeBindings"": [
              ""Akka.Persistence.Hosting.Tests.EventAdapterSpecs+Event1, Akka.Persistence.Hosting.Tests""
            ]
        },
        {
            ""Name"": ""combo"",
            ""Type"": ""Akka.Persistence.Hosting.Tests.EventAdapterSpecs+ComboAdapter, Akka.Persistence.Hosting.Tests"",
            ""TypeBindings"": [
              ""Akka.Persistence.Hosting.Tests.EventAdapterSpecs+Event2, Akka.Persistence.Hosting.Tests""
            ]
        },
        {
            ""Name"": ""tagger"",
            ""Type"": ""Akka.Persistence.Hosting.Tests.EventAdapterSpecs+Tagger, Akka.Persistence.Hosting.Tests"",
            ""TypeBindings"": [
              ""Akka.Persistence.Hosting.Tests.EventAdapterSpecs+Event1, Akka.Persistence.Hosting.Tests"",
              ""Akka.Persistence.Hosting.Tests.EventAdapterSpecs+Event2, Akka.Persistence.Hosting.Tests""
            ]
        }
      ]
    }
  }
}";

    [Fact(DisplayName = "JournalOptions should be able to bind adapters from IConfiguration")]
    public void AdaptersBindingTest()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(Json));
        var jsonConfig = new ConfigurationBuilder().AddJsonStream(stream).Build();
        
        var options = jsonConfig.GetSection("Akka:JournalOptions").Get<MockJournalOptions>();
        
        // Adapter binding deserialization test
        var config = options.ToConfig().GetConfig("akka.persistence.journal.custom");
        config.GetStringList($"event-adapter-bindings.\"{typeof(EventAdapterSpecs.Event1).TypeQualifiedName()}\"").Should()
            .BeEquivalentTo("mapper1", "reader1", "tagger");
        config.GetStringList($"event-adapter-bindings.\"{typeof(EventAdapterSpecs.Event2).TypeQualifiedName()}\"").Should()
            .BeEquivalentTo("combo", "tagger");
        
        config.GetString("event-adapters.mapper1").Should().Be(typeof(EventAdapterSpecs.EventMapper1).TypeQualifiedName());
        config.GetString("event-adapters.reader1").Should().Be(typeof(EventAdapterSpecs.ReadAdapter).TypeQualifiedName());
        config.GetString("event-adapters.combo").Should().Be(typeof(EventAdapterSpecs.ComboAdapter).TypeQualifiedName());
        config.GetString("event-adapters.tagger").Should().Be(typeof(EventAdapterSpecs.Tagger).TypeQualifiedName());
    }
    
    [Fact(DisplayName = "Adapters property should be compositional")]
    public void AdaptersCompositionTest()
    {
        var options = new MockJournalOptions
        {
            Identifier = "custom"
        };
        options.Adapters = new[]
        {
            new AdapterOptions
            {
                Name = "mapper1",
                Type = "Akka.Persistence.Hosting.Tests.EventAdapterSpecs+EventMapper1, Akka.Persistence.Hosting.Tests",
                TypeBindings = new[]
                {
                    "Akka.Persistence.Hosting.Tests.EventAdapterSpecs+Event1, Akka.Persistence.Hosting.Tests"
                }
            }
        };
        
        options.Adapters = new[]
        {
            new AdapterOptions
            {
                Name = "reader1",
                Type = "Akka.Persistence.Hosting.Tests.EventAdapterSpecs+ReadAdapter, Akka.Persistence.Hosting.Tests",
                TypeBindings = new[]
                {
                    "Akka.Persistence.Hosting.Tests.EventAdapterSpecs+Event1, Akka.Persistence.Hosting.Tests"
                }
            }
        };
        
        options.Adapters = new[]
        {
            new AdapterOptions
            {
                Name = "combo",
                Type = "Akka.Persistence.Hosting.Tests.EventAdapterSpecs+ComboAdapter, Akka.Persistence.Hosting.Tests",
                TypeBindings = new[]
                {
                    "Akka.Persistence.Hosting.Tests.EventAdapterSpecs+Event2, Akka.Persistence.Hosting.Tests"
                }
            }
        };
        
        options.Adapters = new[]
        {
            new AdapterOptions
            {
                Name = "tagger",
                Type = "Akka.Persistence.Hosting.Tests.EventAdapterSpecs+Tagger, Akka.Persistence.Hosting.Tests",
                TypeBindings = new[]
                {
                    "Akka.Persistence.Hosting.Tests.EventAdapterSpecs+Event1, Akka.Persistence.Hosting.Tests",
                    "Akka.Persistence.Hosting.Tests.EventAdapterSpecs+Event2, Akka.Persistence.Hosting.Tests",
                }
            }
        };
        
        // Adapter binding deserialization test
        var config = options.ToConfig().GetConfig("akka.persistence.journal.custom");
        config.GetStringList($"event-adapter-bindings.\"{typeof(EventAdapterSpecs.Event1).TypeQualifiedName()}\"").Should()
            .BeEquivalentTo("mapper1", "reader1", "tagger");
        config.GetStringList($"event-adapter-bindings.\"{typeof(EventAdapterSpecs.Event2).TypeQualifiedName()}\"").Should()
            .BeEquivalentTo("combo", "tagger");
        
        config.GetString("event-adapters.mapper1").Should().Be(typeof(EventAdapterSpecs.EventMapper1).TypeQualifiedName());
        config.GetString("event-adapters.reader1").Should().Be(typeof(EventAdapterSpecs.ReadAdapter).TypeQualifiedName());
        config.GetString("event-adapters.combo").Should().Be(typeof(EventAdapterSpecs.ComboAdapter).TypeQualifiedName());
        config.GetString("event-adapters.tagger").Should().Be(typeof(EventAdapterSpecs.Tagger).TypeQualifiedName());
    }

    [Fact(DisplayName = "Adapters property should throw on invalid name and types")]
    public void AdaptersCompositionExceptionTest()
    {
        var options = new MockJournalOptions
        {
            Identifier = "custom"
        };

        Invoking(() => options.Adapters = new[]
            {
                new AdapterOptions
                {
                    Name = "mapper1",
                    Type =
                        "Akka.Persistence.Hosting.Tests.DoesNotExist, Akka.Persistence.Hosting.Tests",
                    TypeBindings = Array.Empty<string>()
                }
            }
        ).Should().Throw<Exception>().WithMessage("Could not find adapter with Type*");

        Invoking(() => options.Adapters = new[]
            {
                new AdapterOptions
                {
                    Name = "mapper1",
                    Type = typeof(InvalidAdapter).AssemblyQualifiedName,
                    TypeBindings = Array.Empty<string>()
                }
            }
        ).Should().Throw<Exception>().WithMessage($"Type {typeof(InvalidAdapter).AssemblyQualifiedName} should implement*");
        
        Invoking(() => options.Adapters = new[]
            {
                new AdapterOptions
                {
                    Name = "[invalid]",
                    Type = "Akka.Persistence.Hosting.Tests.EventAdapterSpecs+Tagger, Akka.Persistence.Hosting.Tests",
                    TypeBindings = Array.Empty<string>()
                }
            }
        ).Should().Throw<Exception>().WithMessage("Invalid adapter name [invalid], contains illegal character(s)*");

        Invoking(() => options.Adapters = new[]
            {
                new AdapterOptions
                {
                    Name = "mapper1",
                    Type = "Akka.Persistence.Hosting.Tests.EventAdapterSpecs+Tagger, Akka.Persistence.Hosting.Tests",
                    TypeBindings = new []
                    {
                        "Akka.Persistence.Hosting.Tests.DoesNotExist, Akka.Persistence.Hosting.Tests"
                    }
                }
            }
        ).Should().Throw<Exception>().WithMessage($"Could not find Type Akka.Persistence.Hosting.Tests.DoesNotExist, Akka.Persistence.Hosting.Tests to bind to adapter*");
    }
}

public sealed class InvalidAdapter
{ }

internal sealed class MockJournalOptions : JournalOptions
{
    public MockJournalOptions() : base(true)
    {
    }

    public override string Identifier { get; set; }
    protected override Config InternalDefaultConfig => Config.Empty;
}