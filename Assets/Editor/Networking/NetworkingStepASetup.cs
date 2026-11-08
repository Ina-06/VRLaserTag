using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.UI;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using VRLaserTag.Combat;
using VRLaserTag.Networking;

namespace VRLaserTag.EditorTools
{
    public static class NetworkingStepASetup
    {
        public const string ScenePath = "Assets/Scenes/ArenaGameplay.unity";

        [MenuItem("VR Laser Tag/Networking/Configure Step A Scene")]
        public static void Configure()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            NetworkManager[] managers = UnityEngine.Object.FindObjectsByType<NetworkManager>();
            if (managers.Length > 1) throw new InvalidOperationException("More than one NetworkManager; review the scene first.");
            NetworkManager manager = managers.FirstOrDefault();
            if (manager == null) manager = new GameObject("NetworkManager").AddComponent<NetworkManager>();
            UnityTransport transport = manager.GetComponent<UnityTransport>();
            if (transport == null) transport = manager.gameObject.AddComponent<UnityTransport>();
            manager.NetworkConfig ??= new Unity.Netcode.NetworkConfig();
            manager.NetworkConfig.NetworkTransport = transport;
            manager.NetworkConfig.PlayerPrefab = null; // Step B creates the network player.
            manager.NetworkConfig.ConnectionApproval = false; // Capacity policy belongs to step E.
            manager.NetworkConfig.EnableSceneManagement = true;
            manager.NetworkConfig.TickRate = 30;
            transport.SetConnectionData("127.0.0.1", 7777, "0.0.0.0");
            transport.ConnectTimeoutMS = 1000;
            transport.MaxConnectAttempts = 5;
            transport.DisconnectTimeoutMS = 5000;
            if (manager.GetComponent<LanSessionController>() == null) manager.gameObject.AddComponent<LanSessionController>();
            if (manager.GetComponent<LanConnectionPanel>() == null) manager.gameObject.AddComponent<LanConnectionPanel>();

            if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() == null)
                new GameObject("LAN Event System", typeof(EventSystem), typeof(XRUIInputModule));

            // Scene-only staging: offline combat must not execute on a connected client.
            // Restore/replace these components as steps C and D introduce authority and RPCs.
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (ArenaSkyDisplay sky in root.GetComponentsInChildren<ArenaSkyDisplay>(true))
                {
                    var serialized = new SerializedObject(sky);
                    serialized.FindProperty("automaticStart").boolValue = false;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
                foreach (Behaviour behaviour in root.GetComponentsInChildren<Behaviour>(true))
                {
                    if (behaviour is WeaponInput || behaviour is LaserWeapon ||
                        behaviour is LocalCombatNetworkBridge || behaviour is ShotValidator)
                    {
                        behaviour.enabled = false;
                        if (PrefabUtility.IsPartOfPrefabInstance(behaviour))
                            PrefabUtility.RecordPrefabInstancePropertyModifications(behaviour);
                    }
                }
                if (root.name == "XR Interaction Simulator")
                {
                    root.tag = "EditorOnly";
                    PrefabUtility.RecordPrefabInstancePropertyModifications(root);
                }
            }

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) }
                .Concat(EditorBuildSettings.scenes.Where(entry => entry.path != ScenePath)).ToArray();
            PlayerSettings.Android.forceInternetPermission = true;
            PlayerSettings.runInBackground = true;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Validate();
            Debug.Log("[StepA] Scene configured and saved.");
        }

        [MenuItem("VR Laser Tag/Networking/Validate Step A Scene")]
        public static void Validate()
        {
            var managers = UnityEngine.Object.FindObjectsByType<NetworkManager>();
            Require(managers.Length == 1, "Exactly one NetworkManager is required.");
            NetworkManager manager = managers[0];
            Require(manager.NetworkConfig.NetworkTransport is UnityTransport, "Unity Transport must be assigned.");
            Require(manager.NetworkConfig.PlayerPrefab == null, "Step A must not spawn a player prefab.");
            Require(manager.GetComponent<LanSessionController>() != null, "LAN session controller is missing.");
            Require(manager.GetComponent<LanConnectionPanel>() != null, "LAN panel is missing.");
            Require(PlayerSettings.Android.forceInternetPermission, "Android Internet Access must be Require.");
            Require(EditorBuildSettings.scenes.First(entry => entry.enabled).path == ScenePath, "ArenaGameplay must launch first.");
            Require(UnityEngine.Object.FindObjectsByType<EventSystem>().Length == 1,
                "Exactly one EventSystem is required.");
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                    Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) == 0,
                        "Missing script: " + child.name);
                foreach (ArenaSkyDisplay sky in root.GetComponentsInChildren<ArenaSkyDisplay>(true))
                    Require(!new SerializedObject(sky).FindProperty("automaticStart").boolValue, "Offline auto-start is still active.");
                foreach (WeaponInput input in root.GetComponentsInChildren<WeaponInput>(true))
                    Require(!input.enabled, "Offline firing input must stay disabled in step A.");
                if (root.name == "XR Interaction Simulator") Require(root.CompareTag("EditorOnly"), "Simulator must be editor-only.");
            }
            Require(LanSessionController.TryParseAddress("192.168.1.20", out _), "Valid LAN address rejected.");
            Require(LanSessionController.TryParseAddress("127.0.0.1", out _), "Loopback smoke-test address rejected.");
            foreach (string invalid in new[] { "", "127.1", "256.1.1.1", "0.0.0.0", "224.0.0.1", "1.2.3.x" })
                Require(!LanSessionController.TryParseAddress(invalid, out _), "Invalid address accepted: " + invalid);
            Debug.Log("[StepA] Scene validation passed.");
        }

        public static void BuildDesktopTest()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Validate();
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = "Builds/StepA-Windows/VRLaserTag.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            });
            Require(report.summary.result == BuildResult.Succeeded, "Desktop test build failed.");
            Debug.Log("[StepA] Desktop test build passed.");
        }

        public static void ConfigureAndBuildDesktop()
        {
            Configure();
            BuildDesktopTest();
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
