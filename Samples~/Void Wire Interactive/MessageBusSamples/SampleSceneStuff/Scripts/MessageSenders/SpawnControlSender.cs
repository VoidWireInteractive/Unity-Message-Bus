using UnityEditor;
using UnityEngine;
using VoidWireInteractive.Messaging.Core;

namespace VoidWireInteractive.Messaging.Samples
{
    public class SpawnControlSender : MonoBehaviour
    {
        [SerializeField]
        private MessageBus _broadcastBus;

        [SerializeField]
        private MessageBus _queueBus;

        [SerializeField]
        private MessageBus _requestReplyBus;

        public void DispatchBroadcastMode()
        {
            _broadcastBus.Publish(new SpawnerActiveStateMessage(true));
            _queueBus.Publish(new SpawnerActiveStateMessage(false));
            _requestReplyBus.Publish(new SpawnerActiveStateMessage(false));
        }
        public void DispatchQueueMode()
        {
            _broadcastBus.Publish(new SpawnerActiveStateMessage(false));
            _queueBus.Publish(new SpawnerActiveStateMessage(true));
            _requestReplyBus.Publish(new SpawnerActiveStateMessage(false));
        }
        public void DispatchRequestReplyMode()
        {
            _broadcastBus.Publish(new SpawnerActiveStateMessage(false));
            _queueBus.Publish(new SpawnerActiveStateMessage(false));
            _requestReplyBus.Publish(new SpawnerActiveStateMessage(true));
        }
    }
    [CustomEditor(typeof(SpawnControlSender))]
    public class SpawnControlSenderEditor : Editor
    {
        private readonly string[] labels = new[]
        {
            "In playmode, click a button to view an example and a brief description here. Each button will send a message along a different bus indicating to the receiving object whether it should activate or not. \r\n\r\n" +
            "Note, those receivers are in a completely different scene and are subscribing at run time when they are enabled and unsubscribed upon disable/destroy/disposal. No difficulty with cross scene communications. Which is pretty neat right?\r\n\r\n" +
            " Once the receiver is activated, it has it's own logic to showcase the individual behavior of the routing mode you select below.",

            "~Broadcast Mode~\r\n\r\n Message of type 'PlayerPositionMessage' is dispatched by the 'Position_Sender' object along the 'player.position.update.bus' bus asset to be received by all it's subscribers, all of which are spawned upon pressing this button." +
            " This mode ensures every single active subscription of 'player.position.update.bus' receives the message when it is sent. \r\n\r\nThe receivers [BroadcastFollowerPrefab] have a PositionReceiver component, which listens to the bus 'player.position.update.bus' acting as a subscriber. These are effectively 'Topics' if you're" +
            "familiar with Azure Service Bus resources. \r\n\r\n Upon receipt of the message, which is a record with primary constructor accepting the sender's name and posiiton, the receiver performs a Vector3.MoveTowards on the position provided through the message.",

            "Message of type 'PlayerPositionMessage' is dispatched by the 'Position_Sender' object along the 'player.position.update.bus' bus asset. However, now via the Queue mode, which allows only one subscriber to recieve the message.\r\n\r\n" +
            " One by one, the subscribers to 'player.position.update.bus' take turns round robin style, as the messages are sent by the dispatcher. This is effectively a 'Queue' if you're familiar with Azure Service Bus resources. Similarly to the Broadcast mode," +
            "The chosen receiver will perform a Vector3.MoveTowards the position it received.",

            "A bit more involved than the other 2 methods, the showcase of this mode mimics a Customer/Store relationship. This time, the GameObjects spawned in the Receiver scene act as the Store (the replier), and the CustomerSpawner Gameobject in " +
            "This sender scene will spawn Customers (the requester). The Customer is allocated 3 separate message buses and chooses one when OnEnable invokes. It will use the chosen bus to send a message of type 'PositionQueryRequestMessage' along " +
            "the bus, requesting the position of a subscriber to that message bus. ie the 'store' location. It will then begin a Vector3.MoveTowards on that location.\r\n\r\n The important thing to be aware of is that the relationship between Requester" +
            "and Replier is that the requester will ask for the response, but as soon as it receives the response it has been satisfied and will not receive another reponse. Even if there are multiple subscribers capable of responding. \r\n\r\n " +
            "The Responder/Replier, on the other hand, will re-register itself after replying so that it can serve another request if it were to recieve one. So in the terms of this showcase, a customer spawns and requests a store. The store that matches replies with it's position" +
            "The requester receives it's position and moves towards it until it triggers a delete script on the store object. No matter the number of requesters, the stores will be able to serve them one time each."
        };
        int selectedIndex = 0;
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            SpawnControlSender senderscrpt = (SpawnControlSender)target;
            EditorGUILayout.Separator();
            EditorGUILayout.Separator();
            GUI.skin.label.wordWrap = true;
            if (!Application.isPlaying)
                selectedIndex = 0;

            if (selectedIndex >= 0)
                GUILayout.Label(labels[selectedIndex]);
            
            EditorGUILayout.Separator();
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (GUILayout.Button("Activate Broadcast Example"))
                {
                    selectedIndex = 1;
                    senderscrpt.DispatchBroadcastMode();
                }
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (GUILayout.Button("Activate Queue Example"))
                {
                    selectedIndex = 2;
                    senderscrpt.DispatchQueueMode();
                }
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (GUILayout.Button("Activate Request/Reply Example"))
                {
                    selectedIndex = 3;
                    senderscrpt.DispatchRequestReplyMode();
                }
            }
        }
    }
}
