using UnityEditor;
using UnityEngine;
using VRC.Core;
using System;
using System.Reflection;
using System.Linq;
using System.Threading.Tasks; // Added for Task.Delay
// Namespace should ideally match the assembly name for clarity, but keep existing for now
// or update across all files if changing asmdef name significantly.
using VRC.ReloadAvatarHook.Editor; // For OscSender

namespace VRC.ReloadAvatarHook.Editor // Keep namespace consistent for now
{
    [InitializeOnLoad]
    public static class AvatarUploadHookCore
    {
        private static object _builderInstance;
        private static EventInfo _uploadSuccessEvent;
        private static FieldInfo _selectedAvatarField;

        private static int _retryCount = 0;
        private const int MAX_RETRIES = 10;
        private const int RETRY_DELAY_MS = 1000; // Delay for hook initialization retry

        static AvatarUploadHookCore()
        {
            EditorApplication.delayCall += InitializeHookWithRetry;
        }

        static void InitializeHookWithRetry()
        {
            if (TryInitializeHook())
            {
                Debug.Log("[VRC Reload Avatar Hook] Successfully initialized hook.");
            }
            else if (_retryCount < MAX_RETRIES)
            {
                _retryCount++;
                Debug.Log($"[VRC Reload Avatar Hook] Initialization failed, retrying in {RETRY_DELAY_MS}ms (Attempt {_retryCount}/{MAX_RETRIES})...");
                EditorApplication.delayCall += () => {
                     System.Threading.Tasks.Task.Delay(RETRY_DELAY_MS).ContinueWith(_ => EditorApplication.delayCall += InitializeHookWithRetry);
                };
            }
            else
            {
                Debug.LogError("[VRC Reload Avatar Hook] Failed to initialize hook after multiple retries. Automatic OSC sending might not work. Use 'VRChat SDK/Reload Avatar Hook/Retry Hook Initialization' to try again.");
            }
        }

        static bool TryInitializeHook()
        {
             Debug.Log("[VRC Reload Avatar Hook] Attempting to initialize hook...");
            try
            {
                // Find VRCSdkControlPanel type
                Assembly sdkBaseEditorAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "VRC.SDKBase.Editor");
                if (sdkBaseEditorAssembly == null) {
                     Debug.LogError("[VRC Reload Avatar Hook] VRC.SDKBase.Editor assembly not found.");
                     return false;
                }
                Type panelType = sdkBaseEditorAssembly.GetType("VRC.Editor.VRCSdkControlPanel");
                if (panelType == null)
                {
                    Debug.LogError("[VRC Reload Avatar Hook] Could not find VRCSdkControlPanel type.");
                    return false;
                }

                // Get SDK Control Panel window instance
                var panelWindow = EditorWindow.GetWindow(panelType, false, "VRChat SDK", false);
                if (panelWindow == null)
                {
                    Debug.LogWarning("[VRC Reload Avatar Hook] VRCSdkControlPanel window is not open. Retrying...");
                    return false; // Window not open, retry might succeed later
                }

                // Find the avatar builder field instance
                FieldInfo builderField = panelType.GetField("_avatarBuilder", BindingFlags.Instance | BindingFlags.NonPublic)
                                      ?? panelType.GetField("m_AvatarBuilder", BindingFlags.Instance | BindingFlags.NonPublic); // Try alternative name
                if (builderField == null)
                {
                    Debug.LogError("[VRC Reload Avatar Hook] Could not find the avatar builder field in VRCSdkControlPanel. SDK structure might have changed.");
                    return false;
                }
                _builderInstance = builderField.GetValue(panelWindow);
                if (_builderInstance == null)
                {
                    Debug.LogWarning("[VRC Reload Avatar Hook] Avatar builder instance is null (panel might be initializing). Retrying...");
                    return false; // Builder might not be ready yet
                }

                // Find VRCSdkControlPanelAvatarBuilder type
                Assembly sdk3aEditorAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "VRC.SDK3A.Editor");
                 if (sdk3aEditorAssembly == null) {
                     Debug.LogError("[VRC Reload Avatar Hook] VRC.SDK3A.Editor assembly not found.");
                     return false;
                }
                Type builderType = sdk3aEditorAssembly.GetType("VRC.SDK3A.Editor.VRCSdkControlPanelAvatarBuilder");
                if (builderType == null) {
                     Debug.LogError("[VRC Reload Avatar Hook] Could not find VRCSdkControlPanelAvatarBuilder type.");
                     return false;
                }

                // Verify instance type
                if (!_builderInstance.GetType().IsSubclassOf(builderType) && _builderInstance.GetType() != builderType)
                {
                     Debug.LogError($"[VRC Reload Avatar Hook] Builder instance type mismatch. Expected {builderType} or subclass, Got {_builderInstance.GetType()}.");
                     return false;
                }

                // Get OnSdkUploadSuccess event
                _uploadSuccessEvent = builderType.GetEvent("OnSdkUploadSuccess", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (_uploadSuccessEvent == null)
                {
                    Debug.LogError("[VRC Reload Avatar Hook] Could not find OnSdkUploadSuccess event on the builder instance.");
                    return false;
                }

                // Get _selectedAvatar field (static private)
                _selectedAvatarField = builderType.GetField("_selectedAvatar", BindingFlags.Static | BindingFlags.NonPublic);
                 if (_selectedAvatarField == null)
                {
                    Debug.LogWarning("[VRC Reload Avatar Hook] Could not find _selectedAvatar field. Will try to use Selection.activeGameObject as fallback in handler.");
                }

                // Create and add event handler
                Delegate handler = Delegate.CreateDelegate(_uploadSuccessEvent.EventHandlerType,
                    typeof(AvatarUploadHookCore).GetMethod(nameof(OnUploadSuccessReflected), BindingFlags.Static | BindingFlags.NonPublic));

                // Remove existing handler first to prevent duplicates
                try { _uploadSuccessEvent.RemoveEventHandler(_builderInstance, handler); } catch {}

                _uploadSuccessEvent.AddEventHandler(_builderInstance, handler);
                _retryCount = 0; // Reset retry count on success
                return true; // Success
            }
            catch (Exception e)
            {
                Debug.LogError($"[VRC Reload Avatar Hook] Error during reflection setup: {e}");
                return false;
            }
        }


        static void OnUploadSuccessReflected(object sender, string apiAvatarId)
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

        private static async void SendReloadSequenceAsync(string finalBlueprintId)
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

        public static void ManualRetryInit()
        {
            _retryCount = 0;
            InitializeHookWithRetry();
        }
    }
}
