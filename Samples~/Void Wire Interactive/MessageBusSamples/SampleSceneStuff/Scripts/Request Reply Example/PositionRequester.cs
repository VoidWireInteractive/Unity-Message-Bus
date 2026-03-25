using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VoidWireInteractive.Messaging.Core;
using static VoidWireInteractive.Messaging.Samples.RequesReplyMessages;

namespace VoidWireInteractive.Messaging.Samples
{
    public sealed class PositionRequester : MonoBehaviour
    {
        [SerializeField]
        private MessageBus[] possibleRequests;
        private MessageBus _bus;
        [SerializeField]
        private float MessageFrequency = 3f;
        WaitForSeconds wfs;
        private Vector3 targetPosition;
        Color[] handleColors = new []{ Color.blue , Color.red , Color.green };
        KeyValuePair<string, Color> thoughtMessage;
        
        private void Awake()
        {
            targetPosition = this.transform.position;
            var selection = Random.Range(0, possibleRequests.Length);
            _bus = possibleRequests[selection]; // basically just randomizing what it would be looking for.
            thoughtMessage = new(_bus.name.Split('.')[1], handleColors[selection]);
        }
        private void Update()
        {
            this.transform.position = Vector3.MoveTowards(transform.position, targetPosition, Time.deltaTime);    
        }

        private async Awaitable SendPositionRequestAsync()
        {
            Debug.Log("[PositionRequester] Sending PositionQueryRequestMessage request...");

            var reply = await _bus.Request<PositionQueryRequestMessage, PositionQueryResponseMessage>(
                new PositionQueryRequestMessage($"{transform.name}"),
                timeoutSeconds: 3f);

            if (reply != null && reply.Position!= Vector3.zero)
            {
                targetPosition = reply.Position;
                Debug.Log($"[PositionRequester] Request gave reply: {reply.Position}");
            }
            else
                Debug.LogWarning("[PositionRequester] Request timed out");
        }

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
            _ = SendPositionRequestAsync();
        }

        private void OnDrawGizmos()
        {
            DoTextHandles();
        }
        private void DoTextHandles()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                return;
            GUIStyle style = new GUIStyle();

            // Set the color for the label
            Handles.color = thoughtMessage.Value;
            // Draw the label at the object's position with custom style
            style.normal.textColor = thoughtMessage.Value; // Example of a custom color
            Handles.Label(transform.position, $"I want {thoughtMessage.Key}...", style);
#endif
        }
    }
}
