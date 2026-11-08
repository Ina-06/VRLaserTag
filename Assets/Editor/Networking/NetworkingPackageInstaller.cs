using System;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace VRLaserTag.EditorTools
{
    // Kept independent of NGO so this can bootstrap a fresh checkout.
    public static class NetworkingPackageInstaller
    {
        private static AddAndRemoveRequest request;
        private static double deadline;

        [MenuItem("VR Laser Tag/Networking/Install Step A Packages")]
        public static void Install()
        {
            if (request != null) return;
            request = Client.AddAndRemove(new[]
            {
                "com.unity.netcode.gameobjects@2.13.3",
                "com.unity.transport@2.6.0"
            });
            deadline = EditorApplication.timeSinceStartup + 600;
            EditorApplication.update += Poll;
        }

        private static void Poll()
        {
            if (!request.IsCompleted && EditorApplication.timeSinceStartup < deadline) return;
            EditorApplication.update -= Poll;
            bool success = request.IsCompleted && request.Status == StatusCode.Success;
            if (success)
                Debug.Log("[NetworkingPackages] Installed: " + string.Join(", ",
                    request.Result.Where(p => p.name == "com.unity.netcode.gameobjects" ||
                                              p.name == "com.unity.transport")
                        .Select(p => p.name + "@" + p.version)));
            else
                Debug.LogError("[NetworkingPackages] " + (request.Error?.message ?? "Timed out."));
            request = null;
            if (Application.isBatchMode) EditorApplication.Exit(success ? 0 : 1);
        }
    }
}
