using UnityEngine;
using VoidWireInteractive.Messaging.Contracts;

namespace VoidWireInteractive.Messaging.Samples
{
    public record SpawnerActiveStateMessage(bool active) : IMessage; 
}
