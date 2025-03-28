using UnityEditor;
using UnityEngine;

namespace VRC.ReloadAvatarHook.Editor // Keep namespace consistent for now
{
    public static class ReloadHookSettings
    {
        // EditorPrefs Keys
        private const string PREFS_PREFIX = "VRC.ReloadAvatarHook.";
        private const string KEY_OSC_IP = PREFS_PREFIX + "OscIpAddress";
        private const string KEY_OSC_PORT = PREFS_PREFIX + "OscPort";
        private const string KEY_RELOAD_DELAY = PREFS_PREFIX + "ReloadDelayMs";
        private const string KEY_TEMP_AVATAR_ID = PREFS_PREFIX + "TempAvatarId";

        // Default Values
        private const string DEFAULT_OSC_IP = "127.0.0.1";
        private const int DEFAULT_OSC_PORT = 9000;
        private const int DEFAULT_RELOAD_DELAY = 1000; // ms
        private const string DEFAULT_TEMP_AVATAR_ID = "avtr_712e5c3c-2deb-4cae-a414-79b2a814a90b";

        // --- Properties to Access Settings ---

        public static string OscIpAddress
        {
            get => EditorPrefs.GetString(KEY_OSC_IP, DEFAULT_OSC_IP);
            set => EditorPrefs.SetString(KEY_OSC_IP, string.IsNullOrEmpty(value) ? DEFAULT_OSC_IP : value);
        }

        public static int OscPort
        {
            get => EditorPrefs.GetInt(KEY_OSC_PORT, DEFAULT_OSC_PORT);
            set => EditorPrefs.SetInt(KEY_OSC_PORT, value > 0 ? value : DEFAULT_OSC_PORT);
        }

        public static int ReloadDelayMs
        {
            get => EditorPrefs.GetInt(KEY_RELOAD_DELAY, DEFAULT_RELOAD_DELAY);
            set => EditorPrefs.SetInt(KEY_RELOAD_DELAY, value >= 0 ? value : DEFAULT_RELOAD_DELAY); // Allow 0 delay
        }

        public static string TempAvatarId
        {
            get => EditorPrefs.GetString(KEY_TEMP_AVATAR_ID, DEFAULT_TEMP_AVATAR_ID);
            set => EditorPrefs.SetString(KEY_TEMP_AVATAR_ID, string.IsNullOrEmpty(value) ? DEFAULT_TEMP_AVATAR_ID : value);
        }

        // --- Utility ---

        /// <summary>
        /// Resets all settings to their default values.
        /// </summary>
        public static void ResetToDefaults()
        {
            EditorPrefs.DeleteKey(KEY_OSC_IP);
            EditorPrefs.DeleteKey(KEY_OSC_PORT);
            EditorPrefs.DeleteKey(KEY_RELOAD_DELAY);
            EditorPrefs.DeleteKey(KEY_TEMP_AVATAR_ID);
            Debug.Log("[VRC Reload Avatar Hook] Settings reset to defaults.");
        }
    }
}
