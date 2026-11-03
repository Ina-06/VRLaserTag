using UnityEngine;
using UnityEngine.UI;

namespace VRLaserTag.Combat
{
    /// <summary>
    /// Small world-space ammunition readout mounted on the weapon for headset testing.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AmmoController))]
    public sealed class WeaponAmmoDisplay : MonoBehaviour
    {
        [SerializeField] private AmmoController ammo;
        [SerializeField] private Text label;
        [SerializeField] private bool createDisplayIfMissing = true;

        private void Awake()
        {
            if (ammo == null)
            {
                ammo = GetComponent<AmmoController>();
            }

            if (label == null && createDisplayIfMissing)
            {
                label = CreateWorldSpaceLabel();
            }
        }

        private void OnEnable()
        {
            if (ammo == null)
            {
                return;
            }

            ammo.AmmoChanged += OnAmmoChanged;
            ammo.ReloadStateChanged += OnReloadStateChanged;
            Refresh();
        }

        private void OnDisable()
        {
            if (ammo == null)
            {
                return;
            }

            ammo.AmmoChanged -= OnAmmoChanged;
            ammo.ReloadStateChanged -= OnReloadStateChanged;
        }

        private void OnAmmoChanged(int current, int capacity)
        {
            Refresh();
        }

        private void OnReloadStateChanged(bool isReloading)
        {
            Refresh();
        }

        private void Refresh()
        {
            if (label == null || ammo == null)
            {
                return;
            }

            label.text = ammo.IsReloading
                ? "RELOADING"
                : $"AMMO {ammo.CurrentAmmo}/{ammo.MagazineCapacity}";
        }

        private Text CreateWorldSpaceLabel()
        {
            GameObject canvasObject = new GameObject(
                "Ammo Display",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(Image));
            canvasObject.transform.SetParent(transform, false);
            canvasObject.transform.localPosition = new Vector3(0f, 0.16f, 0.03f);
            canvasObject.transform.localRotation = Quaternion.Euler(20f, 0f, 0f);
            canvasObject.transform.localScale = Vector3.one * 0.0005f;

            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(280f, 80f);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;

            Image background = canvasObject.GetComponent<Image>();
            background.color = new Color(0.02f, 0.02f, 0.02f, 0.8f);

            GameObject textObject = new GameObject(
                "Ammo Text",
                typeof(RectTransform),
                typeof(Text));
            textObject.transform.SetParent(canvasObject.transform, false);

            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            Text text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 32;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.cyan;
            text.raycastTarget = false;
            return text;
        }
    }
}
