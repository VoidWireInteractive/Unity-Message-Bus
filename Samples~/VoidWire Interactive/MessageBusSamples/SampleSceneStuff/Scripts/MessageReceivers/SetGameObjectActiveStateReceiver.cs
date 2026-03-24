using UnityEngine;
using VoidWireInteractive.Messaging.Core;

namespace VoidWireInteractive.Messaging.Samples
{
    public class SetGameObjectActiveStateReceiver : MonoBehaviourSubscriber<SpawnerActiveStateMessage>
    {
        [SerializeField]
        private GameObject _go;
        protected override void OnMessageReceived(SpawnerActiveStateMessage message)
        {
            _go.SetActive(message.active);
        }
    }
}
