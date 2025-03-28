using UnityEditor;
using UnityEngine;
using ReloadUploadedAvatar;

namespace ReloadUploadedAvatar
{
    public class ReloadUploadedAvatarSettingsWindow : EditorWindow
    {
        private string _oscIpAddress;
        private int _oscPort;
        private int _reloadDelayMs;
        private string _tempAvatarId;

        [MenuItem("Tools/Reload Uploaded Avatar/Settings")]
        public static void ShowWindow()
        {
            // Get existing open window or if none, make a new one:
            ReloadUploadedAvatarSettingsWindow  window = GetWindow<ReloadUploadedAvatarSettingsWindow >("Reload Uploaded Avatar Settings");
            window.minSize = new Vector2(350, 200); // Set a minimum size for better layout
            window.Show();
        }

        private void OnEnable()
        {
            // Load settings when the window is enabled or re-focused
            LoadSettings();
        }

        private void LoadSettings()
        {
            _oscIpAddress = ReloadHookSettings.OscIpAddress;
            _oscPort = ReloadHookSettings.OscPort;
            _reloadDelayMs = ReloadHookSettings.ReloadDelayMs;
            _tempAvatarId = ReloadHookSettings.TempAvatarId;
        }

        private void OnGUI()
        {
            GUILayout.Label("VRC Reload Uploaded Avatar Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("OSC Target", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            _oscIpAddress = EditorGUILayout.TextField(new GUIContent("IP Address", "The IP address VRChat OSC listens on (usually 127.0.0.1)."), _oscIpAddress);
            _oscPort = EditorGUILayout.IntField(new GUIContent("Port", "The port VRChat OSC listens on (usually 9000)."), _oscPort);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Reload Sequence", EditorStyles.boldLabel);
            _reloadDelayMs = EditorGUILayout.IntField(new GUIContent("Delay (ms)", "Time in milliseconds to wait between switching to the temporary avatar and back to the uploaded one."), _reloadDelayMs);
            _tempAvatarId = EditorGUILayout.TextField(new GUIContent("Temporary Avatar ID", "Blueprint ID (avtr_...) of the avatar to switch to temporarily during the reload sequence."), _tempAvatarId);

            if (EditorGUI.EndChangeCheck())
            {
                // Validate and Save settings if any value changed
                // Ensure port and delay are non-negative
                if (_oscPort <= 0) _oscPort = 9000; // Default port if invalid
                if (_reloadDelayMs < 0) _reloadDelayMs = 0; // Minimum delay is 0

                SaveSettings();
            }

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            if (GUILayout.Button("Reset to Defaults"))
            {
                if (EditorUtility.DisplayDialog("Reset Settings",
                    "Are you sure you want to reset all settings to their default values?", "Reset", "Cancel"))
                {
                    ReloadHookSettings.ResetToDefaults();
                    LoadSettings(); // Reload settings into the window fields
                    Repaint(); // Force repaint to show default values
                }
            }

             EditorGUILayout.Space();
             EditorGUILayout.HelpBox("Changes are saved automatically when you modify a field.", MessageType.Info);
        }

        private void SaveSettings()
        {
            ReloadHookSettings.OscIpAddress = _oscIpAddress;
            ReloadHookSettings.OscPort = _oscPort;
            ReloadHookSettings.ReloadDelayMs = _reloadDelayMs;
            ReloadHookSettings.TempAvatarId = _tempAvatarId;
            // Debug.Log("[VRC Reload Avatar Hook] Settings saved."); // Optional: Log save
        }
    }
}
