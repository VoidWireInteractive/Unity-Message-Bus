using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VoidWireInteractive.Messaging.Core;
using VoidWireInteractive.Messaging.Samples.BasicExample;

namespace VoidWireInteractive.Messaging.Samples
{
    /// <summary>
    /// Sample publisher. Attach to any gameobject alongside a "MessageBus" reference.
    ///<br/>
    /// Controls for playmode:<br/>
    /// - Space: Publish a PlayerLevelUp broadcast and all subscribers receive it.<br/>
    /// - Q: Publish a PlayerLevelUp for Queue routing and one subscriber receives it.<br/>
    /// - R: Send a request and await a PlayerHealthResult reply<br/>
    /// - B: Hold to mass publish a burst of messages and test channel backpressure and/or drop behaviors<br/>
    /// </summary>
    public sealed class TestSender : MonoBehaviour
    {
        [SerializeField] private MessageBus _bus;

        [Header("Burst Testing")]
        [Tooltip("Number of messages published per frame while B is held.")]
        [SerializeField] private int _burstCount = 200;

        private int _levelCounter = 1;

        private void Update()
        {
            if (_bus == null)
            {
                Debug.LogWarning("[TestSender] No MessageBus assigned.", this);
                return;
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                _bus.Publish(new PlayerLevelUp(
                    playerName: "Void Weaver",
                    oldLevel: _levelCounter,
                    newLevel: ++_levelCounter,
                    experienceGained: Random.Range(100f, 500f)));

                Debug.Log($"[TestSender] Published PlayerLevelUp → Level {_levelCounter}");
            }

            // With two TestReceiver components both set to RoutingMode.Queue, each press of Q will be handled by only one of them, alternating.
            if (Input.GetKeyDown(KeyCode.Q))
            {
                _bus.Publish(new PlayerLevelUp("QueuePlayer", 0, 1, 50f));
                Debug.Log("[TestSender] Published PlayerLevelUp for Queue routing.");
            }

            //Request/Reply requires a HealthResponder component in the scene to respond.
            if (Input.GetKeyDown(KeyCode.R))
            {
                _ = SendHealthRequestAsync();
            }

            //Burst tes
            // Hold B to flood the channel. Watch the Bus Monitor for drop counts.
            if (Input.GetKey(KeyCode.B))
            {
                for (int i = 0; i < _burstCount; i++)
                    _bus.Publish(new PlayerLevelUp("Da ting go BRRRRST", i, i + 1, 1f));
            }
        }

        private async Awaitable SendHealthRequestAsync()
        {
            Debug.Log("[TestSender] Sending QueryPlayerHealth request...");

            var reply = await _bus.Request<QueryPlayerHealth, PlayerHealthResult>(
                new QueryPlayerHealth("Void Weaver"),
                timeoutSeconds: 3f);

            if (reply != null)
                Debug.Log($"[TestSender] Reply: {reply.PlayerName} has {reply.CurrentHp}/{reply.MaxHp} HP");
            else
                Debug.LogWarning("[TestSender] Request timed out — is a HealthResponder active in the scene?");
        }
    }
    [CustomEditor(typeof(TestSender))]
    public class TestSenderEditor : Editor
    {
        private Dictionary<string, string> guiElems = new Dictionary<string, string>()
        {
            {"Dispatch via Broadcast mode","Press 'Space' to publish a Broadcast mode message of the type \"PlayerLevelUp\"" },
            {"Dispatch via Queue mode","Press 'Q' to publish a Queue mode message of type \"PlayerLevelUp\"" },
            {"Dispatch via Request/Reply mode","Press 'R' To send a one shot Request of type \"QueryPlayerHealth\", which awaits a response of type \"PlayerHealthResult\". Receiver object holding the \"HealthResponder\" component will respond." },
            {"Mass Dispatch","Hold/Press 'B' to mass publish a message to all subscribers on the bus. Use it for testing channel queue behaviors at max pressure and message count."},
        };
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            TestSender senderscrpt = (TestSender)target;
            EditorGUILayout.Separator();

            foreach (var elem in guiElems)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    GUILayout.Label(elem.Key);
                    GUILayout.Space(5f);
                    GUILayout.TextArea(elem.Value);
                }
                EditorGUILayout.Separator();
            }
        }
    }
}
