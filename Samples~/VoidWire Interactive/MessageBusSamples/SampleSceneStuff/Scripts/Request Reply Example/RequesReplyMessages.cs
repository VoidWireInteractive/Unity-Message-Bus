using UnityEngine;
using VoidWireInteractive.Messaging.Contracts;

namespace VoidWireInteractive.Messaging.Samples
{
    public class RequesReplyMessages
    {
        public record PositionQueryRequestMessage(string requesterName) : IMessage;
        public record PositionQueryResponseMessage(Vector3 Position) : IMessage;
    }
}
