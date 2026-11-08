using System;
using System.Linq;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRLaserTag.Combat;
using VRLaserTag.Networking;

namespace VRLaserTag.EditorTools
{
    public static class NetworkingGameSetup
    {
        private const string ScenePath = "Assets/Scenes/ArenaGameplay.unity";
        private const string PrefabPath = "Assets/Prefabs/Player/NetworkPlayer.prefab";
        private const string LayerName = "PlayerHitbox";

        [MenuItem("VR Laser Tag/Networking/Configure Multiplayer Game")]
        public static void Configure()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Scene scene = EditorSceneManager.OpenScene(ScenePath);
            var manager = Required(UnityEngine.Object.FindAnyObjectByType<NetworkManager>(), "Step A NetworkManager");
            var match = Required(UnityEngine.Object.FindAnyObjectByType<MatchManager>(), "MatchManager");
            var validator = Required(UnityEngine.Object.FindAnyObjectByType<ShotValidator>(), "Shot Validator");
            var sky = Required(UnityEngine.Object.FindAnyObjectByType<ArenaSkyDisplay>(), "ArenaSkyDisplay");
            var lobby = Required(scene.GetRootGameObjects().SingleOrDefault(root => root.name == "XR Origin (XR Rig)"), "XR Origin");
            int layer = EnsureHitboxLayer();
            GameObject player = lobby.GetComponent<LobbyRig>() == null
                ? BuildPlayer(lobby, layer)
                : Required(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), "NetworkPlayer prefab");

            if (lobby.GetComponent<LobbyRig>() == null)
            {
                // Keep the original XR rig as the local menu and tracking rig before joining.
                // Disable its gameplay components without modifying the shared rig asset.
                Disable(lobby.GetComponent<PlayerHealth>());
                Disable(lobby.GetComponent<PlayerScore>());
                foreach (VRGameHUD hud in lobby.GetComponentsInChildren<VRGameHUD>(true)) Disable(hud);
                foreach (LaserWeapon gun in lobby.GetComponentsInChildren<LaserWeapon>(true))
                {
                    gun.gameObject.SetActive(false);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(gun.gameObject);
                }
                lobby.AddComponent<LobbyRig>();
            }

            GameObject dummy = scene.GetRootGameObjects().SingleOrDefault(root => root.name == "TargetPlayer");
            if (dummy != null) dummy.SetActive(false);
            if (match.GetComponent<NetworkObject>() == null) match.gameObject.AddComponent<NetworkObject>();
            manager.NetworkConfig.PlayerPrefab = player;
            manager.NetworkConfig.ConnectionApproval = true;
            validator.enabled = true;
            Modify(validator, data =>
            {
                data.FindProperty("collisionMask").intValue = LayerMask.GetMask("Default", "TeleportObstacle", LayerName);
                data.FindProperty("maximumOriginError").floatValue = 0.60f;
            });
            Modify(sky, data => data.FindProperty("automaticStart").boolValue = true);

