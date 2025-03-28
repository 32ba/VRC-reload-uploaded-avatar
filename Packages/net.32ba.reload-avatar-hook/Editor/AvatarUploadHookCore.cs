using UnityEditor;
using UnityEngine;
using VRC.Core;
using VRC.SDKBase.Editor;
using System;
using System.Reflection;
using System.Threading.Tasks; // Added for Task.Delay

namespace ReloadAvatarHook
{
    [InitializeOnLoad]
    public static class AvatarUploadHookCore
    {
        private static object _builderInstance;
        private static EventInfo _uploadSuccessEvent;
        private static FieldInfo _selectedAvatarField;


        [InitializeOnLoadMethod]
        public static void RegisterSDKCallback()
        {
            VRCSdkControlPanel.OnSdkPanelEnable += AddBuildHook;
        }

        private static void AddBuildHook(object sender, EventArgs e)
        {
            if (VRCSdkControlPanel.TryGetBuilder<IVRCSdkBuilderApi>(out var builder))
            {
                builder.OnSdkUploadSuccess += OnUploadSuccess;
            }
        }
        public static void OnUploadSuccess(object sender, string message)
        {
            PipelineManager[] pipelineManagers = VRC.Tools.FindSceneObjectsOfTypeAll<PipelineManager>();
            var blueprintId = pipelineManagers[0].blueprintId;

            // if not blueprintId start with avtr_ then it's not an avatar
            if (!blueprintId.StartsWith("avtr_"))
            {
                Debug.Log($"[VRC Reload Avatar Hook] Not an avatar, skipping reload sequence.");
                return;
            }

            Debug.Log($"[VRC Reload Avatar Hook] Found Blueprint ID: {blueprintId}");
            // Start the reload sequence asynchronously
            SendReloadSequenceAsync(blueprintId);
        }

        static async void SendReloadSequenceAsync(string finalBlueprintId)
        {
            if (string.IsNullOrEmpty(finalBlueprintId))
            {
                Debug.LogWarning("[VRC Reload Avatar Hook] Final Blueprint ID is empty, skipping reload sequence.");
                return;
            }

            // Get settings dynamically
            string tempAvatarId = ReloadHookSettings.TempAvatarId;
            int reloadDelayMs = ReloadHookSettings.ReloadDelayMs;

            Debug.Log($"[VRC Reload Avatar Hook] Starting reload sequence. Temp ID: {tempAvatarId}, Final ID: {finalBlueprintId}, Delay: {reloadDelayMs}ms");

            // 1. Send temporary avatar ID
            OscSender.SendOscAvatarChangeMessage(tempAvatarId);

            // 2. Wait for a delay
            try
            {
                if (reloadDelayMs > 0) // Only delay if value is positive
                {
                    await Task.Delay(reloadDelayMs);
                }
            }
            catch (Exception e)
            {
                 Debug.LogError($"[VRC Reload Avatar Hook] Error during delay: {e.Message}");
            }

            // 3. Send final (uploaded) avatar ID
            OscSender.SendOscAvatarChangeMessage(finalBlueprintId);

             Debug.Log("[VRC Reload Avatar Hook] Reload sequence completed.");
        }
    }
}
