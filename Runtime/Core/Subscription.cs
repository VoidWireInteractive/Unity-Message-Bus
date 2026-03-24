using System;
using VoidWireInteractive.Messaging.Contracts;

namespace  VoidWireInteractive.Messaging.Core
{
    /// <summary>
    /// Internal representation of a single registered subscriber. Not exposed on purpose. use SubscriptionToken for all external interactions.
    /// </summary>
    internal sealed class Subscription
    {
        /// <summary> The token that uniquely identifies and controls this subscriptions lifetime.</summary>
        internal readonly SubscriptionToken Token;

        /// <summary>
        /// The type erased handler. The generic Action<T> provided by the caller is wrapped here to avoid storing open generic delegates in the dictionary.
        /// </summary>
        internal readonly Action<IMessage> Handler;

        /// <summary>How this subscription participates in message routing.</summary>
        internal readonly RoutingMode Mode;

        internal Subscription(SubscriptionToken token, Action<IMessage> handler, RoutingMode mode)
        {
            Token = token;
            Handler = handler;
            Mode = mode;
        }
    }
}
