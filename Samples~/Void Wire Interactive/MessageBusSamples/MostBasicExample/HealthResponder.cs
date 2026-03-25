using UnityEngine;
using VoidWireInteractive.Messaging.Core;
using VoidWireInteractive.Messaging.Samples.BasicExample;

namespace VoidWireInteractive.Messaging.Samples
{
    /// <summary>
    /// Sample RequestReply responder to demonstrate the raw subscribe/unsubscribe api as an alternative to deriving from MonoBehaviourSubscriber<T>. Use this pattern when a single component needs to handle multiple different message types, or when you want explicit control 
    /// over the token lifetime. RequestReply subscriptions are oneshot. they auto remove after the first invocation. This component reregisters itself inside the handler so it can respond to repeated requests.
    /// </summary>
    public sealed class HealthResponder : MonoBehaviour
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
            _token = _bus.Subscribe<QueryPlayerHealth>(HandleQuery, RoutingMode.RequestReply);
        }

        private void HandleQuery(QueryPlayerHealth query)
        {
            // Publish the reply. request call awaiting will resolve here
            _bus.Publish(new PlayerHealthResult(
                playerName: query.PlayerName,
                currentHp: Random.Range(50f, 100f),
                maxHp: 100f));

            Debug.Log($"[HealthResponder] Replied to health query for '{query.PlayerName}'.");

            // reregister for the next request. RequestReply is one shot per invocation.
            RegisterResponder();
        }
    }
}
