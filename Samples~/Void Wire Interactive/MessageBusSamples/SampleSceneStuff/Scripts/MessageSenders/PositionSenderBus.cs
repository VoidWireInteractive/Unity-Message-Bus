using System.Collections;
using UnityEngine;
using VoidWireInteractive.Messaging.Core;

namespace VoidWireInteractive.Messaging.Samples
{
    public class PositionSenderBus : MonoBehaviour
    {
        [SerializeField] 
        private MessageBus _bus;

        [SerializeField]
        private float MessageFrequency = 1f;
        WaitForSeconds wfs;
        private void Start()
        {
            wfs = new WaitForSeconds(MessageFrequency);
            StartCoroutine(BroadcastMessageOnDelay());
        }

        IEnumerator BroadcastMessageOnDelay()
        {
            while (true)
            {
                yield return wfs;
                DispatchMessage();
            }
        }

        public void DispatchMessage()
        {
            _bus.Publish(new PlayerPositionMessage(this.transform.name, this.transform.position));
        }
    }
}
