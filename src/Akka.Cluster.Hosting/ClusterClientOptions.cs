using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Akka.Actor;
using Akka.Cluster.Tools.Client;
using Akka.Remote;

namespace Akka.Cluster.Hosting
{
    public class ClusterClientOptions
    {
        /// <summary>
        /// Actor paths of the <see cref="ClusterReceptionist"/> actors on the servers (cluster nodes) that the client will try to contact initially.
        /// </summary>
        public HashSet<ActorPath> InitialContacts { get; set; }

        /// <summary>
        /// Interval at which the client retries to establish contact with one of ClusterReceptionist on the servers (cluster nodes)
        /// </summary>
        public TimeSpan? EstablishingGetContactsInterval { get; set; }

        /// <summary>
        /// Interval at which the client will ask the <see cref="ClusterReceptionist"/> for new contact points to be used for next reconnect.
        /// </summary>
        public TimeSpan? RefreshContactsInterval { get; set; }

        /// <summary>
        /// How often failure detection heartbeat messages for detection of failed connections should be sent.
        /// </summary>
        public TimeSpan? HeartbeatInterval { get; set; }

        /// <summary>
        /// Number of potentially lost/delayed heartbeats that will be accepted before considering it to be an anomaly. 
        /// The ClusterClient is using the <see cref="DeadlineFailureDetector"/>, which will trigger if there are 
        /// no heartbeats within the duration <see cref="HeartbeatInterval"/> + <see cref="AcceptableHeartbeatPause"/>.
        /// </summary>
        public TimeSpan? AcceptableHeartbeatPause { get; set; }

        /// <summary>
        /// If connection to the receptionist is not established the client will buffer this number of messages and deliver 
        /// them the connection is established. When the buffer is full old messages will be dropped when new messages are sent via the client. 
        /// Use 0 to disable buffering, i.e. messages will be dropped immediately if the location of the receptionist is unavailable.
        /// </summary>
        public int? BufferSize { get; set; }

        /// <summary>
        /// If the connection to the receptionist is lost and cannot
        /// be re-established within this duration the cluster client will be stopped. This makes it possible
        /// to watch it from another actor and possibly acquire a new list of InitialContacts from some
        /// external service registry
        /// </summary>
        public TimeSpan? ReconnectTimeout { get; set; }

        internal ClusterClientSettings Apply(ClusterClientSettings settings)
        {
            if (InitialContacts != null)
                settings = settings.WithInitialContacts(InitialContacts.ToImmutableHashSet());

            if (EstablishingGetContactsInterval != null)
                settings = settings.WithEstablishingGetContactsInterval(EstablishingGetContactsInterval.Value);

            if (RefreshContactsInterval != null)
                settings = settings.WithRefreshContactsInterval(RefreshContactsInterval.Value);

            if (HeartbeatInterval != null)
                settings = settings.WithHeartbeatInterval(HeartbeatInterval.Value);
            
            // TODO: AcceptableHeartbeatPause setter is missing in Akka!
            //if(AcceptableHeartbeatPause != null)

            if (BufferSize != null)
                settings = settings.WithBufferSize(BufferSize.Value);

            if (ReconnectTimeout != null)
                settings = settings.WithReconnectTimeout(ReconnectTimeout.Value);

            return settings;
        }
    }
}