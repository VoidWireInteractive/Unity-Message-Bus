using UnityEngine;
using VoidWireInteractive.Messaging.Core;
using static VoidWireInteractive.Messaging.Samples.RequesReplyMessages;

namespace VoidWireInteractive.Messaging.Samples
{
    public sealed class PositionReceiveReply : MonoBehaviour
    {
        [SerializeField] private MessageBus _bus;
        private SubscriptionToken _token;

        private void OnEnable() => RegisterResponder();
        private void OnDisable()
        {
            _token?.Dispose();
            _token = null;
        }

        private void RegisterResponder()
        {
            if (_bus == null) return;
            _token?.Dispose();
            _token = _bus.Subscribe<PositionQueryRequestMessage>(HandleQuery, RoutingMode.RequestReply);
        }

        private void HandleQuery(PositionQueryRequestMessage query)
        {
            // Publish the reply. request call awaiting will resolve here
            _bus.Publish(new PositionQueryResponseMessage(this.transform.position));

            Debug.Log($"[PositionResponder] Replied to '{query.requesterName}' request for position.");

            // reregister for the next request. RequestReply is one shot per invocation.
            RegisterResponder();
        }
    }
}