            EditorUtility.SetDirty(manager);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save ArenaGameplay.");
            AssetDatabase.SaveAssets();
            Debug.Log("[Multiplayer] ArenaGameplay configured and saved. NetworkPlayer prefab assigned.");
        }

        private static GameObject BuildPlayer(GameObject lobby, int layer)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Player"))
                AssetDatabase.CreateFolder("Assets/Prefabs", "Player");
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
                throw new InvalidOperationException("The player prefab already exists. Review it before generating again.");

            GameObject rig = UnityEngine.Object.Instantiate(lobby);
            try
            {
                if (PrefabUtility.IsPartOfPrefabInstance(rig))
                    PrefabUtility.UnpackPrefabInstance(rig, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                rig.name = "NetworkPlayer";
                rig.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                rig.AddComponent<NetworkObject>();
                var setup = rig.AddComponent<NetworkPlayerSetup>();
                var relay = rig.AddComponent<NetworkWeaponRelay>();
                var head = Required(rig.GetComponentInChildren<Camera>(true), "head camera").transform;
                var left = FindChild(rig, "Left Controller");
                var right = FindChild(rig, "Right Controller");
                AddTransform(rig.transform, false);
                AddTransform(head, true);
                AddTransform(left, true);
                AddTransform(right, true);
                if (head.parent != rig.transform && head.parent != left && head.parent != right)
                    AddTransform(head.parent, true);

                var health = Required(rig.GetComponent<PlayerHealth>(), "PlayerHealth");
                Modify(health, data =>
                {
                    data.FindProperty("matchManager").objectReferenceValue = null;
                    data.FindProperty("spawnManager").objectReferenceValue = null;
                });
                foreach (var hud in rig.GetComponentsInChildren<VRGameHUD>(true))
                    Modify(hud, data => data.FindProperty("matchManager").objectReferenceValue = null);

                var gun = Required(right.GetComponentInChildren<LaserWeapon>(true), "LaserWeapon");
                var effects = Required(gun.GetComponent<WeaponEffects>(), "WeaponEffects");
                var local = gun.GetComponent<LocalCombatNetworkBridge>();
                var bridge = gun.gameObject.AddComponent<NetworkCombatBridge>();
                Modify(bridge, data => data.FindProperty("relay").objectReferenceValue = relay);
                Modify(gun, data => data.FindProperty("networkBridge").objectReferenceValue = bridge);
                if (local != null) UnityEngine.Object.DestroyImmediate(local);
                gun.enabled = true;
                Modify(relay, data =>
                {
                    data.FindProperty("weapon").objectReferenceValue = gun;
                    data.FindProperty("effects").objectReferenceValue = effects;
                });
                Modify(effects, data =>
                {
                    var property = data.FindProperty("hapticsSource");
                    if (property.objectReferenceValue == null) return;
                    var haptics = gun.gameObject.AddComponent<OwnerWeaponHaptics>();
                    Modify(haptics, settings =>
                    {
                        settings.FindProperty("player").objectReferenceValue = setup;
                        settings.FindProperty("source").objectReferenceValue = property.objectReferenceValue;
                    });
                    property.objectReferenceValue = haptics;
                });

                var headProxy = CreateProxy(PrimitiveType.Sphere, head, "Head Hitbox", new Vector3(.32f,.32f,.32f), layer);
                var leftProxy = CreateProxy(PrimitiveType.Sphere, left, "Left Hand", Vector3.one * .16f, layer);
                var rightProxy = CreateProxy(PrimitiveType.Sphere, right, "Right Hand", Vector3.one * .16f, layer);
                UnityEngine.Object.DestroyImmediate(leftProxy.GetComponent<Collider>());
                UnityEngine.Object.DestroyImmediate(rightProxy.GetComponent<Collider>());
                var bodyRoot = new GameObject("Body Hitbox");
                bodyRoot.layer = layer;
                bodyRoot.transform.SetParent(rig.transform, false);
                var body = bodyRoot.AddComponent<CapsuleCollider>();
                body.radius = .23f;
                body.height = 1.4f;
                var bodyProxy = CreateProxy(PrimitiveType.Capsule, bodyRoot.transform, "Body", new Vector3(.46f,.7f,.46f), layer);
                UnityEngine.Object.DestroyImmediate(bodyProxy.GetComponent<Collider>());
                var remoteRenderers = new[] { headProxy, leftProxy, rightProxy, bodyProxy }
                    .Select(proxy => proxy.GetComponent<Renderer>()).ToArray();

                var ownerOnly = rig.GetComponentsInChildren<Behaviour>(true)
                    .Where(component => component != null && !(component is NetworkBehaviour) &&
                        !(component is PlayerIdentity) && !(component is AmmoController) &&
                        !(component is LaserWeapon) && !(component is WeaponEffects) &&
                        !(component is NetworkCombatBridge) && !(component is OwnerWeaponHaptics) &&
                        !(component is AudioSource) && !(component is WeaponAmmoDisplay) &&
                        (component.enabled || component is WeaponInput)).ToArray();
                foreach (var component in ownerOnly) component.enabled = false;
                var character = rig.GetComponent<CharacterController>();
                if (character != null) character.enabled = false;
                Modify(setup, data =>
                {
                    SetArray(data.FindProperty("ownerOnly"), ownerOnly);
                    SetArray(data.FindProperty("remoteOnly"), remoteRenderers);
                    data.FindProperty("head").objectReferenceValue = head;
                    data.FindProperty("character").objectReferenceValue = character;
                    data.FindProperty("body").objectReferenceValue = body;
                    data.FindProperty("bodyVisual").objectReferenceValue = bodyProxy.transform;
                });
                var saved = PrefabUtility.SaveAsPrefabAsset(rig, PrefabPath);
                return Required(saved, "saved NetworkPlayer prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(rig); }
        }

        private static void AddTransform(Transform target, bool local)
        {
            var sync = target.gameObject.AddComponent<NetworkTransform>();
            sync.AuthorityMode = NetworkTransform.AuthorityModes.Owner;
            sync.InLocalSpace = local;
            sync.SyncScaleX = sync.SyncScaleY = sync.SyncScaleZ = false;
        }

        private static GameObject CreateProxy(PrimitiveType shape, Transform parent, string name, Vector3 size, int layer)
        {
            var proxy = GameObject.CreatePrimitive(shape);
            proxy.name = name;
            proxy.layer = layer;
            proxy.transform.SetParent(parent, false);
            proxy.transform.localScale = size;
            proxy.GetComponent<Renderer>().enabled = false;
            return proxy;
        }

        private static Transform FindChild(GameObject root, string name)
        {
            return Required(root.GetComponentsInChildren<Transform>(true).SingleOrDefault(child => child.name == name), name);
        }

        private static int EnsureHitboxLayer()
        {
            int existing = LayerMask.NameToLayer(LayerName);
            if (existing >= 0) return existing;
            var tags = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tags.FindProperty("layers");
            for (int index = 8; index < layers.arraySize; index++)
            {
                var item = layers.GetArrayElementAtIndex(index);
                if (!string.IsNullOrEmpty(item.stringValue)) continue;
                item.stringValue = LayerName;
                tags.ApplyModifiedPropertiesWithoutUndo();
                return index;
            }
            throw new InvalidOperationException("No user layer is available for PlayerHitbox.");
        }

        private static void SetArray(SerializedProperty property, UnityEngine.Object[] objects)
        {
            property.arraySize = objects.Length;
            for (int index = 0; index < objects.Length; index++)
                property.GetArrayElementAtIndex(index).objectReferenceValue = objects[index];
        }

        private static void Modify(UnityEngine.Object target, Action<SerializedObject> edit)
        {
            var data = new SerializedObject(target);
            edit(data);
            data.ApplyModifiedPropertiesWithoutUndo();
            if (PrefabUtility.IsPartOfPrefabInstance(target))
                PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        }

        private static void Disable(Behaviour component)
        {
            if (component == null) return;
            component.enabled = false;
            PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        }

        private static T Required<T>(T value, string label) where T : UnityEngine.Object
        {
            if (value == null) throw new InvalidOperationException("Missing " + label);
            return value;
        }
    }
}
