using UnityEngine;
using VoidWireInteractive.Messaging.Contracts;

namespace  VoidWireInteractive.Messaging.Core
{
    /// <summary>
    /// Abstract base MonoBehaviour that automatically subscribes to messages of type <typeparamref name="T"/> on OnEnable and unsubscribes on OnDisable.
    /// Derive from this instead of plain MonoBehaviour to eliminate all subscribe/unsubscribe boilerplate and guarantee leak free lifecycle management.<br/><br/>
    ///
    /// Usage:
    /// <code>
    ///   public class PlayerHUDController : MonoBehaviourSubscriber<PlayerLevelUp>
    ///   {
    ///       protected override void OnMessageReceived(PlayerLevelUp msg) => levelText.text = $"Level {msg.NewLevel}";
    ///   }
    /// </code>
    ///
    /// The bus field is assigned in the inspector. If left null, the component falls back to GlobalBus.Default which requires a DefaultMessageBus in Resources.
    /// </summary>
    public abstract class MonoBehaviourSubscriber<T> : MonoBehaviour where T : class, IMessage
    {
        [Tooltip("The MessageBus asset this component subscribes to. Leave null to use GlobalBus.Default which requires Resources/Messaging/DefaultMessageBus.")]
        [SerializeField] private MessageBus _bus;

        [Tooltip("How this component participates in message routing for type T.")]
        [SerializeField] private RoutingMode _routingMode = RoutingMode.Broadcast;

        private SubscriptionToken _token;

        /// <summary>
        /// The resolved bus, either the explicitly assigned one or the global default. Exposed so subclasses can publish replies. useful in RequestReply patterns.
        /// </summary>
        protected MessageBus Bus => _bus != null ? _bus : GlobalBus.Default;

        protected virtual void OnEnable()
        {
            var resolvedBus = Bus;
            if (resolvedBus == null)
            {
                Debug.LogError($"[{GetType().Name}] No MessageBus assigned and no GlobalBus.Default configured. Assign a MessageBus asset in the Inspector or place one at Resources/Messaging/DefaultMessageBus.", this);
                return;
            }

            _token = resolvedBus.Subscribe<T>(OnMessageReceived, _routingMode);
        }

        protected virtual void OnDisable()
        {
            _token?.Dispose();
            _token = null;
        }

        /// <summary>
        /// Implement this to handle incoming messages of type <typeparamref name="T"/>. Always called on the main thread. Do not call long running synchronous work here!!
        /// kick off a coroutine or Awaitable if you need async processing.
        /// </summary>
        protected abstract void OnMessageReceived(T message);
    }
}
