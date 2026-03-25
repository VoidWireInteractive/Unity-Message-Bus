using VoidWireInteractive.Messaging.Core;
using VoidWireInteractive.Messaging.Samples.BasicExample;
using UnityEngine;

namespace VoidWireInteractive.Messaging.Samples
{
    /// <summary>
    /// Sample Broadcast subscriber using MonoBehaviourSubscriber<T>.
    /// Drag a MessageBus asset into the "Bus" field in the inspector. Set "Routing Mode" to Broadcast to receive every "PlayerLevelUp" message. Set "Routing Mode" to Queue to compete with other Queue subscribers, only one "TestReceiver" will receive each individual message.
    /// No subscribe/unsubscribe code needed since MonoBehaviourSubscriber handles the full lifecycle in OnEnable/OnDisable.
    /// </summary>
    public sealed class TestReceiver : MonoBehaviourSubscriber<PlayerLevelUp>
    {
        [Header("Display")]
        [Tooltip("Name shown in the console log to distinguish multiple receivers in the scene.")]
        [SerializeField] private string _receiverName = "ReceiverA";

        protected override void OnMessageReceived(PlayerLevelUp msg)
        {
            Debug.Log($"[{_receiverName}] PlayerLevelUp: {msg.PlayerName}: {msg.OldLevel} => {msg.NewLevel} (+{msg.ExperienceGained:F0} XP)");
        }
    }
}
