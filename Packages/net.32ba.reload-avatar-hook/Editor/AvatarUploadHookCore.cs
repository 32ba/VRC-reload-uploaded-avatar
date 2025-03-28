using UnityEditor;
using UnityEngine;
using VRC.Core;
using VRC.SDKBase.Editor;
using VRC.SDKBase.Editor.Api;
using System;
using System.Reflection;
using System.Linq;
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
        public static void OnUploadSuccessReflected(object sender, string apiAvatarId)
        {
            Debug.Log($"[VRC Reload Avatar Hook] OnUploadSuccessReflected triggered for API Avatar ID: {apiAvatarId}");

            VRC_AvatarDescriptor avatarDescriptor = null;

            // Try getting avatar from reflected _selectedAvatar field
            if (_selectedAvatarField != null)
            {
                 try
                 {
                     avatarDescriptor = _selectedAvatarField.GetValue(null) as VRC_AvatarDescriptor; // Static field
                     if (avatarDescriptor != null)
                         Debug.Log($"[VRC Reload Avatar Hook] Found avatar descriptor '{avatarDescriptor.name}' via reflection.");
                     else
                          Debug.LogWarning("[VRC Reload Avatar Hook] _selectedAvatar field was null.");
                 }
                 catch (Exception e) { Debug.LogError($"[VRC Reload Avatar Hook] Error accessing _selectedAvatar field: {e}"); }
            }

            // Fallback: Use currently selected GameObject
            if (avatarDescriptor == null)
            {
                 Debug.LogWarning("[VRC Reload Avatar Hook] Falling back to Selection.activeGameObject.");
                 GameObject selectedObject = Selection.activeGameObject;
                 if (selectedObject != null)
                 {
                     avatarDescriptor = selectedObject.GetComponentInParent<VRC_AvatarDescriptor>(); // Check parents too
                     if (avatarDescriptor != null)
                          Debug.Log($"[VRC Reload Avatar Hook] Found avatar descriptor '{avatarDescriptor.name}' via Selection.activeGameObject.");
                 }
            }

            if (avatarDescriptor == null)
            {
                 Debug.LogError("[VRC Reload Avatar Hook] Could not determine the uploaded avatar. Cannot get Blueprint ID.");
                 return;
            }

            // Get PipelineManager to find the blueprint ID
            PipelineManager pipelineManager = avatarDescriptor.GetComponent<PipelineManager>();
            if (pipelineManager == null)
            {
                Debug.LogError($"[VRC Reload Avatar Hook] PipelineManager not found on avatar '{avatarDescriptor.name}'.");
                return;
            }

            string blueprintId = pipelineManager.blueprintId;
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
