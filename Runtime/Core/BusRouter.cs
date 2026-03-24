using System;
using System.Threading;
using UnityEngine;
using System.Threading.Tasks;

namespace VoidWireInteractive.Messaging.Core
{
    /// <summary>
    /// Owns the background read loop for a MessageBus instance. Started by MessageBus.OnEnable and is terminated when the bus CancellationToken is cancelled with MessageBus.OnDisable. Also when Complete() is called on the channel. <br/>
    ///
    /// notable info: <br/>
    ///- This is effectively a fire and forget loop, not a task based flow. *async void also can work here. Apparnelty "async void" integrates cleanly with the engine's exception logging but after testing Task vs Void, the exception outputs were identical. <br/>
    /// - WaitToReadAsync returns a plain Task from SemaphoreSlim.WaitAsync. Unity 6 async awaitable runtime can await Task objects, so this is safe to use in an async void method. <br/>
    /// - Awaitable.MainThreadAsync() is called once per message, guaranteeing all handler invocations happen on the main thread regardless of where WaitToReadAsync resumes. <br/>
    /// </summary>
    internal static class BusRouter
    {
        internal static async Task StartRouter(MessageBus bus, CancellationToken ct)
        {
            var channel = bus.Channel;

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    // Block until at least one message is available, the channel is completed, or the cancellation token fires. WaitToReadAsync throws OperationCanceledException when ct token fires.
                    await channel.WaitToReadAsync(ct);

                    // Exit the loop cleanly if the bus was disabled (Complete() was called).
                    if (channel.IsCompleted) break;

                    // Switch to the main thread before draining. all handlers must run there.
                    await Awaitable.MainThreadAsync();

                    // Drain every message that arrived while we were waiting.try read is non-blocking, the loop exits when the queue is empty.
                    while (channel.TryRead(out var message))
                    {
                        // Dispatch is guarded internally per handler. one bad handler cannot stop the loop or affect other subscribers.
                        bus.Dispatch(message);
                    }
                }

                // Drain any messages that arrived between the last WaitToReadAsync and shutdown.
                await Awaitable.MainThreadAsync();
                while (channel.TryRead(out var remaining))
                {
                    bus.Dispatch(remaining);
                }
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown, ct was cancelled by MessageBus.OnDisable. Nothing to do.
            }
            catch (Exception ex)
            {
                // Should not reach here since dispatch catches per handler exceptions, but ya know, Murphys law. If something does escape, log it clearly and let the loop die. The bus next OnEnable will restart the router.
                Debug.LogError($"[BusRouter] Fatal error in routing loop. The bus will stop dispatching until re enabled.\n{ex}");
            }
        }

        // Optional Global Default Bus via Resources folder in project. Place a MessageBus asset at "Assets/Resources/Messaging/DefaultMessageBus.asset". This will register it as GlobalBus.Default before the first scene loads giving
        // projects a nearly zero gui driven entry point. If you prefer explicit Serialized field assignments, you can ignore this entirely.

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitialiseGlobalBus()
        {
            var defaultBus = Resources.Load<MessageBus>("Messaging/DefaultMessageBus");
            if (defaultBus != null)
                GlobalBus.SetDefault(defaultBus);
        }
    }

    /// <summary>
    /// Optional static accessor for a project wide default bus.
    ///
    /// Populated automatically if a MessageBus asset exists at "Assets/Resources/Messaging/DefaultMessageBus.asset"<br/>
    ///
    /// Can also be set manually at startup via <code>  GlobalBus.SetDefault(myBusAsset);</code>
    ///
    /// Usage:
    ///   <code>   GlobalBus.Default.Publish(new MyMessage()); </code>
    ///
    /// Use explicitly set [SerializeField] references for testability. Use GlobalBus.Default only for rapid prototyping or truly project wide events.
    /// </summary>
    public static class GlobalBus
    {
        /// <summary>
        /// The default bus. Null if no default has been configured.
        /// </summary>
        public static MessageBus Default { get; private set; }

        /// <summary>Override or clear the default bus at runtime.</summary>
        public static void SetDefault(MessageBus bus) => Default = bus;
    }
}
