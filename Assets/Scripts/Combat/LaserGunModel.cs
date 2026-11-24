using UnityEngine;

namespace VRLaserTag.Combat
{
    // Replaces the placeholder "Body" cube of a laser gun with a simple blaster built from
    // primitives (receiver, barrel, shroud, grip, trigger guard, sight, stock and a glowing tip).
    // Visual only: no colliders are added, and the muzzle transform is not moved, so aiming,
    // firing and hit validation are unchanged. Added automatically by LaserWeapon.
    [DisallowMultipleComponent]
    public sealed class LaserGunModel : MonoBehaviour
    {
        [SerializeField] private Color bodyColor = new Color(0.13f, 0.14f, 0.17f);
        [SerializeField] private Color accentColor = new Color(0.10f, 0.42f, 0.85f);
        [SerializeField] private Color glowColor = new Color(0.30f, 1.00f, 1.00f);

        private bool built;

        private void Start()
        {
            Build();
        }

        public void Build()
        {
            if (built) return;
            built = true;

            Renderer placeholder = null;
            Transform body = transform.Find("Body");
            if (body != null) placeholder = body.GetComponent<Renderer>();

            Material baseMaterial = placeholder != null ? placeholder.sharedMaterial : null;
            if (placeholder != null) placeholder.enabled = false;

            Material dark = MakeMaterial(baseMaterial, bodyColor, false);
            Material accent = MakeMaterial(baseMaterial, accentColor, false);
            Material glow = MakeMaterial(baseMaterial, glowColor, true);

            var weapon = GetComponent<LaserWeapon>();
            Transform muzzle = weapon != null ? weapon.Muzzle : null;
            float tip = muzzle != null && muzzle != transform
                ? transform.InverseTransformPoint(muzzle.position).z
                : 0.42f;

            var root = new GameObject("Gun Model").transform;
            root.SetParent(transform, false);

            float barrelStart = 0.18f;
            float barrelLength = Mathf.Max(0.05f, tip - barrelStart);

            // Main body, back of it sits behind the hand like a compact blaster.
            Part(root, PrimitiveType.Cube, dark, new Vector3(0f, 0.02f, 0.04f), new Vector3(0.06f, 0.09f, 0.28f));
            // Rear stock block.
            Part(root, PrimitiveType.Cube, accent, new Vector3(0f, 0.025f, -0.12f), new Vector3(0.055f, 0.07f, 0.05f));
            // Barrel (cylinder lies along Z).
            Part(root, PrimitiveType.Cylinder, dark, new Vector3(0f, 0.035f, barrelStart + barrelLength / 2f),
                new Vector3(0.032f, barrelLength / 2f, 0.032f), new Vector3(90f, 0f, 0f));
            // Shroud around the base of the barrel.
            Part(root, PrimitiveType.Cube, accent, new Vector3(0f, 0.035f, barrelStart + 0.05f), new Vector3(0.05f, 0.05f, 0.11f));
            // Grip, angled back.
            Part(root, PrimitiveType.Cube, dark, new Vector3(0f, -0.07f, -0.03f), new Vector3(0.045f, 0.12f, 0.055f),
                new Vector3(-15f, 0f, 0f));
            // Trigger guard and trigger.
            Part(root, PrimitiveType.Cube, dark, new Vector3(0f, -0.045f, 0.06f), new Vector3(0.012f, 0.012f, 0.07f));
            Part(root, PrimitiveType.Cube, accent, new Vector3(0f, -0.035f, 0.035f), new Vector3(0.008f, 0.03f, 0.01f));
            // Top sight.
            Part(root, PrimitiveType.Cube, dark, new Vector3(0f, 0.08f, 0.08f), new Vector3(0.015f, 0.03f, 0.05f));
            // Glowing energy strips along both sides.
            Part(root, PrimitiveType.Cube, glow, new Vector3(0.031f, 0.03f, 0.04f), new Vector3(0.004f, 0.015f, 0.2f));
            Part(root, PrimitiveType.Cube, glow, new Vector3(-0.031f, 0.03f, 0.04f), new Vector3(0.004f, 0.015f, 0.2f));
            // Glowing tip where the laser leaves.
            Part(root, PrimitiveType.Sphere, glow, new Vector3(0f, 0.035f, tip), Vector3.one * 0.03f);
        }

        private static void Part(Transform parent, PrimitiveType type, Material material, Vector3 position,
            Vector3 scale, Vector3 rotation = default)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = type.ToString();
            DestroyImmediate(part.GetComponent<Collider>());
            part.layer = parent.gameObject.layer;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localRotation = Quaternion.Euler(rotation);
            part.transform.localScale = scale;

            var renderer = part.GetComponent<Renderer>();
            if (material != null) renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        private static Material MakeMaterial(Material template, Color color, bool emissive)
        {
            Shader fallback = template == null ? Shader.Find("Universal Render Pipeline/Lit") : null;
            if (template == null && fallback == null) return null;
            Material material = template != null ? new Material(template) : new Material(fallback);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (emissive && material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 1.5f);
            }
            return material;
        }
    }
}
