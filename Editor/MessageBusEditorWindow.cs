#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;
using VoidWireInteractive.Messaging.Core;

namespace VoidWireInteractive.Messaging.Editor
{
    /// <summary>
    /// Realtime MessageBus telemetry monitor. Open via Window > Void Wire Interactive > Messaging > Bus Monitor. During Play Mode, polls all MessageBus assets in the project and displays<br/>
    /// 
    /// - Active subscriber count per message type<br/>
    /// - Total messages published per type<br/>
    /// - Total messages dropped per type (non-zero = capacity warning)<br/>
    /// - Current channel backlog depth<br/>
    ///
    /// A non zero Drop Count is the signal to increase _channelCapacity on that bus asset or investigate a subscriber that is too slow to process messages.
    /// </summary>
    public sealed class MessageBusEditorWindow : EditorWindow
    {
        private MessageBus[] _busAssets= System.Array.Empty<MessageBus>();
        private int _selectedBusIndex = 0;
        private Vector2 _scrollPosition;
        private double _lastRefreshTime;
        private const double RefreshInterval  = 0.25; 

        private static readonly float[] ColumnWidths = { 220f, 80f, 80f, 80f };
        private static readonly string[] ColumnHeaders = { "Message Type", "Subscribers", "Published", "Dropped" };

        private GUIStyle _headerStyle;
        private GUIStyle _dropStyle;
        private GUIStyle _dimStyle;

        [MenuItem("Window/Void Wire Interactive/Messaging/Bus Monitor")]
        public static void ShowWindow()
        {
            var window = GetWindow<MessageBusEditorWindow>();
            window.titleContent = new GUIContent("Bus Monitor");
            window.minSize= new Vector2(420f, 200f);
            window.Show();
        }

        private void OnEnable()
        {
            RefreshBusAssetList();
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        }

        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode ||
                state == PlayModeStateChange.EnteredEditMode)
                RefreshBusAssetList();
        }

        private void Update()
        {
            if (!Application.isPlaying) return;
            if (EditorApplication.timeSinceStartup - _lastRefreshTime < RefreshInterval) return;
            _lastRefreshTime = EditorApplication.timeSinceStartup;
            Repaint();
        }

        private void OnGUI()
        {
            InitStyles();
            DrawToolbar();

            if (_busAssets.Length == 0)
            {
                EditorGUILayout.HelpBox(
                    "No MessageBus assets found in the project. Create one via Assets > Create > Void Wire Interactive > Messaging > Message Bus.",
                    MessageType.Info);
                return;
            }

            var selectedBus = _busAssets[_selectedBusIndex];
            if (selectedBus == null)
            {
                EditorGUILayout.HelpBox("Selected bus asset is null. Refreshing...", MessageType.Warning);
                RefreshBusAssetList();
                return;
            }

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to see live statistics.", MessageType.None);
                DrawBusObjectField(selectedBus);
                return;
            }

            DrawBusObjectField(selectedBus);
            EditorGUILayout.Space(4);

            var stats = selectedBus.GetStats();
            DrawChannelSummary(stats);
            EditorGUILayout.Space(8);
            DrawStatsTable(stats);
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            var names = _busAssets
                .Select((b, i) => b != null ? b.name : $"<null> [{i}]")
                .ToArray();

            _selectedBusIndex = Mathf.Clamp(_selectedBusIndex, 0, Mathf.Max(0, _busAssets.Length - 1));
            _selectedBusIndex = EditorGUILayout.Popup(_selectedBusIndex, names, EditorStyles.toolbarPopup);

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Refresh Assets", EditorStyles.toolbarButton))
                RefreshBusAssetList();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawBusObjectField(MessageBus bus)
        {
            using var _ = new EditorGUI.DisabledScope(true);
            EditorGUILayout.ObjectField("Bus Asset", bus, typeof(MessageBus), false);
        }

        private void DrawChannelSummary(BusStats stats)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            var pendingColor = stats.ChannelPending > 100
                ? new Color(1f, 0.5f, 0.2f)
                : GUI.contentColor;

            var totalDrops = stats.DropCountByType.Values.Sum();
            var dropColor  = totalDrops > 0 ? Color.red : GUI.contentColor;

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Channel backlog", GUILayout.Width(140));
                var prev = GUI.contentColor;
                GUI.contentColor = pendingColor;
                GUILayout.Label(stats.ChannelPending.ToString(), _headerStyle);
                GUI.contentColor = prev;

                GUILayout.FlexibleSpace();

                GUILayout.Label("Total drops", GUILayout.Width(80));
                prev = GUI.contentColor;
                GUI.contentColor = dropColor;
                GUILayout.Label(totalDrops.ToString(), _headerStyle);
                GUI.contentColor = prev;
            }

            EditorGUILayout.EndVertical();

            if (totalDrops > 0)            
                EditorGUILayout.HelpBox("Messages are being dropped. Increase the bus _channelCapacity or reduce publish frequency / handler cost.",MessageType.Warning);
            
        }

        private void DrawStatsTable(BusStats stats)
        {
            var allTypes = stats.SubscriberCountByType.Keys
                .Union(stats.PublishCountByType.Keys)
                .Union(stats.DropCountByType.Keys)
                .OrderBy(k => k)
                .ToList();

            if (allTypes.Count == 0)
            {
                EditorGUILayout.LabelField("No subscriptions registered yet.", _dimStyle);
                return;
            }
            EditorGUILayout.BeginHorizontal();
            for (int i = 0; i < ColumnHeaders.Length; i++)
                GUILayout.Label(ColumnHeaders[i], _headerStyle, GUILayout.Width(ColumnWidths[i]));
            EditorGUILayout.EndHorizontal();

            DrawSeparator();
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            foreach (var typeName in allTypes)
            {
                stats.SubscriberCountByType.TryGetValue(typeName, out var subs);
                stats.PublishCountByType.TryGetValue(typeName, out var pubs);
                stats.DropCountByType.TryGetValue(typeName, out var drops);

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(typeName, GUILayout.Width(ColumnWidths[0]));
                GUILayout.Label(subs.ToString(), GUILayout.Width(ColumnWidths[1]));
                GUILayout.Label(pubs.ToString(), GUILayout.Width(ColumnWidths[2]));

                var prev = GUI.contentColor;
                if (drops > 0) GUI.contentColor = Color.red;
                GUILayout.Label(drops > 0 ? $"{drops}" : "0", drops > 0 ? _dropStyle : EditorStyles.label, GUILayout.Width(ColumnWidths[3]));
                GUI.contentColor = prev;

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();

        }

        private static void DrawSeparator()
        {
            var rect = GUILayoutUtility.GetRect(1, 1, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
            GUILayout.Space(2);
        }

        private void RefreshBusAssetList()
        {
            var guids = AssetDatabase.FindAssets("t:MessageBus");
            _busAssets = guids
                .Select(g => AssetDatabase.LoadAssetAtPath<MessageBus>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(b => b != null)
                .ToArray();

            _selectedBusIndex = Mathf.Clamp(_selectedBusIndex, 0, Mathf.Max(0, _busAssets.Length - 1));
            Repaint();
        }

        
        private void InitStyles()
        {
            if (_headerStyle != null) return;

            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            { alignment = TextAnchor.MiddleLeft };

            _dropStyle = new GUIStyle(EditorStyles.boldLabel)
            { normal = { textColor = Color.red } };

            _dimStyle = new GUIStyle(EditorStyles.label)
            { normal = { textColor = Color.gray } };
        }
    }
}
#endif
