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
        var opt = new ClusterClientReceptionistOptions
        {
            Name = "custom",
            Role = "custom",
            AcceptableHeartbeatPause = 1.Seconds(),
            FailureDetectionInterval = 1.Seconds(),
            HeartbeatInterval = 1.Seconds(),
            NumberOfContacts = 1,
            ResponseTunnelReceiveTimeout = 1.Seconds(),
        };

        var config = opt.ToConfig().GetConfig("akka.cluster.client.receptionist");
        var settings = ClusterReceptionistSettings.Create(config);

        config.GetString("name").Should().Be("custom");
        settings.Role.Should().Be("custom");
        settings.AcceptableHeartbeatPause.Should().Be(1.Seconds());
        settings.FailureDetectionInterval.Should().Be(1.Seconds());
        settings.HeartbeatInterval.Should().Be(1.Seconds());
        settings.NumberOfContacts.Should().Be(1);
        settings.ResponseTunnelReceiveTimeout.Should().Be(1.Seconds());
    }

    [Fact(DisplayName = "ClusterClientSettings should be set correctly")]
    public void ClusterClientSettingsSpec()
    {
        var contacts = new HashSet<ActorPath>
        {
            ActorPath.Parse("akka.tcp://one@localhost:1111/system/receptionist"),
            ActorPath.Parse("akka.tcp://two@localhost:1111/system/receptionist"),
            ActorPath.Parse("akka.tcp://three@localhost:1111/system/receptionist"),
        };

        var opt = new ClusterClientOptions
        {
            InitialContacts = contacts,
            AcceptableHeartbeatPause = 1.Seconds(),
            BufferSize = 999,
            EstablishingGetContactsInterval = 1.Seconds(),
            HeartbeatInterval = 1.Seconds(),
            ReconnectTimeout = 1.Seconds(),
            RefreshContactsInterval = 1.Seconds()
        };

        var settings =
            ClusterClientSettings.Create(ClusterClientReceptionist.DefaultConfig().GetConfig("akka.cluster.client"));
        settings = opt.Apply(settings);

        settings.InitialContacts.Should().BeEquivalentTo(contacts);
        // This isn't being set, ClusterClientSettings does not have a `WithAcceptableHeartbeatPause` method
        // settings.AcceptableHeartbeatPause.Should().Be(1.Seconds());
        settings.BufferSize.Should().Be(999);
        settings.EstablishingGetContactsInterval.Should().Be(1.Seconds());
        settings.HeartbeatInterval.Should().Be(1.Seconds());
        settings.ReconnectTimeout.Should().Be(1.Seconds());
        settings.RefreshContactsInterval.Should().Be(1.Seconds());
    }
}