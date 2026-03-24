using UnityEngine;
using VoidWireInteractive.Messaging.Core;

namespace VoidWireInteractive.Messaging.Samples
{
    public class SpawnerReciever : MonoBehaviourSubscriber<SpawnerActiveStateMessage>
    {
        [SerializeField]
        private SpawnFollowers spawnComponent;
        protected override void OnMessageReceived(SpawnerActiveStateMessage message)
        {
            spawnComponent.enabled = message.active;
        }
    }
}
