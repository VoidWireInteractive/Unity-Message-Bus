using UnityEngine;
using VoidWireInteractive.Messaging.Core;

namespace VoidWireInteractive.Messaging.Samples
{
    public sealed class PositionReceiver : MonoBehaviourSubscriber<PlayerPositionMessage>
    {
        public string MyInstanceId;
        Vector3 targetposition;
        float speed = 1f;

        protected override void OnMessageReceived(PlayerPositionMessage message)
        {
            targetposition = message.updatedPosition;
            Debug.Log($"{MyInstanceId} Recieved {message.PlayerName}'s position at {message.updatedPosition}");
        }

        void Awake()
        {
            speed = Random.Range(.5f, 3f);
            MyInstanceId = $"{this.GetInstanceID()}";
        }
        private void Update()
        {
            if(targetposition!= Vector3.zero)
                transform.position = Vector3.MoveTowards(transform.position, targetposition, .01f * speed);
        }
    }
}