using UnityEngine;
using VoidWireInteractive.Messaging.Contracts;

namespace VoidWireInteractive.Messaging.Samples
{
    public record PlayerPositionMessage(string PlayerName,Vector3 updatedPosition) : IMessage;
}
