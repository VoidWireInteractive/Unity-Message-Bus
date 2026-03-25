using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using VoidWireInteractive.Messaging.Contracts;
using UnityEngine;

namespace VoidWireInteractive.Messaging.Core
{
    /// <summary>
    /// The central message broker. Create via Assets/Void Wire Interactive/Create/Messaging/Message Bus, then assign to a feild on any MonoBehaviour that needs to publish or subscribe.
    ///
    /// Multiple bus assets are supported: one global bus, one per scene, one per subsystem.
    ///"OnEnable" starts the channel and routing loop and "OnDisable" shuts them down cleanly. The asset persists across scene loads because it lives in the project, not in a scene.
    /// </summary>
    [CreateAssetMenu(fileName = "new MessageBus", menuName = "Void Wire Interactive/Messaging/Message Bus")]
    public sealed class MessageBus : ScriptableObject
    {
        [Tooltip("Maximum messages the channel can buffer before applying the FullMode policy. Increase if you see drop warnings in the Bus Monitor or Console.")]
        [SerializeField] private int _channelCapacity = 1024;

        [Tooltip("DropWrite: silently ignore new messages when the channel is full (publisher never blocks).\n DropOldest: discard the oldest buffered message to make room (better for UI / telemetry events).")]
        [SerializeField] private ChannelFullMode _fullMode = ChannelFullMode.DropWrite;

        private BoundedMessageChannel _channel;
        private CancellationTokenSource _cts;

        // MessageType active subscriptions
        private readonly Dictionary<Type, List<Subscription>> _subscriptions = new();

        //Round robin per type, used by Queue routing mode
        private readonly Dictionary<Type, int> _queueIndices = new();

        //Telemetry written under "_lock", snapshot by GetStats()
        private readonly Dictionary<Type, int> _publishCount = new();
        private readonly Dictionary<Type, int> _dropCount = new();

        private readonly object _lock = new();
        // Internal Access for bus router

        internal BoundedMessageChannel Channel => _channel;


        private void OnEnable()
        {
            lock (_lock)
            {
                _subscriptions.Clear();
                _queueIndices.Clear();
                _publishCount.Clear();
                _dropCount.Clear();
            }

            _channel = new BoundedMessageChannel(_channelCapacity, _fullMode);
            _cts = new CancellationTokenSource();

            BusRouter.StartRouter(this, _cts.Token);
        }

        private void OnDisable()
        {
            _cts?.Cancel();
            _channel?.Complete();
            _cts?.Dispose();
            _cts = null;
        }

        /// <summary>
        /// Subscribe to messages of type <typeparamref name="T"/>.
        /// Store the returned <see cref="SubscriptionToken"/> and dispose it when the subscriber is destroyed to prevent memory leaks. MonoBehaviourSubscriber<T> handles this automatically.
        /// </summary>
        /// <param name="handler">Invoked on the Unity main thread for each received message.</param>
        /// <param name="mode">
        /// Broadcast (default): every subscriber receives every message.
        /// Queue: one subscriber per message, rotating round robin.
        /// RequestReply: one shot responder, auto removes after first invocation.
        /// </param>
        public SubscriptionToken Subscribe<T>(
            Action<T> handler,
            RoutingMode mode = RoutingMode.Broadcast)
            where T : class, IMessage
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            // token captures its own removal action via closure
            SubscriptionToken token = null;
            token = new SubscriptionToken(typeof(T), () => RemoveSubscription(token));

            var sub = new Subscription(
                token,
                msg => handler((T)msg),
                mode);

            lock (_lock)
            {
                if (!_subscriptions.TryGetValue(typeof(T), out var list))
                    _subscriptions[typeof(T)] = list = new List<Subscription>();

                list.Add(sub);
            }

            return token;
        }

        /// <summary>
        /// Removes a subscription. This is equivalent to calling token.Dispose(). Safe to call with a null token or multiple times.
        /// </summary>
        public void Unsubscribe(SubscriptionToken token) => token?.Dispose();

        /// <summary>
        /// Publish a non blocking message. the message is written to an internal queue and dispatched to subscribers on the main thread on the next router tick. If a channel is full, dropped messages are logged as warnings and counted in GetStats method.
        /// </summary>
        public void Publish<T>(T message) where T : class, IMessage
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            if (_channel == null)
            {
                Debug.LogWarning($"[MessageBus:{name}] Publish called before OnEnable or after OnDisable. Ignoring.");
                return;
            }

            var type = typeof(T);

