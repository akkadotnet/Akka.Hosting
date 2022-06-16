using System.Collections.Generic;
using Akka.Actor;
using Akka.Cluster.Tools.Client;
using FluentAssertions;
using FluentAssertions.Extensions;
using Xunit;

namespace Akka.Cluster.Hosting.Tests;

public class ClusterClientSpecs
{
    [Fact(DisplayName = "ClusterClientReceptionistSettings should be set correctly")]
    public void ClusterClientReceptionistSettingsSpec()
    {
        var config = AkkaClusterHostingExtensions.CreateReceptionistConfig("customName", "customRole")
            .GetConfig("akka.cluster.client.receptionist");
        var settings = ClusterReceptionistSettings.Create(config);

        config.GetString("name").Should().Be("customName");
        settings.Role.Should().Be("customRole");
    }

    [Fact(DisplayName = "ClusterClientSettings should be set correctly")]
    public void ClusterClientSettingsSpec()
    {
        var contacts = new List<ActorPath>
        {
            ActorPath.Parse("akka.tcp://one@localhost:1111/system/receptionist"),
            ActorPath.Parse("akka.tcp://two@localhost:1111/system/receptionist"),
            ActorPath.Parse("akka.tcp://three@localhost:1111/system/receptionist"),
        };

        var settings = AkkaClusterHostingExtensions.CreateClusterClientSettings(
            ClusterClientReceptionist.DefaultConfig(),
            contacts);

        settings.InitialContacts.Should().BeEquivalentTo(contacts);
    }
}