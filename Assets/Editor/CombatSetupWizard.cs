using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using VRLaserTag.Combat;

namespace VRLaserTag.EditorTools
{
    public static class CombatSetupWizard
    {
        private const string SceneFolder = "Assets/Scenes/Combat";
        private const string PrefabFolder = "Assets/Prefabs/Combat";
        private const string MaterialFolder = "Assets/Materials/Combat";
        private const string ScenePath = SceneFolder + "/CombatTest.unity";
        private const string ArenaScenePath = "Assets/Scenes/ArenaTest.unity";
        private const string IntegrationScenePath =
            SceneFolder + "/ArenaCombatIntegration.unity";
        private const string PrefabPath = PrefabFolder + "/LaserGun.prefab";

        [MenuItem("VR Laser Tag/Combat/Create Arena Combat Integration Scene")]
        public static void CreateArenaCombatIntegrationScene()
        {
            if (!Application.isBatchMode &&
                !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EnsureFolder(SceneFolder);
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ArenaScenePath) == null)
            {
                Debug.LogError($"Arena scene was not found at {ArenaScenePath}.");
                return;
            }

            GameObject gunPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (gunPrefab == null)
            {
                Debug.LogError(
                    "LaserGun prefab is missing. Run 'Create or Rebuild Combat Test Scene' first.");
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(IntegrationScenePath) != null)
            {
                AssetDatabase.DeleteAsset(IntegrationScenePath);
            }

            if (!AssetDatabase.CopyAsset(ArenaScenePath, IntegrationScenePath))
            {
                Debug.LogError("Unity could not copy the arena integration scene.");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(
                IntegrationScenePath,
                OpenSceneMode.Single);
            GameObject integrationRoot = new GameObject("Combat Integration");

            GameObject validatorObject = new GameObject("Shot Validator");
            validatorObject.transform.SetParent(integrationRoot.transform, false);
            validatorObject.AddComponent<ShotValidator>();

            GameObject gun = (GameObject)PrefabUtility.InstantiatePrefab(gunPrefab, scene);
            gun.name = "Local XR Laser Gun";
            gun.transform.SetParent(integrationRoot.transform, false);
            if (gun.GetComponent<XRWeaponHandBinder>() == null)
            {
                gun.AddComponent<XRWeaponHandBinder>();
            }

            if (gun.GetComponent<WeaponAmmoDisplay>() == null)
            {
                gun.AddComponent<WeaponAmmoDisplay>();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, IntegrationScenePath);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = gun;

            const string instructions =
                "ArenaCombatIntegration was created without changing ArenaTest. " +
                "Enter Play Mode with the XR rig: right trigger fires, the right " +
                "secondary button reloads, and the weapon display shows ammunition.";

            if (Application.isBatchMode)
            {
                Debug.Log(instructions);
            }
            else
            {
                EditorUtility.DisplayDialog("Arena combat integration created", instructions, "OK");
            }
        }

        [MenuItem("VR Laser Tag/Combat/Create or Rebuild Combat Test Scene")]
        public static void CreateCombatTestScene()
        {
            if (!Application.isBatchMode &&
                !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EnsureFolder(SceneFolder);
            EnsureFolder(PrefabFolder);
            EnsureFolder(MaterialFolder);

            Material floorMaterial = GetOrCreateMaterial(
                MaterialFolder + "/Floor.mat",
                new Color(0.12f, 0.16f, 0.2f));
            Material coverMaterial = GetOrCreateMaterial(
                MaterialFolder + "/Cover.mat",
                new Color(0.18f, 0.35f, 0.6f));
            Material targetMaterial = GetOrCreateMaterial(
                MaterialFolder + "/Target.mat",
                new Color(0.8f, 0.2f, 0.15f));
            Material laserMaterial = GetOrCreateMaterial(
                MaterialFolder + "/Laser.mat",
                new Color(1f, 0.1f, 0.05f));

            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);

            CreateLighting();
            Camera camera = CreateCamera();
            ShotValidator validator = new GameObject("Shot Validator")
                .AddComponent<ShotValidator>();

            CreateBlock(
                "Floor",
                new Vector3(0f, -0.25f, 5f),
                new Vector3(20f, 0.5f, 24f),
                floorMaterial);
            CreateBlock(
                "Cover - blocks center target",
                new Vector3(0f, 1.1f, 4.5f),
                new Vector3(2.5f, 2.2f, 0.6f),
                coverMaterial);

            CreateTarget("Blocked Target", new Vector3(0f, 1f, 8f), targetMaterial);
            CreateTarget("Clear Target Left", new Vector3(-3.5f, 1f, 8f), targetMaterial);
            CreateTarget("Clear Target Right", new Vector3(3.5f, 1f, 8f), targetMaterial);

            GameObject gun = CreateGun(camera.transform, validator, laserMaterial);
            PrefabUtility.SaveAsPrefabAsset(gun, PrefabPath);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeGameObject = gun;

            const string instructions =
                "Open CombatTest and enter Play Mode. Aim with arrow keys or " +
                "right-mouse drag, fire with left-click/Space, and reload with R. " +
                "The center target is protected by cover; side targets are exposed.";

            if (Application.isBatchMode)
            {
                Debug.Log("Combat test scene created. " + instructions);
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "Combat test scene created",
                    instructions,
                    "OK");
            }
        }

