using System;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Akka.Configuration;

namespace Akka.Cluster.Hosting
{
    public class ClusterClientReceptionistOptions
    {
        /// <summary>
        /// Actor name of the ClusterReceptionist actor, /system/receptionist
        /// </summary>
        public string Name { get; set; } = "receptionist";

        /// <summary>
        /// Start the receptionist on members tagged with this role. All members are used if undefined.
        /// </summary>
        public string Role { get; set; }

        /// <summary>
        /// The receptionist will send this number of contact points to the client.
        /// </summary>
        public int? NumberOfContacts { get; set; }

        /// <summary>
        /// The actor that tunnel response messages to the client will be stopped after this time of inactivity.
        /// </summary>
        public TimeSpan? ResponseTunnelReceiveTimeout { get; set; }

        /// <summary>
        /// How often failure detection heartbeat messages should be received for each ClusterClient
        /// </summary>
        public TimeSpan? HeartbeatInterval { get; set; }

        /// <summary>
        /// Number of potentially lost/delayed heartbeats that will be
        /// accepted before considering it to be an anomaly.
        /// The ClusterReceptionist is using the akka.remote.DeadlineFailureDetector, which
        /// will trigger if there are no heartbeats within the duration
        /// heartbeat-interval + acceptable-heartbeat-pause, i.e. 15 seconds with
        /// the default settings.
        /// </summary>
        public TimeSpan? AcceptableHeartbeatPause { get; set; }

        /// <summary>
        /// Failure detection checking interval for checking all ClusterClients
        /// </summary>
        public TimeSpan? FailureDetectionInterval { get; set; }

        public Config ToConfig()
        {
            const string root = "akka.cluster.client.receptionist.";
            
            var sb = new StringBuilder()
                .Append(root).Append("name:").AppendLine(QuoteIfNeeded(Name));
            
            if(!string.IsNullOrEmpty(Role))
                sb.Append(root).Append("role:").AppendLine(QuoteIfNeeded(Role));

            if(NumberOfContacts != null)
                sb.Append(root).Append("number-of-contacts:").AppendLine(NumberOfContacts.ToString());
            
            if(ResponseTunnelReceiveTimeout != null)
                sb.Append(root).Append("response-tunnel-receive-timeout:").AppendLine(GetMs(ResponseTunnelReceiveTimeout));
            
            if(HeartbeatInterval != null)
                sb.Append(root).Append("heartbeat-interval:").AppendLine(GetMs(HeartbeatInterval));
            
            if(AcceptableHeartbeatPause != null)
                sb.Append(root).Append("acceptable-heartbeat-pause:").AppendLine(GetMs(AcceptableHeartbeatPause));
            
            if(FailureDetectionInterval != null)
                sb.Append(root).Append("failure-detection-interval:").AppendLine(GetMs(FailureDetectionInterval));

            return ConfigurationFactory.ParseString(sb.ToString());
        }
        
        #region Config helpers

        private static readonly Regex EscapeRegex = new Regex("[ \t:]{1}", RegexOptions.Compiled);

        private static string QuoteIfNeeded(string text)
        {
            return text == null 
                ? "" : EscapeRegex.IsMatch(text) 
                    ? $"\"{text}\"" : text;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string GetMs(TimeSpan? value)
            => $"{value!.Value.TotalMilliseconds}ms";
        #endregion
        
    }
}