using System;

namespace  VoidWireInteractive.Messaging.Core
{
    /// <summary>
    /// Opaque handle returned by MessageBus.Subscribe. Holds the subscription alive while referenced. Dispose or pass to MessageBus.Unsubscribe to remove the handler and prevent memory leaks.
    /// Also supports 'using var token = bus.Subscribe(blahblah)' for automatic cleanup in non MonoBehaviour code. MonoBehaviourSubscriber<T> manages this automatically in onEnable and ondisable.
    /// </summary>
    public sealed class SubscriptionToken : IDisposable
    {
        /// <summary>Unique identifier for this subscription.</summary>
        public Guid Id { get; } = Guid.NewGuid();

        /// <summary>The message type this token is bound to.</summary>
        public Type MessageType { get; }

        private Action _unsubscribeAction;
        private bool _disposed;

        internal SubscriptionToken(Type messageType, Action unsubscribeAction)
        {
            MessageType = messageType;
            _unsubscribeAction = unsubscribeAction;
        }

        /// <summary>
        /// Immediately removes the bound handler from the bus. Safe to call multiple times.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _unsubscribeAction?.Invoke();
            _unsubscribeAction = null;
        }
    }
}
