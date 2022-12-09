// -----------------------------------------------------------------------
//  <copyright file="ClusterOptionsSpec.cs" company="Akka.NET Project">
//      Copyright (C) 2009-2022 Lightbend Inc. <http://www.lightbend.com>
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using Akka.Actor;
using Akka.Cluster.Hosting.SBR;
using Akka.Cluster.SBR;
using Akka.Configuration;
using Akka.Hosting;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Akka.Cluster.Hosting.Tests;

public class ClusterOptionsSpec
{
    [Fact(DisplayName = "Empty ClusterOptions should contain default HOCON values")]
    public void EmptyClusterOptionsTest()
    {
        var builder = new AkkaConfigurationBuilder(new ServiceCollection(), "")
            .AddHocon(ConfigurationFactory.FromResource("Akka.Cluster.Configuration.Cluster.conf", typeof(Cluster).Assembly), HoconAddMode.Append)
            .WithActorRefProvider(ProviderSelection.Cluster.Instance)
            .BuildClusterHocon(new ClusterOptions());
        
        builder.Configuration.HasValue.Should().BeTrue();
        var settings = new ClusterSettings(builder.Configuration.Value, "");

        settings.Roles.Count.Should().Be(0);
        settings.AppVersion.CompareTo(Util.AppVersion.Create("assembly-version")).Should().Be(0);
        settings.MinNrOfMembersOfRole.Count.Should().Be(0);
        settings.SeedNodes.Count.Should().Be(0);
        settings.MinNrOfMembers.Should().Be(1);
        settings.LogInfo.Should().BeTrue();
        settings.LogInfoVerbose.Should().BeFalse();
        settings.DowningProviderType.Should().Be(typeof(NoDowning));
    }
    
    [Fact(DisplayName = "ClusterOptions should generate proper HOCON values")]
    public void ClusterOptionsTest()
    {
        var builder = new AkkaConfigurationBuilder(new ServiceCollection(), "")
            .AddHocon(ConfigurationFactory.FromResource("Akka.Cluster.Configuration.Cluster.conf", typeof(Cluster).Assembly), HoconAddMode.Append)
            .BuildClusterHocon(new ClusterOptions
            {
                Roles = new []{ new Role("front-end"), new Role("back-end", 5)},
                AppVersion = "1.0.0",
                MinimumNumberOfMembers = 99,
                SeedNodes = new [] { Address.Parse("akka.tcp://system@somewhere.com:9999") },
                LogInfo = false,
                LogInfoVerbose = true,
                SplitBrainResolver = new KeepMajorityOption
                {
                    Role = "back-end"
                }
            });
        
        builder.Configuration.HasValue.Should().BeTrue();
        var settings = new ClusterSettings(builder.Configuration.Value, "");

        settings.Roles.Should().BeEquivalentTo("front-end", "back-end");
        
        settings.MinNrOfMembersOfRole.Count.Should().Be(1);
        settings.MinNrOfMembersOfRole.ContainsKey("back-end").Should().BeTrue();
        settings.MinNrOfMembersOfRole["back-end"].Should().Be(5);
        
        settings.AppVersion.CompareTo(Util.AppVersion.Create("1.0.0")).Should().Be(0);
        settings.SeedNodes.Should().BeEquivalentTo(new [] { Address.Parse("akka.tcp://system@somewhere.com:9999" )});
        settings.MinNrOfMembers.Should().Be(99);
        settings.LogInfo.Should().BeTrue(); // This is not intuitive, but LogInfo is defined as LogInfoVerbose || LogInfo in ClusterSettings
        settings.LogInfoVerbose.Should().BeTrue();
        settings.DowningProviderType.Should().Be(typeof(SplitBrainResolverProvider));

        var sbrConfig = builder.Configuration.Value.GetConfig("akka.cluster.split-brain-resolver");
        sbrConfig.GetString("active-strategy").Should().Be(SplitBrainResolverSettings.KeepMajorityName);
        sbrConfig.GetString($"{SplitBrainResolverSettings.KeepMajorityName}.role").Should().Be("back-end");
    }
    
}