            if (_channel.TryWrite(message))
            {
                lock (_lock)
                {
                    _publishCount.TryGetValue(type, out var coutn);
                    _publishCount[type] = coutn + 1;
                }
            }
            else
            {
                lock (_lock)
                {
                    _dropCount.TryGetValue(type, out var d);
                    _dropCount[type] = d + 1;
                }
                Debug.LogWarning($"[MessageBus:{name}] DROPPED {type.Name} — channel is full ({_channelCapacity}). Increase _channelCapacity in the Inspector or check for slow subscribers.");
            }
        }

        /// <summary>
        /// Publish a request and await a typed reply. The intended responder should be subscribed with the routing mode "RequestReply". Returns null if no reply arrives within <paramref name="timeoutSeconds"/>.
        /// </summary>
        public async Awaitable<TReply> Request<TRequest, TReply>(
            TRequest request,
            float timeoutSeconds = 5f)
            where TRequest : class, IMessage
            where TReply : class, IMessage
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            TReply result = null;
            var received = false;

            SubscriptionToken replyToken = null;
            replyToken = Subscribe<TReply>(reply =>
            {
                result = reply;
                received = true;
                replyToken?.Dispose();
            });

            Publish(request);

            float elapsed = 0f;
            while (!received && elapsed < timeoutSeconds)
            {
                await Awaitable.NextFrameAsync();
                elapsed += Time.deltaTime;
            }

            if (!received)
            {
                replyToken?.Dispose();
                Debug.LogWarning($"[MessageBus:{name}] Request<{typeof(TRequest).Name}> timed out after {timeoutSeconds}s. Ensure a RequestReply subscriber is registered for {typeof(TRequest).Name}.");
            }

            return result;
        }


        /// <summary>
        /// Returns a snapshot of bus activity. Called by the Bus Monitor EditorWindow during Play Mode.
        /// </summary>
        public BusStats GetStats()
        {
            lock (_lock)
            {
                return new BusStats(

                    _subscriptions.ToDictionary(
                        kv => kv.Key.Name, kv => kv.Value.Count),
                    _publishCount.ToDictionary(
                        kv => kv.Key.Name, kv => kv.Value),
                    _dropCount.ToDictionary(
                        kv => kv.Key.Name, kv => kv.Value),
                    _channel?.Count ?? 0
                );
            }
        }

        // Internal Dispatch called by BusRouter on main thread

        internal void Dispatch(IMessage message)
        {
            var type = message.GetType();

            List<Subscription> broadcastTargets;
            Subscription queueTarget = null;
            Subscription requestReplyTarget = null;

            lock (_lock)
            {
                if (!_subscriptions.TryGetValue(type, out var list) || list.Count == 0)
                    return;

                // Broadcast snapshot to avoid holding the lock during invocation
                broadcastTargets = list
                    .Where(s => s.Mode == RoutingMode.Broadcast)
                    .ToList();

                // Queue picks one subscriber round-robin
                var queueSubs = list.Where(s => s.Mode == RoutingMode.Queue).ToList();
                if (queueSubs.Count > 0)
                {
                    _queueIndices.TryGetValue(type, out var idx);
                    queueTarget = queueSubs[idx % queueSubs.Count];
                    _queueIndices[type] = (idx + 1) % queueSubs.Count;
                }

                // RequestReply picks first registered, remove all RR subs (one-shot per message)
                var rrSubs = list.Where(s => s.Mode == RoutingMode.RequestReply).ToList();
                if (rrSubs.Count > 0)
                {
                    requestReplyTarget = rrSubs[0];
                    list.RemoveAll(s => s.Mode == RoutingMode.RequestReply);
                }
            }

            // Invoke outside the lock. A crashing handler must not affect others
            foreach (var sub in broadcastTargets)
            {
                try { sub.Handler.Invoke(message); }
                catch (Exception e) { LogHandlerError(type.Name, "Broadcast", e); }
            }

            if (queueTarget != null)
            {
                try { queueTarget.Handler.Invoke(message); }
                catch (Exception e) { LogHandlerError(type.Name, "Queue", e); }
            }

            if (requestReplyTarget != null)
            {
                try { requestReplyTarget.Handler.Invoke(message); }
                catch (Exception e) { LogHandlerError(type.Name, "RequestReply", e); }
            }
        }


        private void RemoveSubscription(SubscriptionToken token)
        {
            if (token == null) return;
            lock (_lock)
            {
                if (!_subscriptions.TryGetValue(token.MessageType, out var list)) return;
                list.RemoveAll(s => s.Token.Id == token.Id);
                if (list.Count == 0)
                    _subscriptions.Remove(token.MessageType);
            }
        }

        private void LogHandlerError(string typeName, string mode, Exception e) => Debug.LogError($"[MessageBus:{name}] Unhandled exception in {mode} handler for '{typeName}'. The router will continue. Fix this handler.\n{e}");
    }
}
