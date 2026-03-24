using System.Collections.Generic;

namespace  VoidWireInteractive.Messaging.Core
{
    /// <summary>
    /// Snapshot of bus activity returned by MessageBus.GetStats(). All dictionaries are keyed by the short type name. ex: "PlayerLevelUp"
    /// </summary>
    public sealed class BusStats
    {
        /// <summary>Number of active subscribers per message type.</summary>
        public Dictionary<string, int> SubscriberCountByType { get; init; }

        /// <summary>Total messages successfully written to the channel per type since the bus was enabled.</summary>
        public Dictionary<string, int> PublishCountByType { get; init; }

        /// <summary>
        /// Total messages dropped per type because the channel was full. Any non zero value means _channelCapacity should be increased on that bus asset,
        /// or a slow subscriber is causing backpressure.
        /// </summary>
        public Dictionary<string, int> DropCountByType { get; init; }

        /// <summary>Number of messages currently buffered in the channel awaiting dispatch.</summary>
        public int ChannelPending { get; init; }

        internal BusStats(
            Dictionary<string, int> subscriberCountByType,
            Dictionary<string, int> publishCountByType,
            Dictionary<string, int> dropCountByType,
            int channelPending)
        {
            SubscriberCountByType = subscriberCountByType;
            PublishCountByType = publishCountByType;
            DropCountByType = dropCountByType;
            ChannelPending = channelPending;
        }
    }
}