        private static void CreateLighting()
        {
            GameObject lightObject = new GameObject("Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            lightObject.AddComponent<UniversalAdditionalLightData>();
            lightObject.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
        }

        private static Camera CreateCamera()
        {
            GameObject cameraObject = new GameObject(
                "Combat Test Camera",
                typeof(Camera),
                typeof(AudioListener),
                typeof(CombatTestAimController),
                typeof(UniversalAdditionalCameraData));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 1.7f, -5f);
            cameraObject.transform.rotation = Quaternion.identity;
            return cameraObject.GetComponent<Camera>();
        }

        private static GameObject CreateGun(
            Transform parent,
            ShotValidator validator,
            Material laserMaterial)
        {
            GameObject gun = new GameObject("Laser Gun");
            gun.transform.SetParent(parent, false);
            gun.transform.localPosition = new Vector3(0.3f, -0.25f, 0.6f);

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(gun.transform, false);
            body.transform.localScale = new Vector3(0.18f, 0.18f, 0.7f);
            Object.DestroyImmediate(body.GetComponent<Collider>());

            GameObject muzzleObject = new GameObject("Muzzle");
            muzzleObject.transform.SetParent(gun.transform, false);
            muzzleObject.transform.localPosition = new Vector3(0f, 0f, 0.42f);

            AmmoController ammo = gun.AddComponent<AmmoController>();
            LocalCombatNetworkBridge bridge = gun.AddComponent<LocalCombatNetworkBridge>();
            WeaponEffects effects = gun.AddComponent<WeaponEffects>();
            InputSystemWeaponHaptics haptics = gun.AddComponent<InputSystemWeaponHaptics>();
            AudioSource audioSource = gun.AddComponent<AudioSource>();
            LineRenderer tracer = gun.AddComponent<LineRenderer>();
            LaserWeapon weapon = gun.AddComponent<LaserWeapon>();
            gun.AddComponent<WeaponInput>();
            gun.AddComponent<WeaponAmmoDisplay>();

            tracer.material = laserMaterial;
            tracer.startWidth = 0.025f;
            tracer.endWidth = 0.012f;
            tracer.positionCount = 2;
            tracer.useWorldSpace = true;
            tracer.enabled = false;
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;

            SetObjectReference(bridge, "validator", validator);
            SetObjectReference(effects, "tracer", tracer);
            SetObjectReference(effects, "audioSource", audioSource);
            SetObjectReference(effects, "hapticsSource", haptics);
            SetObjectReference(weapon, "muzzle", muzzleObject.transform);
            SetObjectReference(weapon, "ammo", ammo);
            SetObjectReference(weapon, "networkBridge", bridge);
            SetObjectReference(weapon, "effects", effects);

            return gun;
        }

        private static void CreateTarget(string name, Vector3 position, Material material)
        {
            GameObject target = CreateBlock(
                name,
                position,
                new Vector3(1.2f, 2f, 0.6f),
                material);
            target.AddComponent<DebugDamageableTarget>();
        }

        private static GameObject CreateBlock(
            string name,
            Vector3 position,
            Vector3 scale,
            Material material)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.position = position;
            block.transform.localScale = scale;
            block.GetComponent<Renderer>().sharedMaterial = material;
            return block;
        }

        private static void SetObjectReference(
            Object target,
            string propertyName,
            Object value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Material GetOrCreateMaterial(string path, Color color)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ??
                    Shader.Find("Standard");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];

            for (int index = 1; index < parts.Length; index++)
            {
                string next = current + "/" + parts[index];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[index]);
                }

                current = next;
            }
        }
    }
}
