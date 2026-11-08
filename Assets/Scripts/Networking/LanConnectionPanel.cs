using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace VRLaserTag.Networking
{
    // A small connection-only panel. Existing game HUDs and rules remain owned by their modules.
    public sealed class LanConnectionPanel : MonoBehaviour
    {
        [SerializeField] private LanSessionController session;
        private TMP_Text addressText;
        private TMP_Text statusText;
        private UnityEngine.UI.Button hostButton;
        private UnityEngine.UI.Button joinButton;
        private UnityEngine.UI.Button stopButton;
        private readonly System.Collections.Generic.List<UnityEngine.UI.Button> editButtons = new();
        private GameObject canvasObject;
        private float nextRefresh;
        private Canvas worldCanvas;

        private void Start()
        {
            if (session == null) session = GetComponent<LanSessionController>();
            if (session == null) { enabled = false; return; }
            if (Application.isBatchMode) return;

            if (EventSystem.current == null)
                new GameObject("LAN Event System", typeof(EventSystem), typeof(XRUIInputModule));

            bool inHeadset = Application.platform == RuntimePlatform.Android || XRSettings.isDeviceActive;
            canvasObject = new GameObject("LAN Connection Canvas", typeof(RectTransform), typeof(Canvas),
                typeof(UnityEngine.UI.CanvasScaler));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.sortingOrder = 200;
            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            if (inHeadset)
            {
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.worldCamera = Camera.main;
                canvasRect.sizeDelta = new Vector2(540, 660);
                worldCanvas = canvas;
                canvasRect.SetParent(transform, false);
                canvasRect.localScale = Vector3.one * 0.0012f;
                canvasObject.AddComponent<TrackedDeviceGraphicRaycaster>();
            }
            else
            {
                // The XR simulator suppresses mouse events on XRUIInputModule.
                var eventSystem = EventSystem.current;
                var xrInput = eventSystem.GetComponent<XRUIInputModule>();
                if (xrInput != null) xrInput.enabled = false;
                if (eventSystem.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() == null)
                    eventSystem.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
                scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1280, 720);
                scaler.matchWidthOrHeight = 1;
                canvasObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            }

            RectTransform panel = Rect("Connection Panel", canvasRect, 0, 0, 540, 660);
            var background = panel.gameObject.AddComponent<UnityEngine.UI.Image>();
            background.color = new Color(0.025f, 0.05f, 0.10f, 0.97f);
            background.raycastTarget = false;
            Label(panel, "LASER TAG / LAN", 0, 284, 500, 44, 30);
            Label(panel, "5-player free-for-all", 0, 246, 500, 30, 19);
            Label(panel, "Your IP: " + session.LocalAddresses, 0, 204, 500, 48, 17);
            addressText = Label(panel, "", 0, 152, 500, 44, 26);

            string[] keys = { "1", "2", "3", "4", "5", "6", "7", "8", "9", ".", "0", "Back" };
            for (int index = 0; index < keys.Length; index++)
            {
                string key = keys[index];
                var button = Button(panel, key, (index % 3 - 1) * 155, 95 - index / 3 * 52, 145, 44,
                    () => EditAddress(key));
                editButtons.Add(button);
            }
            editButtons.Add(Button(panel, "Clear IP", 0, -114, 455, 40, () => session.HostAddress = ""));
            hostButton = Button(panel, "HOST", -155, -170, 145, 50, session.StartHost);
            joinButton = Button(panel, "JOIN", 0, -170, 145, 50, session.JoinHost);
            stopButton = Button(panel, "STOP", 155, -170, 145, 50, session.StopSession);
            statusText = Label(panel, "", 0, -238, 490, 76, 19);
            Button(canvasRect, "LAN Menu", 0, -304, 180, 32,
                () => panel.gameObject.SetActive(!panel.gameObject.activeSelf));
            Refresh();
        }

        private void LateUpdate()
        {
            if (worldCanvas == null) return;
            Camera camera = Camera.main;
            worldCanvas.enabled = camera != null;
            if (camera == null) return;
            worldCanvas.worldCamera = camera;
            worldCanvas.transform.SetPositionAndRotation(
                camera.transform.TransformPoint(new Vector3(0, -0.04f, 1.2f)), camera.transform.rotation);
        }

        private void Update()
        {
            if (addressText == null || Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + 0.1f;
            Refresh();
        }

        private void Refresh()
        {
            addressText.text = "Host IP: " + session.HostAddress;
            statusText.text = session.Status + (session.IsHost ? $"\nConnected: {session.ConnectedCount} (including host)" : "");
            hostButton.interactable = joinButton.interactable = session.CanStart;
            stopButton.interactable = session.CanStop;
            foreach (var button in editButtons) button.interactable = session.CanStart;
        }

        private void EditAddress(string key)
        {
            if (!session.CanStart) return;
            string address = session.HostAddress;
            if (key == "Back") session.HostAddress = address.Length > 0 ? address.Substring(0, address.Length - 1) : "";
            else if (address.Length < 15) session.HostAddress += key;
            Refresh();
        }

        private static RectTransform Rect(string name, Transform parent, float x, float y, float width, float height)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.gameObject.layer = 5;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        private static TMP_Text Label(Transform parent, string value, float x, float y, float width, float height, float size)
        {
            var text = Rect(value.Length > 0 ? value : "Status", parent, x, y, width, height)
                .gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = size;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            return text;
        }

        private static UnityEngine.UI.Button Button(Transform parent, string title, float x, float y,
            float width, float height, UnityEngine.Events.UnityAction action)
        {
            RectTransform rect = Rect(title, parent, x, y, width, height);
            var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = new Color(0.08f, 0.31f, 0.42f, 1);
            var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);
            Label(rect, title, 0, 0, width, height, 22);
            return button;
        }

        private void OnDestroy()
        {
            if (canvasObject != null) Destroy(canvasObject);
        }
    }
}
