using UnityEditor;
using UnityEngine;
using VRC.Core; // For PipelineManager
// Namespace should ideally match the assembly name for clarity
using VRC.SDKBase;
using ReloadAvatarHook;

namespace ReloadAvatarHook
{
    public static class ManualActions
    {
        /// <summary>
        /// Manually sends an OSC message using the blueprint ID of the selected avatar.
        /// This now triggers the full reload sequence (temp -> delay -> final).
        /// </summary>
        [MenuItem("Tools/Reload Avatar Hook/Manual Test Send OSC")]
        static void ManualTestSend()
        {
            GameObject selectedObject = Selection.activeGameObject;
            if (selectedObject != null)
            {
                PipelineManager pm = selectedObject.GetComponentInParent<PipelineManager>();
                if (pm != null)
                {
                    // Trigger the async sequence directly for testing
                    AvatarUploadHookCore.SendReloadSequenceAsync(pm.blueprintId);
                }
                else
                {
                    VRC_AvatarDescriptor descriptor = selectedObject.GetComponentInParent<VRC_AvatarDescriptor>();
                    if (descriptor != null)
                    {
                         pm = descriptor.GetComponent<PipelineManager>();
                         if (pm != null)
                         {
                             AvatarUploadHookCore.SendReloadSequenceAsync(pm.blueprintId);
                         } else {
                             Debug.LogError("[VRC Reload Avatar Hook] Selected object or its parents have a VRCAvatarDescriptor but no PipelineManager component.");
                         }
                    } else {
                         Debug.LogError("[VRC Reload Avatar Hook] Selected object or its parents do not have a PipelineManager or VRCAvatarDescriptor component.");
                    }
                }
            }
            else
            {
                Debug.LogError("[VRC Reload Avatar Hook] No GameObject selected in the scene.");
            }
        }

        /// <summary>
        /// Manually triggers the retry mechanism for initializing the SDK event hook.
        /// </summary>
        [MenuItem("Tools/Reload Avatar Hook/Retry Hook Initialization")]
        static void ManualRetryInit()
        {
            AvatarUploadHookCore.ManualRetryInit();
        }
    }
}
